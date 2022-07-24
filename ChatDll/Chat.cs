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
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using WindowsInput;

namespace ChatDll
{
    public class Chat : IDisposable
    {
        private Settings _settings;
        private readonly ILogger<Chat> _logger;

        private TwitchClient _client;
        private Api _api;
        private Spotify _spotify;
        private Random Rng = new Random(Guid.NewGuid().GetHashCode());
        private Guid InvalidGuid = Guid.Parse("12345678-1234-1234-1234-123456789000");
        private Dictionary<string, DateTime> AntiSpamTimer = new Dictionary<string, DateTime>();

        public Chat(ILogger<Chat> logger, IConfiguration configuration, Spotify spotify)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = new(configuration, false);
            _spotify = spotify;

            ConnectionCredentials credentials = new ConnectionCredentials(_settings.Channel, _api.AccessToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, _settings.Channel);

            _client.OnLog += Client_OnLog;
            _client.OnJoinedChannel += Client_OnJoinedChannel;
            _client.OnConnected += Client_OnConnected;
            _client.OnChatCommandReceived += Client_OnChatCommandReceived;
            _client.OnConnectionError += Client_OnConnectionError;

            if (!_client.Connect())
            {
                _logger.LogError("Couldn't connect IRC client");
			}
        }

        public bool IsConnected
        {
            get
            {
                return _client.IsConnected;
            }
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            _logger.LogInformation($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            _logger.LogInformation($"Connected to {_settings.Channel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            _logger.LogInformation($"Joined channel {_settings.Channel}");
            SendMessage("Coucou, tu veux voir ma build?");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            _logger.LogInformation($"Couldn't connect: {e.Error.Message}");
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
                    SendMessage("Commandes disponibles:");
                    if (_settings.CheckUptimeFunction.ComputeUptime)
                    {
                        SendMessage("!uptime : Affiche l'uptime d'un viewer");
                    }
                    if (_settings.ChatFunction.Timeout)
                    {
                        SendMessage("!timeout : TO quelqu'un, mais faut du courage");
                    }
                    if (_settings.ChatFunction.AddCustomCommands)
                    {
                        SendMessage("!addcmd : Ajoute une commande");
                        SendMessage("!delcmd : Supprime une commande");
                    }
                    SendMessage("!song : Affiche la musique en cours");
                    SendMessage("!nextsong : Passe à la musique suivante");
                    SendMessage("!barrelroll : Do a barrel roll!");
                    SendMessage("!rocketleague : This is rocket league!");
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
                            SendMessage($"@{username} a regardé le stream pendant {hours} heures et {minutes.ToString().PadLeft(2, '0')} minutes. Il est passé {viewer.Seen} fois sur le stream.");
                            updateTimer = true;
                        }
                        else
                        {
                            SendMessage($"{e.Command.ChatMessage.Username}, je connais pas ce con");
                        }
                    }
                }
                else if (_settings.ChatFunction.Timeout && (string.Equals(e.Command.CommandText, "timeout", StringComparison.InvariantCultureIgnoreCase)
                                                        || string.Equals(e.Command.CommandText, "to", StringComparison.InvariantCultureIgnoreCase)))
                {
                    if (e.Command.ArgumentsAsList.Count > 0)
                    {
                        string username = e.Command.ArgumentsAsList[0].Replace("@", "");
                        List<Moderator> mods = await _api.GetModerators();
                        Moderator mod = mods.Where(x => string.Equals(username, x.UserName)).FirstOrDefault();
                        if (mod == null && !string.Equals(e.Command.ChatMessage.Username, _settings.Channel, StringComparison.InvariantCultureIgnoreCase))
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
                                if (e.Command.ArgumentsAsList.Count == 0)
                                {
                                    SendMessage($"{e.Command.ChatMessage.Username} Idiot!");
                                    _api.BanUser(username);
                                }
                                else
                                {
                                    int dice = Rng.Next(5);
                                    int timer = Rng.Next(300);
                                    if (dice == 0)
                                    {
                                        SendMessage($"Roll: {dice}/300. Dommage {e.Command.ChatMessage.Username}!");
                                        _api.BanUser(e.Command.ChatMessage.Username, timer);
                                    }
                                    else if (dice == 1)
                                    {
                                        SendMessage($"Roll: {dice}/300. Désolé {e.Command.ArgumentsAsList[0]}!");
                                        _api.BanUser(username, timer);
                                    }
                                    else if (dice == 2)
                                    {
                                        SendMessage($"Roll: {dice}/300. Allez ça dégage {e.Command.ChatMessage.Username} et {e.Command.ArgumentsAsString}!");
                                        _api.BanUser(e.Command.ChatMessage.Username, timer);
                                        _api.BanUser(username, timer);
                                    }
                                    else
                                    {
                                        SendMessage($"{e.Command.ChatMessage.Username} Non, pas envie aujourd'hui");
                                    }
                                }
                                updateTimer = true;
                            }
                        }
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
                        SendMessage($"{e.Command.ChatMessage.Username}, le titre du stream a été changé en: {title}");
                    }
                }
                else if (string.Equals(e.Command.CommandText, "setgame", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                    {
                        ModifyChannelInformationResponse response = await _api.ModifyChannelInformation(null, e.Command.ArgumentsAsString);
                        if (response != null)
                        {
                            SendMessage($"{e.Command.ChatMessage.Username}, le jeu du stream a été changé en: {response.Game}");
                        }
                        else
                        {
                            SendMessage($"{e.Command.ChatMessage.Username}, je connais pas ton jeu de merde");
                        }
                    }
                }
                else if (_settings.ChatFunction.AddCustomCommands && string.Equals(e.Command.CommandText, "addcmd", StringComparison.InvariantCultureIgnoreCase))
                {
                    if ((e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator) && e.Command.ArgumentsAsList.Count >= 2)
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
                                    SendMessage($"{e.Command.ChatMessage.Username} Une commande du même nom existe déja");
                                    return;
                                }
                                SendMessage($"{e.Command.ChatMessage.Username} Command {e.Command.ArgumentsAsList[0]} créée");
                                updateTimer = true;
                            }
                        }
                        else
                        {
                            SendMessage($"{e.Command.ChatMessage.Username} Utilisateur inconnu");
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
                                    SendMessage($"{e.Command.ChatMessage.Username} Utilisateur inconnu");
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
                                    SendMessage($"{e.Command.ChatMessage.Username} Command {e.Command.ArgumentsAsList[0]} supprimée");
                                    updateTimer = true;
                                }
                                else
                                {
                                    SendMessage($"{e.Command.ChatMessage.Username} Pas touche!");
                                }
                            }
                            else
                            {
                                SendMessage($"{e.Command.ChatMessage.Username} Commande inconnue");
                            }
                        }
                    }
                }
                else if (string.Equals(e.Command.CommandText, "song", StringComparison.InvariantCultureIgnoreCase))
                {
                    FullTrack song = await _spotify.GetCurrentSong();
                    if (song != null)
                    {
                        SendMessage($"{e.Command.ChatMessage.Username} SingsNote {song.Artists[0].Name} - {song.Name} SingsNote");
                    }
                    else
                    {
                        SendMessage($"{e.Command.ChatMessage.Username} On écoute pas de musique bouffon");
                    }
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "nextsong", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!(await _spotify.SkipSong()))
                    {
                        SendMessage($"{e.Command.ChatMessage.Username} On écoute pas de musique bouffon");
                    }
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "barrelroll", StringComparison.InvariantCultureIgnoreCase))
                {
                    SoundPlayer player = new SoundPlayer(@"D:\Dev\Twitch\Bot\Assets\barrelroll.wav");
                    player.Play();
                    var simulator = new InputSimulator();
                    simulator.Mouse.LeftButtonDown();
                    simulator.Mouse.RightButtonDown();
                    simulator.Keyboard.KeyDown(VirtualKeyCode.VK_A);
                    Task.Delay(1100).Wait();
                    simulator.Keyboard.KeyUp(VirtualKeyCode.VK_A);
                    simulator.Mouse.RightButtonUp();
                    simulator.Mouse.LeftButtonUp();
                    SendMessage("DO A BARREL ROLL!");
                    updateTimer = true;
                }
                else if (string.Equals(e.Command.CommandText, "rocketleague", StringComparison.InvariantCultureIgnoreCase))
                {
                    SoundPlayer player = new SoundPlayer(@"D:\Dev\Twitch\Bot\Assets\rocketleague.wav");
                    player.Play();
                    var simulator = new InputSimulator();
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
                    SendMessage("THIS IS ROCKET LEAGUE!");
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
                            SendMessage($"{message}");
                            db.SaveChanges();
                            updateTimer = true;
                        }
                        else
                        {
                            SendMessage($"{e.Command.ChatMessage.Username} Commande inconnue");
                        }
                    }
                }
                else
                {
                    SendMessage($"{e.Command.ChatMessage.Username} Commande inconnue");
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

        public void SendMessage(string message)
        {
            _client.SendMessage(_settings.Channel, message);
        }

        public void Dispose()
        {
            _logger.LogInformation($"Disposing of ChatDll");
            if (_client.IsConnected)
            {
                SendMessage($"Allez Bisous, mon peuple m'attend!");
            }
        }
    }
}