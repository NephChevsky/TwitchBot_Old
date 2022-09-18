using ApiDll;
using ChatDll;
using DbDll;
using HelpersDll;
using HotKeyManager;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;
using ObsDll;
using SpotifyAPI.Web;
using SpotifyDll;
using System.Diagnostics;
using System.Media;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Client.Events;
using WindowsInput;

namespace Bot.Workers
{
    public class ChatBot : IHostedService
    {
        private Settings _settings;
        private readonly ILogger<ChatBot> _logger;

        public BasicChat _chat;
        private Api _api;
        private Spotify _spotify;
        private Obs _obs;
        
        private Dictionary<string, DateTime> AntiSpamTimer = new Dictionary<string, DateTime>();
        private HubConnection _connection;

        public ChatBot(ILogger<ChatBot> logger, IConfiguration configuration, BasicChat chat, Api api, Spotify spotify, Obs obs)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = api;
            _spotify = spotify;
            _chat = chat;
            _obs = obs;
            _connection = new HubConnectionBuilder().WithUrl(_settings.SignalRUrl).WithAutomaticReconnect().Build();
            _connection.On<Dictionary<string, string>>("TriggerReward", (reward) => OnTriggerReward(null, reward));

            HotKeyHandler.RegisterHotKey(Keys.NumPad0, KeyModifiers.Alt);
            HotKeyHandler.RegisterHotKey(Keys.Subtract, KeyModifiers.Alt);
            HotKeyHandler.RegisterHotKey(Keys.End, KeyModifiers.Alt);
            HotKeyHandler.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HandleHotKeys);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            bool waitForStart = false;
            if (Process.GetProcessesByName("Spotify").Length == 0)
            {
                Process.Start(_settings.SpotifyFunction.ExePath);
                waitForStart = true;
            }

            if (Process.GetProcessesByName("obs64").Length == 0)
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.UseShellExecute = true;
                info.WorkingDirectory = Path.GetDirectoryName(_settings.ObsFunction.ExePath);
                info.FileName = _settings.ObsFunction.ExePath;
                Process.Start(info);
                waitForStart = true;
            }

            if (waitForStart)
            {
                Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            }

            if (!_obs.IsConnected)
            {
                _obs.Connect();
            }
            Console.Beep();

            _connection.StartAsync().Wait();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task OnTriggerReward(object sender, Dictionary<string, string> e)
        {
            bool success = false;
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
                success = true;
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
                success = true;
            }
            else if (string.Equals(e["type"], "What a save!", StringComparison.InvariantCultureIgnoreCase))
            {
                var simulator = new InputSimulator();
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_2);
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4);
                success = true;
            }
            else if (string.Equals(e["type"], "Wow!", StringComparison.InvariantCultureIgnoreCase))
            {
                var simulator = new InputSimulator();
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
                success = true;
            }
            else if (string.Equals(e["type"], "Faking.", StringComparison.InvariantCultureIgnoreCase))
            {
                var simulator = new InputSimulator();
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_4);
                simulator.Keyboard.KeyPress(VirtualKeyCode.VK_3);
                success = true;
            }
            else if (string.Equals(e["type"], "Vider mon chargeur", StringComparison.InvariantCultureIgnoreCase))
            {
                var simulator = new InputSimulator();
                simulator.Mouse.LeftButtonDown();
                Task.Delay(1000).Wait();
                simulator.Mouse.LeftButtonUp();
                success = true;
            }

            if (success)
            {
                await Helpers.ValidateRewardRedemption(_api, e["type"], e["reward-id"], e["event-id"]);
            }
            else
            {
                await Helpers.CancelRewardRedemption(_api, e["reward-id"], e["event-id"]);
            }
        }

        public async void HandleHotKeys(object sender, HotKeyEventArgs e)
        {
            if (e.Modifiers == KeyModifiers.Alt)
            {
                if (e.Key == Keys.NumPad0 && e.Modifiers == KeyModifiers.Alt)
                {
                    _obs.ToggleMic();
                }
                else if (e.Key == Keys.Subtract)
                {
                    await _spotify.StartPlaylist(_settings.SpotifyFunction.Playlist);
                    _obs.StartSteam();
				}
                else if (e.Key == Keys.End)
                {
                    Task.Delay(5 * 1000).Wait();
                    _obs.StopStream();
                    Task.Delay(5 * 1000).Wait();
                    await _spotify.StopPlayback();
				}
            }
        }
    }
}