using ModelsDll.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelsDll.Db
{
	public class Command : ISoftDeleteable, IDateTimeTrackable, IOwnable
	{
		public string Name { get; set; }
		public string Message { get; set; }
		public int Value { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }

		// IOwnable
		public Guid Owner { get; set; }

		// ISoftDeleteable
		public bool Deleted { get; set; }
	}
}
