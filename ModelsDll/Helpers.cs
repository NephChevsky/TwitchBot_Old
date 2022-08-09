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
        public static void UpdateTokens(string name, string configpath, string accessToken, string refreshToken)
        {
            Mutex mut = new Mutex(false, "appSettings");
            mut.WaitOne();
            try
            {
                dynamic config = LoadAppSettings(configpath);
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
                SaveAppSettings(configpath, config);
            }
            finally
            {
                mut.ReleaseMutex();
			}
        }

        public static dynamic LoadAppSettings(string configpath)
        {
            var json = File.ReadAllText(configpath);
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            return JsonConvert.DeserializeObject<ExpandoObject>(json, jsonSettings);
        }

        public static void SaveAppSettings(string configpath, dynamic config)
        {
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            var newJson = JsonConvert.SerializeObject(config, Formatting.Indented, jsonSettings);
            File.WriteAllText(configpath, newJson);
        }
    }
}
