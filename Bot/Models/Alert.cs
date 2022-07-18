using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Models
{
    public class Alert
    {
        public string Type { get; set; }
        public string Username { get; set; }
        public double Value { get; set; }

        public Alert(string type, string username, double value = -1)
        {
            Type = type;
            Username = username;
            Value = value;
        }
    }
}

