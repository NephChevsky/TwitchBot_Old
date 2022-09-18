using ApiDll;
using DbDll;
using ModelsDll;
using ModelsDll.Db;

namespace WebApp.Services
{
	public class DailyTasks : BackgroundService
	{
		private readonly ILogger<DailyTasks> _logger;
		private readonly Settings _settings;
		private Api _api;

		public DailyTasks(ILogger<DailyTasks> logger, IConfiguration configuration, Api api)
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

				DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

				using (TwitchDbContext db = new())
				{
					List<Uptime> uptimes = db.Uptimes.Where(x => x.CreationDateTime < now.AddMonths(-1)).ToList();
					db.Uptimes.RemoveRange(uptimes);
					db.SaveChanges();

					List<ChatMessage> messages = db.Messages.Where(x => x.CreationDateTime < now.AddMonths(-1)).ToList();
					db.Messages.RemoveRange(messages);
					db.SaveChanges();
				}

				using (TwitchDbContext db = new())
				{
					List<TwitchLib.Api.Helix.Models.Subscriptions.Subscription> subs = await _api.GetSubscribers();
					foreach (TwitchLib.Api.Helix.Models.Subscriptions.Subscription sub in subs)
					{
						Viewer viewer = await _api.GetOrCreateUserById(sub.UserId);
						db.Viewers.Attach(viewer);
						viewer.IsSub = true;
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
							tmp.EndDateTime = now.AddMonths(1);
							db.Subscriptions.Add(tmp);
						}
					}
					db.SaveChanges();
				}

				await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
	}
}
