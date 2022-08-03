using DbDll;
using Microsoft.AspNetCore.Mvc;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;

namespace WebApp.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class CommandsController : ControllerBase
	{
		private readonly ILogger<CommandsController> _logger;
		private Settings _settings;

		public CommandsController(ILogger<CommandsController> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
		}

		[HttpGet]
		public ActionResult<CommandsListResponse> Get()
		{
			CommandsListResponse response = new();
			response.ComputeUpTime = _settings.CheckUptimeFunction.ComputeUptime;
			response.AddCustomCommands = _settings.ChatFunction.AddCustomCommands;
			response.CustomCommands = new List<KeyValuePair<string, string>>();
			using (TwitchDbContext db = new(Guid.Empty))
			{
				List<Command> commands = db.Commands.ToList();
				commands.ForEach(x =>
				{
					response.CustomCommands.Add(new KeyValuePair<string, string>(x.Name, x.Message));
				});
			}
			return Ok(response);
		}
	}
}
