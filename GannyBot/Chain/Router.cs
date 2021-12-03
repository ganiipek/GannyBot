using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GannyBot.Chain
{
    enum router
    {
        PancakeSwapV2
    }
    internal class Router
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string ABI { get; set; }
    }
}
