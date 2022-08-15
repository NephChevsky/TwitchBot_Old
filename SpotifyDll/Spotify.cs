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
		private Timer RefreshTokenTimer;

		public Spotify(ILogger<Spotify> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();

			if (!Directory.GetCurrentDirectory().Contains("wwwroot"))
			{
				_server = new EmbedIOAuthServer(new Uri("http://localhost:5001/callback"), 5001);
				_server.Start().Wait(); 
				
				_server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

				var request = new LoginRequest(_server.BaseUri, _settings.SpotifyFunction.ClientId, LoginRequest.ResponseType.Code)
				{
					Scope = new List<string> { Scopes.UserReadCurrentlyPlaying, Scopes.UserModifyPlaybackState, Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic }
				};

				Uri uri = request.ToUri();
				try
				{
					BrowserUtil.Open(request.ToUri());
					BrowserUtil.Open(new Uri("https://accounts.spotify.com/authorize?client_id=e3e3047455fb4f85b889cc251133b9c9&response_type=code&redirect_uri=https%3A%2F%2Fbot-neph.azurewebsites.net%2Fcallback&scope=user-read-currently-playing+user-modify-playback-state+playlist-modify-public+playlist-modify-private"));
				}
				catch (Exception)
				{
					_logger.LogInformation("Unable to open URL for spotify connection, manually open: {0}", uri);
				}
			}

			RefreshTokenTimer = new Timer(RefreshToken, null, TimeSpan.FromMinutes(55), TimeSpan.FromMinutes(55));
		}

		public async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse code)
		{
			var oauth = new OAuthClient();
			Uri uri;
			if (Directory.GetCurrentDirectory().Contains("wwwroot"))
			{
				uri = new Uri("https://bot-neph.azurewebsites.net/callback");
			}
			else
			{
				uri = new Uri("http://localhost:5001/callback");
			}
			var tokenRequest = new AuthorizationCodeTokenRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, code.Code, uri);
			var tokenResponse = await oauth.RequestToken(tokenRequest);
			_accessToken = tokenResponse.AccessToken;
			_refreshToken = tokenResponse.RefreshToken;

			_client = new SpotifyClient(_accessToken);
		}

		private async void RefreshToken(object o) 
		{
			try
			{
				var tokenResponse = await new OAuthClient().RequestToken(new AuthorizationCodeRefreshRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, _refreshToken));
				_accessToken = tokenResponse.AccessToken;
				if (tokenResponse.RefreshToken != null)
					_refreshToken = tokenResponse.RefreshToken;
				_client = new SpotifyClient(_accessToken);
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
			if (_client != null)
			{
				return await _client.Player.SkipNext();
			}
			return false;
		}

		public async Task<int> AddSong(string name)
		{
			int ret = 0;
			if (_client != null)
			{
				SearchRequest searchQuery = new(SearchRequest.Types.Track, name);
				SearchResponse tracks = await _client.Search.Item(searchQuery);

				if (tracks != null)
				{
					Paging<SimplePlaylist> playlists = await _client.Playlists.CurrentUsers();
					if (playlists != null)
					{
						SimplePlaylist playlist = playlists.Items.Where(x => x.Name == _settings.SpotifyFunction.Playlist).FirstOrDefault();
						if (playlist != null)
						{
							int offset = 0;
							do
							{
								PlaylistGetItemsRequest query = new();
								query.Offset = offset;
								Paging<PlaylistTrack<IPlayableItem>> playlistTracks = await _client.Playlists.GetItems(playlist.Id, query);
								PlaylistTrack<IPlayableItem> existingSong = playlistTracks.Items.Where(x => ((FullTrack)x.Track).Uri == tracks.Tracks.Items[0].Uri).FirstOrDefault();
								if (playlistTracks.Items.Count == 0)
								{
									offset = 0;
								}
								else if (existingSong != null)
								{
									return 2;
								}
								else
								{
									offset += playlistTracks.Items.Count;
								}
							} while (offset != 0);
							
							PlaylistAddItemsRequest addQuery = new(new List<string> { tracks.Tracks.Items[0].Uri });
							SnapshotResponse response = await _client.Playlists.AddItems(playlist.Id, addQuery);
							ret = (response != null && response.SnapshotId != null) ? 1 : 0;
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
					Paging<SimplePlaylist> playlists = await _client.Playlists.CurrentUsers();
					if (playlists != null)
					{
						SimplePlaylist playlist = playlists.Items.Where(x => x.Name == _settings.SpotifyFunction.Playlist).FirstOrDefault();
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