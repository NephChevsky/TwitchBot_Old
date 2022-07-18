using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DTO
{
    public class AccessTokenQuery
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string grant_type { get; set; }

        public AccessTokenQuery(string client_id, string client_secret, string grant_type)
        {
            this.client_id = client_id;
            this.client_secret = client_secret;
            this.grant_type = grant_type;
        }
    }
}
