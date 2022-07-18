using OBSWebsocketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Services
{
    public class OBSService : IDisposable
    {
        private OBSWebsocket _obs { get; set; }

        private readonly Settings _options;
        private readonly ILogger<BotService> _logger;

        public OBSService(ILogger<BotService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _options = configuration.GetSection("Settings").Get<Settings>();

            _obs = new OBSWebsocket();

            //_obs.Connect(_options.OBSFunction.Server, _options.OBSFunction.Password);
        }

        public void Dispose()
        {
            if (_obs.IsConnected)
            {
                _obs.Disconnect();
            }
        }
    }
}
