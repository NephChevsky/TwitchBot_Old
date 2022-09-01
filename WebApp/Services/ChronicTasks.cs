using ApiDll;
using DbDll;
using ModelsDll;
using ModelsDll.Db;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Bits;

namespace WebApp.Services
{
	public class ChronicTasks : BackgroundService
	{
		private readonly ILogger<ChronicTasks> _logger;
		private readonly Settings _settings;
		private Api _api;

		public ChronicTasks(ILogger<ChronicTasks> logger, IConfiguration configuration, Api api)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_api = api;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

				using (TwitchDbContext db = new())
				{
					List<TwitchLib.Api.Helix.Models.Subscriptions.Subscription> subs = await _api.GetSubscribers();
					DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
					foreach (TwitchLib.Api.Helix.Models.Subscriptions.Subscription sub in subs)
					{
						Subscription tmp = db.Subscriptions.Where(x => x.Owner == sub.UserId && x.CreationDateTime >= now.AddMonths(-1)).FirstOrDefault();
						if (tmp == null)
						{
							tmp = new();
							tmp.Owner = sub.UserId;
							tmp.Tier = sub.Tier;
							tmp.IsGift = sub.IsGift;
							tmp.GifterId = sub.GiftertId;
							tmp.CreationDateTime = now;
							tmp.LastModificationDateTime = now;
							db.Subscriptions.Add(tmp);
						}
					}
					db.SaveChanges();

					Cheer lastCheer = db.Cheers.OrderByDescending(x => x.CreationDateTime).FirstOrDefault();
					
					if (lastCheer != null)
					{
						List<Listing> cheers = await _api.GetBitsLeaderBoard(10, BitsLeaderboardPeriodEnum.Day);
						foreach (Listing cheer in cheers)
						{
							DateTime limit = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
							limit = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
							int currentTotal = db.Cheers.Where(x => x.Owner == cheer.UserId && x.CreationDateTime >= limit).Sum(x => x.Amount);
							if (currentTotal < cheer.Score)
							{
								Cheer tmp = new();
								tmp.Owner = cheer.UserId;
								tmp.Amount = cheer.Score - currentTotal;
								db.Cheers.Add(tmp);
							}
						}
					}
					else
					{
						List<Listing> cheers = await _api.GetBitsLeaderBoard(10, BitsLeaderboardPeriodEnum.All);
						foreach (Listing cheer in cheers)
						{
							Cheer tmp = new();
							tmp.Owner = cheer.UserId;
							tmp.Amount = cheer.Score;
							db.Cheers.Add(tmp);
						}
					}
					db.SaveChanges();
				}

				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
		}
	}
}
