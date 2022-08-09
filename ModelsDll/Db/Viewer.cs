using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
    public class Viewer : ISoftDeleteable, IDateTimeTrackable
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string TwitchId { get; set; }
        public bool IsBot { get; set; }
        public int Seen { get; set; }
        public DateTime LastViewedDateTime { get; set; }
        public long Uptime { get; set; }
        public int MessageCount { get; set; }

        // IDateTimeTrackable
        public DateTime CreationDateTime { get; set; }
        public DateTime LastModificationDateTime { get; set; }

        // ISoftDeleteable
        public bool Deleted { get; set; }

        public Viewer()
        {

        }

        public Viewer(string username, string displayname, string id)
        {
            Username = username;
            DisplayName = displayname;
            TwitchId = id;
            CreationDateTime = DateTime.Now;
            LastViewedDateTime = CreationDateTime;
            Uptime = 0;
            Seen = 1;
            IsBot = false;
        }
    }
}
