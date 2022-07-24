using ModelsDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Diagnostics;

namespace SpotifyDll
{
	public class Spotify : IDisposable
	{
		private Settings _settings;
		private readonly ILogger<Spotify> _logger;
		private SpotifyClient _client;

		public Spotify(ILogger<Spotify> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();

			Task refresh = Task.Run(async () =>
			{
				await RefreshToken();
			});
			refresh.Wait();
		}

		public async Task OnAuthorizationCodeReceived(string code)
		{
			var oauth = new OAuthClient();

			var tokenRequest = new AuthorizationCodeTokenRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, code, new Uri("https://bot-neph.azurewebsites.net/callback"));
			var tokenResponse = await oauth.RequestToken(tokenRequest);

			Helpers.UpdateTokens("spotifyapi", tokenResponse.AccessToken, tokenResponse.RefreshToken);
			_settings.SpotifyFunction.AccessToken = tokenResponse.AccessToken;
			_settings.SpotifyFunction.RefreshToken = tokenResponse.RefreshToken;

			_client = new SpotifyClient(tokenResponse.AccessToken);
		}

		private async Task RefreshToken() 
		{
			try
			{
				var newResponse = await new OAuthClient().RequestToken(new AuthorizationCodeRefreshRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, _settings.SpotifyFunction.RefreshToken));

				Helpers.UpdateTokens("spotifyapi", newResponse.AccessToken, newResponse.RefreshToken);
				_settings.SpotifyFunction.AccessToken = newResponse.AccessToken;
				_settings.SpotifyFunction.RefreshToken = newResponse.RefreshToken;

				_client = new SpotifyClient(newResponse.AccessToken);
			}
			catch
			{
				_logger.LogError("Couldn't refresh token for spotify");
			}
		}

		public async Task<FullTrack> GetCurrentSong()
		{
			if (_client != null)
			{
				CurrentlyPlaying song = await _client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
				if (song != null)
				{
					return (FullTrack)song.Item;
				}
			}
			return null;
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
			_logger.LogInformation("Closing spotify api");
		}
	}
}