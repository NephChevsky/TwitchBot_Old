using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelsDll;
using OBSWebsocketDotNet;

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
			_obs.Connected += onConnect;
			_obs.Disconnected += onDisconnect;
		}

		private void onConnect(object sender, EventArgs e)
		{
			_logger.LogInformation("Connected to obs websocket");
		}

		private void onDisconnect(object sender, EventArgs e)
		{
			_logger.LogInformation("Disconnected from obs websocket");
		}
	}
}