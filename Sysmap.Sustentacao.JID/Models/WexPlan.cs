using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sysmap.Sustentacao.JID.Models
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
}
