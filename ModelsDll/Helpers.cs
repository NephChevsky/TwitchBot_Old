using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelsDll
{
	public static class Helpers
	{
        public static void UpdateTokens(string name, string accessToken, string refreshToken)
        {
            Mutex mut = new Mutex(false, "appSettings");
            mut.WaitOne();
            try
            {
                dynamic config = LoadAppSettings();
                if (name == "twitchapi")
                {
                    config.Settings.StreamerAccessToken = accessToken;
                    config.Settings.StreamerRefreshToken = refreshToken;
                }
                if (name == "twitchchat")
                {
                    config.Settings.BotAccessToken = accessToken;
                    config.Settings.BotRefreshToken = refreshToken;
                }
                SaveAppSettings(config);
            }
            finally
            {
                mut.ReleaseMutex();
			}
        }

        public static dynamic LoadAppSettings()
        {
            string path = "config.json";
            if (!File.Exists(path))
            {
                path = Path.Combine(@"D:\Dev\Twitch", path);
            }
            var json = File.ReadAllText(path);
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            return JsonConvert.DeserializeObject<ExpandoObject>(json, jsonSettings);
        }

        public static void SaveAppSettings(dynamic config)
        {
            string path = "config.json";
            if (!File.Exists(path))
            {
                path = Path.Combine(@"D:\Dev\Twitch", path);
            }
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            var newJson = JsonConvert.SerializeObject(config, Formatting.Indented, jsonSettings);
            File.WriteAllText(path.Replace("config.json", "config.temp"), newJson);
            File.Move(path.Replace("config.json", "config.temp"), path, true);
        }
    }
}
