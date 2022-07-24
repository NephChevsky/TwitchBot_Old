namespace ModelsDll
{
    public class Settings
    {
        public string Channel { get; set; }
        public string TwitchId { get; set; }
        public string ClientId { get; set; }
        public string Secret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string EventSubUrl { get; set; }

        public ChatFunction ChatFunction { get; set; }
        public CheckUptimeFunction CheckUptimeFunction { get; set; }
        public UpdateButtonsFunction UpdateButtonsFunction { get; set; }
        public SpotifyFunction SpotifyFunction { get; set; }
    }

    public class CheckUptimeFunction
    {
        public int Timer { get; set; }
        public bool ComputeUptime { get; set; }
        public bool WelcomeOnFirstJoin { get; set; }
        public bool WelcomeOnReJoin { get; set; }
        public int WelcomeOnJoinTimer { get; set; }
    }

    public class UpdateButtonsFunction
    {
        public int Timer { get; set; }
        public int FollowerGoal { get; set; }
        public List<string> AvailableButtons { get; set; }
    }

    public class ChatFunction
    {
        public bool Timeout { get; set; }
        public bool AddBotSuffixInTitle { get; set; }
        public bool AddCustomCommands { get; set; }
        public bool AntiSpam { get; set; }
    }

    public class SpotifyFunction
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}