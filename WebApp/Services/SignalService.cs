﻿using Microsoft.AspNetCore.SignalR;
using ModelsDll;

namespace WebApp.Services
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