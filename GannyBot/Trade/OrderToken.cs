using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Util;

namespace GannyBot.Trade
{
    internal class OrderToken: Chain.Token
    {
        public BigDecimal Balance { get; set; }
        public BigDecimal Approved { get; set; }
        public BigDecimal Price { get; set; }

        public OrderToken()
        {
            Balance = 0;
            Approved = 0;
            Price = 0;
        }
    }
}
