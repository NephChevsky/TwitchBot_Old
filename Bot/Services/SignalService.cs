using Bot.Models;
using Db.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Services
{
    public class SignalService : Hub
    {
        private readonly ILogger<SignalService> _logger;

		public SignalService(ILogger<SignalService> logger)
		{
			_logger = logger;
		}

		public void TriggerAlert(Alert alert)
        {
			Clients.All.SendAsync("TriggerAlert", alert);
		}
	}
}
