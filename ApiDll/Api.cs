using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll.DTO;
using ModelsDll;
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
using ModelsDll.Db;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Bits;
using DbDll;

namespace ApiDll
{
    public class Api : IDisposable
    {
        private Settings _settings;
        private ILogger<Api> _logger;
        private TwitchAPI api;
        private Timer RefreshTokenTimer;
        private List<string> _bots;

        public Api(IConfiguration configuration, ILogger<Api> logger)
        {
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _bots = configuration.GetSection("TwitchBotList").Get<List<string>>();
            _logger = logger;

            TimeSpan firstRefresh = TimeSpan.FromSeconds(4 * 60 * 60 - 600);

            api = new TwitchAPI();
            api.Settings.ClientId = _settings.ClientId;
            api.Settings.Secret = _settings.Secret;

            Task.Run(async () =>
            {
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
            }).Wait();

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
                    RefreshResponse newToken = await api.Auth.RefreshAuthTokenAsync(refreshToken.Value, _settings.Secret);
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
            await Task.Run(async () => await RefreshTokenAsync());
        }

        public async Task<ModifyChannelInformationResponse> ModifyChannelInformation(string title = null, string game=null)
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

        public async Task<ChannelInformation> GetChannelInformation()
        {
            GetChannelInformationResponse channelInformation = await api.Helix.Channels.GetChannelInformationAsync(_settings.StreamerTwitchId);
            return channelInformation.Data[0];
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
            if (vips.Count() >= _settings.ChatFunction.MaxVIPs)
            {
                await api.Helix.Channels.RemoveChannelVIPAsync(_settings.StreamerTwitchId, vips[0].UserId);
			}
            await api.Helix.Channels.AddChannelVIPAsync(_settings.StreamerTwitchId, userId);
		}

        public async Task<List<ChannelVIPsResponseModel>> GetVIPs()
        {
            GetChannelVIPsResponse vips = await api.Helix.Channels.GetVIPsAsync(_settings.StreamerTwitchId);
            return vips.Data.ToList();
        }

        public async Task<List<Listing>> GetBitsLeaderBoard(int count, BitsLeaderboardPeriodEnum period)
        {
            DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
            GetBitsLeaderboardResponse response = await api.Helix.Bits.GetBitsLeaderboardAsync(count, period, now);
            return response.Listings.ToList();
		}

        public Viewer GetOrCreateUserById(string id)
        {
            using (TwitchDbContext db = new())
            {
                Viewer viewer = db.Viewers.Where(x => x.Id == id).FirstOrDefault();
                if (viewer == null)
                {
                    Task<GetUsersResponse> query = Task.Run(async () => {
                        return await api.Helix.Users.GetUsersAsync(new List<string>() { id });
                    });
                    query.Wait();
                    GetUsersResponse response = query.Result;
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

        public Viewer GetOrCreateUserByUsername(string username)
        {
            using (TwitchDbContext db = new())
            {
                Viewer viewer = db.Viewers.Where(x => x.Username == username).FirstOrDefault();
                if (viewer == null)
                {
                    Task<GetUsersResponse> query = Task.Run(async () => {
                        return await api.Helix.Users.GetUsersAsync(null, new List<string>() { username });
                    });
                    query.Wait();
                    GetUsersResponse response = query.Result;
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

        public void Dispose()
        {
            _logger.LogInformation($"Disposing of ApiDll");
        }
    }
}