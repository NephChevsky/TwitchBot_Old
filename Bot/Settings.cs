using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot
{
    public class Settings
    {
        public string Channel { get; set; }
        public string TwitchId { get; set; }
        public string ClientId { get; set; }
        public string Secret { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TwitchAppScope { get; set; }
        public string ChatScope { get; set; }

        public BotFunction BotFunction { get; set; }
        public CheckUptimeFunction CheckUptimeFunction { get; set; }
        public UpdateFilesFunction UpdateFilesFunction { get; set; }
        public OBSFunction OBSFunction { get; set; }
    }

    public class CheckUptimeFunction
    {
        public int Timer { get; set; }
        public bool ComputeUptime { get; set; }
        public bool WelcomeOnJoin { get; set; }
        public int WelcomeOnJoinTimer { get; set; }
    }

    public class UpdateFilesFunction
    {
        public int Timer { get; set; }
        public string OutputFolder { get; set; }
    }

    public class BotFunction
    {
        public bool Timeout { get; set; }
        public bool AddBotSuffixInTitle { get; set; }
    }

    public class OBSFunction
    {
        public string Server { get; set; }
        public string Password { get; set; }
    }
}
