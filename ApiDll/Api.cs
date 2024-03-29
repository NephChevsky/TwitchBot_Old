﻿using DbDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll.DTO;
using ModelsDll;
using ModelsDll.Db;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Bits;
using TwitchLib.Api.Helix.Models.Chat.Badges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;
using Polly;

namespace ApiDll
{
    public class Api : IDisposable
    {
        private Settings _settings;
        private Secret _secret;
        private static ILogger<Api> _logger;
        private TwitchAPI api;
        private List<string> _bots;

        private Timer RefreshTokenTimer;

        private readonly IAsyncPolicy<dynamic> _retryPolicy = Policy.WrapAsync(Policy<dynamic>.Handle<Exception>().FallbackAsync(fallbackValue: null, onFallbackAsync: (result, context) =>
        {
            _logger.LogWarning($"Spotify API failed again, return null");
            return Task.CompletedTask;
        }), Policy<dynamic>.Handle<Exception>()
            .WaitAndRetryAsync(1, retry =>
            {
                return TimeSpan.FromMilliseconds(500);
            }, (exception, timespan) =>
            {
                _logger.LogWarning($"Twitch API call failed: {exception.Exception.Message}");
                _logger.LogWarning($"Retrying in {timespan.Milliseconds} ms");
            }));

        public Api(IConfiguration configuration, ILogger<Api> logger)
        {
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _secret = configuration.GetSection("Secret").Get<Secret>();
            _bots = configuration.GetSection("TwitchBotList").Get<List<string>>();
            _logger = logger;

            api = new TwitchAPI();
            api.Settings.ClientId = _secret.Twitch.ClientId;
            api.Settings.Secret = _secret.Twitch.ClientSecret;

            CheckAndUpdateTokenStatus().GetAwaiter().GetResult();
        }

        public async Task CheckAndUpdateTokenStatus()
        {
            TimeSpan firstRefresh = TimeSpan.FromSeconds(4 * 60 * 60 - 600);
            using (TwitchDbContext db = new())
            {
                Token accessToken = db.Tokens.Where(x => x.Name == "StreamerAccessToken").FirstOrDefault();
                if (accessToken != null)
                {
                    api.Settings.AccessToken = accessToken.Value;
                    ValidateAccessTokenResponse response = await api.Auth.ValidateAccessTokenAsync();
                    if (response == null)
                    {
                        await RefreshTokenAsync();
                    }
                    else
                    {
                        firstRefresh = TimeSpan.FromSeconds(Math.Max(0, response.ExpiresIn - 600));
                    }
                }
                else
                {
                    // https://github.com/swiftyspiffy/Twitch-Auth-Example/tree/main/TwitchAuthExample
                    throw new Exception("Implement auth flow for api");
                }
            }

            RefreshTokenTimer = new Timer(RefreshToken, null, firstRefresh, TimeSpan.FromSeconds(4 * 60 * 60 - 600));
        }

        public async Task RefreshTokenAsync()
        {
            using (TwitchDbContext db = new())
            {
                Token accessToken = db.Tokens.Where(x => x.Name == "StreamerAccessToken").FirstOrDefault();
                Token refreshToken = db.Tokens.Where(x => x.Name == "StreamerRefreshToken").FirstOrDefault();
                if (refreshToken != null)
                {
                    RefreshResponse newToken = await api.Auth.RefreshAuthTokenAsync(refreshToken.Value, _secret.Twitch.ClientSecret);
                    accessToken.Value = newToken.AccessToken;
                    refreshToken.Value = newToken.RefreshToken;
                    db.SaveChanges();
                    api.Settings.AccessToken = newToken.AccessToken;
                }
                else
                {
                    // https://github.com/swiftyspiffy/Twitch-Auth-Example/tree/main/TwitchAuthExample
                    throw new Exception("Implement auth flow for api");
                }
            }
        }

        public async void RefreshToken(object state = null)
        {
            await RefreshTokenAsync();
        }

        public async Task<ChannelInformation> GetChannelInformations()
        {
            GetChannelInformationResponse response = await api.Helix.Channels.GetChannelInformationAsync(_settings.StreamerTwitchId);
            return response.Data[0];
		}

        public async Task<ModifyChannelInformationResponse> ModifyChannelInformations(string title = null, string game=null)
        {
            ModifyChannelInformationRequest request = new();
            ModifyChannelInformationResponse response = new();
            if (!string.IsNullOrEmpty(title))
            {
                request.Title = title;
                response.Title = title;
            }
            if (!string.IsNullOrEmpty(game))
            {
                GetGamesResponse games = await api.Helix.Games.GetGamesAsync(null, new List<string>() { game });
                if (games.Games.Length == 0)
                {
                    return null;
                }
                request.GameId = games.Games[0].Id;
                response.Game = games.Games[0].Name;
            }
            await api.Helix.Channels.ModifyChannelInformationAsync(_settings.StreamerTwitchId, request);
            return response;
        }

