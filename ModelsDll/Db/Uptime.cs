using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
	public class Uptime : IOwnable, IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public int Sum { get; set; }

		//IOwnable
		public string Owner { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }
	}
}
