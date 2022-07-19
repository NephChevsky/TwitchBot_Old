using ApiDll;
using ChatDll;
using Microsoft.AspNetCore.SignalR;
using ModelsDll;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Stream;

namespace WebApp.Services
{
    public class EventSubService : IHostedService
    {
        private readonly ILogger<EventSubService> _logger;
        private readonly ITwitchEventSubWebhooks _eventSubWebhooks;
        readonly IHubContext<SignalService> _hub;
        private Settings _settings;
        private Chat _chat;
        private Api _api;
        private List<EventSubSubscription> Subscriptions;

        public EventSubService(ILogger<EventSubService> logger, IConfiguration configuration, ITwitchEventSubWebhooks eventSubWebhooks, IHubContext<SignalService> hub, Chat chat)
        {
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
            _hub = hub;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _chat = chat;
            _api = new(configuration, true);

            Subscriptions = new();
            Task<bool> subscriptionsTask = Task.Run(async () =>
            {
                List<EventSubSubscription> subs = await _api.GetEventSubSubscription();
                _api.DeleteEventSubSubscription(subs);
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.follow"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.raid"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("stream.online"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("stream.offline"));

                return true;
            });
            subscriptionsTask.Wait();
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
            _api.DeleteEventSubSubscription(Subscriptions);

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
            _chat.SendMessage("Le live vient de commencer!");
        }

        private void OnStreamOffline(object sender, StreamOfflineArgs e)
        {
            _logger.LogInformation($"{e.Notification.Event.BroadcasterUserName} is not live anymore");
            _chat.SendMessage("Le live est terminé!");
        }

        private void OnError(object sender, OnErrorArgs e)
        {
            _logger.LogError($"Reason: {e.Reason} - Message: {e.Message}");
        }
    }
}
