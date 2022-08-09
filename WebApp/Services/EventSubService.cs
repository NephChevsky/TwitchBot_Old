using ApiDll;
using DbDll;
using Microsoft.AspNetCore.SignalR;
using ModelsDll;
using ModelsDll.Db;
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
        private Api _api;
        private DiscordDll.Discord _discord;
        private List<EventSubSubscription> Subscriptions;
        private List<string> HandledEvents = new List<string>();

        public EventSubService(ILogger<EventSubService> logger, IConfiguration configuration, ITwitchEventSubWebhooks eventSubWebhooks, IHubContext<SignalService> hub, DiscordDll.Discord discord)
        {
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
            _hub = hub;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = new(configuration, "twitchapp");
            _discord = discord;

            Subscriptions = new();
            Task<bool> subscriptionsTask = Task.Run(async () =>
            {
                List<EventSubSubscription> subs = await _api.GetEventSubSubscription();
                _api.DeleteEventSubSubscription(subs);
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.follow"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.subscribe"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.subscription.gift"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.subscription.message"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.cheer"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.raid"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.hype_train.begin"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("channel.channel_points_custom_reward_redemption.add"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("stream.online"));
                Subscriptions.Add(await _api.CreateEventSubSubscription("stream.offline"));

                return true;
            });
            subscriptionsTask.Wait();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Service starting");
            _eventSubWebhooks.OnError += OnError;
            _eventSubWebhooks.OnChannelFollow += OnChannelFollow;
            _eventSubWebhooks.OnChannelSubscribe += OnChannelSubscribe;
            _eventSubWebhooks.OnChannelSubscriptionGift += OnChannelSubscriptionGift;
            _eventSubWebhooks.OnChannelSubscriptionMessage += OnChannelSubscriptionMessage;
            _eventSubWebhooks.OnChannelCheer += OnChannelCheer;
            _eventSubWebhooks.OnChannelRaid += OnChannelRaid;
            _eventSubWebhooks.OnChannelHypeTrainBegin += OnChannelHypeTrainBegin;
            _eventSubWebhooks.OnChannelPointsCustomRewardRedemptionAdd += OnChannelPointsCustomRewardRedemptionAdd;
            _eventSubWebhooks.OnStreamOnline += OnStreamOnline;
            _eventSubWebhooks.OnStreamOffline += OnStreamOffline;
            _logger.LogInformation($"Service started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Service stoping");
            _api.DeleteEventSubSubscription(Subscriptions);
            _eventSubWebhooks.OnError -= OnError;
            _eventSubWebhooks.OnChannelFollow -= OnChannelFollow;
            _eventSubWebhooks.OnChannelSubscribe += OnChannelSubscribe;
            _eventSubWebhooks.OnChannelSubscriptionGift += OnChannelSubscriptionGift;
            _eventSubWebhooks.OnChannelSubscriptionMessage += OnChannelSubscriptionMessage;
            _eventSubWebhooks.OnChannelCheer += OnChannelCheer;
            _eventSubWebhooks.OnChannelRaid -= OnChannelRaid;
            _eventSubWebhooks.OnChannelHypeTrainBegin -= OnChannelHypeTrainBegin;
            _eventSubWebhooks.OnChannelPointsCustomRewardRedemptionAdd -= OnChannelPointsCustomRewardRedemptionAdd;
            _eventSubWebhooks.OnStreamOnline -= OnStreamOnline;
            _eventSubWebhooks.OnStreamOffline -= OnStreamOffline;
            _logger.LogInformation($"Service stopped");
            return Task.CompletedTask;
        }

        private void OnChannelFollow(object sender, ChannelFollowArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Subscription.Id))
            {
                _logger.LogInformation($"{e.Notification.Event.UserName} followed {e.Notification.Event.BroadcasterUserName}");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.follow");
                alert.Add("username", e.Notification.Event.UserName);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Notification.Subscription.Id);
            }
        }

        private void OnChannelSubscribe(object sender, ChannelSubscribeArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Subscription.Id))
            {
                _logger.LogInformation($"{e.Notification.Event.UserName} subscribed to {e.Notification.Event.BroadcasterUserName}");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.subscribe");
                alert.Add("username", e.Notification.Event.UserName);
                alert.Add("isGift", e.Notification.Event.IsGift);
                alert.Add("tier", e.Notification.Event.Tier);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Notification.Subscription.Id);
            }
        }

        private void OnChannelSubscriptionGift(object sender, ChannelSubscriptionGiftArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Subscription.Id))
            {
                _logger.LogInformation($"{e.Notification.Event.UserName} gifted a subscription to {e.Notification.Event.BroadcasterUserName}");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.subscription.gift");
                alert.Add("username", e.Notification.Event.UserName);
                alert.Add("isAnonymous", e.Notification.Event.IsAnonymous);
                alert.Add("tier", e.Notification.Event.Tier);
                alert.Add("total", e.Notification.Event.Total);
                alert.Add("cumulativeTotal", e.Notification.Event.CumulativeTotal);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Notification.Subscription.Id);
            }
        }

        private void OnChannelSubscriptionMessage(object sender, ChannelSubscriptionMessageArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Subscription.Id))
            {
                _logger.LogInformation($"{e.Notification.Event.UserName} re-subscribed to {e.Notification.Event.BroadcasterUserName}");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.subscription.message");
                alert.Add("username", e.Notification.Event.UserName);
                alert.Add("message", e.Notification.Event.Message);
                alert.Add("tier", e.Notification.Event.Tier);
                alert.Add("durationMonths", e.Notification.Event.DurationMonths);
                alert.Add("cumulativeTotal", e.Notification.Event.CumulativeTotal);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Notification.Subscription.Id);
            }
        }

        private void OnChannelCheer(object sender, ChannelCheerArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Subscription.Id))
            {
                _logger.LogInformation($"{e.Notification.Event.UserName} gifted {e.Notification.Event.Bits} cheers to {e.Notification.Event.BroadcasterUserName}");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.cheer");
                alert.Add("username", e.Notification.Event.UserName);
                alert.Add("isAnonymous", e.Notification.Event.IsAnonymous);
                alert.Add("bits", e.Notification.Event.Bits);
                alert.Add("message", e.Notification.Event.Message);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                using (TwitchDbContext db = new(Guid.Empty))
                {
                    Viewer viewer = db.Viewers.Where(x => x.TwitchId == e.Notification.Event.UserId).FirstOrDefault();
                    Cheer cheer = new Cheer();
                    cheer.Owner = viewer.Id;
                    cheer.Amount = e.Notification.Event.Bits;
                    viewer.CheersCount += e.Notification.Event.Bits;
                    db.Cheers.Add(cheer);
                    db.SaveChanges();
                }
                HandledEvents.Add(e.Notification.Subscription.Id);
            }
        }

        private void OnChannelRaid(object sender, ChannelRaidArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Subscription.Id))
            {
                _logger.LogInformation($"{e.Notification.Event.FromBroadcasterUserName} raided {e.Notification.Event.ToBroadcasterUserName} with {e.Notification.Event.Viewers} person");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.raid");
                alert.Add("username", e.Notification.Event.FromBroadcasterUserName);
                alert.Add("viewers", e.Notification.Event.Viewers);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Notification.Subscription.Id);
            }
        }

        private void OnChannelHypeTrainBegin(object sender, ChannelHypeTrainBeginArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Subscription.Id))
            {
                _logger.LogInformation($"Hype train started");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.hype_train.begin");
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Notification.Subscription.Id);
            }
        }

        private void OnChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Event.Id))
            {
                _logger.LogInformation($"{e.Notification.Event.UserLogin} redeemed channel point reward {e.Notification.Event.Reward.Title}");
                Dictionary<string, object> reward = new Dictionary<string, object>();
                reward.Add("type", e.Notification.Event.Reward.Title);
                reward.Add("username", e.Notification.Event.UserName);
                reward.Add("user-id", e.Notification.Event.UserId);
                reward.Add("reward-id", e.Notification.Event.Reward.Id);
                reward.Add("event-id", e.Notification.Event.Id);
                if (!string.IsNullOrEmpty(e.Notification.Event.UserInput))
                {
                    reward.Add("user-input", e.Notification.Event.UserInput);
                }
                _hub.Clients.All.SendAsync("TriggerReward", reward);
                HandledEvents.Add(e.Notification.Event.Id);
            }
        }

        private void OnStreamOnline(object sender, StreamOnlineArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Event.Id))
            {
                _discord.SendMessage(_settings.DiscordFunction.NewsChannelId, "@Neph a lancé un live. Viens foutre le bordel avec nous sur https://www.twitch.tv/nephchevsky !").Wait();
            }
            HandledEvents.Add(e.Notification.Event.Id);
        }

        private void OnStreamOffline(object sender, StreamOfflineArgs e)
        {
            if (!HandledEvents.Contains(e.Notification.Subscription.Id))
            {
                _discord.DeleteMessage(_settings.DiscordFunction.NewsChannelId, "NephBot", "@Neph a lancé un live. Viens foutre le bordel avec nous sur https://www.twitch.tv/nephchevsky !").Wait();
            }
            HandledEvents.Add(e.Notification.Subscription.Id);
        }

        private void OnError(object sender, OnErrorArgs e)
        {
            _logger.LogError($"Reason: {e.Reason} - Message: {e.Message}");
        }
    }
}
