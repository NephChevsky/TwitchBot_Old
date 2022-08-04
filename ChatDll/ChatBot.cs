using ApiDll;
using DbDll;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;
using SpotifyAPI.Web;
using SpotifyDll;
using System.Media;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Client.Events;
using WindowsInput;

namespace ChatDll
{
    public class ChatBot : IHostedService
    {
        private Settings _settings;
        private readonly ILogger<ChatBot> _logger;

        public BasicChat _chat;
        private Api _api;
        private Spotify _spotify;
        private Random Rng = new Random(Guid.NewGuid().GetHashCode());
        private Guid InvalidGuid = Guid.Parse("12345678-1234-1234-1234-123456789000");
        private Dictionary<string, DateTime> AntiSpamTimer = new Dictionary<string, DateTime>();
        private DateTime SessionBeginning;
        private HubConnection _connection;

        public ChatBot(ILogger<ChatBot> logger, IConfiguration configuration, BasicChat chat, Spotify spotify)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = new(configuration, false);
            _spotify = spotify;
            _chat = chat;
            _connection = new HubConnectionBuilder().WithUrl("https://bot-neph.azurewebsites.net/hub").WithAutomaticReconnect().Build();
            _connection.On<Dictionary<string, string>>("TriggerReward", (reward) => OnTriggerReward(null, reward));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _chat._client.OnChatCommandReceived += Client_OnChatCommandReceived;
            _chat._client.OnMessageReceived += Client_OnMessageReceived;
            Task hub = _connection.StartAsync();
            hub.Wait();

