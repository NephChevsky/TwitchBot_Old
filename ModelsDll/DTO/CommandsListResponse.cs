using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelsDll.DTO
{
	public class CommandsListResponse
	{
		public bool ComputeUpTime { get; set; }
		public bool Timeout { get; set; }
		public bool AddCustomCommands { get; set; }
		public List<KeyValuePair<string, string>> CustomCommands { get; set; }
	}
}
