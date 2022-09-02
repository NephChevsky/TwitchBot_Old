using ApiDll;
using DbDll;
using Microsoft.AspNetCore.Mvc;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;

namespace WebApp.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class StatisticsController : ControllerBase
	{
		private readonly ILogger<StatisticsController> _logger;
		private Settings _settings;

		public StatisticsController(ILogger<StatisticsController> logger, IConfiguration configuration, Api api)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
		}

		[HttpGet]
		public ActionResult<List<UserStatsResponse>> Get()
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

				DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
				DateTime limit = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
				var uptimes = db.Uptimes.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Sum) }).ToList();
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

				limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
				uptimes = db.Uptimes.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Sum) }).ToList();
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

				limit = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
				var messages = db.Messages.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count()}).ToList();
				foreach (var message in messages)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == message.Owner).FirstOrDefault();
					if (viewer != null)
					{
						viewer.MessageCountMonth = message.Count;
					}
				}

				limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
				messages = db.Messages.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count() }).ToList();
				foreach (var message in messages)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == message.Owner).FirstOrDefault();
					if (viewer != null)
					{
						viewer.MessageCountDay = message.Count;
					}
				}

				var cheerers = db.Cheers.GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Amount) }).ToList();
				foreach (var cheer in cheerers)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == cheer.Owner).FirstOrDefault();
					if (viewer != null)
					{
						viewer.BitsTotal = cheer.Sum;
					}
				}

				limit = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
				cheerers = db.Cheers.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Amount) }).ToList();
				foreach (var cheer in cheerers)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == cheer.Owner).FirstOrDefault();
					if (viewer != null)
					{
						viewer.BitsMonth = cheer.Sum;
					}
				}

				limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
				cheerers = db.Cheers.Where(x => x.CreationDateTime >= limit).GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Sum = g.Sum(x => x.Amount) }).ToList();
				foreach (var cheer in cheerers)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == cheer.Owner).FirstOrDefault();
					if (viewer != null)
					{
						viewer.BitsDay = cheer.Sum;
					}
				}

				var subs = db.Subscriptions.GroupBy(x => x.Owner).Select(g => new { Owner = g.Key, Count = g.Count() }).ToList();
				foreach (var sub in subs)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == sub.Owner).FirstOrDefault();
					if (viewer != null)
					{
						viewer.Subs = sub.Count;
					}
				}

				var subgifts = db.Subscriptions.Where(x => x.IsGift == true).GroupBy(x => x.GifterId).Select(g => new { GifterId = g.Key, Count = g.Count() }).ToList();
				foreach (var subgift in subgifts)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == subgift.GifterId).FirstOrDefault();
					if (viewer != null)
					{
						viewer.SubGiftsTotal = subgift.Count;
					}
				}

				limit = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
				subgifts = db.Subscriptions.Where(x => x.IsGift == true && x.CreationDateTime >= limit).GroupBy(x => x.GifterId).Select(g => new { GifterId = g.Key, Count = g.Count() }).ToList();
				foreach (var subgift in subgifts)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == subgift.GifterId).FirstOrDefault();
					if (viewer != null)
					{
						viewer.SubGiftsMonth = subgift.Count;
					}
				}

				limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
				subgifts = db.Subscriptions.Where(x => x.IsGift == true && x.CreationDateTime >= limit).GroupBy(x => x.GifterId).Select(g => new { GifterId = g.Key, Count = g.Count() }).ToList();
				foreach (var subgift in subgifts)
				{
					UserStatsResponse viewer = response.Where(x => x.Id == subgift.GifterId).FirstOrDefault();
					if (viewer != null)
					{
						viewer.SubGiftsDay = subgift.Count;
					}
				}
			}
			return Ok(response);
		}
	}
}
