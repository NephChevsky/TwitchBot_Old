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
		private string _accessToken;
		private string _refreshToken;
		private static EmbedIOAuthServer _server;

		public Spotify(ILogger<Spotify> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();

			if (!Directory.GetCurrentDirectory().Contains("wwwroot"))
			{
				_server = new EmbedIOAuthServer(new Uri("http://localhost:5001/callback"), 5001);
				Task credentials = Task.Run(async () => {
					await _server.Start();
				});
				credentials.Wait();
				_server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

				var request = new LoginRequest(_server.BaseUri, _settings.SpotifyFunction.ClientId, LoginRequest.ResponseType.Code)
				{
					Scope = new List<string> { Scopes.UserReadCurrentlyPlaying, Scopes.UserModifyPlaybackState, Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic }
				};

				Uri uri = request.ToUri();
				try
				{
					BrowserUtil.Open(uri);
				}
				catch (Exception)
				{
					_logger.LogInformation("Unable to open URL for spotify connection, manually open: {0}", uri);
				}
			}
		}

		public async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse code)
		{
			var oauth = new OAuthClient();
			Uri uri;
			if (!Directory.GetCurrentDirectory().Contains("wwwroot"))
			{
				uri = new Uri("http://localhost:5001/callback");
			}
			else
			{
				uri = new Uri("https://bot-neph.azurewebsites.net/callback");
			}
			var tokenRequest = new AuthorizationCodeTokenRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, code.Code, uri);
			var tokenResponse = await oauth.RequestToken(tokenRequest);
			_accessToken = tokenResponse.AccessToken;
			_refreshToken = tokenResponse.RefreshToken;

			_client = new SpotifyClient(_accessToken);
		}

		public async Task RefreshToken() 
		{
			try
			{
				var tokenResponse = await new OAuthClient().RequestToken(new AuthorizationCodeRefreshRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, _refreshToken));
				_accessToken = tokenResponse.AccessToken;
				_refreshToken = tokenResponse.RefreshToken;
				_client = new SpotifyClient(_accessToken);
			}
			catch
			{
				_logger.LogError("Couldn't refresh token for spotify");
			}
		}

		private async Task<FullTrack> GetCurrentSong()
		{
			if (_client != null)
			{
				CurrentlyPlaying song;
				try
				{
					song = await _client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
				}
				catch (APIUnauthorizedException)
				{
					await RefreshToken();
					song = await _client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
				}

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
			if (_client != null)
			{
				try
				{
					ret = await _client.Player.SkipNext();
				}
				catch (APIUnauthorizedException)
				{
					await RefreshToken();
					ret = await _client.Player.SkipNext();
				}
			}
			
			return ret;
		}

		public async Task<bool> AddSong(string name)
		{
			bool ret = false;
			if (_client != null)
			{
				SearchRequest searchQuery = new(SearchRequest.Types.Track, name);
				SearchResponse tracks;
				try
				{
					tracks = await _client.Search.Item(searchQuery);
				}
				catch (APIUnauthorizedException)
				{
					await RefreshToken();
					tracks = await _client.Search.Item(searchQuery);
				}

				if (tracks != null)
				{
					Paging<SimplePlaylist> playlists = await _client.Playlists.CurrentUsers();
					if (playlists != null)
					{
						SimplePlaylist playlist = playlists.Items.Where(x => x.Name == "Streaming").FirstOrDefault();
						if (playlist != null)
						{
							PlaylistAddItemsRequest addQuery = new(new List<string> { tracks.Tracks.Items[0].Uri });
							SnapshotResponse response = await _client.Playlists.AddItems(playlist.Id, addQuery);
							ret = response != null && response.SnapshotId != null;
						}
					}
				}
			}
			
			return ret;
		}

		public async Task<bool> RemoveSong()
		{
			bool ret = false;
			if (_client != null)
			{
				FullTrack song = await GetCurrentSong();
				if (song != null)
				{
					Paging<SimplePlaylist> playlists;
					try
					{
						playlists = await _client.Playlists.CurrentUsers();
					}
					catch (APIUnauthorizedException)
					{
						await RefreshToken();
						playlists = await _client.Playlists.CurrentUsers();
					}

					if (playlists != null)
					{
						SimplePlaylist playlist = playlists.Items.Where(x => x.Name == "Streaming").FirstOrDefault();
						PlaylistRemoveItemsRequest removeQuery = new();
						PlaylistRemoveItemsRequest.Item item = new PlaylistRemoveItemsRequest.Item();
						item.Uri = song.Uri;
						removeQuery.Tracks = new List<PlaylistRemoveItemsRequest.Item>() { item };
						SnapshotResponse response = await _client.Playlists.RemoveItems(playlist.Id, removeQuery);
						ret = response != null && response.SnapshotId != null;
					}
				}
			}

			return ret;
		}

		public void Dispose()
		{
			_logger.LogInformation("Closing spotify api");
		}
	}
}