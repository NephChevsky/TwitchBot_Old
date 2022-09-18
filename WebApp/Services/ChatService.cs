using ApiDll;
using ChatDll;
using DbDll;
using Microsoft.AspNetCore.SignalR;
using ModelsDll;
using ModelsDll.Db;
using ModelsDll.DTO;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;

namespace WebApp.Services
{
	public class ChatService : IHostedService
	{
		private readonly ILogger<EventSubService> _logger;
		readonly IHubContext<SignalService> _hub;
		private Settings _settings;
		private BasicChat _chat;
		private Api _api;
		private Dictionary<string, string> BadgesCache;

		public ChatService(ILogger<EventSubService> logger, IConfiguration configuration, IHubContext<SignalService> hub, BasicChat chat, Api api)
		{
			_logger = logger;
			_hub = hub;
			_settings = configuration.GetSection("Settings").Get<Settings>();
			_chat = chat;
			_api = api;
		}

		public Task StartAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation($"Service starting");
			Task.Run(async () => BadgesCache = await _api.GetBadges()).Wait();
			_chat._client.OnMessageReceived += Client_OnMessageReceived;
			_chat._client.OnGiftedSubscription += Client_OnGiftedSubscription;
			_logger.LogInformation($"Service started");
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Service stopping");
			_chat._client.OnMessageReceived -= Client_OnMessageReceived;
			_logger.LogInformation($"Service stopped");
			return Task.CompletedTask;
		}


		private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
		{
			using (TwitchDbContext db = new())
			{
				Viewer dbViewer = _api.GetOrCreateUserByUsername(e.ChatMessage.Username);
				if (dbViewer != null)
				{
					dbViewer.MessageCount++;
					ChatMessage message = new(dbViewer.Id, e.ChatMessage.Message);
					db.Messages.Add(message);
					db.Viewers.Attach(dbViewer);
					db.SaveChanges();
				}
				ChatMessageResponse data = new (e.ChatMessage);
				data.Badges = UpdateBadges(data.Badges);
				_hub.Clients.All.SendAsync("ChatOverlay", data);
			}
		}

		private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
		{
			using (TwitchDbContext db = new TwitchDbContext())
			{
				DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
				Subscription sub = new Subscription();
				sub.Owner = e.GiftedSubscription.MsgParamRecipientId;
				sub.Tier = e.GiftedSubscription.MsgParamSubPlan.ToString();
				sub.IsGift = true;
				sub.GifterId = e.GiftedSubscription.UserId;
				sub.EndDateTime = now.AddMonths(1);
				db.Subscriptions.Add(sub);
				db.SaveChanges();
			}
		}

		private List<string> UpdateBadges(List<string> badges)
		{
			for (int i = 0; i < badges.Count; i++)
			{
				if (BadgesCache.ContainsKey(badges[i]))
				{
					badges[i] = BadgesCache[badges[i]];
				}
				else
				{
					badges.RemoveAt(i);
					_logger.LogInformation($"Badge {badges[i]} not found");
				}
			}
			return badges;
		}
	}
}
