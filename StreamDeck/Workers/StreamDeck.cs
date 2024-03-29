﻿using ApiDll;
using ChatDll;
using HelpersDll;
using HotKeyManager;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ObsDll;
using SpeechDll;
using SpotifyDll;
using StreamDeck.Forms;
using System.Diagnostics;
using System.Media;
using System.Speech.Recognition;
using WindowsInput;

namespace StreamDeck.Workers
{
    public class StreamDeck : IHostedService
    {
        private Settings _settings;
        private readonly ILogger<StreamDeck> _logger;

        public BasicChat _chat;
        private Api _api;
        private Spotify _spotify;
        private Obs _obs;
        private Speech _speech;
        
        private Dictionary<string, DateTime> AntiSpamTimer = new Dictionary<string, DateTime>();
        private HubConnection _connection;

        public StreamDeck(ILogger<StreamDeck> logger, IConfiguration configuration, BasicChat chat, Api api, Spotify spotify, Obs obs, Speech speech)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _api = api;
            _spotify = spotify;
            _chat = chat;
            _obs = obs;
            _speech = speech;

            _connection = new HubConnectionBuilder().WithUrl(_settings.SignalRUrl).WithAutomaticReconnect().Build();
            _connection.On<Dictionary<string, string>>("TriggerReward", (reward) => OnTriggerReward(null, reward));

            HotKeyHandler.RegisterHotKey(Keys.NumPad0, KeyModifiers.Alt);
            HotKeyHandler.RegisterHotKey(Keys.Subtract, KeyModifiers.Alt);
            HotKeyHandler.RegisterHotKey(Keys.End, KeyModifiers.Alt);
            HotKeyHandler.RegisterHotKey(Keys.NumPad1, KeyModifiers.Alt);
            HotKeyHandler.RegisterHotKey(Keys.NumPad2, KeyModifiers.Alt);
            HotKeyHandler.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HandleHotKeys);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!Debugger.IsAttached)
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
                SoundPlayer player = new SoundPlayer(@"D:\Dev\Twitch\StreamDeck\Assets\barrelroll.wav");
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
                SoundPlayer player = new SoundPlayer(@"D:\Dev\Twitch\StreamDeck\Assets\rocketleague.wav");
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
                simulator.Keyboard.KeyDown(VirtualKeyCode.VK_S);
                Task.Delay(200).Wait();
                simulator.Keyboard.KeyUp(VirtualKeyCode.VK_S);
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
                if (e.Key == Keys.NumPad0)
                {
                    _obs.ToggleMic();
                }
                else if (e.Key == Keys.Subtract)
                {
                    if (_obs.IsStreaming())
                    {
                        _obs.UnMuteAll();
                        await _spotify.ChangeVolume(40);
                        _obs.SwitchScene("Playing");
                        _speech.StartSpeechToText(SpeechRecognized);
                    }
                    else
                    {
                        StartStream popup = new(_api);
                        DialogResult dialogResult = popup.ShowDialog();
                        if (dialogResult == DialogResult.OK)
                        {
                            await _spotify.StartPlaylist(_settings.SpotifyFunction.Playlist);
                            await _spotify.ChangeVolume(65);
                            _obs.StartSteam();
                        }
                        popup.Dispose();
                    }
                }
                else if (e.Key == Keys.End)
                {
                    _speech.StopSpeechToText();
                    Task.Delay(5 * 1000).Wait();
                    _obs.StopStream();
                    Task.Delay(2 * 1000).Wait();
                    await _spotify.StopPlayback();
				}
                else if (e.Key == Keys.NumPad1)
                {
                    _obs.SwitchScene("Playing");
                }
                else if (e.Key == Keys.NumPad2)
                {
                    _obs.SwitchScene("Desktop");
                }
            }
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            
        }
    }
}