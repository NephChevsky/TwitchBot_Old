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
        private Chat _chat;
        private Api _api;
        private List<ChatterFormatted> CurrentChatters = new List<ChatterFormatted>();

        public CheckUptime(ILogger<CheckUptime> logger, IConfiguration configuration, Chat chat)
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

                chatters.ForEach(async x =>
                {
                    using (TwitchDbContext db = new(Guid.Empty))
                    {
                        Viewer dbViewer = db.Viewers.Where(obj => obj.Username == x.Username).FirstOrDefault();
                        if (dbViewer != null)
                        {
                            if (!CurrentChatters.Select(y => y.Username).ToList().Contains(x.Username, StringComparer.OrdinalIgnoreCase))
                            {
                                if (_settings.CheckUptimeFunction.WelcomeOnReJoin && !dbViewer.IsBot && dbViewer.LastViewedDateTime < DateTime.Now.AddSeconds(-_settings.CheckUptimeFunction.WelcomeOnJoinTimer))
                                {
                                    _logger.LogInformation($"Say hi to known viewer {dbViewer.Username}");
                                    _chat.SendMessage($"Salut {dbViewer.Username} ! Bon retour sur le stream !");
                                }
                                dbViewer.Seen++;
                            }
                            else
                            {
                                dbViewer.Uptime += (long)(DateTime.Now - dbViewer.LastViewedDateTime).TotalSeconds;
                            }
                            dbViewer.LastViewedDateTime = DateTime.Now;
                        }
                        else
                        {
                            User user = await _api.GetUser(x.Username);
                            dbViewer = new Viewer(user.Login, user.DisplayName, user.Id);
                            db.Viewers.Add(dbViewer);
                            if (_settings.CheckUptimeFunction.WelcomeOnFirstJoin)
                            {
                                _logger.LogInformation($"Show commands when a new viewer is here");
                                _chat.SendMessage($"Je développe un bot Twitch pour créer des intéractions entre le chat, mon stream et mon gameplay. Teste le en tapant !bot pour voir les commandes disponibles ;)");
                            }
                        }
                        db.SaveChanges();
                    }
                });
                CurrentChatters = chatters;

                await Task.Delay(TimeSpan.FromSeconds(_settings.CheckUptimeFunction.Timer), stoppingToken);
            }
        }
    }
}