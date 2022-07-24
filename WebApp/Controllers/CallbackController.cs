using Microsoft.AspNetCore.Mvc;
using SpotifyDll;

namespace WebApp.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class CallbackController : Controller
	{
		private readonly ILogger<CallbackController> _logger;
		private Spotify _spotify;

		public CallbackController(ILogger<CallbackController> logger, Spotify spotify)
		{
			_logger = logger;
			_spotify = spotify;
		}

		[HttpGet]
		public async Task Get([FromQuery] string code)
		{
			await _spotify.OnAuthorizationCodeReceived(code);
		}
	}
}
