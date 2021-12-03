using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Util;

namespace GannyBot.Trade
{
    class LimitOrder : Order
    {
        public int ID { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public BigDecimal Price { get; set; }
        public DateTime Date { get; set; }
        public bool Process { get; set; }
        public int ErrorCount { get; set; }
    }

    class LimitOrderList
    {
        public readonly List<LimitOrder> orders;

        public LimitOrderList()
        {
            orders = new List<LimitOrder>();
        }

        public void Add(LimitOrder limitOrder)
        {
            limitOrder.ID = orders.Count > 0 ? orders.Max(x => x.ID) + 1 : 1;
            limitOrder.Date = DateTime.Now;
            limitOrder.Process = false;
            limitOrder.ErrorCount = 5;
            orders.Add(limitOrder);
        }

        public void Remove(LimitOrder limitOrder)
        {
            orders.Remove(limitOrder);
            // orders.ForEach((x) => { if (x.ID > limitOrder.ID) x.ID = x.ID - 1; });
        }

        public int Count()
        {
            return orders.Count;
        }
    }
}
