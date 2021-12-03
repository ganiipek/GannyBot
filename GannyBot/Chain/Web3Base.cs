using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace GannyBot.Chain
{
    internal class Web3Base
    {
        public Web3 Web3 { get; set; }
        public Account Account { get; set; }
    }
}
