﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;

namespace ObsDll
{
	public class Obs
	{
		private Settings _settings;
		private Secret _secret;
		private readonly ILogger<Obs> _logger;
		private OBSWebsocket _obs;
		public bool IsConnected = false;

		public Obs(ILogger<Obs> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_secret = configuration.GetSection("Secret").Get<Secret>();
		}

		public void Connect()
		{
			_obs = new OBSWebsocket();
			_obs.Connected += Connect;
			_obs.Disconnected += Disconnected;
			_obs.ConnectAsync(_settings.ObsFunction.Url, _secret.Obs.Password);
		}

		public void ToggleMic()
		{
			bool muted = _obs.GetInputMute("Mic/Aux");
			_obs.SetInputMute("Mic/Aux", !muted);
		}

		public void UnMuteAll()
		{
			_obs.SetInputMute("Desktop Audio", false);
			_obs.SetInputMute("Mic/Aux", false);
		}

		public void SwitchScene(string name)
		{
			_obs.SetCurrentProgramScene(name);
		}

		public void StartSteam()
		{
			_obs.SetInputMute("Desktop Audio", true);
			_obs.SetInputMute("Mic/Aux", true);
			_obs.SetCurrentProgramScene("Playing");
			Task.Delay(500).Wait();
			_obs.SetCurrentProgramScene("Start screen");
			_obs.StartStream();
		}

		public void StopStream()
		{
			Task.Delay(5000);
			_obs.StopStream();
		}

		public bool IsStreaming()
		{
			OutputStatus response = _obs.GetStreamStatus();
			return response.IsActive;
		}

		private void Connect(object sender, EventArgs e)
		{
			IsConnected = true;
			_logger.LogInformation("Connected to obs websocket");
		}

		private void Disconnected(object sender, ObsDisconnectionInfo e)
		{
			IsConnected = false;
			_logger.LogInformation("Disconnected from obs websocket");
		}
	}
}