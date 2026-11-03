using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyTrader.Worker
{
    public class AccountInfo
    {
        public string Symbol { get; set; }
        public int Leverage { get; set; }
        public FuturesMarginType MarginType { get; set; }
    }
}
