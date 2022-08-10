using ApiDll;
using DbDll;
using Microsoft.AspNetCore.SignalR;
using ModelsDll;
using ModelsDll.Db;
using SpotifyAPI.Web;
using SpotifyDll;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Bits;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;

namespace WebApp.Services
{
	public class UpdateButtons : BackgroundService
    {
        private readonly ILogger<UpdateButtons> _logger;
        private readonly Settings _settings;
        private Api _api;
        private Spotify _spotify;
        readonly IHubContext<SignalService> _hub;

        private int CurrentButtonCursor = 0;

        public UpdateButtons(ILogger<UpdateButtons> logger, IConfiguration configuration, Spotify spotify, IHubContext<SignalService> hub)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = new Api(configuration, "twitchapi");
            _spotify = spotify;
            _hub = hub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                string buttonTitle = "";
                string buttonValue = "";
                Dictionary<string, object> button1 = new Dictionary<string, object>();
                Dictionary<string, object> button2 = new Dictionary<string, object>();
                (buttonTitle, buttonValue) = await GetNextButton();
                button1.Add("index", 1);
                button1.Add("title", buttonTitle);
                button1.Add("value", buttonValue);
                buttonTitle = "";
                buttonValue = "";
                (buttonTitle, buttonValue) = await GetNextButton();
                button2.Add("index", 2);
                button2.Add("title", buttonTitle);
                button2.Add("value", buttonValue);

                await _hub.Clients.All.SendAsync("UpdateButton", button1);
                await _hub.Clients.All.SendAsync("UpdateButton", button2);

