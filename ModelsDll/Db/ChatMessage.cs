using ModelsDll.Interfaces;

namespace ModelsDll.Db
{
	public class ChatMessage : IOwnable, ISoftDeleteable, IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public string Message { get; set; }

		//IOwnable
		public Guid Owner { get; set; }

		// ISoftDeleteable
		public bool Deleted { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }

		public ChatMessage (Guid owner, string message)
		{
			Message = message;
			Owner = owner;
		}
	}
}
