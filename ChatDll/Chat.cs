using ApiDll;
using DbDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace ChatDll
{
    public class Chat : IDisposable
    {
        private Settings _settings;
        private readonly ILogger<Chat> _logger;

        private TwitchClient _client;
        private Api _api;
        private Random Rng = new Random(Guid.NewGuid().GetHashCode());

        public Chat(ILogger<Chat> logger, IConfiguration configuration)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = new(configuration, false);

            ConnectionCredentials credentials = new ConnectionCredentials(_settings.Channel, _settings.AccessToken);
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

            _client.Connect();
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
            _client.SendMessage(e.Channel, "Coucou, tu veux voir ma build?");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            _logger.LogInformation($"Couldn't connect: {e.Error.Message}");
        }

        private async void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (string.Equals(e.Command.CommandText, "bot", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "command", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "commande", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "commands", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(e.Command.CommandText, "commandes", StringComparison.InvariantCultureIgnoreCase))
            {
                _client.SendMessage(_settings.Channel, "Commandes disponibles:");
                if (_settings.CheckUptimeFunction.ComputeUptime)
                {
                    _client.SendMessage(_settings.Channel, "!uptime : Affiche le temps de stream visionné");
                }
                if (_settings.ChatFunction.Timeout)
                {
                    _client.SendMessage(_settings.Channel, "!timeout : Permet de TO quelqu'un, mais faut du courage");
                }
                return;
            }

            if (_settings.CheckUptimeFunction.ComputeUptime && string.Equals(e.Command.CommandText, "uptime", StringComparison.InvariantCultureIgnoreCase))
            {
                using (TwitchDbContext db = new())
                {
                    string username = e.Command.ArgumentsAsList.Count > 0 ? e.Command.ArgumentsAsList[0] : e.Command.ChatMessage.Username;
                    Viewer viewer = db.Viewers.Where(obj => obj.Username == username).FirstOrDefault();
                    if (viewer != null)
                    {
                        int hours = (int)Math.Floor((decimal)viewer.Uptime / 3600);
                        int minutes = (int)Math.Floor((decimal)(viewer.Uptime % 3600) / 60);
                        _client.SendMessage(_settings.Channel, $"@{username} a regardé le stream pendant {hours} heures et {minutes.ToString().PadLeft(2, '0')} minutes. Il est passé {viewer.Seen} fois sur le stream.");
                    }
                    else
                    {
                        _client.SendMessage(_settings.Channel, $"{e.Command.ChatMessage.Username}, je connais pas ce con");
                    }
                }
                return;
            }

            if (_settings.ChatFunction.Timeout && (string.Equals(e.Command.CommandText, "timeout", StringComparison.InvariantCultureIgnoreCase)
                                                    || string.Equals(e.Command.CommandText, "to", StringComparison.InvariantCultureIgnoreCase)))
            {
                if (e.Command.ArgumentsAsList.Count > 0)
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
                                _client.SendMessage(_settings.Channel, $"Roll: {dice}/300. Dommage {e.Command.ChatMessage.Username}!");
                                _api.BanUser(e.Command.ChatMessage.Username, timer);
                            }
                            else if (dice == 1)
                            {
                                _client.SendMessage(_settings.Channel, $"Roll: {dice}/300. Désolé {e.Command.ArgumentsAsList[0]}!");
                                _api.BanUser(username, timer);
                            }
                            else if (dice == 2)
                            {
                                _client.SendMessage(_settings.Channel, $"Roll: {dice}/300. Allez ça dégage {e.Command.ChatMessage.Username} et {e.Command.ArgumentsAsString}!");
                                _api.BanUser(e.Command.ChatMessage.Username, timer);
                                _api.BanUser(username, timer);
                            }
                            else
                            {
                                _client.SendMessage(_settings.Channel, $"Non, pas envie aujourd'hui");
                            }
                        }
                    }
                }
                return;
            }

            if (string.Equals(e.Command.CommandText, "settitle", StringComparison.InvariantCultureIgnoreCase))
            {
                if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                {
                    string title = e.Command.ArgumentsAsString;
                     if (_settings.ChatFunction.AddBotSuffixInTitle && !title.Contains("!bot", StringComparison.InvariantCultureIgnoreCase))
                    {
                        title += " !bot";
                    }
                    await _api.ModifyChannelInformation(title, null);
                    _client.SendMessage(_settings.Channel, $"{e.Command.ChatMessage.Username}, le titre du stream a été changé en: {title}");
                }
                return;
            }

            if (string.Equals(e.Command.CommandText, "setgame", StringComparison.InvariantCultureIgnoreCase))
            {
                if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                {
                    ModifyChannelInformationResponse response = await _api.ModifyChannelInformation(null, e.Command.ArgumentsAsString);
                    if (response != null)
                    {
                        _client.SendMessage(_settings.Channel, $"{e.Command.ChatMessage.Username}, le jeu du stream a été changé en: {response.Game}");
                    }
                    else
                    {
                        _client.SendMessage(_settings.Channel, $"{e.Command.ChatMessage.Username}, je connais pas ton jeu de merde");
                    }
                }
                return;
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
                _client.SendMessage(_settings.Channel, $"Allez Bisous, mon peuple m'attend!");
            }
        }
    }
}