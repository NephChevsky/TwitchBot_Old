﻿using ApiDll;
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
		private Dictionary<string, DateTime> AntiSpamTimer = new Dictionary<string, DateTime>();

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
			_chat._client.OnChatCommandReceived += Client_OnChatCommandReceived;
			_logger.LogInformation($"Service started");
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"Service stopping");
			_chat._client.OnMessageReceived -= Client_OnMessageReceived;
			_chat._client.OnGiftedSubscription -= Client_OnGiftedSubscription;
			_chat._client.OnChatCommandReceived -= Client_OnChatCommandReceived;
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

		private async void Client_OnChatCommandReceived(object send, OnChatCommandReceivedArgs e)
		{
			DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));

			if (!AntiSpamTimer.ContainsKey(e.Command.CommandText.ToLower()) || (AntiSpamTimer.ContainsKey(e.Command.CommandText.ToLower()) && AntiSpamTimer[e.Command.CommandText.ToLower()].AddSeconds(60) < now))
			{
				bool updateTimer = false;
				if (string.Equals(e.Command.CommandText, "bot", StringComparison.InvariantCultureIgnoreCase))
				{
					_chat.SendMessage("Commandes disponibles: https://bit.ly/3f30iXi");
					updateTimer = true;
				}
				else if (string.Equals(e.Command.CommandText, "stats", StringComparison.InvariantCultureIgnoreCase))
				{
					_chat.SendMessage("Statistiques des viewers: https://bit.ly/3LqsFe8");
					updateTimer = true;
				}
				else if (_settings.CheckUptimeFunction.ComputeUptime && string.Equals(e.Command.CommandText, "uptime", StringComparison.InvariantCultureIgnoreCase))
				{
					using (TwitchDbContext db = new())
					{
						string username = e.Command.ArgumentsAsList.Count > 0 ? e.Command.ArgumentsAsList[0] : e.Command.ChatMessage.Username;
						Viewer viewer = _api.GetOrCreateUserByUsername(username);
						if (viewer != null)
						{
							int hours = (int)Math.Floor((decimal)viewer.Uptime / 3600);
							int minutes = (int)Math.Floor((decimal)(viewer.Uptime % 3600) / 60);
							_chat.SendMessage($"@{viewer.DisplayName} a regardé le stream pendant {hours} heures et {minutes.ToString().PadLeft(2, '0')} minutes. Il est passé {viewer.Seen} fois sur le stream.");
							updateTimer = true;
						}
						else
						{
							_chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : je connais pas ce con");
						}
					}
				}
				else if ((string.Equals(e.Command.CommandText, "timeout", StringComparison.InvariantCultureIgnoreCase) || string.Equals(e.Command.CommandText, "to", StringComparison.InvariantCultureIgnoreCase)))
				{
					if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
					{
						if (e.Command.ArgumentsAsList.Count > 0)
						{
							string username = e.Command.ArgumentsAsList[0].Replace("@", "");
							if (e.Command.ArgumentsAsList.Count > 1)
							{
								_api.BanUser(username, int.Parse(e.Command.ArgumentsAsList[1]));
							}
							else
							{
								_api.BanUser(username);
							}
						}
						else if (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator)
						{
							_chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : T'es bourré?");
						}
					}
					else
					{
						_chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : Idiot!");
						_api.BanUser(e.Command.ChatMessage.Username);
					}
				}
				else if (string.Equals(e.Command.CommandText, "so", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
				{
					_chat.SendMessage($"Saviez vous que @{e.Command.ArgumentsAsList[0]} stream? Ca claque des culs alors allez lui lacher votre meilleur follow sur only f... Pardon, c'est sur Twitch: https://www.twitch.tv/{e.Command.ArgumentsAsList[0]} <3 <3 <3");
					_chat.SendMessage($"https://www.twitch.tv/{e.Command.ArgumentsAsList[0]}");
					_chat.SendMessage($"https://www.twitch.tv/{e.Command.ArgumentsAsList[0]}");
					_chat.SendMessage($"https://www.twitch.tv/{e.Command.ArgumentsAsList[0]}");
				}
				else if (string.Equals(e.Command.CommandText, "settitle", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
				{
					string title = e.Command.ArgumentsAsString;
					if (_settings.ChatFunction.AddBotSuffixInTitle && !title.Contains("!bot", StringComparison.InvariantCultureIgnoreCase))
					{
						title += " !bot";
					}
					await _api.ModifyChannelInformation(title, null);
					_chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : le titre du stream a été changé en: {title}");
				}
				else if (string.Equals(e.Command.CommandText, "setgame", StringComparison.InvariantCultureIgnoreCase) && (e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator))
				{
					ModifyChannelInformationResponse response = await _api.ModifyChannelInformation(null, e.Command.ArgumentsAsString);
					if (response != null)
					{
						_chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : le jeu du stream a été changé en: {response.Game}");
					}
					else
					{
						_chat.SendMessage($"{e.Command.ChatMessage.DisplayName} : je connais pas ton jeu de merde");
					}
				}

				if (updateTimer)
				{
					if (AntiSpamTimer.ContainsKey(e.Command.CommandText.ToLower()))
					{
						AntiSpamTimer[e.Command.CommandText.ToLower()] = now;
					}
					else
					{
						AntiSpamTimer.Add(e.Command.CommandText.ToLower(), now);
					}
				}
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
