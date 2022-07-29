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
            else if (name == "spotifyapi")
            {
                config.Settings.SpotifyFunction.AccessToken = accessToken;
                config.Settings.SpotifyFunction.RefreshToken = refreshToken;
            }
            SaveAppSettings(config);
        }

        public static dynamic LoadAppSettings()
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var json = File.ReadAllText(appSettingsPath);
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            return JsonConvert.DeserializeObject<ExpandoObject>(json, jsonSettings);
        }

        public static void SaveAppSettings(dynamic config)
        {
            var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new ExpandoObjectConverter());
            jsonSettings.Converters.Add(new StringEnumConverter());
            var newJson = JsonConvert.SerializeObject(config, Formatting.Indented, jsonSettings);
            File.WriteAllText(appSettingsPath, newJson);
        }
    }
}
