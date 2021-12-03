using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Util;

namespace GannyBot.Trade
{
    class Order
    {
        public string WalletAddress { get; set; }
        public string InputAddress { get; set; }
        public string OutputAddress { get; set; }
        public BigDecimal Quantity { get; set; }
        public decimal Slippage { get; set; }
        public int GasPrice { get; set; }
    }
}
