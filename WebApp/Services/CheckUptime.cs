using ApiDll;
using ChatDll;
using DbDll;
using ModelsDll;
using ModelsDll.Db;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;

namespace WebApp.Services
{
    public class CheckUptime : BackgroundService
    {
        public TwitchAPI api = new TwitchAPI();

        private readonly ILogger<CheckUptime> _logger;
        private readonly Settings _settings;
        private BasicChat _chat;
        private Api _api;
        private List<ChatterFormatted> CurrentChatters = new List<ChatterFormatted>();

        public CheckUptime(ILogger<CheckUptime> logger, IConfiguration configuration, Api api, BasicChat chat)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _chat = chat;
            _api = api;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.CheckUptimeFunction.ComputeUptime)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                if (await _api.IsStreamerLive())
                {
                    List<ChatterFormatted> chatters = await _api.GetChatters();
                    chatters.Add(new ChatterFormatted(_settings.Streamer, UserType.Broadcaster));
                    bool genericWelcome = false;
                    DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                    foreach (ChatterFormatted chatter in chatters)
                    {
                        using (TwitchDbContext db = new())
                        {
                            Viewer dbViewer = await _api.GetOrCreateUserByUsername(chatter.Username);
                            db.Viewers.Attach(dbViewer);
                            if (dbViewer != null)
                            {
                                if (dbViewer.Seen == 0)
                                {
                                    if (_settings.CheckUptimeFunction.WelcomeOnFirstJoin && !dbViewer.IsBot)
                                    {
                                        _logger.LogInformation($"Show commands when a new viewer is here");
                                        _chat.SendMessage($"Bienvenue sur le stream {dbViewer.DisplayName}. Tu peux venir rigoler avec nous ou bien taper ton meilleur lurk ;)");
                                    }
                                    if (_settings.CheckUptimeFunction.GenericWelcome && !dbViewer.IsBot)
                                    {
                                        genericWelcome = true;
                                    }
                                    dbViewer.Seen++;
                                }
                                else if (!CurrentChatters.Select(x => x.Username).ToList().Contains(chatter.Username, StringComparer.OrdinalIgnoreCase))
                                {
                                    if (dbViewer.LastViewedDateTime < now.AddSeconds(-_settings.CheckUptimeFunction.WelcomeOnJoinTimer))
                                    {
                                        dbViewer.Seen++;
                                        if (_settings.CheckUptimeFunction.WelcomeOnReJoin && !dbViewer.IsBot)
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
                                    DateTime limit = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
                                    new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                                    limit = TimeZoneInfo.ConvertTime(limit, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                                    Uptime dbUptime = db.Uptimes.Where(x => x.CreationDateTime >= limit).FirstOrDefault();
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
                }
                else
                {
                    CurrentChatters = new List<ChatterFormatted>();
				}

                await Task.Delay(TimeSpan.FromSeconds(_settings.CheckUptimeFunction.Timer), stoppingToken);
            }
        }
    }
}