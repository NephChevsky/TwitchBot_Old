using ChatDll;
using GTAReader.Models;
using HotKeyManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelsDll;
using TwitchLib.Client.Events;
using WindowsInput;

namespace GTAReader
{
	public class InputService : IHostedService
    {
        private Settings _settings;
        private readonly ILogger<InputService> _logger;

        public BasicChat _chat;
        public Dictionary<string, VirtualKeyCode> ModSpammerKeyMapping;
        public InputSimulator Simulator = new InputSimulator();
        public ModSpammer ModSpammer = new ModSpammer();
        public Random Rng = new Random(Guid.NewGuid().GetHashCode());

        public bool IsRunning = false;
        public bool IsModSpammerOn = false;
        public bool RandomMod = false;

        public System.Threading.Timer RandomModTimer;

        public List<string> CurrentPath = new List<string>();
        public Entry Menus;

        public InputService(ILogger<InputService> logger, IConfiguration configuration, BasicChat chat)
        {
            _logger = logger;
            _settings = configuration.GetSection("Settings").Get<Settings>();
            _chat = chat;
            ModSpammerKeyMapping = configuration.GetSection("KeyMapping").GetSection("ModSpammer").Get<Dictionary<string, VirtualKeyCode>>();
            IsModSpammerOn = configuration.GetSection("ModSpammer").Get<bool>();
            RandomMod = configuration.GetSection("ModSpammer").Get<bool>();
            Menus = configuration.GetSection("ModSpammerMenus").Get<Entry>();
            HotKeyHandler.RegisterHotKey(Keys.A, KeyModifiers.Alt);
            HotKeyHandler.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_TogglePause);
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
            if (e.ChatMessage.Message.ToLower() == "!help")
            {
                if (IsModSpammerOn)
                {
                    _chat.SendMessage("Commandes disponibles: " + string.Join(", ", ModSpammerKeyMapping.Keys));
                }
			}
            if (IsRunning)
            {
                if (IsModSpammerOn)
                {
                    var input = e.ChatMessage.Message.ToLower();
                    if (ModSpammerKeyMapping.ContainsKey(input))
                    {
                        _logger.LogInformation(input + " " + ModSpammerKeyMapping[input].ToString());
                        SendKey(ModSpammerKeyMapping[input]);
                    }
                }
            }
        }

        public void RandomModGenerator(object o)
        {
            Entry current = Menus;
            List<string> path = new List<string>();
            
            while (current.Childs != null)
            {
                List<Entry> choosablePath = current.Childs.Where(x => x.IsEnabled == true).ToList();
                int i = Rng.Next(0, choosablePath.Count() - 1);
                path.Add(choosablePath[i].Name);
                current = choosablePath[i];
            }

            CloseMenu();
            OpenMenu();

            _logger.LogInformation("Navigate to menu :" + String.Join(", ", path));
            GoToMod(Menus, path);
            _logger.LogInformation("Navigation done");

            if (current.IsActivable || current.IsActivable || current.IsSettable)
            {
                SendKey(VirtualKeyCode.NUMPAD5);
                if (current.IsSettable)
                {
                    string input = Rng.Next(current.SettableMin, current.SettableMax + 1).ToString();
                    _logger.LogInformation("Input value: " + input);
                    for (int i = 0; i < input.Length; i++)
                    {
                        SendKey(VirtualKeyCode.VK_0 + (int)input[i]);
                        SendKey(VirtualKeyCode.RETURN);
                    }
                }
            }
            _logger.LogInformation("Mod Activated");

            CloseMenu();
        }

        public void GoToMod(Entry current, List<string> path)
        {
            Entry next = current.Childs.Where(x => x.Name == path[0]).FirstOrDefault();
            int index = current.Childs.IndexOf(next);
            for (int i = 0; i < index; i++)
            {
                SendKey(VirtualKeyCode.NUMPAD2);
            }
            path.RemoveAt(0);
            if (path.Count > 0)
            {
                SendKey(VirtualKeyCode.NUMPAD5);
                GoToMod(next, path);
			}
        }

        public void OpenMenu()
        {
            SendKey(VirtualKeyCode.F8);
            CurrentPath.Add("Menu");
        }

        public void CloseMenu()
        {
            _logger.LogInformation("Closing Menu");
            SendKey(VirtualKeyCode.NUMPAD0);
            SendKey(VirtualKeyCode.NUMPAD0);
            SendKey(VirtualKeyCode.NUMPAD0);
            SendKey(VirtualKeyCode.NUMPAD0);
            SendKey(VirtualKeyCode.NUMPAD0);
            CurrentPath.Clear();
            _logger.LogInformation("Menu Closed");
        }

        public void SendKey(VirtualKeyCode key)
        {
            Simulator.Keyboard.KeyDown(key);
            Task.Delay(16).Wait();
            Simulator.Keyboard.KeyUp(key);
            Task.Delay(50).Wait();
        }

        public void HotKeyManager_TogglePause(object sender, HotKeyEventArgs e)
        {
            IsRunning = !IsRunning;
            if (IsRunning && RandomMod)
            {
                RandomModTimer = new System.Threading.Timer(RandomModGenerator, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
            }
            else if (!IsRunning && RandomMod)
            {
                RandomModTimer.Dispose();
			}
            string message = "Script is " + (IsRunning ? "ON" : "OFF");
            _chat.SendMessage(message);
            _logger.LogInformation(message);
        }
    }
}
