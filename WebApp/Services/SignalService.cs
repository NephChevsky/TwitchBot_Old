﻿using Microsoft.AspNetCore.SignalR;

namespace WebApp.Services
{
	public class SignalService : Hub
	{
		private readonly ILogger<SignalService> _logger;

		public SignalService(ILogger<SignalService> logger)
		{
			_logger = logger;
		} 
	}
}
