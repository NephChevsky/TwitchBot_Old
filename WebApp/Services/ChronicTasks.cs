using ApiDll;
using DbDll;
using ModelsDll;
using ModelsDll.Db;
using TwitchLib.Api.Helix.Models.Channels.GetChannelVIPs;
using TwitchLib.Api.Helix.Models.Moderation.GetModerators;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;

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

				DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"));

				using (TwitchDbContext db = new())
				{
					List<Follow> followers = await _api.GetFollowers();
					foreach (var follow in followers)
					{
						Viewer viewer = await _api.GetOrCreateUserById(follow.FromUserId);
						db.Viewers.Attach(viewer);
						viewer.IsFollower = true;
						if (viewer.FirstFollowDateTime == DateTime.MinValue)
						{
							viewer.FirstFollowDateTime = follow.FollowedAt;
						}
					}
					db.SaveChanges();
				}

				using (TwitchDbContext db = new())
				{
					List<Moderator> mods = await _api.GetModerators();
					foreach (var mod in mods)
					{
						Viewer viewer = await _api.GetOrCreateUserById(mod.UserId);
						db.Viewers.Attach(viewer);
						viewer.IsMod = true;
					}
					db.SaveChanges();
				}

				using (TwitchDbContext db = new())
				{
					List<ChannelVIPsResponseModel> vips = await _api.GetVIPs();
					foreach (var vip in vips)
					{
						Viewer viewer = await _api.GetOrCreateUserById(vip.UserId);
						db.Viewers.Attach(viewer);
						viewer.IsVIP = true;
					}
					db.SaveChanges();
				}

				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
		}
	}
}
