using ApiDll;
using DbDll;
using Microsoft.AspNetCore.Mvc;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Bits;

namespace WebApp.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class StatisticsController : ControllerBase
	{
		private readonly ILogger<StatisticsController> _logger;
		private Settings _settings;
		private Api _api;

		public StatisticsController(ILogger<StatisticsController> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_api = new(configuration, "twitchapi");
		}

		[HttpGet]
		public async Task<ActionResult<List<UserStatsResponse>>> Get()
		{
			List<UserStatsResponse> response = new();
			using (TwitchDbContext db = new())
			{
				List<Viewer> viewers = db.Viewers.Where(x => x.IsBot == false && x.Username != _settings.Streamer).OrderByDescending(x => x.Uptime).ToList();
				int i = 1;
				foreach (Viewer viewer in viewers)
				{
					UserStatsResponse entry = new UserStatsResponse(viewer);
					entry.Position = i;
					i++;
					response.Add(entry);
				}

				var uptimes = db.Uptimes.Where(x => x.CreationDateTime > DateTime.Now.AddMonths(-1)).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Sum) }).ToList();
				foreach (var uptime in uptimes)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == uptime.Owner).FirstOrDefault();
					if (viewer != null)
					{
						string hours = Math.Floor((double) uptime.Sum / 3600).ToString();
						string minutes = Math.Floor((double)(uptime.Sum % 3600) / 60).ToString();
						viewer.UptimeMonth = $"{hours.PadLeft(2, '0')}h{minutes.PadLeft(2, '0')}";
					}
				}

				uptimes = db.Uptimes.Where(x => x.CreationDateTime > DateTime.Now.AddDays(-1)).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Sum) }).ToList();
				foreach (var uptime in uptimes)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == uptime.Owner).FirstOrDefault();
					if (viewer != null)
					{
						string hours = Math.Floor((double)uptime.Sum / 3600).ToString();
						string minutes = Math.Floor((double)(uptime.Sum % 3600) / 60).ToString();
						viewer.UptimeDay = $"{hours.PadLeft(2, '0')}h{minutes.PadLeft(2, '0')}";
					}
				}

				var messages = db.Messages.Where(x => x.CreationDateTime > DateTime.Now.AddMonths(-1)).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count()}).ToList();
				foreach (var message in messages)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == message.Owner).FirstOrDefault();
					if (viewer != null)
					{
						viewer.MessageCountMonth = message.Count;
					}
				}

				messages = db.Messages.Where(x => x.CreationDateTime > DateTime.Now.AddDays(-1)).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count() }).ToList();
				foreach (var message in messages)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == message.Owner).FirstOrDefault();
					if (viewer != null)
					{
						viewer.MessageCountDay = message.Count;
					}
				}

				List<Listing> allTimesBits = await _api.GetBitsLeaderBoard(viewers.Count, BitsLeaderboardPeriodEnum.All);
				foreach (var bits in allTimesBits)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == bits.UserId).FirstOrDefault();
					if (viewer != null)
					{
						viewer.BitsTotal = bits.Score;
					}
				}

				List<Listing> monthlyBits = await _api.GetBitsLeaderBoard(viewers.Count, BitsLeaderboardPeriodEnum.Month);
				foreach (var bits in allTimesBits)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == bits.UserId).FirstOrDefault();
					if (viewer != null)
					{
						viewer.BitsMonth = bits.Score;
					}
				}

				List<Listing> dailyBits = await _api.GetBitsLeaderBoard(viewers.Count, BitsLeaderboardPeriodEnum.Day);
				foreach (var bits in allTimesBits)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == bits.UserId).FirstOrDefault();
					if (viewer != null)
					{
						viewer.BitsMonth = bits.Score;
					}
				}
			}
			return Ok(response);
		}
	}
}
