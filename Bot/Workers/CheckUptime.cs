using Bot.Services;
using Db;
using Db.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchLib.Api;

namespace Bot.Workers
{
    public class CheckUptime : BackgroundService
    {
        public TwitchAPI api = new TwitchAPI();

        private readonly ILogger<CheckUptime> _logger;
        private readonly Settings _options;
        private BotService _bot;

        public CheckUptime(ILogger<CheckUptime> logger, IConfiguration configuration, BotService bot)
        {
            _logger = logger;
            _options = configuration.GetSection("Settings").Get<Settings>();
            _bot = bot;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.CheckUptimeFunction.ComputeUptime)
            {
                return;
            }

            while (!_bot.IsConnected)
            {
                await Task.Delay(25);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                List<string> chatters = await _bot.GetChatters();

                chatters.ForEach(x =>
                {
                    using (TwitchDbContext db = new())
                    {
                        bool shouldGreet = _options.CheckUptimeFunction.WelcomeOnJoin && !string.Equals(x, _options.Channel, StringComparison.InvariantCultureIgnoreCase);
                        Viewer dbViewer = db.Viewers.Where(obj => obj.Username == x).FirstOrDefault();
                        if (dbViewer != null)
                        {
                            shouldGreet &= dbViewer.LastViewedDateTime < DateTime.Now.AddSeconds(-_options.CheckUptimeFunction.WelcomeOnJoinTimer) && !dbViewer.IsBot;

                            if (!_bot.CurrentViewerList.Contains(x, StringComparer.OrdinalIgnoreCase))
                            {
                                dbViewer.Seen++;
                                if (shouldGreet)
                                {
                                    _logger.LogInformation($"Say hi to known viewer {dbViewer.Username}");
                                    _bot.SendMessage($"Salut {dbViewer.Username} ! Bon retour sur le stream !");
                                }
                            }
                            else
                            {
                                dbViewer.Uptime += (long)(DateTime.Now - dbViewer.LastViewedDateTime).TotalSeconds;
                            }
                            dbViewer.LastViewedDateTime = DateTime.Now;
                        }
                        else
                        {
                            dbViewer = new Viewer(x);
                            db.Viewers.Add(dbViewer);
                            if (shouldGreet)
                            {
                                _logger.LogInformation($"Say hi to new viewer {dbViewer.Username}");
                                _bot.SendMessage($"Salut {dbViewer.Username} ! Bienvenue sur le stream. Tu peux nous faire un petit coucou ou bien taper ton meilleur lurk. Tape !bot pour voir les commandes disponibles ;)");
                            }
                        }
                        db.SaveChanges();
                    }
                });
                _bot.CurrentViewerList = chatters;

                await Task.Delay(TimeSpan.FromSeconds(_options.CheckUptimeFunction.Timer), stoppingToken);
            }
        }
    }
}