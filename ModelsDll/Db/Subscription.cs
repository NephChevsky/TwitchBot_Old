using ModelsDll.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelsDll.Db
{
	public class Subscription : IOwnable, IDateTimeTrackable, ISoftDeleteable
	{
		public Guid Id { get; set; }
		public bool IsGift { get; set; }
		public string Tier { get; set; }

		// IOwnable
		public string Owner { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }

		// ISoftDeleteable
		public bool Deleted { get; set; }
	}
}
