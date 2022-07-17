using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db.Interfaces
{
    public interface IDateTimeTrackable
    {
        public DateTime CreationDateTime { get; set; }
        public DateTime LastModificationDateTime { get; set; }
    }
}
