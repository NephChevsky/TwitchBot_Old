using ApiDll;
using DbDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using ModelsDll.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace ChatDll
{
	public class BasicChat
	{
		private Settings _settings;
		private readonly ILogger<BasicChat> _logger;
		public TwitchClient _client;

		public BasicChat(ILogger<BasicChat> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();

			Task.Run(async () => {
				TwitchAPI api = new();
				api.Settings.ClientId = _settings.ClientId;
				api.Settings.Secret = _settings.Secret;
				using (TwitchDbContext db = new())
				{
					Token accessToken = db.Tokens.Where(x => x.Name == "BotAccessToken").FirstOrDefault();
					if (accessToken != null)
					{
						ValidateAccessTokenResponse response = await api.Auth.ValidateAccessTokenAsync(accessToken.Value);
						if (response == null)
						{
							Token refreshToken = db.Tokens.Where(x => x.Name == "BotRefreshToken").FirstOrDefault();
							if (refreshToken != null)
							{
								RefreshResponse newToken = await api.Auth.RefreshAuthTokenAsync(refreshToken.Value, _settings.Secret);
								accessToken.Value = newToken.AccessToken;
								refreshToken.Value = newToken.RefreshToken;
								db.SaveChanges();
							}
							else
							{
								throw new Exception("Implement auth flow for chat bot");
							}
						}
					}
				}
			}).Wait();

			var clientOptions = new ClientOptions
			{
				MessagesAllowedInPeriod = 750,
				ThrottlingPeriod = TimeSpan.FromSeconds(30)
			};
			WebSocketClient customClient = new WebSocketClient(clientOptions);
			_client = new TwitchClient(customClient);

			using (TwitchDbContext db = new())
			{
				Token token = db.Tokens.Where(x => x.Name == "BotAccessToken").FirstOrDefault();
				ConnectionCredentials credentials = new(_settings.Bot, token.Value);
				_client.Initialize(credentials, _settings.Streamer);
			}

			_client.OnLog += Client_OnLog;
			_client.OnJoinedChannel += Client_OnJoinedChannel;
			_client.OnConnected += Client_OnConnected;
			_client.OnConnectionError += Client_OnConnectionError;
			_client.OnDisconnected += Client_OnDisconnected;
			_client.OnError += Client_OnError;
			_client.OnFailureToReceiveJoinConfirmation += Client_OnFailureToReceiveJoinConfirmation;
			_client.OnIncorrectLogin += Client_OnIncorrectLogin;

			if (!_client.Connect())
			{
				_logger.LogError("Couldn't connect IRC client");
			}
		}

		public bool IsConnected
		{
			get
			{
				return _client != null ? _client.IsConnected : false;
			}
		}

		private void Client_OnLog(object sender, OnLogArgs e)
		{
			_logger.LogInformation($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
		}

		private void Client_OnConnected(object sender, OnConnectedArgs e)
		{
			_logger.LogInformation($"Connected to {_settings.Streamer}");
		}

		private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
		{
			_logger.LogInformation($"Joined channel {_settings.Streamer}");
		}

		private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
		{
			_logger.LogInformation($"Couldn't connect: {e.Error.Message}");
		}

		private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
		{
			_logger.LogInformation($"Chat disconnect");
		}

		private void Client_OnError(object sender, OnErrorEventArgs e)
		{
			_logger.LogInformation($"An error occured: {e.Exception.Message}");
		}

		private void Client_OnFailureToReceiveJoinConfirmation(object sender, OnFailureToReceiveJoinConfirmationArgs e)
		{
			_logger.LogInformation($"Fail to join channel: {e.Exception.Details}");
		}

		private void Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e)
		{
			_logger.LogInformation($"Fail to join channel: {e.Exception.Message}");
		}

		public void SendMessage(string message)
		{
			_client.SendMessage(_settings.Streamer, message);
		}
	}
}
