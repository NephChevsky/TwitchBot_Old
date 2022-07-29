using ApiDll;
using DbDll;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;
using SpotifyAPI.Web;
using SpotifyDll;
using System.Media;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using WindowsInput;

namespace ChatDll
{
    public class ChatBot
    {
        private Settings _settings;
        private readonly ILogger<ChatBot> _logger;

        public BasicChat _chat;
        private Api _api;
        private Spotify _spotify;
        private Random Rng = new Random(Guid.NewGuid().GetHashCode());
        private Guid InvalidGuid = Guid.Parse("12345678-1234-1234-1234-123456789000");
        private Dictionary<string, DateTime> AntiSpamTimer = new Dictionary<string, DateTime>();

        public ChatBot(ILogger<ChatBot> logger, IConfiguration configuration, BasicChat chat, Spotify spotify)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = new(configuration, false);
            _spotify = spotify;
            _chat = chat;

            _chat._client.OnChatCommandReceived += Client_OnChatCommandReceived;
            _chat._client.OnMessageReceived += Client_OnMessageReceived;
        }

        private async void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (!AntiSpamTimer.ContainsKey(e.Command.CommandText.ToLower()) || (AntiSpamTimer.ContainsKey(e.Command.CommandText.ToLower()) && AntiSpamTimer[e.Command.CommandText.ToLower()].AddSeconds(60) < DateTime.Now))
            {
                bool updateTimer = false;
                if (string.Equals(e.Command.CommandText, "bot", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "cmd", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "command", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "commands", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "commande", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "commandes", StringComparison.InvariantCultureIgnoreCase))
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
                else if (_settings.ChatFunction.Timeout && (string.Equals(e.Command.CommandText, "timeout", StringComparison.InvariantCultureIgnoreCase)
                                                        || string.Equals(e.Command.CommandText, "to", StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (e.Command.ArgumentsAsList.Count > 0 && !string.Equals(e.Command.ChatMessage.Username, e.Command.ArgumentsAsList[0], StringComparison.InvariantCultureIgnoreCase))
                    {
                        string username = e.Command.ArgumentsAsList[0].Replace("@", "");
                        List<Moderator> mods = await _api.GetModerators();
                        Moderator mod = mods.Where(x => string.Equals(username, x.UserName)).FirstOrDefault();
                        if (mod == null)
                        {
                            if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                            {
                                if (e.Command.ArgumentsAsList.Count > 1)
                                {
                                    _api.BanUser(username, int.Parse(e.Command.ArgumentsAsList[1]));
                                }
                                else
                                {
                                    _api.BanUser(username);
                                }
                            }
                            else
                            {
                                int dice = Rng.Next(5);
                                int timer = Rng.Next(300);
                                if (dice == 0)
                                {
                                    _chat.SendMessage($"Roll: {dice}/300. Dommage {e.Command.ChatMessage.DisplayName}!");
                                    _api.BanUser(e.Command.ChatMessage.Username, timer);
                                }
                                else if (dice == 1)
                                {
                                    _chat.SendMessage($"Roll: {dice}/300. Désolé {e.Command.ArgumentsAsList[0]}!");
                                    _api.BanUser(username, timer);
                                }
                                else if (dice == 2)
                                {
                                    _chat.SendMessage($"Roll: {dice}/300. Allez ça dégage {e.Command.ChatMessage.DisplayName} et {e.Command.ArgumentsAsList[0]}!");
                                    _api.BanUser(e.Command.ChatMessage.Username, timer);
                                    _api.BanUser(username, timer);
                                }
                                else
                                {
                                    _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Non, pas envie aujourd'hui");
                                }
                                updateTimer = true;
                            }
                        }
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Idiot!");
                        _api.BanUser(e.Command.ChatMessage.Username);
                    }
                }
                else if (string.Equals(e.Command.CommandText, "settitle", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                    {
                        string title = e.Command.ArgumentsAsString;
                        if (_settings.ChatFunction.AddBotSuffixInTitle && !title.Contains("!bot", StringComparison.InvariantCultureIgnoreCase))
                        {
                            title += " !bot";
                        }
                        await _api.ModifyChannelInformation(title, null);
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : le titre du stream a été changé en: {title}");
                    }
                }
                else if (string.Equals(e.Command.CommandText, "setgame", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
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
                }
                else if (_settings.ChatFunction.AddCustomCommands && string.Equals(e.Command.CommandText, "addcmd", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (e.Command.ArgumentsAsList.Count >= 2)
                    {
                        Command cmd = new();
                        cmd.Name = e.Command.ArgumentsAsList[0].Replace("!", "");
                        cmd.Message = e.Command.ArgumentsAsString.Substring(e.Command.ArgumentsAsList[0].Length + 1);
                        Guid guid = GetUserGuid(e.Command.ChatMessage.Username);
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
                                    _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Une commande du même nom existe déja");
                                    return;
                                }
                                _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Command {e.Command.ArgumentsAsList[0]} créée");
                                updateTimer = !(e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator);
                            }
                        }
                        else
                        {
                            _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Utilisateur inconnu");
                        }
                    }
                    return;
                }
                else if (_settings.ChatFunction.AddCustomCommands && string.Equals(e.Command.CommandText, "delcmd", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (e.Command.ArgumentsAsList.Count == 1)
                    {
                        using (TwitchDbContext db = new(Guid.Empty))
                        {
                            Guid guid = Guid.Empty;
                            if (!e.Command.ChatMessage.IsBroadcaster && !e.Command.ChatMessage.IsModerator)
                            {
                                Viewer dbViewer = db.Viewers.Where(x => string.Equals(x.Username, e.Command.ChatMessage.Username, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                if (dbViewer != null)
                                {
                                    guid = dbViewer.Id;
                                }
                                else
                                {
                                    _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Utilisateur inconnu");
                                    return;
                                }
                            }
                            Command dbCmd = db.Commands.Where(x => x.Name == e.Command.ArgumentsAsList[0]).FirstOrDefault();
                            if (dbCmd != null)
                            {
                                if (guid == Guid.Empty || guid == dbCmd.Owner)
                                {
                                    db.Remove(dbCmd);
                                    db.SaveChanges();
                                    _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Command {e.Command.ArgumentsAsList[0]} supprimée");
                                    updateTimer = !(e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator);
                                }
                                else
                                {
                                    _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Pas touche!");
                                }
                            }
                            else
                            {
                                _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Commande inconnue");
                            }
                        }
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
                else if (string.Equals(e.Command.CommandText, "nextsong", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!(await _spotify.SkipSong()))
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : On écoute pas de musique bouffon");
                    }
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "addsong", StringComparison.InvariantCultureIgnoreCase))
                {
                    int ret = await _spotify.AddSong(e.Command.ArgumentsAsString);
                    if (ret == 1)
                    {
                        updateTimer = true;
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : La musique a été ajoutée à la playlist");
                    }
                    else if (ret == 2)
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : La musique est déjà dans la playlist");
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : La musique n'a pas pu être ajoutée à la playlist");
                    }
                }
                else if (string.Equals(e.Command.CommandText, "delsong", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (await _spotify.RemoveSong())
                    {
                        updateTimer = !(e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator);
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : La musique a été supprimée de la playlist");
                    }
                    else
                    {
                        _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : La musique n'a pas pu être supprimée de la playlist");
                    }
                }
                else if (string.Equals(e.Command.CommandText, "barrelroll", StringComparison.InvariantCultureIgnoreCase))
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
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "rocketleague", StringComparison.InvariantCultureIgnoreCase))
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
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "whatasave", StringComparison.InvariantCultureIgnoreCase))
                {
                    var simulator = new InputSimulator();
                    simulator.Keyboard.KeyPress(VirtualKeyCode.VK_2);
                    simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4);
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "wow", StringComparison.InvariantCultureIgnoreCase))
                {
                    var simulator = new InputSimulator();
                    simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
                    simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "faking", StringComparison.InvariantCultureIgnoreCase))
                {
                    var simulator = new InputSimulator();
                    simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4);
                    simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "fire", StringComparison.InvariantCultureIgnoreCase))
                {
                    var simulator = new InputSimulator();
                    simulator.Mouse.LeftButtonDown();
                    Task.Delay(1000).Wait();
                    simulator.Mouse.LeftButtonUp();
                    updateTimer = true;
                }
                else if (_settings.ChatFunction.AddCustomCommands)
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
                else
                {
                    _chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Commande inconnue");
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
                    db.SaveChanges();
                }
			}
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