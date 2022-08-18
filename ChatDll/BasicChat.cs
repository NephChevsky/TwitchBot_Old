using ApiDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
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

			Task.Run(() => RefreshToken()).Wait();

			var clientOptions = new ClientOptions
			{
				MessagesAllowedInPeriod = 750,
				ThrottlingPeriod = TimeSpan.FromSeconds(30)
			};
			WebSocketClient customClient = new WebSocketClient(clientOptions);
			_client = new TwitchClient(customClient);

			ConnectionCredentials credentials = new (_settings.Bot, _settings.BotAccessToken);
			_client.Initialize(credentials, _settings.Streamer);

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

		public async void RefreshToken(object state = null)
		{
			TwitchAPI api = new();
			api.Settings.ClientId = _settings.ClientId;
			api.Settings.Secret = _settings.Secret;
			api.Settings.AccessToken = _settings.BotAccessToken;
			RefreshResponse token = await api.Auth.RefreshAuthTokenAsync(_settings.BotRefreshToken, _settings.Secret);
			Helpers.UpdateTokens("twitchchat", token.AccessToken, token.RefreshToken);
			_settings.BotAccessToken = token.AccessToken;
			_settings.BotRefreshToken = token.RefreshToken;
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
