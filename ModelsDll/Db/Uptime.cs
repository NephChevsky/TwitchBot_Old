using ModelsDll.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelsDll.Db
{
	public class Uptime : IOwnable, ISoftDeleteable, IDateTimeTrackable
	{
		public Guid Id { get; set; }
		public int Sum { get; set; }

		//IOwnable
		public Guid Owner { get; set; }

		// ISoftDeleteable
		public bool Deleted { get; set; }

		// IDateTimeTrackable
		public DateTime CreationDateTime { get; set; }
		public DateTime LastModificationDateTime { get; set; }
	}
}
