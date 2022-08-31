using ModelsDll.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
