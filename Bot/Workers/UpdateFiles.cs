using ApiDll;
using DbDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;
using SpotifyAPI.Web;
using SpotifyDll;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;

namespace Bot.Workers
{
    public class UpdateFiles : BackgroundService
    {
        private readonly ILogger<UpdateFiles> _logger;
        private readonly Settings _options;
        private Api _api;
        private Spotify _spotify;

        private int CurrentButtonCursor = 0;

        public UpdateFiles(ILogger<UpdateFiles> logger, IConfiguration configuration, Spotify spotify)
        {
            _logger = logger;
            _options = configuration.GetSection("Settings").Get<Settings>();
            _api = new Api(configuration, false);
            _spotify = spotify;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                List<Follow> followers = await _api.GetFollowers();
                long follower_count = followers.Count;
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

                int viewer_count = await _api.GetViewerCount();
                File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "viewer_count.txt"), viewer_count.ToString());

                using (TwitchDbContext db = new(Guid.Empty))
                {
                    Viewer bestViewer = db.Viewers.Where(x => x.IsBot == false).OrderByDescending(x => x.Uptime).FirstOrDefault();
                    if (bestViewer != null)
                    {
                        File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "best_viewer.txt"), viewer_count.ToString());
                    }
                }

                File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "last_follower.txt"), followers[0].FromUserName);

                /*FullTrack song = await _spotify.GetCurrentSong();
                if (song != null)
                {
                    File.WriteAllText(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "current_song.txt"), $"{song.Artists[0].Name} - {song.Name}");
                }
                else
                {
                    File.Delete(Path.Combine(_options.UpdateFilesFunction.OutputFolder, "current_song.txt"));
                }*/

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
                case "current_song":
                    value = "Musique";
                    break;
            }
            return value;
        }
    }
}
