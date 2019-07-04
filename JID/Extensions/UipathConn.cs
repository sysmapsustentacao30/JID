using JID.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JID.Extensions
{
    public class UipathConn : IUipathConn
    {
        public List<IccidModel> InitVerifyIccid(List<IccidModel> iccids)
        {

            return iccids;
        }
    }

    public interface IUipathConn
    {
        List<IccidModel> InitVerifyIccid(List<IccidModel> iccids);
    }
}
