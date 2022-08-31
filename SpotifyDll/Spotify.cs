using ModelsDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.Diagnostics;
using System.Web;
using DbDll;
using ModelsDll.Db;

namespace SpotifyDll
{
	public class Spotify : IDisposable
	{
		private Settings _settings;
		private readonly ILogger<Spotify> _logger;
		private SpotifyClient _client;
		public EmbedIOAuthServer _server;
		private Timer RefreshTokenTimer;

		public Spotify(ILogger<Spotify> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();

			TimeSpan firstRefresh = TimeSpan.FromMinutes(55);

			using (TwitchDbContext db = new())
			{
				Token accessToken = db.Tokens.Where(x => x.Name == "SpotifyAccessToken").FirstOrDefault();
				if (accessToken == null)
				{
					FetchTokens();
				}
				else
				{
					DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Time"));
					if (accessToken.LastModificationDateTime < now.AddMinutes(-55))
					{
						Task.Run(async () => await RefreshTokenAsync()).Wait();
					}
					else
					{
						_client = new SpotifyClient(accessToken.Value);
						firstRefresh = TimeSpan.FromSeconds(Math.Max(0, 55 * 60 - (now - accessToken.LastModificationDateTime).TotalSeconds));
					}
				}
			}

			RefreshTokenTimer = new Timer(RefreshToken, null, firstRefresh, TimeSpan.FromMinutes(55));
		}

		public void FetchTokens()
		{
			if (!Directory.GetCurrentDirectory().Contains("wwwroot"))
			{
				_server = new EmbedIOAuthServer(new Uri(_settings.SpotifyFunction.LocalCallbackUrl), 5001);
				_server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
				_server.Start().Wait();

				var request = new LoginRequest(_server.BaseUri, _settings.SpotifyFunction.ClientId, LoginRequest.ResponseType.Code)
				{
					Scope = new List<string> { Scopes.UserReadCurrentlyPlaying, Scopes.UserModifyPlaybackState, Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic }
				};
				BrowserUtil.Open(request.ToUri());
			}
		}

		public async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse code)
		{
			var oauth = new OAuthClient();
			Uri uri = new Uri(_settings.SpotifyFunction.LocalCallbackUrl);
			var tokenRequest = new AuthorizationCodeTokenRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, code.Code, uri);
			var tokenResponse = await oauth.RequestToken(tokenRequest);
			UpdateTokens(tokenResponse.AccessToken, tokenResponse.RefreshToken);
			_client = new SpotifyClient(tokenResponse.AccessToken);
			RefreshTokenTimer = new Timer(RefreshToken, null, TimeSpan.FromMinutes(55), TimeSpan.FromMinutes(55));
			_server.Stop().Wait();
		}

		private void UpdateTokens(string newAccessToken, string newRefreshToken)
		{
			using (TwitchDbContext db = new())
			{
				Token accessToken = db.Tokens.Where(x => x.Name == "SpotifyAccessToken").FirstOrDefault();
				Token refreshToken = db.Tokens.Where(x => x.Name == "SpotifyRefreshToken").FirstOrDefault();
				if (accessToken == null)
				{
					accessToken = new();
					accessToken.Name = "SpotifyAccessToken";
					accessToken.Value = newAccessToken;
					db.Add(accessToken);
				}
				else
				{
					accessToken.Value = newAccessToken;
				}
				if (newRefreshToken != null)
				{
					if (refreshToken == null)
					{
						refreshToken = new();
						refreshToken.Name = "SpotifyRefreshToken";
						refreshToken.Value = newRefreshToken;
						db.Add(refreshToken);
					}
					else
					{
						refreshToken.Value = newRefreshToken;
					}
				}
				db.SaveChanges();
			}
		}

		private async void RefreshToken(object state = null)
		{
			await Task.Run(async () => await RefreshTokenAsync());
		}

		private async Task RefreshTokenAsync() 
		{
			using (TwitchDbContext db = new())
			{
				Token token = db.Tokens.Where(x => x.Name == "SpotifyRefreshToken").FirstOrDefault();
				if (token == null)
				{
					if (!Directory.GetCurrentDirectory().Contains("wwwroot"))
					{
						FetchTokens();
					}
					else
					{
						RefreshTokenTimer = new Timer(RefreshToken, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
					}
				}
				else
				{
					var tokenResponse = await new OAuthClient().RequestToken(new AuthorizationCodeRefreshRequest(_settings.SpotifyFunction.ClientId, _settings.SpotifyFunction.ClientSecret, token.Value));
					UpdateTokens(tokenResponse.AccessToken, tokenResponse.RefreshToken);
					_client = new SpotifyClient(tokenResponse.AccessToken);
					RefreshTokenTimer = new Timer(RefreshToken, null, TimeSpan.FromMinutes(55), TimeSpan.FromMinutes(55));
				}
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