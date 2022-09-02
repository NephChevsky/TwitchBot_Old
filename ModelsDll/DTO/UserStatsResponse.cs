using ModelsDll.Db;

namespace ModelsDll.DTO
{
	public class UserStatsResponse
	{
		public int Position { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }
		public int Seen { get; set; } = 0;
		public string Presence { get; set; } = "";
		public string UptimeTotal { get; set; } = "00h00";
		public string UptimeMonth { get; set; } = "00h00";
		public string UptimeDay { get; set; } = "00h00";
		public int MessageCountTotal { get; set; } = 0;
		public int MessageCountMonth { get; set; } = 0;
		public int MessageCountDay { get; set; } = 0;
		public int BitsTotal { get; set; } = 0;
		public int BitsMonth { get; set; } = 0;
		public int BitsDay { get; set; } = 0;
		public int Subs { get; set; } = 0;
		public int SubGiftsTotal { get; set; } = 0;
		public int SubGiftsMonth { get; set; } = 0;
		public int SubGiftsDay { get; set; } = 0;
		public bool IsFollower { get; set; } = false;
		public bool IsSub { get; set; } = false;
		public bool IsMod { get; set; } = false;
		public bool IsVIP { get; set; } = false;
		public DateTime FirstFollowDateTime { get; set; }

		public UserStatsResponse(Viewer viewer)
		{
			Id = viewer.Id;
			Name = viewer.DisplayName;
			Seen = viewer.Seen;
			Presence = $"{viewer.CreationDateTime.ToString("dd/MM/yyyy")} - {viewer.LastViewedDateTime.ToString("dd/MM/yyyy")}";
			string hours = Math.Floor( (double) viewer.Uptime / 3600).ToString();
			string minutes = Math.Floor((double) (viewer.Uptime % 3600) / 60).ToString();
			UptimeTotal = $"{hours.PadLeft(2, '0')}h{minutes.PadLeft(2, '0')}";
			MessageCountTotal = viewer.MessageCount;
			IsMod = viewer.IsMod;
			IsVIP = viewer.IsVIP;
			IsSub = viewer.IsSub;
			IsFollower = viewer.IsFollower;
			FirstFollowDateTime = viewer.FirstFollowDateTime;
		}
	}
}
