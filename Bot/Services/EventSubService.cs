using Bot.Models;
using Microsoft.AspNetCore.SignalR;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;
using TwitchLib.EventSub.Webhooks.Core.EventArgs.Channel;

namespace Bot.Services
{
    public class EventSubService : IHostedService
    {
        private readonly ILogger<EventSubService> _logger;
        private readonly ITwitchEventSubWebhooks _eventSubWebhooks;
        private OBSService _OBSService;
        readonly IHubContext<SignalService> _hub;

        public EventSubService(ILogger<EventSubService> logger, IConfiguration configuration, OBSService obs, ITwitchEventSubWebhooks eventSubWebhooks, IHubContext<SignalService> hub)
        {
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
            _OBSService = obs;
            _hub = hub;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.OnError += OnError;
            _eventSubWebhooks.OnChannelFollow += OnChannelFollow;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.OnError -= OnError;
            _eventSubWebhooks.OnChannelFollow -= OnChannelFollow;
            return Task.CompletedTask;
        }

        private void OnChannelFollow(object sender, ChannelFollowArgs e)
        {
            _logger.LogInformation($"{e.Notification.Event.UserName} followed {e.Notification.Event.BroadcasterUserName} at {e.Notification.Event.FollowedAt.ToUniversalTime()}");
            Alert alert = new("follow", e.Notification.Event.UserName);
            _hub.Clients.All.SendAsync("TriggerAlert", alert);
        }

        private void OnError(object sender, OnErrorArgs e)
        {
            _logger.LogError($"Reason: {e.Reason} - Message: {e.Message}");
        }
    }
}
