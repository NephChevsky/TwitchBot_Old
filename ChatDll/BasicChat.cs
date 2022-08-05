using ApiDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace ChatDll
{
	public class BasicChat
	{
		private Settings _settings;
		private string _configPath;
		private readonly ILogger<BasicChat> _logger;
		public TwitchClient _client;
		private Api _api;

		public BasicChat(ILogger<BasicChat> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_configPath = configuration.GetValue<string>("ConfigPath");
			_api = new(configuration, false);

			Task<RefreshResponse> refreshToken = Task.Run(async () =>
			{
				return await _api.RefreshToken(true);
			});
			refreshToken.Wait();
			RefreshResponse token = refreshToken.Result;
			Helpers.UpdateTokens("twitchchat", _configPath, token.AccessToken, token.RefreshToken);
			_settings.BotAccessToken = token.AccessToken;
			_settings.BotRefreshToken = token.RefreshToken;
			ConnectionCredentials credentials = new ConnectionCredentials(_settings.Bot, _settings.BotAccessToken);
			var clientOptions = new ClientOptions
			{
				MessagesAllowedInPeriod = 750,
				ThrottlingPeriod = TimeSpan.FromSeconds(30)
			};
			WebSocketClient customClient = new WebSocketClient(clientOptions);
			_client = new TwitchClient(customClient);
			_client.Initialize(credentials, _settings.Streamer);

			_client.OnLog += Client_OnLog;
			_client.OnJoinedChannel += Client_OnJoinedChannel;
			_client.OnConnected += Client_OnConnected;
			_client.OnConnectionError += Client_OnConnectionError;

			if (!_client.Connect())
			{
				_logger.LogError("Couldn't connect IRC client");
			}
		}

		public bool IsConnected
		{
			get
			{
				return _client.IsConnected;
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

		public void SendMessage(string message)
		{
			_client.SendMessage(_settings.Streamer, message);
		}
	}
}
