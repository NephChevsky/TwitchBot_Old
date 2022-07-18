using Db;
using Db.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Helix.Models.ChannelPoints.GetCustomReward;
using TwitchLib.Api.Helix.Models.Channels;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Moderation.BanUser;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Bot.Services
{
    public class BotService : IDisposable
    {
        private TwitchClient client;
        private TwitchAPI api;
        private Random Rng = new Random();
        private Settings _options;
        private readonly ILogger<BotService> _logger;

        public List<string> CurrentViewerList { get; set; } = new List<string>();
        public bool IsConnected { get; set; }

        public BotService(ILogger<BotService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _options = configuration.GetSection("Settings").Get<Settings>();

            api = new TwitchAPI();
            api.Settings.ClientId = _options.ClientId;
            api.Settings.Secret = _options.Secret;
            api.Settings.AccessToken = _options.AccessToken;

            var task = Task.Run(async () =>
            {
                return await api.Auth.RefreshAuthTokenAsync(_options.RefreshToken, _options.Secret);
            });
            task.Wait();
            RefreshResponse token = task.Result;
            UpdateTokens(token.AccessToken, token.RefreshToken);
            api.Settings.AccessToken = _options.AccessToken;

            ConnectionCredentials credentials = new ConnectionCredentials(_options.Channel, _options.AccessToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, _options.Channel);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnConnected += Client_OnConnected;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnConnectionError += Client_OnConnectionError;

            client.Connect();
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            _logger.LogInformation($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            _logger.LogInformation($"Connected to {e.AutoJoinChannel}");
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            _logger.LogInformation($"Joined channel {_options.Channel}");
            client.SendMessage(e.Channel, "Coucou, tu veux voir ma build?");
            IsConnected = true;
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
                client.SendMessage(_options.Channel, "Commandes disponibles:");
                if (_options.CheckUptimeFunction.ComputeUptime)
                {
                    client.SendMessage(_options.Channel, "!uptime : Affiche le temps de stream visionné");
                }
                if (_options.BotFunction.Timeout)
                {
                    client.SendMessage(_options.Channel, "!timeout : Permet de TO quelqu'un, mais faut du courage");
                }
                return;
            }

            if (_options.CheckUptimeFunction.ComputeUptime && string.Equals(e.Command.CommandText, "uptime", StringComparison.InvariantCultureIgnoreCase))
            {
                using (TwitchDbContext db = new())
                {
                    string username = e.Command.ArgumentsAsList.Count > 0 ? e.Command.ArgumentsAsList[0] : e.Command.ChatMessage.Username;
                    Viewer viewer = db.Viewers.Where(obj => obj.Username == username).FirstOrDefault();
                    if (viewer != null)
                    {
                        int hours = (int)Math.Floor((decimal)viewer.Uptime / 3600);
                        int minutes = (int)Math.Floor((decimal)(viewer.Uptime % 3600) / 60);
                        client.SendMessage(_options.Channel, $"@{username} a regardé le stream pendant {hours} heures et {minutes.ToString().PadLeft(2, '0')} minutes. Il est passé {viewer.Seen} fois sur le stream.");
                    }
                    else
                    {
                        client.SendMessage(_options.Channel, $"{e.Command.ChatMessage.Username}, je connais pas ce con");
                    }
                }
                return;
            }

            if (_options.BotFunction.Timeout && string.Equals(e.Command.CommandText, "timeout", StringComparison.InvariantCultureIgnoreCase))
            {
                if (e.Command.ArgumentsAsList.Count > 0)
                {
                    if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                    {
                        if (e.Command.ArgumentsAsList.Count > 1)
                        {
                            BanUser(e.Command.ArgumentsAsList[0], int.Parse(e.Command.ArgumentsAsList[1]));
                        }
                        else
                        {
                            BanUser(e.Command.ArgumentsAsList[0]);
                        }

                    }
                    else
                    {
                        int dice = Rng.Next(5);
                        int timer = Rng.Next(300);
                        if (dice == 0)
                        {
                            client.SendMessage(_options.Channel, $"Roll: {dice}/300. Dommage {e.Command.ChatMessage.Username}!");
                            BanUser(e.Command.ChatMessage.Username, timer);
                        }
                        else if (dice == 1)
                        {
                            client.SendMessage(_options.Channel, $"Roll: {dice}/300. Désolé {e.Command.ArgumentsAsString[0]}!");
                            BanUser(e.Command.ArgumentsAsList[0], timer);
                        }
                        else if (dice == 2)
                        {
                            client.SendMessage(_options.Channel, $"Roll: {dice}/300. Allez ça dégage {e.Command.ChatMessage.Username} et {e.Command.ArgumentsAsString}!");
                            BanUser(e.Command.ChatMessage.Username, timer);
                            BanUser(e.Command.ArgumentsAsList[0], timer);
                        }
                        else
                        {
                            client.SendMessage(_options.Channel, $"Non, pas envie aujourd'hui");
                        }
                    }
                }
                return;
            }

            if (string.Equals(e.Command.CommandText, "settitle", StringComparison.InvariantCultureIgnoreCase))
            {
                if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                {
                    ModifyChannelInformationRequest request = new();
                    request.Title = e.Command.ArgumentsAsString;
                    if (_options.BotFunction.AddBotSuffixInTitle && !request.Title.Contains("!bot", StringComparison.InvariantCultureIgnoreCase))
                    {
                        request.Title += " !bot";
                    }
                    await api.Helix.Channels.ModifyChannelInformationAsync(_options.TwitchId, request);
                    client.SendMessage(_options.Channel, $"{e.Command.ChatMessage.Username}, le titre du stream a été changé en: {request.Title}");
                }
                return;
            }

            if (string.Equals(e.Command.CommandText, "setgame", StringComparison.InvariantCultureIgnoreCase))
            {
                if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
                {
                    GetGamesResponse games = await api.Helix.Games.GetGamesAsync(null, new List<string>() { e.Command.ArgumentsAsString });
                    if (games.Games.Length > 0)
                    {
                        ModifyChannelInformationRequest request = new();
                        request.GameId = games.Games[0].Id;
                        await api.Helix.Channels.ModifyChannelInformationAsync(_options.TwitchId, request);
                        client.SendMessage(_options.Channel, $"{e.Command.ChatMessage.Username}, le jeu du stream a été changé en: {games.Games[0].Name}");
                    }
                    else
                    {
                        client.SendMessage(_options.Channel, $"{e.Command.ChatMessage.Username}, je connais pas ton jeu de merde");
                    }
                }
                return;
            }
        }

        public async Task<string> GetLastFollower()
        {
            GetUsersFollowsResponse followers = await api.Helix.Users.GetUsersFollowsAsync(null, null, 100, null, _options.TwitchId);
            return followers.Follows[0].FromUserName;
        }

        public async Task<List<CustomReward>> GetChannelRewards()
        {
            GetCustomRewardsResponse rewards = await api.Helix.ChannelPoints.GetCustomRewardAsync(_options.TwitchId, null, true);
            return rewards.Data.ToList();
        }

        public async Task<string> GetCurrentGame()
        {
            GetChannelInformationResponse game = await api.Helix.Channels.GetChannelInformationAsync(_options.TwitchId);
            return game.Data[0].Title;
        }

        public async void BanUser(string username, int duration = 300)
        {
            GetUsersResponse user = await api.Helix.Users.GetUsersAsync(null, new List<string>() { username }, null);
            if (user.Users.Length > 0)
            {
                BanUserRequest banUserRequest = new();
                banUserRequest.UserId = user.Users[0].Id;
                banUserRequest.Duration = duration;
                banUserRequest.Reason = "No reason";
                await api.Helix.Moderation.BanUserAsync(_options.ClientId, _options.ClientId, banUserRequest);
            }
        }

        public async Task<List<string>> GetChatters()
        {
            List<ChatterFormatted> chatters = await api.Undocumented.GetChattersAsync(_options.Channel);
            return chatters.Select(x => x.Username).ToList();
        }

        public async Task<long> GetFollowerCount()
        {
            GetUsersFollowsResponse followers = await api.Helix.Users.GetUsersFollowsAsync(null, null, 100, null, _options.TwitchId);
            return followers.TotalFollows;
        }

        public async Task<int> GetViewerCount()
        {
            GetStreamsResponse stream = await api.Helix.Streams.GetStreamsAsync(null, null, 1, null, null, "all", new List<string>() { _options.TwitchId });
            if (stream.Streams.Length == 0)
                return 0;
            else
                return stream.Streams[0].ViewerCount;
        }

        public void SendMessage(string message)
        {
            client.SendMessage(_options.Channel, message);
        }

        public void UpdateTokens(string accessToken, string refreshToken)
        {
            dynamic config = LoadAppSettings();
            config.Settings.AccessToken = accessToken;
            config.Settings.RefreshToken = refreshToken;
            SaveAppSettings(config);
            _options.AccessToken = accessToken;
            _options.RefreshToken = refreshToken;
        }

        public dynamic LoadAppSettings()
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(appSettingsPath);
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            return JsonConvert.DeserializeObject<ExpandoObject>(json, jsonSettings);
        }

        public void SaveAppSettings(dynamic config)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            var newJson = JsonConvert.SerializeObject(config, Formatting.Indented, jsonSettings);
            File.WriteAllText(appSettingsPath, newJson);
        }

        public void Dispose()
        {
            if (client.IsConnected)
            {
                client.SendMessage(_options.Channel, $"Allez Bisous, mon peuple m'attend!");
            }
        }
    }
}
