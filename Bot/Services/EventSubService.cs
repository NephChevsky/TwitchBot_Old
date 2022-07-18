using Bot.Models;
using Microsoft.AspNetCore.SignalR;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Stream;

namespace Bot.Services
{
    public class EventSubService : IHostedService
    {
        private readonly ILogger<EventSubService> _logger;
        private readonly ITwitchEventSubWebhooks _eventSubWebhooks;
        private OBSService _OBSService;
        readonly IHubContext<SignalService> _hub;
        private Settings _options;
        private BotService _bot;
        private TwitchAPI serverAPI;
        private List<EventSubSubscription> Subscriptions;

        public EventSubService(ILogger<EventSubService> logger, IConfiguration configuration, OBSService obs, ITwitchEventSubWebhooks eventSubWebhooks, IHubContext<SignalService> hub, BotService bot)
        {
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
            _OBSService = obs;
            _hub = hub;
            _options = configuration.GetSection("Settings").Get<Settings>();
            _bot = bot;
            serverAPI = new();
            serverAPI.Settings.ClientId = _options.ClientId;
            serverAPI.Settings.Secret = _options.Secret;
            serverAPI.Settings.AccessToken = _options.AccessToken;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.OnError += OnError;
            _eventSubWebhooks.OnChannelFollow += OnChannelFollow;
            _eventSubWebhooks.OnChannelRaid += OnChannelRaid;
            _eventSubWebhooks.OnStreamOnline += OnStreamOnline;
            _eventSubWebhooks.OnStreamOffline += OnStreamOffline;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _bot.UnSubscribe();

            _eventSubWebhooks.OnError -= OnError;
            _eventSubWebhooks.OnChannelFollow -= OnChannelFollow;
            _eventSubWebhooks.OnChannelRaid -= OnChannelRaid;
            _eventSubWebhooks.OnStreamOnline -= OnStreamOnline;
            _eventSubWebhooks.OnStreamOffline -= OnStreamOffline;

            return Task.CompletedTask;
        }

        private void OnChannelFollow(object sender, ChannelFollowArgs e)
        {
            _logger.LogInformation($"{e.Notification.Event.UserName} followed {e.Notification.Event.BroadcasterUserName} at {e.Notification.Event.FollowedAt.ToUniversalTime()}");
            Alert alert = new("follow", e.Notification.Event.UserName);
            _hub.Clients.All.SendAsync("TriggerAlert", alert);
        }

        private void OnChannelRaid(object sender, ChannelRaidArgs e)
        {
            _logger.LogInformation($"{e.Notification.Event.FromBroadcasterUserName} raided {e.Notification.Event.ToBroadcasterUserName} with {e.Notification.Event.Viewers} person");
            Alert alert = new("raid", e.Notification.Event.FromBroadcasterUserName, e.Notification.Event.Viewers);
            _hub.Clients.All.SendAsync("TriggerAlert", alert);
        }

        private void OnStreamOnline(object sender, StreamOnlineArgs e)
        {
            _logger.LogInformation($"{e.Notification.Event.BroadcasterUserName} is now live");
            _bot.SendMessage("Le live vient de commencer!");
        }

        private void OnStreamOffline(object sender, StreamOfflineArgs e)
        {
            _logger.LogInformation($"{e.Notification.Event.BroadcasterUserName} is not live anymore");
            _bot.SendMessage("Le live est terminé!");
        }

        private void OnError(object sender, OnErrorArgs e)
        {
            _logger.LogError($"Reason: {e.Reason} - Message: {e.Message}");
        }
    }
}
