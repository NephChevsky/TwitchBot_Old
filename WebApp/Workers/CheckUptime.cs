﻿using ApiDll;
using ChatDll;
using DbDll;
using ModelsDll;
using ModelsDll.Db;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;

namespace WebApp.Workers
{
    public class CheckUptime : BackgroundService
    {
        public Api _api;
        private Chat _chat;
        private List<ChatterFormatted> _chatters = new();

        private readonly ILogger<CheckUptime> _logger;
        private readonly Settings _options;

        public CheckUptime(ILogger<CheckUptime> logger, IConfiguration configuration, Chat chat)
        {
            _logger = logger;
            _options = configuration.GetSection("Settings").Get<Settings>();
            _chat = chat;
            _api = new(configuration, false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.CheckUptimeFunction.ComputeUptime)
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

                chatters.ForEach(x =>
                {
                    using (TwitchDbContext db = new())
                    {
                        bool shouldGreet = _options.CheckUptimeFunction.WelcomeOnJoin && !string.Equals(x.Username, _options.Channel, StringComparison.InvariantCultureIgnoreCase);
                        Viewer dbViewer = db.Viewers.Where(obj => obj.Username == x.Username).FirstOrDefault();
                        if (dbViewer != null)
                        {
                            shouldGreet &= dbViewer.LastViewedDateTime < DateTime.Now.AddSeconds(-_options.CheckUptimeFunction.WelcomeOnJoinTimer) && !dbViewer.IsBot;

                            if (!_chatters.Select(x => x.Username).Contains(x.Username, StringComparer.OrdinalIgnoreCase))
                            {
                                if (shouldGreet)
                                {
                                    dbViewer.Seen++;
                                    _logger.LogInformation($"Say hi to known viewer {dbViewer.Username}");
                                    _chat.SendMessage($"Salut {dbViewer.Username} ! Bon retour sur le stream !");
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
                            dbViewer = new Viewer(x.Username);
                            db.Viewers.Add(dbViewer);
                            if (shouldGreet)
                            {
                                _logger.LogInformation($"Say hi to new viewer {dbViewer.Username}");
                                _chat.SendMessage($"Salut {dbViewer.Username} ! Bienvenue sur le stream. Tu peux nous faire un petit coucou ou bien taper ton meilleur lurk. Tape !bot pour voir les commandes disponibles ;)");
                            }
                        }
                        db.SaveChanges();
                    }
                });
                _chatters = chatters;

                await Task.Delay(TimeSpan.FromSeconds(_options.CheckUptimeFunction.Timer), stoppingToken);
            }
        }
    }
}