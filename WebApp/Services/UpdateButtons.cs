﻿using ApiDll;
using DbDll;
using Microsoft.AspNetCore.SignalR;
using ModelsDll;
using ModelsDll.Db;
using SpotifyAPI.Web;
using SpotifyDll;
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
            _api = new Api(configuration, false);
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
            if (name == "follower_goal")
            {
                return (await _api.GetFollowers()).Count.ToString() + " / " + _settings.UpdateButtonsFunction.FollowerGoal;
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
                    var messagesCount = db.Messages.Where(x => x.CreationDateTime > limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count()}).OrderByDescending(g => g.Count).ToList();
                    Viewer dbViewer;
                    do
                    {
                        dbViewer = db.Viewers.Where(x => x.Id == messagesCount[0].Owner).FirstOrDefault();
                        messagesCount.RemoveAt(0);
                    } while (messagesCount.Count != 0 && dbViewer != null && (dbViewer.IsBot || dbViewer.Username == _settings.Streamer));
                    if (dbViewer != null && !dbViewer.IsBot && dbViewer.Username != _settings.Streamer)
                    {
                        return dbViewer.DisplayName;
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
            }
            return value;
        }
    }
}