            SessionBeginning = DateTime.Now;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (!AntiSpamTimer.ContainsKey(e.Command.CommandText.ToLower()) || (AntiSpamTimer.ContainsKey(e.Command.CommandText.ToLower()) && AntiSpamTimer[e.Command.CommandText.ToLower()].AddSeconds(60) < DateTime.Now))
            {
                bool updateTimer = false;
                if (string.Equals(e.Command.CommandText, "bot", StringComparison.InvariantCultureIgnoreCase))
                {
                    _chat.SendMessage("Commandes disponibles: https://bit.ly/3J4wUdP");
                    updateTimer = true;
                }
                else if (_settings.CheckUptimeFunction.ComputeUptime && string.Equals(e.Command.CommandText, "uptime", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (TwitchDbContext db = new(Guid.Empty))
                    {
                        string username = e.Command.ArgumentsAsList.Count > 0 ? e.Command.ArgumentsAsList[0] : e.Command.ChatMessage.Username;
                        Viewer viewer = db.Viewers.Where(obj => obj.Username == username).FirstOrDefault();
                        if (viewer != null)
                        {
                            int hours = (int)Math.Floor((decimal)viewer.Uptime / 3600);
                            int minutes = (int)Math.Floor((decimal)(viewer.Uptime % 3600) / 60);
                            _chat.SendMessage($"@{viewer.DisplayName} a regardé le stream pendant {hours} heures et {minutes.ToString().PadLeft(2, '0')} minutes. Il est passé {viewer.Seen} fois sur le stream.");
                            updateTimer = true;
                        }
                        else
                        {
                            _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : je connais pas ce con");
                        }
                    }
                }
                else if ((string.Equals(e.Command.CommandText, "timeout", StringComparison.InvariantCultureIgnoreCase) || string.Equals(e.Command.CommandText, "to", StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                    {
                        if (e.Command.ArgumentsAsList.Count > 0)
                        {
                            string username = e.Command.ArgumentsAsList[0].Replace("@", "");
                            if (e.Command.ArgumentsAsList.Count > 1)
                            {
                                _api.BanUser(username, int.Parse(e.Command.ArgumentsAsList[1]));
                            }
                            else
                            {
                                _api.BanUser(username);
                            }
                        }
                        else if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                        {
                            _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : T'es bourré?");
                        }
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Idiot!");
                        _api.BanUser(e.Command.ChatMessage.Username);
                    }
                }
                else if (string.Equals(e.Command.CommandText, "settitle", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
                {
                    string title = e.Command.ArgumentsAsString;
                    if (_settings.ChatFunction.AddBotSuffixInTitle && !title.Contains("!bot", StringComparison.InvariantCultureIgnoreCase))
                    {
                        title += " !bot";
                    }
                    await _api.ModifyChannelInformation(title, null);
                    _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : le titre du stream a été changé en: {title}");
                }
                else if (string.Equals(e.Command.CommandText, "setgame", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
                {
                    ModifyChannelInformationResponse response = await _api.ModifyChannelInformation(null, e.Command.ArgumentsAsString);
                    if (response != null)
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : le jeu du stream a été changé en: {response.Game}");
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : je connais pas ton jeu de merde");
                    }
                }
                else if (_settings.ChatFunction.AddCustomCommands && string.Equals(e.Command.CommandText, "addcmd", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
                {
                    if (e.Command.ArgumentsAsList.Count >= 2)
                    {
                        AddCommand(e.Command.ArgumentsAsList[0].Replace("!", ""), e.Command.ArgumentsAsString.Substring(e.Command.ArgumentsAsList[0].Length + 1), true, e.Command.ChatMessage.DisplayName);
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : T'es bourré?");
                    }
                    return;
                }
                else if (_settings.ChatFunction.AddCustomCommands && string.Equals(e.Command.CommandText, "delcmd", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
                {
                    if (e.Command.ArgumentsAsList.Count == 1)
                    {
                        DeleteCommand(e.Command.ArgumentsAsList[0].Replace("!", ""), true, e.Command.ChatMessage.DisplayName);
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : T'es bourré?");
                    }
                }
                else if (string.Equals(e.Command.CommandText, "song", StringComparison.InvariantCultureIgnoreCase))
                {
                    FullTrack song = await _spotify.GetCurrentSong();
                    if (song != null)
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : SingsNote {song.Artists[0].Name} - {song.Name} SingsNote");
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : On écoute pas de musique bouffon");
                    }
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "nextsong", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
                {
                    await SkipSong(e.Command.ChatMessage.DisplayName);
                }
                else if (string.Equals(e.Command.CommandText, "addsong", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
                {
                    await AddSong(e.Command.ArgumentsAsString, e.Command.ChatMessage.DisplayName);
                }
                else if (string.Equals(e.Command.CommandText, "delsong", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
                {
                    await RemoveSong(e.Command.ChatMessage.DisplayName);
                }
                else if (_settings.ChatFunction.AddCustomCommands && string.IsNullOrEmpty(e.Command.ChatMessage.CustomRewardId))
                {
                    using (TwitchDbContext db = new(Guid.Empty))
                    {
                        Command dbCmd = db.Commands.Where(x => x.Name == e.Command.CommandText).FirstOrDefault();
                        if (dbCmd != null)
                        {
                            dbCmd.Value++;
                            string message = dbCmd.Message.Replace("{0}", dbCmd.Value.ToString());
                            _chat.SendMessage($"{message}");
                            db.SaveChanges();
                            updateTimer = true;
                        }
                        else
                        {
                            _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Commande inconnue");
                        }
                    }
                }

                if (updateTimer)
                {
                    if (AntiSpamTimer.ContainsKey(e.Command.CommandText.ToLower()))
                    {
                        AntiSpamTimer[e.Command.CommandText.ToLower()] = DateTime.Now;
					}
                    else
                    {
                        AntiSpamTimer.Add(e.Command.CommandText.ToLower(), DateTime.Now);
                    }
				}
            }
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            using (TwitchDbContext db = new(Guid.Empty))
            {
                Viewer dbViewer = db.Viewers.Where(x => x.Username == e.ChatMessage.Username).FirstOrDefault();
                if (dbViewer != null)
                {
                    dbViewer.MessageCount++;
                    using (TwitchDbContext db1 = new(dbViewer.Id))
                    {
                        ModelsDll.Db.ChatMessage message = new(dbViewer.Id, e.ChatMessage.Message);
                        db1.Messages.Add(message);
                        db1.SaveChanges();
                    }
                    db.SaveChanges();
                }
			}
		}

        private async Task OnTriggerReward(object sender, Dictionary<string, string> e)
        {
            if (string.Equals(e["type"], "Do a barrel roll!", StringComparison.InvariantCultureIgnoreCase))
            {
                SoundPlayer player = new SoundPlayer(@"D:\Dev\Twitch\Bot\Assets\barrelroll.wav");
                player.Play();
                _chat.SendMessage("DO A BARREL ROLL!");
                var simulator = new InputSimulator();
                simulator.Mouse.LeftButtonDown();
                simulator.Mouse.RightButtonDown();
                simulator.Keyboard.KeyDown(VirtualKeyCode.VK_A);
                Task.Delay(1100).Wait();
                simulator.Keyboard.KeyUp(VirtualKeyCode.VK_A);
                simulator.Mouse.RightButtonUp();
                simulator.Mouse.LeftButtonUp();
            }
            else if (string.Equals(e["type"], "This is Rocket League!", StringComparison.InvariantCultureIgnoreCase))
            {
                SoundPlayer player = new SoundPlayer(@"D:\Dev\Twitch\Bot\Assets\rocketleague.wav");
                player.Play();
                _chat.SendMessage("THIS IS ROCKET LEAGUE!");
                var simulator = new InputSimulator();
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4);
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_1);
                simulator.Mouse.LeftButtonDown();
                simulator.Mouse.RightButtonDown();
                simulator.Keyboard.KeyDown(VirtualKeyCode.VK_S);
                Task.Delay(250).Wait();
                simulator.Keyboard.KeyUp(VirtualKeyCode.VK_S);
                simulator.Mouse.RightButtonUp();
                simulator.Mouse.RightButtonDown();
                Task.Delay(1300).Wait();
                simulator.Mouse.RightButtonUp();
                simulator.Mouse.LeftButtonUp();
            }
            else if (string.Equals(e["type"], "What a save!", StringComparison.InvariantCultureIgnoreCase))
            {
                var simulator = new InputSimulator();
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_2);
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4);
            }
            else if (string.Equals(e["type"], "Wow!", StringComparison.InvariantCultureIgnoreCase))
            {
                var simulator = new InputSimulator();
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
            }
            else if (string.Equals(e["type"], "Faking.", StringComparison.InvariantCultureIgnoreCase))
            {
                var simulator = new InputSimulator();
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4);
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
            }
            else if (string.Equals(e["type"], "Vider mon chargeur", StringComparison.InvariantCultureIgnoreCase))
            {
                var simulator = new InputSimulator();
                simulator.Mouse.LeftButtonDown();
                Task.Delay(1000).Wait();
                simulator.Mouse.LeftButtonUp();
            }
            else if (string.Equals(e["type"], "Passer à la musique suivante", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!(await _spotify.SkipSong()))
                {
                    _chat.SendMessage($"{e["username"]} : On écoute pas de musique bouffon");
                }
            }
            else if (string.Equals(e["type"], "Ajouter une musique", StringComparison.InvariantCultureIgnoreCase))
            {
                await AddSong(e["user-input"], e["username"]);
            }
            else if (string.Equals(e["type"], "Supprimer une musique", StringComparison.InvariantCultureIgnoreCase))
            {
                await RemoveSong(e["username"]);
            }
            else if (string.Equals(e["type"], "Ajouter une commande", StringComparison.InvariantCultureIgnoreCase))
            {
                int offset = e["user-input"].IndexOf(" ");
                if (offset > -1)
                {
                    string commandName = e["user-input"].Substring(0, offset).Replace("!", "");
                    string commandMessage = e["user-input"].Substring(offset + 1);
                    AddCommand(commandName, commandMessage, false, e["username"]);
                }
            }
            else if (string.Equals(e["type"], "Supprimer une commande", StringComparison.InvariantCultureIgnoreCase))
            {
                DeleteCommand(e["user-input"].Replace("!", ""), false, e["username"]);
            }
            else if (string.Equals(e["type"], "Timeout un viewer", StringComparison.InvariantCultureIgnoreCase))
            {
                e["user-input"] = e["user-input"].Replace("@", "");
                List<Moderator> mods = await _api.GetModerators();
                Moderator mod = mods.Where(x => string.Equals(e["user-input"], x.UserName)).FirstOrDefault();
                if (mod == null)
                {
                    Viewer firstViewer, secondViewer;
                    using (TwitchDbContext db = new(Guid.Empty))
                    {
                        firstViewer = db.Viewers.Where(x => x.Username == e["username"]).FirstOrDefault();
                        secondViewer = db.Viewers.Where(x => x.Username == e["user-input"]).FirstOrDefault();
                    }
                    if (firstViewer != null && secondViewer != null)
                    {
                        int dice = Rng.Next(5);
                        int timer = Rng.Next(300);
                        if (dice == 0)
                        {
                            _chat.SendMessage($"Roll: {timer}/300. Dommage {firstViewer.DisplayName}!");
                            _api.BanUser(firstViewer.Username, timer);
                        }
                        else if (dice == 1)
                        {
                            _chat.SendMessage($"Roll: {timer}/300. Désolé {secondViewer.DisplayName}!");
                            _api.BanUser(secondViewer.Username, timer);
                        }
                        else if (dice == 2)
                        {
                            _chat.SendMessage($"Roll: {timer}/300. Allez ça dégage {e["username"]} et {secondViewer.DisplayName}!");
                            _api.BanUser(secondViewer.Username, timer);
                            _api.BanUser(firstViewer.Username, timer);
                        }
                        else
                        {
                            _chat.SendMessage($"{firstViewer.DisplayName} : Non, pas envie aujourd'hui");
                        }
                    }
                    else
                    {
                        _chat.SendMessage($"{firstViewer.DisplayName} : Utilisateur inconnu");
                    }
                }
                else
                {
                    _chat.SendMessage($"{e["username"]} : T'as cru t'allais timeout un modo?");
                    _api.BanUser(e["username"]);
                }
            }
        }

        public void AddCommand(string commandName, string commandMessage, bool isMod, string displayName)
        {
            Command cmd = new();
            cmd.Name = commandName;
            cmd.Message = commandMessage;
            Guid guid = GetUserGuid(displayName.ToLower());
            if (guid != InvalidGuid)
            {
                using (TwitchDbContext db = new(guid))
                {
                    db.Commands.Add(cmd);
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbUpdateException ex)
                    when ((ex.InnerException as SqlException)?.Number == 2601 || (ex.InnerException as SqlException)?.Number == 2627)
                    {
                        _chat.SendMessage($"{displayName} : Une commande du même nom existe déja");
                        return;
                    }
                    _chat.SendMessage($"{displayName} : Command {commandName} créée");
                }
            }
            else
            {
                _chat.SendMessage($"{displayName} : Utilisateur inconnu");
            }
        }

        public void DeleteCommand(string commandName, bool isMod, string displayName)
        {
            Guid guid = GetUserGuid(displayName.ToLower());
            if (guid != InvalidGuid)
            {
                using (TwitchDbContext db = new(guid))
                {
                    Command dbCmd = db.Commands.Where(x => x.Name == commandName).FirstOrDefault();
                    if (dbCmd != null)
                    {
                        if (guid == Guid.Empty || guid == dbCmd.Owner)
                        {
                            db.Remove(dbCmd);
                            db.SaveChanges();
                            _chat.SendMessage($"{displayName} : Command {commandName} supprimée");
                        }
                        else
                        {
                            _chat.SendMessage($"{displayName} : Pas touche!");
                        }
                    }
                    else
                    {
                        _chat.SendMessage($"{displayName} : Commande inconnue");
                    }
                }
            }
            else
            {
                _chat.SendMessage($"{displayName} : Utilisateur inconnu");
            }
        }
        public async Task SkipSong(string displayName)
        {
            if (!(await _spotify.SkipSong()))
            {
                _chat.SendMessage($"{displayName} : On écoute pas de musique bouffon");
            }
        }

        public async Task<int> AddSong(string song, string displayName)
        {
            int ret = await _spotify.AddSong(song);
            if (ret == 1)
            {
                _chat.SendMessage($"{displayName} : La musique a été ajoutée à la playlist");
            }
            else if (ret == 2)
            {
                _chat.SendMessage($"{displayName} : La musique est déjà dans la playlist");
            }
            else
            {
                _chat.SendMessage($"{displayName} : La musique n'a pas pu être ajoutée à la playlist");
            }
            return ret;
        }

        public async Task<bool> RemoveSong(string displayName)
        {
            bool ret = await _spotify.RemoveSong();
            if (ret)
            {
                _chat.SendMessage($"{displayName} : La musique a été supprimée de la playlist");
            }
            else
            {
                _chat.SendMessage($"{displayName} : La musique n'a pas pu être supprimée de la playlist");
            }
            return ret;
        }

        public Guid GetUserGuid(string name)
        {
            using (TwitchDbContext db = new(Guid.Empty))
            {
                Viewer dbViewer = db.Viewers.Where(x => x.Username == name).FirstOrDefault();
                if (dbViewer != null)
                {
                    return dbViewer.Id;
                }
                else
                {
                    return InvalidGuid;
                }
			}
        }
	}
}