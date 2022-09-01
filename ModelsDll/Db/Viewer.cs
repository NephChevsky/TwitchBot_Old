using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
    public class Viewer : ISoftDeleteable, IDateTimeTrackable
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
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

        public Viewer(string id, string username, string displayname)
        {
            Id = id;
            Username = username;
            DisplayName = displayname;
            LastViewedDateTime = CreationDateTime;
            Uptime = 0;
            Seen = 0;
            IsBot = false;
        }
    }
}