        public async Task<List<Moderator>> GetModerators()
        {
            GetModeratorsResponse mods = await api.Helix.Moderation.GetModeratorsAsync(_settings.StreamerTwitchId);
            return mods.Data.ToList();
        }

        public async Task<Follow> GetLastFollower()
        {
            GetUsersFollowsResponse followers = await api.Helix.Users.GetUsersFollowsAsync(null, null, 100, null, _settings.StreamerTwitchId);
            return followers.Follows[0];
        }

        public async Task<List<CustomReward>> GetChannelRewards(List<string> ids = null)
        {
            GetCustomRewardsResponse rewards = await api.Helix.ChannelPoints.GetCustomRewardAsync(_settings.StreamerTwitchId, ids, true);
            return rewards.Data.ToList();
        }

        public async Task<CustomReward> CreateChannelReward(ChannelReward channelReward)
        {
            CreateCustomRewardsRequest request = new();
            request.Title = channelReward.Name;
            request.Prompt = channelReward.Description;
            request.BackgroundColor = channelReward.BackgroundColor;
            request.IsUserInputRequired = channelReward.UserText;
            request.Cost = channelReward.CurrentCost;
            request.ShouldRedemptionsSkipRequestQueue = channelReward.SkipRewardQueue;
            request.IsGlobalCooldownEnabled = channelReward.RedemptionCooldownTime != 0;
            request.GlobalCooldownSeconds = channelReward.RedemptionCooldownTime;
            request.IsMaxPerStreamEnabled = channelReward.RedemptionPerStream != 0;
            request.MaxPerStream = channelReward.RedemptionPerStream;
            request.IsMaxPerUserPerStreamEnabled = channelReward.RedemptionPerUserPerStream != 0;
            request.MaxPerUserPerStream = channelReward.RedemptionPerUserPerStream;
            request.IsEnabled = true;
            CreateCustomRewardsResponse response = await api.Helix.ChannelPoints.CreateCustomRewardsAsync(_settings.StreamerTwitchId, request);
            return response.Data[0];
		}

        public async Task<bool> UpdateChannelReward(ChannelReward channelReward)
        {
            UpdateCustomRewardRequest request = new();
            request.Title = channelReward.Name;
            request.Prompt = channelReward.Description;
            request.IsEnabled = channelReward.IsEnabled;
            request.BackgroundColor = channelReward.BackgroundColor;
            request.IsUserInputRequired = channelReward.UserText;
            request.Cost = channelReward.CurrentCost;
            request.ShouldRedemptionsSkipRequestQueue = channelReward.SkipRewardQueue;
            request.IsGlobalCooldownEnabled = channelReward.RedemptionCooldownTime != 0;
            request.GlobalCooldownSeconds = channelReward.RedemptionCooldownTime;
            request.IsMaxPerStreamEnabled = channelReward.RedemptionPerStream != 0;
            request.MaxPerStream = channelReward.RedemptionPerStream;
            request.IsMaxPerUserPerStreamEnabled = channelReward.RedemptionPerUserPerStream != 0;
            request.MaxPerUserPerStream = channelReward.RedemptionPerUserPerStream;
            UpdateCustomRewardResponse response = await api.Helix.ChannelPoints.UpdateCustomRewardAsync(_settings.StreamerTwitchId, channelReward.TwitchId.ToString(), request);
            return response.Data.Count() != 0;
		}

        public async void BanUser(string username, int duration = 300)
        {
            GetUsersResponse user = await api.Helix.Users.GetUsersAsync(null, new List<string>() { username }, null);
            if (user.Users.Length > 0)
            {
                BanUserRequest banUserRequest = new();
                banUserRequest.UserId = user.Users[0].Id;
                banUserRequest.Duration = duration;
                banUserRequest.Reason = "No reason";
                await api.Helix.Moderation.BanUserAsync(_settings.StreamerTwitchId, _settings.StreamerTwitchId, banUserRequest);
            }
        }

        public async Task<User> GetUser(string username)
        {
            GetUsersResponse users = await api.Helix.Users.GetUsersAsync(null, new List<string>() { username }, null);
            if (users.Users.Length > 0)
            {
                return users.Users[0];
			}
            else
            {
                return null;
            }
        }

        public async Task<List<ChatterFormatted>> GetChatters()
        {
            List<ChatterFormatted> chatters = await api.Undocumented.GetChattersAsync(_settings.Streamer);
            return chatters.ToList();
        }

        public async Task<List<Follow>> GetFollowers()
        {
            GetUsersFollowsResponse followers = await api.Helix.Users.GetUsersFollowsAsync(null, null, 100, null, _settings.StreamerTwitchId);
            return followers.Follows.ToList();
        }

        public async Task<List<TwitchLib.Api.Helix.Models.Subscriptions.Subscription>> GetSubscribers()
        {
            GetBroadcasterSubscriptionsResponse subscribers = await api.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(_settings.StreamerTwitchId, 100);
            return subscribers.Data.ToList();
        }

