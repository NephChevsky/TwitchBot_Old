using ApiDll;
using ChatDll;
using DbDll;
using Microsoft.AspNetCore.SignalR;
using ModelsDll;
using GoogleDll;
using ModelsDll.Db;
using SpotifyAPI.Web;
using SpotifyDll;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
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
        private Secret _secret;
        private DiscordDll.Discord _discord;
        private TwitchAPI _eventSubApi;
        private Api _api;
        private BasicChat _chat;
        private Spotify _spotify;
        private GoogleDll.Google _google;

        private List<EventSubSubscription> Subscriptions;
        private List<string> HandledEvents = new List<string>();
        private int BitsCounter = 0;
        private Timer BitsCounterTimer;
        private Random Rng = new Random(Guid.NewGuid().GetHashCode());

        public EventSubService(ILogger<EventSubService> logger, IConfiguration configuration, ITwitchEventSubWebhooks eventSubWebhooks, IHubContext<SignalService> hub, DiscordDll.Discord discord, Api api, BasicChat chat, Spotify spotify, GoogleDll.Google google)
		{
			_logger = logger;
			_eventSubWebhooks = eventSubWebhooks;
			_hub = hub;
			_settings = configuration.GetSection("Settings").Get<Settings>();
            _secret = configuration.GetSection("Secret").Get<Secret>();
            _discord = discord;
			_api = api;
			_chat = chat;
			_spotify = spotify;
            _google = google;

			_eventSubApi = new();
			_eventSubApi.Settings.ClientId = _secret.Twitch.ClientId;
			_eventSubApi.Settings.Secret = _secret.Twitch.ClientSecret;

			Start().GetAwaiter().GetResult();

			BitsCounterTimer = new Timer(BitsCounterReset, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
		}

		public async Task Start()
        {
            _eventSubApi.Settings.AccessToken = await _eventSubApi.Auth.GetAccessTokenAsync();
            Subscriptions = new();
            List<EventSubSubscription> subs = await GetEventSubSubscription();
            DeleteEventSubSubscription(subs);
            Subscriptions.Add(await CreateEventSubSubscription("channel.follow"));
            Subscriptions.Add(await CreateEventSubSubscription("channel.subscribe"));
            Subscriptions.Add(await CreateEventSubSubscription("channel.subscription.gift"));
            Subscriptions.Add(await CreateEventSubSubscription("channel.subscription.message"));
            Subscriptions.Add(await CreateEventSubSubscription("channel.subscription.end"));
            Subscriptions.Add(await CreateEventSubSubscription("channel.cheer"));
            Subscriptions.Add(await CreateEventSubSubscription("channel.raid"));
            Subscriptions.Add(await CreateEventSubSubscription("channel.hype_train.begin"));
            Subscriptions.Add(await CreateEventSubSubscription("channel.channel_points_custom_reward_redemption.add"));
            Subscriptions.Add(await CreateEventSubSubscription("stream.online"));
            Subscriptions.Add(await CreateEventSubSubscription("stream.offline"));
        }

		private async Task<EventSubSubscription> CreateEventSubSubscription(string type)
        {
            Dictionary<string, string> conditions = new Dictionary<string, string>();
            switch (type)
            {
                case "channel.raid":
                    conditions.Add("to_broadcaster_user_id", _settings.StreamerTwitchId);
                    break;
                default:
                    conditions.Add("broadcaster_user_id", _settings.StreamerTwitchId);
                    break;
            }
            CreateEventSubSubscriptionResponse response = await _eventSubApi.Helix.EventSub.CreateEventSubSubscriptionAsync(type, "1", conditions, "webhook", _settings.EventSubUrl, _secret.Twitch.ClientSecret);
            return response.Subscriptions[0];
        }

        private void DeleteEventSubSubscription(List<EventSubSubscription> subscriptions)
        {
            subscriptions.ForEach(async x =>
            {
                await _eventSubApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(x.Id);
            });
        }

        private async Task<List<EventSubSubscription>> GetEventSubSubscription()
        {
            GetEventSubSubscriptionsResponse response = await _eventSubApi.Helix.EventSub.GetEventSubSubscriptionsAsync();
            return response.Subscriptions.ToList();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Service starting");
            _eventSubWebhooks.OnError += OnError;
            _eventSubWebhooks.OnChannelFollow += OnChannelFollow;
            _eventSubWebhooks.OnChannelSubscribe += OnChannelSubscribe;
            _eventSubWebhooks.OnChannelSubscriptionGift += OnChannelSubscriptionGift;
            _eventSubWebhooks.OnChannelSubscriptionMessage += OnChannelSubscriptionMessage;
            _eventSubWebhooks.OnChannelSubscriptionEnd += OnChannelSubscriptionEnd;
            _eventSubWebhooks.OnChannelCheer += OnChannelCheer;
            _eventSubWebhooks.OnChannelRaid += OnChannelRaid;
            _eventSubWebhooks.OnChannelHypeTrainBegin += OnChannelHypeTrainBegin;
            _eventSubWebhooks.OnChannelPointsCustomRewardRedemptionAdd += OnChannelPointsCustomRewardRedemptionAdd;
            _eventSubWebhooks.OnStreamOnline += OnStreamOnline;
            _logger.LogInformation($"Service started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Service stopping");
            DeleteEventSubSubscription(Subscriptions);
            _eventSubWebhooks.OnError -= OnError;
            _eventSubWebhooks.OnChannelFollow -= OnChannelFollow;
            _eventSubWebhooks.OnChannelSubscribe -= OnChannelSubscribe;
            _eventSubWebhooks.OnChannelSubscriptionGift -= OnChannelSubscriptionGift;
            _eventSubWebhooks.OnChannelSubscriptionMessage -= OnChannelSubscriptionMessage;
            _eventSubWebhooks.OnChannelSubscriptionEnd -= OnChannelSubscriptionEnd;
            _eventSubWebhooks.OnChannelCheer -= OnChannelCheer;
            _eventSubWebhooks.OnChannelRaid -= OnChannelRaid;
            _eventSubWebhooks.OnChannelHypeTrainBegin -= OnChannelHypeTrainBegin;
            _eventSubWebhooks.OnChannelPointsCustomRewardRedemptionAdd -= OnChannelPointsCustomRewardRedemptionAdd;
            _eventSubWebhooks.OnStreamOnline -= OnStreamOnline;
            _logger.LogInformation($"Service stopped");
            return Task.CompletedTask;
        }

        private void OnChannelFollow(object sender, ChannelFollowArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                _logger.LogInformation($"{e.Notification.Event.UserName} followed {e.Notification.Event.BroadcasterUserName}");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.follow");
                alert.Add("username", e.Notification.Event.UserName);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void OnChannelSubscribe(object sender, ChannelSubscribeArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                _logger.LogInformation($"{e.Notification.Event.UserName} subscribed to {e.Notification.Event.BroadcasterUserName}");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.subscribe");
                alert.Add("username", e.Notification.Event.UserName);
                alert.Add("isGift", e.Notification.Event.IsGift);
                alert.Add("tier", e.Notification.Event.Tier);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                using (TwitchDbContext db = new())
                {
                    DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    Subscription sub = new Subscription();
                    sub.Owner = e.Notification.Event.UserId;
                    sub.Tier = e.Notification.Event.Tier;
                    sub.EndDateTime = now.AddMonths(1);
                    db.Subscriptions.Add(sub);
                    db.SaveChanges();
				}
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void OnChannelSubscriptionGift(object sender, ChannelSubscriptionGiftArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
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
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void OnChannelSubscriptionMessage(object sender, ChannelSubscriptionMessageArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                _logger.LogInformation($"{e.Notification.Event.UserName} re-subscribed to {e.Notification.Event.BroadcasterUserName}");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.subscription.message");
                alert.Add("username", e.Notification.Event.UserName);
                alert.Add("message", e.Notification.Event.Message.Text);
                alert.Add("tier", e.Notification.Event.Tier);
                alert.Add("durationMonths", e.Notification.Event.DurationMonths);
                alert.Add("cumulativeTotal", e.Notification.Event.CumulativeTotal);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                using (TwitchDbContext db = new())
                {
                    DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    Subscription lastSub = db.Subscriptions.Where(x => x.Owner == e.Notification.Event.UserId && x.CreationDateTime <= now.AddDays(-15) && x.EndDateTime >= now.AddDays(15)).FirstOrDefault();
                    if (lastSub != null && lastSub.EndDateTime >= now)
                    {
                        lastSub.EndDateTime = now;
					}
                    Subscription sub = new Subscription();
                    sub.Owner = e.Notification.Event.UserId;
                    sub.Tier = e.Notification.Event.Tier;
                    sub.EndDateTime = now.AddMonths(1);
                    db.Subscriptions.Add(sub);
                    db.SaveChanges();
                }
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void OnChannelSubscriptionEnd(object send, ChannelSubscriptionEndArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                using (TwitchDbContext db = new())
                {
                    DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    Subscription sub = db.Subscriptions.Where(x => x.Owner == e.Notification.Event.UserId).OrderByDescending(x => x.EndDateTime).FirstOrDefault();
                    sub.EndDateTime = now;
                    db.SaveChanges();
                }
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void OnChannelCheer(object sender, ChannelCheerArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                
                _logger.LogInformation($"{e.Notification.Event.UserName} gifted {e.Notification.Event.Bits} cheers");
                using (TwitchDbContext db = new())
                {
                    if (e.Notification.Event.Bits > BitsCounter)
                    {
                        Dictionary<string, object> alert = new Dictionary<string, object>();
                        alert.Add("type", "channel.cheer");
                        alert.Add("username", e.Notification.Event.UserName);
                        alert.Add("isAnonymous", e.Notification.Event.IsAnonymous);
                        alert.Add("bits", e.Notification.Event.Bits);
                        alert.Add("message", e.Notification.Event.Message);
                        if (e.Notification.Event.Bits >= 100)
                        {
                            MemoryStream speech = _google.ConvertToSpeech(e.Notification.Event.Message);
                            alert.Add("tts", Convert.ToBase64String(speech.ToArray()));
						}
                        _hub.Clients.All.SendAsync("TriggerAlert", alert);
                        BitsCounter++;
                    }
                    Cheer cheer = new();
                    cheer.Owner = e.Notification.Event.UserId;
                    cheer.Amount = e.Notification.Event.Bits;
                    db.Cheers.Add(cheer);
                    db.SaveChanges();
                }
                
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void OnChannelRaid(object sender, ChannelRaidArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                _logger.LogInformation($"{e.Notification.Event.FromBroadcasterUserName} raided {e.Notification.Event.ToBroadcasterUserName} with {e.Notification.Event.Viewers} viewers");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.raid");
                alert.Add("username", e.Notification.Event.FromBroadcasterUserName);
                alert.Add("viewers", e.Notification.Event.Viewers);
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void OnChannelHypeTrainBegin(object sender, ChannelHypeTrainBeginArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                _logger.LogInformation($"Hype train started");
                Dictionary<string, object> alert = new Dictionary<string, object>();
                alert.Add("type", "channel.hype_train.begin");
                _hub.Clients.All.SendAsync("TriggerAlert", alert);
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private async void OnChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                _logger.LogInformation($"{e.Notification.Event.UserLogin} redeemed channel point reward {e.Notification.Event.Reward.Title}");

                bool validate = false;
                bool cancel = false;
                if (string.Equals(e.Notification.Event.Reward.Title, "Ajouter une commande", StringComparison.InvariantCultureIgnoreCase))
                {
                    int offset = e.Notification.Event.UserInput.IndexOf(" ");
                    if (offset > -1)
                    {
                        string commandName = e.Notification.Event.UserInput.Substring(0, offset).Replace("!", "");
                        string commandMessage = e.Notification.Event.UserInput.Substring(offset + 1);
                        validate = Helpers.Commands.AddCommand(_chat, commandName, commandMessage, e.Notification.Event.UserId, e.Notification.Event.UserName);
                        cancel = !validate;
                    }
                }
                else if (string.Equals(e.Notification.Event.Reward.Title, "Supprimer une commande", StringComparison.InvariantCultureIgnoreCase))
                {
                    validate = Helpers.Commands.DeleteCommand(_chat, e.Notification.Event.UserInput.Replace("!", ""), e.Notification.Event.UserName);
                    cancel = !validate;
                }
                else if (string.Equals(e.Notification.Event.Reward.Title, "Passer à la musique suivante", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (await _spotify.SkipSong())
                    {
                        validate = true;
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Notification.Event.UserName} : On n'écoute pas de musique bouffon");
                        cancel = true;
                    }
                }
                else if (string.Equals(e.Notification.Event.Reward.Title, "Ajouter une musique", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (TwitchDbContext db = new())
                    {
                        FullTrack song = await _spotify.SearchSong(e.Notification.Event.UserInput);
                        SongToAdd tmp = new SongToAdd(e.Notification.Event.UserId, song.Uri, e.Notification.Event.Reward.Id, e.Notification.Event.Id);
                        db.SongsToAdd.Add(tmp);
                        db.SaveChanges();
                        _chat.SendMessage($"{e.Notification.Event.UserName} : Ajouter \"{song.Artists[0].Name} - {song.Name}\" ? (!oui/!non)");
                    }
                }
                else if (string.Equals(e.Notification.Event.Reward.Title, "Supprimer une musique", StringComparison.InvariantCultureIgnoreCase))
                {
                    bool ret = await _spotify.RemoveSong();
                    if (ret)
                    {
                        _chat.SendMessage($"{e.Notification.Event.UserName} : La musique a été supprimée de la playlist");
                        validate = true;
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Notification.Event.UserName} : La musique n'a pas pu être supprimée de la playlist");
                        cancel = true;
                    }
                }
                else if (string.Equals(e.Notification.Event.Reward.Title, "Timeout un viewer", StringComparison.InvariantCultureIgnoreCase))
                {
                    e.Notification.Event.UserInput = e.Notification.Event.UserInput.Replace("@", "").Split(" ")[0];
                    List<Moderator> mods = await _api.GetModerators();
                    Moderator firstmod = mods.Where(x => string.Equals(e.Notification.Event.UserInput, x.UserName)).FirstOrDefault();
                    Moderator secondmod = mods.Where(x => string.Equals(e.Notification.Event.UserName, x.UserName)).FirstOrDefault();
                    if (firstmod == null && secondmod == null)
                    {
                        Viewer firstViewer, secondViewer;
                        using (TwitchDbContext db = new())
                        {
                            firstViewer = await _api.GetOrCreateUserByUsername(e.Notification.Event.UserName);
                            secondViewer = await _api.GetOrCreateUserByUsername(e.Notification.Event.UserInput);
                        }
                        if (firstViewer != null && secondViewer != null)
                        {
                            int dice = Rng.Next(5);
                            int timer = Rng.Next(300);
                            if (dice == 0)
                            {
                                _chat.SendMessage($"Roll: {timer}/300. Dommage {firstViewer.DisplayName}! LUL");
                                _api.BanUser(firstViewer.Username, timer);
                            }
                            else if (dice == 1 || dice == 2)
                            {
                                _chat.SendMessage($"Roll: {timer}/300. Désolé {secondViewer.DisplayName}! LUL");
                                _api.BanUser(secondViewer.Username, timer);
                            }
                            else if (dice == 3)
                            {
                                _chat.SendMessage($"Roll: {timer}/300. Allez ça dégage {e.Notification.Event.UserName} et {secondViewer.DisplayName}! LUL");
                                _api.BanUser(secondViewer.Username, timer);
                                _api.BanUser(firstViewer.Username, timer);
                            }
                            else
                            {
                                _chat.SendMessage($"{firstViewer.DisplayName} : Non, pas envie aujourd'hui Kappa");
                            }
                            validate = true;
                        }
                        else
                        {
                            _chat.SendMessage($"{(firstViewer != null ? firstViewer.DisplayName : e.Notification.Event.UserName)} : Utilisateur inconnu");
                            cancel = true;
                        }
                    }
                    else
                    {
                        if (firstmod == null)
                        {
                            _chat.SendMessage($"{e.Notification.Event.UserName} : T'as cru t'allais timeout un modo?");
                            _api.BanUser(e.Notification.Event.UserName);
                            validate = true;
                        }
                        else
                        {
                            _chat.SendMessage($"{e.Notification.Event.UserName} : T'es un modo gros bouff'!");
                            validate = true;
                        }
                    }
                }
                else if ((string.Equals(e.Notification.Event.Reward.Title, "VIP", StringComparison.InvariantCultureIgnoreCase)))
                {
                    List<Moderator> mods = await _api.GetModerators();
                    Moderator mod = mods.Where(x => string.Equals(e.Notification.Event.UserId, x.UserId)).FirstOrDefault();
                    if (mod == null)
                    {
                        await _api.AddVIP(e.Notification.Event.UserId);
                        validate = true;
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Notification.Event.UserName} T'es modo ducon!");
                        cancel = true;
                    }
                }
                else
                {
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

                    await _hub.Clients.All.SendAsync("TriggerReward", reward);
                }

                if (validate)
                {
                    await HelpersDll.Helpers.ValidateRewardRedemption(_api, e.Notification.Event.Reward.Title, e.Notification.Event.Reward.Id, e.Notification.Event.Id);
				}

                if (cancel)
                {
                    await HelpersDll.Helpers.CancelRewardRedemption(_api, e.Notification.Event.Reward.Id, e.Notification.Event.Id);
                }
                
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void OnStreamOnline(object sender, StreamOnlineArgs e)
        {
            if (!HandledEvents.Contains(e.Headers["Twitch-Eventsub-Message-Id"]))
            {
                _discord.SendMessage(_settings.DiscordFunction.NewsChannelId, "@everyone Neph a lancé un live. Viens foutre le bordel avec nous sur https://www.twitch.tv/nephchevsky !").Wait();
                HandledEvents.Add(e.Headers["Twitch-Eventsub-Message-Id"]);
            }
        }

        private void BitsCounterReset(object sender)
        {
            if (BitsCounter>0)
            {
                BitsCounter--;
            }
		}

        private void OnError(object sender, OnErrorArgs e)
        {
            _logger.LogError($"Reason: {e.Reason} - Message: {e.Message}");
        }
    }
}
