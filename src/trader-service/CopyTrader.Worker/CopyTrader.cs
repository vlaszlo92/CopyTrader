using Binance.Net.Clients;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace CopyTrader.Worker
{
    public class CopyTrader
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int UserApiId { get; set; }
        public string? ApiKey { get; set; }
        public string? SecKey { get; set; }
        public Balance Balance { get; set; }
        public decimal QuantityRatio { get; set; }
        public int TraderGroupId { get; set; }

        public bool IsActive { get; set; }
        public BlockingCollection<TransactionData> TraderTransactions;
        public List<AccountInfo> AccountInfos { get; set; }

        public BinanceRestClient Client { get; set; }
        public CopyTrader() {
            TraderTransactions = new BlockingCollection<TransactionData>();
        }
        public CopyTrader(int id)
        {
            Id = id;
            TraderTransactions = new BlockingCollection<TransactionData>();
        }
        
        public void AddTransaction(string transaction, int traderId)
        {
            TransactionData transactionData = new TransactionData { TraderId = traderId, Transaction = transaction };            
            TraderTransactions.Add(transactionData);
        }
    }
}
