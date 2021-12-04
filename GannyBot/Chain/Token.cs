using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GannyBot.Chain
{
    public class Token
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Address { get; set; }
        public int Decimals { get; set; }
        public string Abi { get; set; }
        public BigDecimal Price { get; set; }
        public BigDecimal Balance { get; set; }

        public Token()
        {
            Price = 0;
            Balance = 0;
        }
    }
}
