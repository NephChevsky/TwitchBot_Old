using ApiDll;
using DbDll;
using ModelsDll;
using ModelsDll.Db;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Bits;
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

				DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

				using (TwitchDbContext db = new())
				{
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

				/* wait for fix in TwitchLib api
				using (TwitchDbContext db = new())
				{
					List<ChannelVIPsResponseModel> vips = await _api.GetVIPs();
					foreach (var vip in vips)
					{
						Viewer viewer = _api.GetOrCreateUserById(vip.UserId);
						db.Viewers.Attach(viewer);
						viewer.IsVIP = true;
					}
					db.SaveChanges();
				}*/

				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
		}
	}
}
