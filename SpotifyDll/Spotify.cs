using ModelsDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace SpotifyDll
{
	public class Spotify : IDisposable
	{
		private Settings _settings;
		private readonly ILogger<Spotify> _logger;
		private SpotifyClient _client;
		private EmbedIOAuthServer _server;
		private string _accessToken;
		private string _refreshToken;

		public Spotify(ILogger<Spotify> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();

			_server = new EmbedIOAuthServer(new Uri("http://localhost:5001/callback"), 5001);
			Task server = Task.Run(async () =>
			{
				await _server.Start();
			});
			server.Wait();
			_server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

			LoginRequest loginRequest = new(new Uri("http://localhost:5001/callback"), _settings.SpotifyFunction.ClientId, LoginRequest.ResponseType.Code)
			{
				Scope = new[] { Scopes.UserReadCurrentlyPlaying, Scopes.UserModifyPlaybackState }
			};
			Uri uri = loginRequest.ToUri();
			BrowserUtil.Open(uri);
		}

		private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
		{
			var oauth = new OAuthClient();

			var tokenRequest = new AuthorizationCodeTokenRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, response.Code, new Uri("http://localhost:5001/callback"));
			var tokenResponse = await oauth.RequestToken(tokenRequest);
			_accessToken = tokenResponse.AccessToken;
			_refreshToken = tokenResponse.RefreshToken;

			_client = new SpotifyClient(tokenResponse.AccessToken);
		}

		// may be usefull to refresh token one day
		private async Task RefreshToken() 
		{
			var newResponse = await new OAuthClient().RequestToken(new AuthorizationCodeRefreshRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, _refreshToken));
			_client = new SpotifyClient(newResponse.AccessToken);
		}

		public async Task<FullTrack> GetCurrentSong()
		{
			CurrentlyPlaying song = await _client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
			if (song != null)
			{
				return (FullTrack)song.Item;
			}
			else
			{
				return null;
			}
		}

		public async Task<bool> SkipSong()
		{
			bool ret = false;
			try
			{
				ret = await _client.Player.SkipNext();
			}
			catch
			{
				return false;
			}
			return ret;
		}

		public void Dispose()
		{
			_server.Stop();
		}
	}
}