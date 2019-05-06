using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sysmap.Sustentacao.JID.Models
{
    public class IssuePrudential
    {
        public int ID { get; set; }

        public string Summary { get; set; }

        public string IssueType { get; set; }

        public string ServiceNow { get; set; }

        public int WexId { get; set; }

        public string Status { get; set; }
    }
}
