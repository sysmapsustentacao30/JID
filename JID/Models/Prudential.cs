using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JID.Models
{
    public class WexPlan
    {
        public int IdWex { get; set; }

        public string OrdemServico { get; set; }

        public string Responsavel { get; set; }

        public string Documento { get; set; }

        public string Status { get; set; }

        public DateTime Data { get; set; }
    }

    public class IssuePrudent
    {
        public int ID { get; set; }

        public string Summary { get; set; }

        public string IssueType { get; set; }

        public string ServiceNow { get; set; }

        public int WexId { get; set; }

        public string Status { get; set; }
    }

    public class StatusJira
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
