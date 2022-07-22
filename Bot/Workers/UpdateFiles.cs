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
        private readonly Settings _settings;
        private Api _api;
        private Spotify _spotify;

        private int CurrentButtonCursor = 0;

        public UpdateFiles(ILogger<UpdateFiles> logger, IConfiguration configuration, Spotify spotify)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
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
                    File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "follower_goal.txt"), $"{follower_count} / {_settings.UpdateFilesFunction.FollowerGoal}");
                    File.Delete(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "follower_count.txt"));
                }
                else
                {
                    File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "follower_count.txt"), $"{follower_count}");
                    File.Delete(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "follower_goal.txt"));
                }

                int viewer_count = await _api.GetViewerCount();
                File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "viewer_count.txt"), viewer_count.ToString());

                using (TwitchDbContext db = new(Guid.Empty))
                {
                    Viewer bestViewer = db.Viewers.Where(x => x.IsBot == false).OrderByDescending(x => x.Uptime).FirstOrDefault();
                    if (bestViewer != null)
                    {
                        File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "best_viewer.txt"), bestViewer.Username);
                    }
                }

                File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "last_follower.txt"), followers[0].FromUserName);

                FullTrack song = await _spotify.GetCurrentSong();
                if (song != null)
                {
                    File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "current_song_artist.txt"), $"{song.Artists[0].Name}");
                    File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "current_song_title.txt"), $"{song.Name}");
                }
                else
                {
                    File.Delete(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "current_song_artist.txt"));
                    File.Delete(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "current_song_title.txt"));
                }

                List<string> files = Directory.GetFiles(_settings.UpdateFilesFunction.OutputFolder).Where(name => !name.Contains("button")).ToList();
                string button1_title = Path.GetFileNameWithoutExtension(files[CurrentButtonCursor]);
                string button2_title = Path.GetFileNameWithoutExtension(files[(CurrentButtonCursor + 1) % files.Count]);
                string button1_value = File.ReadAllText(files[CurrentButtonCursor]);
                string button2_value = File.ReadAllText(files[(CurrentButtonCursor + 1) % files.Count]);
                File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "button1_title.txt"), GetButtonNiceName(button1_title));
                File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "button1_value.txt"), button1_value);
                if (button2_title == "current_song_artist")
                {
                    File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "button2_title.txt"), string.Empty);
                    File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "button2_value.txt"), string.Empty);
                    CurrentButtonCursor = (CurrentButtonCursor + 1) % files.Count;
                }
                else
                {
                    File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "button2_title.txt"), GetButtonNiceName(button2_title));
                    File.WriteAllText(Path.Combine(_settings.UpdateFilesFunction.OutputFolder, "button2_value.txt"), button2_value);
                    CurrentButtonCursor = (CurrentButtonCursor + 2) % files.Count;
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.UpdateFilesFunction.Timer), stoppingToken);
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
                    value = "Viewer le plus présent";
                    break;
                case "last_follower":
                    value = "Dernier follower";
                    break;
                case "current_song_artist":
                    value = "Artiste de la musique";
                    break;
                case "current_song_title":
                    value = "Titre de la musique";
                    break;
            }
            return value;
        }
    }
}