        public async Task<int> GetViewerCount()
        {
            GetStreamsResponse stream = await api.Helix.Streams.GetStreamsAsync(null, null, 1, null, null, "all", new List<string>() { _settings.StreamerTwitchId });
            if (stream.Streams.Length == 0)
                return 0;
            else
                return stream.Streams[0].ViewerCount;
        }

        public async Task UpdateRedemptionStatus(string rewardId, string eventId, CustomRewardRedemptionStatus status)
        {
            UpdateCustomRewardRedemptionStatusRequest request = new();
            request.Status = status;
            await api.Helix.ChannelPoints.UpdateRedemptionStatusAsync(_settings.StreamerTwitchId, rewardId, new List<string>() { eventId }, request);
		}

        public async Task AddVIP(string userId)
        {
            List<ChannelVIPsResponseModel> vips = await GetVIPs();
            while (vips.Count() >= _settings.ChatFunction.MaxVIPs)
            {
                await api.Helix.Channels.RemoveChannelVIPAsync(_settings.StreamerTwitchId, vips[0].UserId);
                vips.RemoveAt(0);
			}
            await api.Helix.Channels.AddChannelVIPAsync(_settings.StreamerTwitchId, userId);
		}

        public async Task<List<ChannelVIPsResponseModel>> GetVIPs()
        {
            GetChannelVIPsResponse vips = await api.Helix.Channels.GetVIPsAsync(_settings.StreamerTwitchId);
            return vips.Data.ToList();
        }

        public async Task<GetBitsLeaderboardResponse> GetBitsLeaderBoard(int count, BitsLeaderboardPeriodEnum period, DateTime time)
        {
            return await api.Helix.Bits.GetBitsLeaderboardAsync(count, period, time);
		}

        public async Task<Viewer> GetOrCreateUserById(string id)
        {
            using (TwitchDbContext db = new())
            {
                Viewer viewer = db.Viewers.Where(x => x.Id == id).FirstOrDefault();
                if (viewer == null)
                {
                    GetUsersResponse response = await api.Helix.Users.GetUsersAsync(new List<string>() { id });
                    if (response != null && response.Users.Count() != 0)
                    {
                        viewer = new(response.Users[0].Id, response.Users[0].Login, response.Users[0].DisplayName);
                        if (_bots.Contains(response.Users[0].Login.ToLower()))
                        {
                            viewer.IsBot = true;
                        }
                        db.Viewers.Add(viewer);
                        db.SaveChanges();
					}
                    else
                    {
                        return null;
					}
				}
                return viewer;
			}
		}

        public async Task<Viewer> GetOrCreateUserByUsername(string username)
        {
            using (TwitchDbContext db = new())
            {
                Viewer viewer = db.Viewers.Where(x => x.Username == username).FirstOrDefault();
                if (viewer == null)
                {
                    GetUsersResponse response = await api.Helix.Users.GetUsersAsync(null, new List<string>() { username });
                    if (response != null && response.Users.Count() != 0)
                    {
                        viewer = db.Viewers.Where(x => x.Id == response.Users[0].Id).FirstOrDefault();
                        if (viewer != null)
                        {
                            viewer.Username = response.Users[0].Login;
                            viewer.DisplayName = response.Users[0].DisplayName;
						}
                        else
                        {
                            viewer = new(response.Users[0].Id, response.Users[0].Login, response.Users[0].DisplayName);
                            if (_bots.Contains(response.Users[0].Login.ToLower()))
                            {
                                viewer.IsBot = true;
                            }
                            db.Viewers.Add(viewer);
                        }
                        db.SaveChanges();
                    }
                    else
                    {
                        return null;
                    }
                }
                return viewer;
            }
        }

        public async Task<bool> IsStreamerLive()
        {
            GetStreamsResponse response = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await api.Helix.Streams.GetStreamsAsync(null, null, 1, null, null, "all", new List<string>() { _settings.StreamerTwitchId });
            });
            if (response != null && response.Streams.Count() != 0)
            {
                return true;
			}
            return false;
		}

        public async Task<Dictionary<string, string>> GetBadges()
        {
            Dictionary<string, string> badges = new Dictionary<string, string>();
            GetGlobalChatBadgesResponse globalBadges = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await api.Helix.Chat.GetGlobalChatBadgesAsync();
            });
            foreach (BadgeEmoteSet badge in globalBadges.EmoteSet)
            {
                badges.Add(badge.SetId, badge.Versions[0].ImageUrl1x);
			}
            GetChannelChatBadgesResponse channelBadges = await _retryPolicy.ExecuteAsync(async () =>
            {
                return await api.Helix.Chat.GetChannelChatBadgesAsync(_settings.StreamerTwitchId);
            });
            foreach (BadgeEmoteSet badge in channelBadges.EmoteSet)
            {
                badges.Add(badge.SetId, badge.Versions[0].ImageUrl1x);
            }
            return badges;
		}

        public async Task<List<Cheermote>> GetCheermotes()
        {
            GetCheermotesResponse response = await api.Helix.Bits.GetCheermotesAsync();
            return response.Listings.ToList();
		}

        public void Dispose()
        {
            _logger.LogInformation($"Disposing of ApiDll");
        }
    }
}