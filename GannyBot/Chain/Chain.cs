using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GannyBot.Chain
{
    enum chain
    {
        binance_smart_chain,
        binance_smart_chain_test
    }
    internal class Chain
    {
        public string Name { get; set; }
        public string RPCUrl { get; set; }
        public Int32 ChainID { get; set; }
        public string ExplorerUrl { get; set; }
        public Token Token { get; set; }
        public Token USDT { get; set; }
    }
}
