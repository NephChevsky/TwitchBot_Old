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

		public DailyTasks(ILogger<DailyTasks> logger, IConfiguration configuration)
		{
			_logger = logger;
			_settings = configuration.GetSection("Settings").Get<Settings>();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
				
				using (TwitchDbContext db = new())
				{
					DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Time"));

					List<Uptime> uptimes = db.Uptimes.Where(x => x.CreationDateTime < now.AddMonths(-1)).ToList();
					db.Uptimes.RemoveRange(uptimes);
					db.SaveChanges();

					List<ChatMessage> messages = db.Messages.Where(x => x.CreationDateTime < now.AddMonths(-1)).ToList();
					db.Messages.RemoveRange(messages);
					db.SaveChanges();
				}

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
	}
}
