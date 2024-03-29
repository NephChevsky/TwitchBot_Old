﻿using ChatDll;
using HotKeyManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        public string Application;
        public Process Process;

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        public InputService(ILogger<InputService> logger, IConfiguration configuration, BasicChat chat)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _chat = chat;
            KeyMapping = configuration.GetSection("KeyMapping").Get<Dictionary<string, VirtualKeyCode>>();
            Application = configuration.GetValue<string>("Application");
            HotKeyHandler.RegisterHotKey(Keys.A, KeyModifiers.Alt);
            HotKeyHandler.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_TogglePause);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _chat._client.OnMessageReceived += Client_OnMessageReceived;
            if (!string.IsNullOrEmpty(Application))
            {
                Process[] processes = Process.GetProcessesByName(Application);
                Process = processes[0];
                if (Process == null)
                {
                    throw new Exception("Couldn't find process " + Application);
				}
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.ToLower() == "!help")
            {
                _chat.SendMessage("Commandes disponibles: a, b, up, down, left, right, start, select");
            }
            if (IsRunning)
            {
                string[] inputs = e.ChatMessage.Message.ToLower().Split(" ");
                if (KeyMapping.ContainsKey(inputs[0]))
                {
                    int repeat = 1;
                    if (inputs.Length > 1)
                    {
                        int buffer;
                        if (int.TryParse(inputs[1], out buffer))
                        {
                            repeat = buffer;
                            if (repeat > 5)
                            {
                                repeat = 5;
							}
						}
					}
                    PostMessage(Process.MainWindowHandle, 0x0100, (int)KeyMapping[inputs[0]], 0);
                    Task.Delay(50 + 300 * (repeat-1)).Wait();
                    PostMessage(Process.MainWindowHandle, 0x0101, (int)KeyMapping[inputs[0]], 0);
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
