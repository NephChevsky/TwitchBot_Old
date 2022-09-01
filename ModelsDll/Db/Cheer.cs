using ModelsDll.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
