using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
	public class SongToAdd : IDateTimeTrackable, IOwnable
	{
		public Guid Id { get; set; }
		public string Uri { get; set; }
		public string RewardId { get; set; }
		public string EventId { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }

		// IOwnable
		public string Owner { get; set; }

		public SongToAdd()
		{

		}

		public SongToAdd(string userId, string songUri, string rewardId, string eventId)
		{
			Owner = userId;
			Uri = songUri;
			RewardId = rewardId;
			EventId = eventId;
		}

		public SongToAdd(string userId, string uri)
		{
			Owner = userId;
			Uri = uri;
		}
	}
}