using ModelsDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace SpotifyDll
{
	public class Spotify
	{
		private Settings _settings;
		private readonly ILogger<Spotify> _logger;
		private ILoggerFactory _loggerFactory;
		private SpotifyClient _client;

		public Spotify(IConfiguration configuration)
		{
			_loggerFactory = LoggerFactory.Create(lf => { lf.AddAzureWebAppDiagnostics(); });
			_logger = _loggerFactory.CreateLogger<Spotify>();
			_settings = configuration.GetSection("Settings").Get<Settings>();

			SpotifyClientConfig config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new ClientCredentialsAuthenticator(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret)).WithToken("BQC6YKgctwZHpHm3GcVXUb8eTBgR460aByXLd3Ic0qPb6jjbgaIDO8AatXvd5OP8_9nLfJHI1E5jWbNm7NUD5XGfzP9rtSgSRp0jjajlezLdzIxYdjer6ZzVR-SO66sTRPF9QBiOvNKG6NVOGukVh16KaX6IRmGmYia_rj8eNT69t0AvtskHTyA");

			_client = new SpotifyClient(config);
		}

		public async Task<FullTrack> GetCurrentSong()
		{
			CurrentlyPlaying song = await _client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
			if (song != null)
			{
				return (FullTrack) song.Item;
			}
			else
			{
				return null;
			}
		}

		public async Task<bool> SkipSong()
		{
			bool ret = false;
			try {
				ret = await _client.Player.SkipNext();
			}
			catch {
				return false;
			}
			return ret;
		}
	}
}