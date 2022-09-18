using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
	public class Command : ISoftDeleteable, IDateTimeTrackable, IOwnable
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Message { get; set; }
		public int Value { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }

		// IOwnable
		public string Owner { get; set; }

		// ISoftDeleteable
		public bool Deleted { get; set; }
	}
}
