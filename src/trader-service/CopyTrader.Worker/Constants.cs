using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyTrader.Worker
{
    public static class Constants
    {      
        public static string DB_CONNECTION_STRING =>
            Environment.GetEnvironmentVariable("COPYTRADER_DB_CONNECTION_STRING")
            ?? throw new InvalidOperationException("COPYTRADER_DB_CONNECTION_STRING is not set. See .env.example.");
        public const string BASE_API_URL = "https://fapi.binance.com"; //"https://testnet.binancefuture.com"
        public const string BASE_WEB_SOCKET_URL = "wss://fstream.binance.com"; //"wss://stream.binancefuture.com"

    }
}
