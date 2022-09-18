using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
	public class Cheer : IOwnable, IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public int Amount { get; set; }

		// IOwnable
		public string Owner { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }
	}
}
