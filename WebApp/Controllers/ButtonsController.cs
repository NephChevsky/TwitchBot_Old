using ApiDll;
using DbDll;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;
using SpotifyAPI.Web;
using SpotifyDll;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Bits;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;

namespace WebApp.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ButtonsController : ControllerBase
	{
		private readonly ILogger<ButtonsController> _logger;
		private readonly Settings _settings;
		private Api _api;
		private Spotify _spotify;

        private static int CurrentButtonCursor = 0;

        public ButtonsController(ILogger<ButtonsController> logger, IConfiguration configuration, Api api, Spotify spotify)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_api = api;
			_spotify = spotify;
		}

		[HttpGet]
		public async Task<ActionResult<List<ButtonResponse>>> Get()
		{
            _logger.LogInformation("Get buttons value");
            List<ButtonResponse> response = new();
            response.Add(await GetNextButton());
            response.Add(await GetNextButton());
            _logger.LogInformation("Return buttons value");
            return Ok(response);
		}

        private async Task<ButtonResponse> GetNextButton()
        {
            ButtonResponse button = new();
            while (string.IsNullOrEmpty(button.Value))
            {
                button.Title = GetButtonNiceName(_settings.UpdateButtonsFunction.AvailableButtons[CurrentButtonCursor]);
                button.Value = await GetButtonValue(_settings.UpdateButtonsFunction.AvailableButtons[CurrentButtonCursor]);
                CurrentButtonCursor = (CurrentButtonCursor + 1) % _settings.UpdateButtonsFunction.AvailableButtons.Count;
            }
            return button;
        }

        private async Task<string> GetButtonValue(string name)
        {
            _logger.LogInformation($"Fetching button value: {name}");
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
            else if (name == "viewer_count")
            {
                return (await _api.GetViewerCount()).ToString();
            }
            else if (name == "most_present_viewer_daily" || name == "most_present_viewer_monthly")
            {
                using (TwitchDbContext db = new())
                {
                    DateTime limit = DateTime.MinValue;
                    DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    if (name == "most_present_viewer_daily")
                    {
                        limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    }
                    else if (name == "most_present_viewer_monthly")
                    {
                        limit = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                    }
                    var uptime = db.Uptimes.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Sum) }).OrderByDescending(g => g.Sum).ToList();
                    if (uptime.Count != 0)
                    {
                        Viewer dbViewer;
                        do
                        {
                            dbViewer = _api.GetOrCreateUserById(uptime[0].Owner);
                            uptime.RemoveAt(0);
                        } while (uptime.Count != 0 && dbViewer != null && (dbViewer.IsBot || dbViewer.Username == _settings.Streamer));
                        if (dbViewer != null) // TODO: need to fix this. This is dumb and it doesn't work
                        {
                            return dbViewer.DisplayName;
                        }
                    }
                    return "";
                }
            }
            else if (name == "most_present_viewer_total")
            {
                using (TwitchDbContext db = new())
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
                using (TwitchDbContext db = new())
                {
                    DateTime limit = DateTime.MinValue;
                    DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    if (name == "most_speaking_viewer_daily")
                    {
                        limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                    }
                    else if (name == "most_speaking_viewer_monthly")
                    {
                        limit = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                    }
                    var messages = db.Messages.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count() }).OrderByDescending(g => g.Count).ToList();
                    if (messages.Count != 0)
                    {
                        Viewer dbViewer;
                        do
                        {
                            dbViewer = _api.GetOrCreateUserById(messages[0].Owner);
                            messages.RemoveAt(0);
                        } while (messages.Count != 0 && dbViewer != null && (dbViewer.IsBot || dbViewer.Username == _settings.Streamer));
                        if (dbViewer != null && !dbViewer.IsBot && dbViewer.Username != _settings.Streamer)
                        {
                            return dbViewer.DisplayName;
                        }
                    }
                }
            }
            else if (name == "most_speaking_viewer_total")
            {
                using (TwitchDbContext db = new())
                {
                    Viewer dbViewer = db.Viewers.Where(x => x.IsBot == false && x.Username != _settings.Streamer).OrderByDescending(x => x.MessageCount).FirstOrDefault();
                    if (dbViewer != null)
                    {
                        return dbViewer.DisplayName;
                    }
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
            }
            else if (name == "top_cheers_daily" || name == "top_cheers_monthly" || name == "top_cheers_total")
            {
                using (TwitchDbContext db = new())
                {
                    DateTime limit = DateTime.MinValue;
                    if (name == "top_cheers_daily" || name == "top_cheers_monthly")
                    {
                        DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                        if (name == "top_cheers_daily")
                        {
                            limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                        }
                        else if (name == "top_cheers_monthly")
                        {
                            limit = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                        }
                    }
                    var topCheerer = db.Cheers.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Amount) }).OrderByDescending(g => g.Sum).FirstOrDefault();
                    if (topCheerer != null)
                    {
                        Viewer viewer = _api.GetOrCreateUserById(topCheerer.Owner);
                        if (viewer != null)
                        {
                            return $"{viewer.DisplayName} ({topCheerer.Sum})";
                        }
                    }
                }
            }
            else if (name == "subscriber_count" || name == "subscriber_goal")
            {
                using (TwitchDbContext db = new())
                {
                    DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    string value = db.Subscriptions.Where(x => x.Owner != _settings.StreamerTwitchId && x.EndDateTime >= now).Count().ToString();
                    if (name == "subscriber_goal")
                    {
                        value += " / " + _settings.UpdateButtonsFunction.SubscriptionGoal;
                    }
                    return value;
                }
            }
            else if (name == "last_subscriber")
            {
                using (TwitchDbContext db = new())
                {
                    Subscription sub = db.Subscriptions.Where(x => x.Owner != _settings.StreamerTwitchId).OrderByDescending(x => x.CreationDateTime).FirstOrDefault();
                    if (sub != null)
                    {
                        Viewer viewer = _api.GetOrCreateUserById(sub.Owner);
                        if (viewer != null)
                        {
                            return viewer.DisplayName;
                        }
                    }
                }
            }
            else if (name == "last_subscription_gifter")
            {
                using(TwitchDbContext db = new())
                {
                    Subscription subgift = db.Subscriptions.Where(x => x.IsGift == true).OrderByDescending(x => x.CreationDateTime).FirstOrDefault();
                    if (subgift != null)
                    {
                        Viewer viewer = _api.GetOrCreateUserById(subgift.GifterId);
                        if (viewer != null)
                        {
                            return viewer.DisplayName;
						}
					}
				}
            }
            else if (name == "top_subscription_gifter_daily" || name == "top_subscription_gifter_monthly" || name == "top_subscription_gifter_total")
            {
                using (TwitchDbContext db = new())
                {
                    DateTime limit = DateTime.MinValue;
                    if (name == "top_subscription_gifter_daily" || name == "top_subscription_gifter_monthly")
                    {
                        DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                        if (name == "top_subscription_gifter_daily")
                        {
                            limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                        }
                        else if (name == "top_subscription_gifter_monthly")
                        {
                            limit = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                        }
                    }
                    var topSubGifter = db.Subscriptions.Where(x => x.IsGift == true && x.CreationDateTime >= limit).GroupBy(x => x.GifterId).Select(g => new { GifterId = g.Key, Count = g.Count() }).OrderByDescending(g => g.Count).FirstOrDefault();
                    if (topSubGifter != null)
                    {
                        Viewer viewer = _api.GetOrCreateUserById(topSubGifter.GifterId);
                        if (viewer != null)
                        {
                            return $"{viewer.DisplayName} ({topSubGifter.Count})";
                        }
                    }
				}
            }
            else if (name == "longest_subscriber")
            {
                using (TwitchDbContext db = new())
                {
                    var subers = db.Subscriptions.Where(x => x.Owner != _settings.StreamerTwitchId).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count() }).OrderByDescending(g => g.Count).ToList();
                    Viewer viewer = _api.GetOrCreateUserById(subers[0].Owner);
                    if (viewer != null)
                    {
                        return viewer.DisplayName;
					}
				}
            }
            else if (name == "last_cheer")
            {
                using (TwitchDbContext db = new())
                {
                    var lastCheer = db.Cheers.Where(x => x.Owner != _settings.StreamerTwitchId).OrderByDescending(x => x.CreationDateTime).FirstOrDefault();
                    if (lastCheer != null)
                    {
                        Viewer viewer = _api.GetOrCreateUserById(lastCheer.Owner);
                        if (viewer != null)
                        {
                            return $"{viewer.DisplayName} ({lastCheer.Amount})";
                        }
                    }
                }
            }
            return "";
        }

        private string GetButtonNiceName(string name)
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
                case "top_subscription_gifter_daily":
                    value = "Meilleur subgifter (jour)";
                    break;
                case "top_subscription_gifter_monthly":
                    value = "Meilleur subgifter (mois)";
                    break;
                case "top_subscription_gifter_total":
                    value = "Meilleur subgifter (total)";
                    break;
                case "longest_subscriber":
                    value = "Plus long subscriber";
                    break;
                case "last_cheer":
                    value = "Dernier Bits";
                    break;
            }
            return value;
        }
    }
}
