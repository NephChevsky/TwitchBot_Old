using Bot.Services;
using Db;
using Db.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Workers
{
    public class UpdateFiles : BackgroundService
    {
        private readonly ILogger<UpdateFiles> _logger;
        private readonly Settings _options;
        private BotService _bot;

        private int CurrentButtonCursor = 0;

        public UpdateFiles(ILogger<UpdateFiles> logger, IConfiguration configuration, BotService bot)
        {
            _logger = logger;
            _options = configuration.GetSection("Settings").Get<Settings>();
            _bot = bot;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!_bot.ClientIsConnected)
            {
                await Task.Delay(25);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                long follower_count = await _bot.GetFollowerCount();
                if (follower_count > 0)
                {
                    File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "follower_goal.txt"), $"{follower_count} / {_options.UpdateFilesFunction.FollowerGoal}");
                    File.Delete(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "follower_count.txt"));
                }
                else
                {
                    File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "follower_count.txt"), $"{follower_count}");
                    File.Delete(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "follower_goal.txt"));
                }

                int viewer_count = await _bot.GetViewerCount();
                File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "viewer_count.txt"), viewer_count.ToString());

                using (TwitchDbContext db = new())
                {
                    Viewer bestViewer = db.Viewers.Where(x => x.IsBot == false).OrderByDescending(x => x.Uptime).FirstOrDefault();
                    if (bestViewer != null)
                    {
                        File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "best_viewer.txt"), viewer_count.ToString());
                    }
                }

                File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "last_follower.txt"), await _bot.GetLastFollower());

                List<string> files = Directory.GetFiles(_options.UpdateFilesFunction.OutputFolder).Where(name => !name.Contains("button")).ToList();
                string button1_title = Path.GetFileNameWithoutExtension(files[CurrentButtonCursor]);
                string button2_title = Path.GetFileNameWithoutExtension(files[(CurrentButtonCursor + 1) % files.Count]);
                string button1_value = File.ReadAllText(files[CurrentButtonCursor]);
                string button2_value = File.ReadAllText(files[(CurrentButtonCursor + 1) % files.Count]);
                File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "button1_title.txt"), GetButtonNiceName(button1_title));
                File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "button1_value.txt"), button1_value);
                File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "button2_title.txt"), GetButtonNiceName(button2_title));
                File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "button2_value.txt"), button2_value);
                CurrentButtonCursor = (CurrentButtonCursor + 2) % files.Count;

                await Task.Delay(TimeSpan.FromSeconds(_options.CheckUptimeFunction.Timer), stoppingToken);
            }
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
                case "best_viewer":
                    value = "View le plus présent";
                    break;
                case "last_follower":
                    value = "Dernier follower";
                    break;
            }
            return value;
        }
    }
}
