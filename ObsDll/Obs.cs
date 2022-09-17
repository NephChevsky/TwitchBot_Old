using Microsoft.Extensions.Configuration;
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
		private readonly ILogger<Obs> _logger;
		private OBSWebsocket _obs;

		public Obs(ILogger<Obs> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();

			_obs = new OBSWebsocket();
			_obs.Connected += Connect;
			_obs.Disconnected += Disconnected;

			_obs.Connect(_settings.ObsFunction.Url, _settings.ObsFunction.Password);
		}

		public void ToggleMic()
		{
			bool muted = _obs.GetInputMute("Mic/Aux");
			_obs.SetInputMute("Mic/Aux", !muted);
		}

		private void Connect(object sender, EventArgs e)
		{
			_logger.LogInformation("Connected to obs websocket");
		}

		private void Disconnected(object sender, ObsDisconnectionInfo e)
		{
			_logger.LogInformation("Disconnected from obs websocket");
		}
	}
}