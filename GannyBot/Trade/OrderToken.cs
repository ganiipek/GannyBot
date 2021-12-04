using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Util;

namespace GannyBot.Trade
{
    public class OrderToken : Chain.Token
    {
        public BigDecimal Approved { get; set; }
        

        public OrderToken()
        {
            Approved = 0;
        }
    }
}
