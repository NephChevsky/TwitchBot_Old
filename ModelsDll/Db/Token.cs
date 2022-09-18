using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
	public class Token : IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Value { get; set; }
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }
	}
}
