using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelsDll
{
	public class Secret
	{
		public TwitchCredentials Twitch { get; set; }
		public SpotifyCredentials Spotify { get; set; }
		public DiscordCredentials Discord { get; set; }
		public ObsCredentials Obs { get; set; }
	}

	public class TwitchCredentials
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
	}

	public class SpotifyCredentials
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
	}

	public class DiscordCredentials
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
	}

	public class ObsCredentials
	{
		public string Password { get; set; }
	}
}
