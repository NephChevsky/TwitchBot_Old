using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll.DTO;
using ModelsDll;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
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
using TwitchLib.Api.Helix.Models.EventSub;

namespace ApiDll
{
    public class Api : IDisposable
    {
        private Settings _settings;
        private ILogger<Api> _logger;
        private ILoggerFactory _loggerFactory;
        private TwitchAPI api;
        public Api(IConfiguration configuration, bool useAppAccessToken)
        {
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _loggerFactory = LoggerFactory.Create(lf => { lf.AddAzureWebAppDiagnostics(); });
            _logger = _loggerFactory.CreateLogger<Api>();
            Init(useAppAccessToken);
        }

        public void Init(bool useAppAccessToken)
        {
            _logger.LogInformation("Starting api's initialisation");
            api = new TwitchAPI();
            api.Settings.ClientId = _settings.ClientId;
            api.Settings.Secret = _settings.Secret;
            if (useAppAccessToken)
            {
                Task<string> credentialsTask = Task.Run(async () =>
                {
                    return await api.Auth.GetAccessTokenAsync();
                });
                credentialsTask.Wait();
                api.Settings.AccessToken = credentialsTask.Result;
            }
            else
            {
                api.Settings.AccessToken = _settings.StreamerAccessToken;
                Task<RefreshResponse> task = Task.Run(async () =>
                {
                    return await RefreshToken();
                });
                task.Wait();
                RefreshResponse token = task.Result;
                Helpers.UpdateTokens("twitchapi", token.AccessToken, token.RefreshToken);
                _settings.StreamerAccessToken = token.AccessToken;
                _settings.StreamerRefreshToken = token.RefreshToken;
            }
            _logger.LogInformation("End of api's initialisation");
        }

        public async Task<EventSubSubscription> CreateEventSubSubscription(string type)
        {
            Dictionary<string, string> conditions = new Dictionary<string, string>();
            switch (type)
            {
                case "channel.raid":
                    conditions.Add("to_broadcaster_user_id", _settings.StreamerTwitchId);
                    break;
                case "channel.follow":
                case "channel.subscribe":
                case "channel.subscription.gift":
                case "channel.subscription.message":
                case "channel.cheer":
                case "stream.online":
                case "stream.offline":
                    conditions.Add("broadcaster_user_id", _settings.StreamerTwitchId);
                    break;
            }
            CreateEventSubSubscriptionResponse response = await api.Helix.EventSub.CreateEventSubSubscriptionAsync(type, "1", conditions, "webhook", _settings.EventSubUrl, _settings.Secret);
            return response.Subscriptions[0];
        }

        public void DeleteEventSubSubscription(List<EventSubSubscription> subscriptions)
        {
            subscriptions.ForEach(async x =>
            {
                await api.Helix.EventSub.DeleteEventSubSubscriptionAsync(x.Id);
            });
        }

        public async Task<List<EventSubSubscription>> GetEventSubSubscription()
        {
            GetEventSubSubscriptionsResponse response = await api.Helix.EventSub.GetEventSubSubscriptionsAsync();
            return response.Subscriptions.ToList();
		}

        public async Task<RefreshResponse> RefreshToken(bool bot = false)
        {
            if (bot)
            {
                api.Settings.AccessToken = _settings.BotAccessToken;
            }
            RefreshResponse token = await api.Auth.RefreshAuthTokenAsync(bot ? _settings.BotRefreshToken : _settings.StreamerRefreshToken, _settings.Secret);
            if (bot)
            {
                api.Settings.AccessToken = _settings.StreamerAccessToken;
            }
            return token;
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

        public async Task<List<CustomReward>> GetChannelRewards()
        {
            GetCustomRewardsResponse rewards = await api.Helix.ChannelPoints.GetCustomRewardAsync(_settings.StreamerTwitchId, null, true);
            return rewards.Data.ToList();
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

        public async Task<int> GetViewerCount()
        {
            GetStreamsResponse stream = await api.Helix.Streams.GetStreamsAsync(null, null, 1, null, null, "all", new List<string>() { _settings.StreamerTwitchId });
            if (stream.Streams.Length == 0)
                return 0;
            else
                return stream.Streams[0].ViewerCount;
        }

        public void Dispose()
        {
            _logger.LogInformation($"Disposing of ApiDll");
        }
    }
}