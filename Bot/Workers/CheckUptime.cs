using ApiDll;
using ChatDll;
using DbDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;
using TwitchLib.Api;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Bot.Workers
{
    public class CheckUptime : BackgroundService
    {
        public TwitchAPI api = new TwitchAPI();

        private readonly ILogger<CheckUptime> _logger;
        private readonly Settings _settings;
        private BasicChat _chat;
        private Api _api;
        private List<ChatterFormatted> CurrentChatters = new List<ChatterFormatted>();
        private List<string> _bots;

        public CheckUptime(ILogger<CheckUptime> logger, IConfiguration configuration, Api api, BasicChat chat)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _bots = configuration.GetSection("TwitchBotList").Get<List<string>>();
            _chat = chat;
            _api = api;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.CheckUptimeFunction.ComputeUptime)
            {
                return;
            }

            while (!_chat.IsConnected)
            {
                await Task.Delay(25);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                List<ChatterFormatted> chatters = await _api.GetChatters();
                bool genericWelcome = false;
                DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                foreach (ChatterFormatted chatter in chatters)
                {
                    using (TwitchDbContext db = new())
                    {
                        User user;
                        Viewer dbViewer = db.Viewers.Where(obj => obj.Username == chatter.Username).FirstOrDefault();
                        if (dbViewer == null)
                        {
                            user = await _api.GetUser(chatter.Username.ToLower());
                            dbViewer = db.Viewers.Where(obj => obj.Id == user.Id).FirstOrDefault();
                            if (dbViewer != null)
                            {
                                dbViewer.Username = user.Login;
                                dbViewer.DisplayName = user.DisplayName;
							}
                            else
                            {
                                if (!_bots.Contains(chatter.Username.ToLower()))
                                {
                                    dbViewer = new Viewer(user.Login, user.DisplayName, user.Id);
                                    db.Viewers.Add(dbViewer);
                                    if (_settings.CheckUptimeFunction.WelcomeOnFirstJoin)
                                    {
                                        _logger.LogInformation($"Show commands when a new viewer is here");
                                        _chat.SendMessage($"Bienvenue sur le stream {dbViewer.DisplayName}. Tu peux venir rigoler avec nous où bien taper ton meilleur lurk ;)");
                                    }
                                    if (_settings.CheckUptimeFunction.GenericWelcome)
                                    {
                                        genericWelcome = true;
                                    }
                                    db.SaveChanges();
                                }
                                continue;
                            }
                        }
                        if (dbViewer != null && !dbViewer.IsBot)
                        {
                            if (!CurrentChatters.Select(x => x.Username).ToList().Contains(chatter.Username, StringComparer.OrdinalIgnoreCase))
                            {
                                if (dbViewer.LastViewedDateTime < now.AddSeconds(-_settings.CheckUptimeFunction.WelcomeOnJoinTimer))
                                {
                                    dbViewer.Seen++;
                                    if (_settings.CheckUptimeFunction.WelcomeOnReJoin)
                                    {
                                        _logger.LogInformation($"Say hi to known viewer {dbViewer.Username}");
                                        _chat.SendMessage($"Salut {dbViewer.DisplayName} ! Bon retour sur le stream !");
                                    }
                                }
                            }
                            else
                            {
                                int uptime = (int)(now - dbViewer.LastViewedDateTime).TotalSeconds;
                                dbViewer.Uptime += uptime;
                                Uptime dbUptime = db.Uptimes.Where(x => x.CreationDateTime >= now.AddDays(-1)).FirstOrDefault();
                                if (dbUptime != null)
                                {
                                    dbUptime.Sum += uptime;
								}
                                else
                                {
                                    dbUptime = new();
                                    dbUptime.Sum = uptime;
                                    dbUptime.Owner = dbViewer.Id;
                                    db.Uptimes.Add(dbUptime);
								}
                            }
                            dbViewer.LastViewedDateTime = now;
                            db.SaveChanges();
                        }
                    }
                }
                if (_settings.CheckUptimeFunction.GenericWelcome && genericWelcome)
                {
                    _chat.SendMessage($"Je développe un bot Twitch pour créer des intéractions entre le chat, mon stream et mon gameplay. Teste le en tapant !bot pour voir les commandes disponibles ;)");
                }
                CurrentChatters = chatters;

                await Task.Delay(TimeSpan.FromSeconds(_settings.CheckUptimeFunction.Timer), stoppingToken);
            }
        }
    }
}