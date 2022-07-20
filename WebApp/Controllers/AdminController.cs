using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class AdminController : ControllerBase
	{
		private readonly ILogger<AdminController> _logger;

		public AdminController(ILogger<AdminController> logger)
		{
			_logger = logger;
		}

		/*[HttpGet]
		public IEnumerable<WeatherForecast> Get()
		{
			return Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = DateTime.Now.AddDays(index),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = Summaries[Random.Shared.Next(Summaries.Length)]
			})
			.ToArray();
		}*/
	}
}