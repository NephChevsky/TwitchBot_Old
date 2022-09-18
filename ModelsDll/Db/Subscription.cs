using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
	public class Subscription : IOwnable, IDateTimeTrackable, ISoftDeleteable
	{
		public Guid Id { get; set; }
		public string Tier { get; set; }
		public bool IsGift { get; set; }
		public string GifterId { get; set; }
		public DateTime EndDateTime { get; set; }

		// IOwnable
		public string Owner { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }

		// ISoftDeleteable
		public bool Deleted { get; set; }
	}
}