                await Task.Delay(TimeSpan.FromSeconds(_settings.UpdateButtonsFunction.Timer), stoppingToken);
            }
        }

        public async Task<(string, string)> GetNextButton()
        {
            string buttonValue = "";
            string buttonTitle = "";
            while (string.IsNullOrEmpty(buttonValue))
            {
                buttonTitle = GetButtonNiceName(_settings.UpdateButtonsFunction.AvailableButtons[CurrentButtonCursor]);
                buttonValue = await GetButtonValue(_settings.UpdateButtonsFunction.AvailableButtons[CurrentButtonCursor]);
                CurrentButtonCursor = (CurrentButtonCursor + 1) % _settings.UpdateButtonsFunction.AvailableButtons.Count;
            }
            return (buttonTitle, buttonValue);
        }

        public async Task<string> GetButtonValue(string name)
        {
            if (name == "follower_goal" || name == "follower_count")
            {
                List<Follow> followers = await _api.GetFollowers();
                string value = followers.Count.ToString();
                if (name == "follower_goal")
                {
                    value += " / " + _settings.UpdateButtonsFunction.FollowerGoal;
                }
                return value;
            }
            else if (name == "follower_count")
            {
                return (await _api.GetFollowers()).Count.ToString();
            }
            else if (name == "viewer_count")
            {
                return (await _api.GetViewerCount()).ToString();
            }
            else if (name == "most_present_viewer_daily" || name == "most_present_viewer_monthly")
            {
                using (TwitchDbContext db = new(Guid.Empty))
                {
                    DateTime limit = DateTime.MinValue;
                    if (name == "most_present_viewer_daily")
                    {
                        limit = DateTime.Now.AddDays(-1);
                    }
                    else if (name == "most_present_viewer_monthly")
                    {
                        limit = DateTime.Now.AddMonths(-1);
                    }
                    var uptime = db.Uptimes.Where(x => x.CreationDateTime > limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Sum) }).OrderByDescending(g => g.Sum).ToList();
                    if (uptime.Count != 0)
                    {
                        Viewer dbViewer;
                        do
                        {
                            dbViewer = db.Viewers.Where(x => x.Id == uptime[0].Owner).FirstOrDefault();
                            uptime.RemoveAt(0);
                        } while (uptime.Count != 0 && dbViewer != null && (dbViewer.IsBot || dbViewer.Username == _settings.Streamer));
                        if (dbViewer != null && !dbViewer.IsBot && dbViewer.Username != _settings.Streamer)
                        {
                            return dbViewer.DisplayName;
                        }
                    }
                    return "";
                }
            }
            else if (name == "most_present_viewer_total")
            {
                using (TwitchDbContext db = new(Guid.Empty))
                {
                    Viewer mostPresentViewer = db.Viewers.Where(x => x.IsBot == false && x.Username != _settings.Streamer).OrderByDescending(x => x.Uptime).FirstOrDefault();
                    if (mostPresentViewer != null)
                    {
                        return mostPresentViewer.Username;
                    }
                    return "";
                }
            }
            else if (name == "most_speaking_viewer_daily" || name == "most_speaking_viewer_monthly")
            {
                using (TwitchDbContext db = new(Guid.Empty))
                {
                    DateTime limit = DateTime.MinValue;
                    if (name == "most_speaking_viewer_daily")
                    {
                        limit = DateTime.Now.AddDays(-1);
					}
                    else if (name == "most_speaking_viewer_monthly")
                    {
                        limit = DateTime.Now.AddMonths(-1);
                    }
                    var messages = db.Messages.Where(x => x.CreationDateTime > limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count()}).OrderByDescending(g => g.Count).ToList();
                    if (messages.Count != 0)
                    {
                        Viewer dbViewer;
                        do
                        {
                            dbViewer = db.Viewers.Where(x => x.Id == messages[0].Owner).FirstOrDefault();
                            messages.RemoveAt(0);
                        } while (messages.Count != 0 && dbViewer != null && (dbViewer.IsBot || dbViewer.Username == _settings.Streamer));
                        if (dbViewer != null && !dbViewer.IsBot && dbViewer.Username != _settings.Streamer)
                        {
                            return dbViewer.DisplayName;
                        }
                    }
                    return "";
                }
            }
            else if (name == "most_speaking_viewer_total")
            {
                using (TwitchDbContext db = new(Guid.Empty))
                {
                    Viewer dbViewer = db.Viewers.Where(x => x.IsBot == false && x.Username != _settings.Streamer).OrderByDescending(x => x.MessageCount).FirstOrDefault();
                    if (dbViewer != null )
                    {
                        return dbViewer.DisplayName;
					}
                    return "";
				}
            }
            else if (name == "last_follower")
            {
                return (await _api.GetFollowers())[0].FromUserName;
            }
            else if (name == "current_song")
            {
                FullTrack song = await _spotify.GetCurrentSong();
                if (song != null)
                {
                    return song.Artists[0].Name + " - " + song.Name;
                }
                return "";
            }
            else if (name == "top_cheers_daily" || name == "top_cheers_monthly" || name == "top_cheers_total")
            {
                BitsLeaderboardPeriodEnum period = BitsLeaderboardPeriodEnum.All;
                if (name == "top_cheers_daily")
                {
                    period = BitsLeaderboardPeriodEnum.Day;
                }
                else if (name == "top_cheers_monthly")
                {
                    period = BitsLeaderboardPeriodEnum.Month;
                }
                List<Listing> cheers = await _api.GetBitsLeaderBoard(1, period);
                if (cheers.Count != 0)
                {
                    return cheers[0].UserName + " (" + cheers[0].Score + ")";
				}
                return "";
			}
            else if (name == "subscriber_count" || name == "subscriber_goal" || name == "last_subscriber" || name == "last_subscription_gifter")
            {
                List<Subscription> subscribers = await _api.GetSubscribers();
                if (subscribers != null)
                {
                    if (name == "subscriber_count" || name == "subscriber_goal")
                    {
                        string value = (subscribers.Count - 1).ToString();
                        if (name == "subscriber_goal")
                        {
                            value += " / " + _settings.UpdateButtonsFunction.SubscriptionGoal;
                        }
                        return value;
                    }
                    else if (name == "last_subscriber")
                    {
                        return subscribers[subscribers.Count - 2].UserName;
                    }
                    else if (name == "last_subscription_gifter")
                    {
                        subscribers = subscribers.Where(x => x.IsGift == true).ToList();
                        return subscribers[subscribers.Count - 1].GifterName;
                    }
                    return "";
				}
                return "";
			}
            return "";
        }

        public string GetButtonNiceName(string name)
        {
            string value = "";
            switch (name)
            {
                case "follower_goal":
                    value = "Follower goal";
                    break;
                case "follower_count":
                    value = "Nombre de followers";
                    break;
                case "viewer_count":
                    value = "Nombre de viewers";
                    break;
                case "most_present_viewer_daily":
                    value = "Le plus présent (jour)";
                    break;
                case "most_speaking_viewer_daily":
                    value = "Le plus bavard (jour)";
                    break;
                case "most_present_viewer_monthly":
                    value = "Le plus présent (mois)";
                    break;
                case "most_speaking_viewer_monthly":
                    value = "Le plus bavard (mois)";
                    break;
                case "most_present_viewer_total":
                    value = "Le plus présent (total)";
                    break;
                case "most_speaking_viewer_total":
                    value = "Le plus bavard (total)";
                    break;
                case "last_follower":
                    value = "Dernier follower";
                    break;
                case "current_song":
                    value = "Musique";
                    break;
                case "top_cheers_daily":
                    value = "Top bits (daily)";
                    break;
                case "top_cheers_monthly":
                    value = "Top bits (monthly)";
                    break;
                case "top_cheers_total":
                    value = "Top bits (total)";
					break;
                case "subscriber_goal":
                    value = "Sub goal";
                    break;
                case "subscriber_count":
                    value = "Nombre de subs";
                    break;
                case "last_subscriber":
                    value = "Dernier sub";
                    break;
                case "last_subscription_gifter":
                    value = "Dernier sub gifter";
                    break;
            }
            return value;
        }
    }
}
