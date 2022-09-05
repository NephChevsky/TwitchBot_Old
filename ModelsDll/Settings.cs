namespace ModelsDll
{
    public class Settings
    {
        public string Streamer { get; set; }
        public string Bot { get; set; }
        public string StreamerTwitchId { get; set; }
        public string BotTwitchId { get; set; }
        public string ClientId { get; set; }
        public string Secret { get; set; }
        public string EventSubUrl { get; set; }
        public string SignalRUrl { get; set; }

        public ChatFunction ChatFunction { get; set; }
        public CheckUptimeFunction CheckUptimeFunction { get; set; }
        public UpdateButtonsFunction UpdateButtonsFunction { get; set; }
        public SpotifyFunction SpotifyFunction { get; set; }
        public CheckChannelRewardsFunction CheckChannelRewardsFunction { get; set; }
        public DiscordFunction DiscordFunction { get; set; }
        public ObsFunction ObsFunction { get; set; }

        public Tags Tags { get; set; }
    }

    public class CheckUptimeFunction
    {
        public int Timer { get; set; }
        public bool ComputeUptime { get; set; }
        public bool WelcomeOnFirstJoin { get; set; }
        public bool WelcomeOnReJoin { get; set; }
        public bool GenericWelcome { get; set; }
        public int WelcomeOnJoinTimer { get; set; }
    }

    public class UpdateButtonsFunction
    {
        public int Timer { get; set; }
        public int FollowerGoal { get; set; }
        public int SubscriptionGoal { get; set; }
        public List<string> AvailableButtons { get; set; }
    }

    public class ChatFunction
    {
        public bool AddBotSuffixInTitle { get; set; }
        public bool AddCustomCommands { get; set; }
        public bool AntiSpam { get; set; }
        public int MaxVIPs { get; set; }
    }

    public class SpotifyFunction
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Playlist { get; set; }
        public string LocalCallbackUrl { get; set; }
    }

    public class CheckChannelRewardsFunction
    {
        public int Timer { get; set; }
	}

    public class DiscordFunction
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public ulong NewsChannelId { get; set; }
	}

    public class ObsFunction
    {
        public string Url { get; set; }
        public string Password { get; set; }
    }

    public class Tags
    {
        public List<string> Fps { get; set; }
	}
}