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

        public CheckUptime(ILogger<CheckUptime> logger, IConfiguration configuration, BasicChat chat)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _chat = chat;
            _api = new(configuration, false);
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
                DateTime now = DateTime.Now;
                chatters.ForEach(async x =>
                {
                    using (TwitchDbContext db = new(Guid.Empty))
                    {
                        Viewer dbViewer = db.Viewers.Where(obj => obj.Username == x.Username).FirstOrDefault();
                        if (dbViewer != null && !dbViewer.IsBot)
                        {
                            if (!CurrentChatters.Select(y => y.Username).ToList().Contains(x.Username, StringComparer.OrdinalIgnoreCase))
                            {
                                if (dbViewer.LastViewedDateTime < DateTime.Now.AddSeconds(-_settings.CheckUptimeFunction.WelcomeOnJoinTimer))
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
                                using (TwitchDbContext db1 = new(dbViewer.Id))
                                {
                                    Uptime dbUptime = db1.Uptimes.Where(y => y.CreationDateTime >= now.AddDays(-1)).FirstOrDefault();
                                    if (dbUptime != null)
                                    {
                                        dbUptime.Sum += uptime;
									}
                                    else
                                    {
                                        dbUptime = new();
                                        dbUptime.Sum = uptime;
                                        db1.Uptimes.Add(dbUptime);
									}
                                    db1.SaveChanges();
                                }
                            }
                            dbViewer.LastViewedDateTime = now;
                        }
                        else
                        {
                            if (!_settings.TwitchBotList.Contains(x.Username.ToLower()))
                            {
                                User user = await _api.GetUser(x.Username.ToLower());
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
                            }
                        }
                        db.SaveChanges();
                    }
                });
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