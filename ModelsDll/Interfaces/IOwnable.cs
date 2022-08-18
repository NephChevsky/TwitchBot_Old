using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelsDll.Interfaces
{
	public interface IOwnable
	{
		public string Owner { get; set; }
	}
}
