using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyTrader.Worker
{
    public class Balance
    {
        public string? Type { get; set; }
        public double MaxValue { get; set; }
        public double CurrentValue { get; set; }
    }
}
