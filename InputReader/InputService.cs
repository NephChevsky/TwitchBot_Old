using ChatDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using WindowsInput;

namespace InputReader
{
	public class InputService : IHostedService
	{
        private Settings _settings;
        private readonly ILogger<InputService> _logger;

        public BasicChat _chat;
        public Dictionary<string, VirtualKeyCode> KeyMapping;
        public bool IsRunning = false;
        public InputSimulator Simulator = new InputSimulator();

        public InputService(ILogger<InputService> logger, IConfiguration configuration, BasicChat chat)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _chat = chat;
            KeyMapping = configuration.GetSection("KeyMapping").Get<Dictionary<string, VirtualKeyCode>>();
            HotKeyManager.RegisterHotKey(Keys.A, KeyModifiers.Alt);
            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_TogglePause);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _chat._client.OnMessageReceived += Client_OnMessageReceived;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (IsRunning)
            {
                var input = e.ChatMessage.Message.ToLower();
                if (KeyMapping.ContainsKey(input))
                {
                    Simulator.Keyboard.KeyDown(KeyMapping[input]);
                    Task.Delay(16).Wait();
                    Simulator.Keyboard.KeyUp(KeyMapping[input]);
                    _chat.SendMessage("ok");
                }
			}
        }
        public void HotKeyManager_TogglePause(object sender, HotKeyEventArgs e)
        {
            IsRunning = !IsRunning;
            _chat.SendMessage("Script is " + (IsRunning ? "ON" : "OFF"));
        }
    }
}
