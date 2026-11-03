using Binance.Net.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace CopyTrader.Worker
{
    public class Trader
    {
        public int Id { get; set; }
        public List<int> GroupIds { get; set; }
        public string? Name { get; set; }
        public int UserApiId { get; set; }
        public string? ApiKey { get; set; }
        public string? SecKey { get; set; }
        public string? ListenKey { get; set; }
        public bool IsActive { get; set; }
        public Balance Balance { get; set; }
        public List<CopyTrader>? CopyTraders { get; set; }
        //TODO: client which contains Client(apikey, apisecret)
        public List<AccountInfo> AccountInfos { get; set; }
        public ClientWebSocket ClientWebSocket { get; set; }
        public HttpClient HttpClient { get; set; }
        public BinanceRestClient Client { get; set; }

        public Trader()
        {
            ClientWebSocket = new ClientWebSocket();
            HttpClient = new HttpClient();
        }

    }

}
