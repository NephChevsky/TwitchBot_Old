using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAReader.Models
{
	public class Entry
	{
		public string Name { get; set; }
		public bool IsEnabled { get; set; } = true;
		public bool IsToggle { get; set; } = false;
		public bool IsSettable { get; set; } = false;
		public bool IsActivable { get; set; } = false;
		public int SettableMin { get; set; }
		public int SettableMax { get; set; }
		public List<Entry> Childs { get; set; }
		public List<string> Values { get; set; }
		public string Default { get; set; }
	}
}
