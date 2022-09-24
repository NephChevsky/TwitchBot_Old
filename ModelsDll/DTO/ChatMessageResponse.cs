namespace ModelsDll.DTO
{
	public class ChatMessageResponse
	{
		public string Username { get; set; }
		public string Message { get; set; }
		public string UserColor { get; set; }
		public List<string> Badges { get; set; }
		public List<Emote> Emotes { get; set; }
		

		public ChatMessageResponse(TwitchLib.Client.Models.ChatMessage x)
		{
			Username = x.DisplayName;
			Message = x.Message;
			UserColor = x.ColorHex;
			Emotes = x.EmoteSet.Emotes.Select(x => new Emote(x)).ToList();
			Badges = x.Badges.Select(x => x.Key).ToList();
		}
	}

	public class Emote
	{
		public int StartIndex { get; set; }
		public int EndIndex { get; set; }
		public string ImageUrl { get; set; }

		public Emote(TwitchLib.Client.Models.Emote x)
		{
			StartIndex = x.StartIndex;
			EndIndex = x.EndIndex;
			ImageUrl = x.ImageUrl;
		}

		public Emote(TwitchLib.EventSub.Webhooks.Core.Models.Subscriptions.SubscriptionMessageEmote x)
		{
			StartIndex = x.Begin;
			EndIndex = x.End;
			ImageUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{x.Id}/default/light/1.0";
		}
	}
}
