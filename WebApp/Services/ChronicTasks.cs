using ApiDll;
using DbDll;
using ModelsDll;
using ModelsDll.Db;

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
					DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Time"));
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
				}

				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
		}
	}
}
