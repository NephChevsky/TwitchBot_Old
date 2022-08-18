using ModelsDll.Db;

namespace ModelsDll.DTO
{
	public class UserStatsResponse
	{
		public int Position { get; set; }
		public Guid Id { get; set; }
		public string TwitchId { get; set; }
		public string Name { get; set; }
		public int Seen { get; set; }
		public string Presence { get; set; }
		public string UptimeTotal { get; set; }
		public string UptimeMonth { get; set; }
		public string UptimeDay { get; set; }
		public int MessageCountTotal { get; set; }
		public int MessageCountMonth { get; set; }
		public int MessageCountDay { get; set; }
		public int BitsTotal { get; set; }
		public int BitsMonth { get; set; }
		public int BitsDay { get; set; }

		public UserStatsResponse(Viewer viewer)
		{
			Id = viewer.Id;
			TwitchId = viewer.TwitchId;
			Name = viewer.DisplayName;
			Seen = viewer.Seen;
			Presence = $"{viewer.CreationDateTime.ToString("dd/MM/yyyy")} - {viewer.LastViewedDateTime.ToString("dd/MM/yyyy")}";
			string hours = Math.Floor( (double) viewer.Uptime / 3600).ToString();
			string minutes = Math.Floor((double) (viewer.Uptime % 3600) / 60).ToString();
			UptimeTotal = $"{hours.PadLeft(2, '0')}h{minutes.PadLeft(2, '0')}";
			MessageCountTotal = viewer.MessageCount;
		}
	}
}
