using System;
using Newtonsoft.Json;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using Serilog;
using Serilog.Context;
using Serilog.Core;

namespace CopyTrader.Worker
{
    public class MessageHandler
    {
        private static Logger logger;
        public static async Task ProcessMessageAsync(string message, CopyTrader ct, Trader t)
        {
            logger = Program.Service1.Logger;
            var data = JsonConvert.DeserializeObject<MessageEventData>(message);

            switch (data?.e)
            {
                case "ACCOUNT_UPDATE":
                    await ProcessAccountUpdateAsync(message, ct, t);
                    break;
                case "ACCOUNT_CONFIG_UPDATE":
                    await ProcessAccountConfigUpdateAsync(message, ct);
                    break;
                case "ORDER_TRADE_UPDATE":
                    await ProcessOrderTradeUpdateAsync(message, ct, t);
                    break;
                default:
                    break;
            }
        }

        public static async Task ProcessAccountUpdateAsync(string pData, CopyTrader ct, Trader t)
        {
            var data = JsonConvert.DeserializeObject<AccountUpdateEventData>(pData);

            if (data?.A?.M == "MARGIN_TYPE_CHANGE")
            {
                foreach (var (lSymbol, lMarginType) in from position in data.A.P
                                                       let lSymbol = position.S ?? ""
                                                       let lMarginType = position.MT ?? ""
                                                       select (lSymbol, lMarginType))
                {
                    await MarginTypeChangedAsync(ct.Client, lSymbol, lMarginType);
                }
            }
            var symbolinfo = data.A.P.FirstOrDefault();
            if (symbolinfo == null)
            {
                //TODO: ERROR LOG
                return;
            }

            var accinfo = ct.AccountInfos.FirstOrDefault(ai => ai.Symbol == symbolinfo.S);
            if (accinfo == null)
            {
                await UpdateAccountInfo(ct, t, symbolinfo.S);
            }

            var margintype = ct.AccountInfos.FirstOrDefault(ai => ai.Symbol == symbolinfo.S).MarginType;
            ct.Client.UsdFuturesApi.Account.ChangeMarginTypeAsync(symbolinfo.S, margintype);
        }

        public static async Task ProcessAccountConfigUpdateAsync(string pData, CopyTrader ct)
        {
            var data = JsonConvert.DeserializeObject<AccountConfigUpdateEventData>(pData);
            var symbol = data?.AC?.S ?? "";
            var leverage = data?.AC?.L ?? 0;
            await LeverageChangedAsync(ct.Client, symbol, leverage, ct);
        }

        public static async Task ProcessOrderTradeUpdateAsync(string pData, CopyTrader ct, Trader t)
        {
            var data = JsonConvert.DeserializeObject<OrderTradeUpdateEventData>(pData);
            var symbol = data.O.s;
            OrderSide side = data.O.S.ToLower() == OrderSide.Buy.ToString().ToLower() ? OrderSide.Buy : OrderSide.Sell;
            var quantity = data.O.q * ct.QuantityRatio;
            quantity = Math.Ceiling((quantity ?? 0) * 1000) / 1000;

            UpdateAccountInfo(ct, t, symbol);
            // this is  the order book.
            if (data != null && data.O != null && data.O.X == "NEW")
            {
                // data.O.o will be LIMIT - MARKET - TRAILING_STOP_MARKET - STOP_MARKET - TAKE_PROFIT_MARKET
                if (data.O.o == "LIMIT")
                {
                    var price = data.O.p;

                    var result = await ct.Client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, FuturesOrderType.Limit, quantity, price, timeInForce: Binance.Net.Enums.TimeInForce.GoodTillCanceled);
                    //await SaveDataToDb(ct.UserApiId, data, result);
                }
                else if (data.O.o == "TRAILING_STOP_MARKET")
                {
                    var callbackRate = data.O.cr;
                    var activationPrice = data.O.AP;

                    var result = await ct.Client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, FuturesOrderType.TrailingStopMarket, quantity: quantity, reduceOnly: true, activationPrice: activationPrice, callbackRate: callbackRate);
                    //await SaveDataToDb(ct.UserApiId, data, result);
                }
                else if (data.O.o == "STOP_MARKET" || data.O.o == "TAKE_PROFIT_MARKET")
                {
                    var stopPrice = data.O.sp;
                    var type = data.O.o == "STOP_MARKET" ? FuturesOrderType.StopMarket : FuturesOrderType.TakeProfitMarket;

                    var result = await ct.Client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, type, quantity: quantity, stopPrice: stopPrice, reduceOnly: true);
                    //await SaveDataToDb(ct.UserApiId, data, result);
                }
                else if (data.O.o == "STOP" || data.O.o == "TAKE_PROFIT")
                {
                    var stopPrice = data.O.sp;
                    var price = data.O.p;
                    var type = data.O.o.ToLower() == FuturesOrderType.Stop.ToString().ToLower() ? FuturesOrderType.Stop : FuturesOrderType.TakeProfit;

                    var result = await ct.Client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, type, quantity: quantity, price: price, reduceOnly: true, stopPrice: stopPrice);
                    //await SaveDataToDb(ct.UserApiId, data, result);
                }
                else
                {
                    logger.Information("NEW transaction but cant catch. Maybe starts as MARKET.");
                    Console.WriteLine("NEW transaction but cant catch. Maybe starts as MARKET.");
                    return;
                }
                logger.Information($"Transaction executed: {data?.O.s ?? ""} {data?.O.S} {data?.O.o} quant:{data?.O.q} price:{data?.O.p}");
                Console.WriteLine($"Indítva egy új tranzakció: {data?.O.s ?? ""} {data?.O.S} {data?.O.o} quant:{data?.O.q} price:{data?.O.p}");

            }

            // this is the order book cancel
            else if (data != null && data.O != null && (data.O.X == "CANCELED" || data.O.X == "EXPIRED"))
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                //MEGJ: itt egyet kell kiütni, utána break -- ha 2 trader is nyit pozíciót, csak az egyiket offolja, a másik maradjon imo.
                var result = await ct.Client.UsdFuturesApi.CommonFuturesClient.GetOpenOrdersAsync(data.O.s);

                if (result == null)
                {
                    return;
                }
                foreach (var order in result.Data)
                {
                    var so = order.SourceObject as BinanceFuturesOrder;
                    if (so != null && so.Quantity == quantity && so.Side.ToString().ToLower() == data.O.S.ToLower()
                        && (so.OriginalType == FuturesOrderType.TrailingStopMarket && so.ActivatePrice == data.O.AP && so.CallbackRate == data.O.cr)
                        || (so.OriginalType == FuturesOrderType.Limit && so.Price == data.O.p)
                        || (so.Price == data.O.p && so.StopPrice == data.O.sp)) //stop, stopmarket, takeprofit, takeprofitmarket, liquidation, market
                    {
                        var res = await ct.Client.UsdFuturesApi.Trading.CancelOrderAsync(symbol, orderId: so.Id);
                        logger.Information($"Transaction {data.O.X} ({data.O.o}): {data.O.s} {data.O.S} {data.O.o} trader quant:{data.O.q}, ct quant:{so.Quantity} calced quant: {quantity} price:{data.O.p}");
                        Console.WriteLine($"Kiütve egy tranzakció ({data.O.o}): {data.O.s} {data.O.S} {data.O.o} trader quant:{data.O.q}, ct quant:{so.Quantity} calced quant: {quantity} price:{data.O.p}");

                        break;
                    }
                }
            }
            else if (data != null && data.O != null && (data.O.X == "FILLED" || data.O.X == "PARTIALLY_FILLED"))
            {
                if (data.O.o == "MARKET")
                {
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var position = await ct.Client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol, timestamp); //TODO: itt kiütötte nullpointerrel 0.033 btc 29499.90 árfol
                    //TODO: margin type, leverage bennevan ebben. ->ha eltérő, változtasd meg.
                    BinancePositionDetailsUsdt? pos = position.Data.FirstOrDefault() as BinancePositionDetailsUsdt;
                    if (data.O.rp == 0 || pos == null
                        || pos.Quantity == 0
                        || pos.Quantity > 0 && data.O.S == "BUY"
                        || pos.Quantity < 0 && data.O.S == "SELL")
                    {
                        var result = await ct.Client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, FuturesOrderType.Market, quantity);
                        //await SaveDataToDb(ct.UserApiId, data, result);
                        logger.Information($"Position opened: ({data.O.o}): {data.O.s} {data.O.S} {data.O.o} quant:{data.O.q}");
                        Console.WriteLine($"Pozíció megnyitva: ({data.O.o}): {data.O.s} {data.O.S} {data.O.o} quant:{data.O.q}");

                    }
                    else
                    {
                        var result = await ct.Client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, FuturesOrderType.Market, quantity, reduceOnly: true);
                        //await SaveDataToDb(ct.UserApiId, data, result);
                        logger.Information($"Position closed: ({data.O.o}): {data.O.s} {data.O.S} {data.O.o} quant:{data.O.q} price:{data.O.p}");
                        Console.WriteLine($"Pozíció zárva: ({data.O.o}): {data.O.s} {data.O.S} {data.O.o} quant:{data.O.q} price:{data.O.p}");
                    }
                }
                if (data.O.o == "LIMIT")
                {
                    //await SaveDataToDb(t.UserApiId, data, "FILLED");
                }
                // TODO: WHAT IF A TRADER'S LIMIT IS FILLED BUT NOT ON COPYTRADER OR VICE VERSA


            }
        }
        private static async Task SaveDataToDb(int userapiid, OrderTradeUpdateEventData data, string oStatus)
        {
            var orderSymbol = data.O.s;
            var clientOrderId = data.O.c;
            var orderId = data.O.i ?? 0;
            var orderSide = data.O.S;
            var orderType = data.O.o;
            var orderQuantity = data.O.q ?? 0;
            var orderPrice = data.O.AP ?? 0;
            var orderStatus = oStatus;
            decimal? commissionAsset = data.O.n;
            var workingType = data.O.wt;
            var orderTypeDetail = data.O.ot;
            var positionSide = data.O.ps;
            decimal? realizedProfit = data.O.rp;
            var json = data.ToString();

            using (var dbContext = new DatabaseContext(Constants.DB_CONNECTION_STRING))
            {
                // Call the method with the extracted properties
                dbContext.ExecuteTransactionInsertUpdate(
                    userapiid,
                    null,
                    orderSymbol,
                    clientOrderId,
                    orderId,
                    orderSide,
                    orderType,
                    orderQuantity,
                    orderPrice,
                    orderStatus,
                    commissionAsset,
                    workingType,
                    orderTypeDetail,
                    positionSide,
                    realizedProfit,
                    json
                );
            }
        }

        private static async Task SaveDataToDb(int userapiid, OrderTradeUpdateEventData data, WebCallResult<BinanceFuturesPlacedOrder> result)
        {

            var transactionIdTrader = data.O.i;
            var orderSymbol = result.Data.Symbol;
            var clientOrderId = result.Data.ClientOrderId;
            var orderId = result.Data.Id;
            var orderSide = result.Data.Side.ToString();
            var orderType = result.Data.Type.ToString(); // Order type
            var orderQuantity = result.Data.Quantity; // Quantity of the order
            var orderPrice = result.Data.Price; // Price
            var orderStatus = result.Data.Status.ToString(); // Current order status
            decimal? commissionAsset = data.O.n;
            var workingType = data.O.wt;
            var orderTypeDetail = result.Data.OriginalType.ToString(); // Order Type like "TRAILING_STOP_MARKET" (nullable)
            var positionSide = result.Data.PositionSide.ToString();
            decimal? realizedProfit = null;
            var json = data.ToString();

            // Assuming you have the 'dbContext' instance
            using (var dbContext = new DatabaseContext(Constants.DB_CONNECTION_STRING))
            {
                // Call the method with the extracted properties
                dbContext.ExecuteTransactionInsertUpdate(
                    userapiid,
                    transactionIdTrader,
                    orderSymbol,
                    clientOrderId,
                    orderId,
                    orderSide,
                    orderType,
                    orderQuantity,
                    orderPrice,
                    orderStatus,
                    commissionAsset,
                    workingType,
                    orderTypeDetail,
                    positionSide,
                    realizedProfit,
                    json
                );
            }
        }

        public static async Task UpdateAccountInfo(CopyTrader ct, Trader t, string symbol)
        {
            Console.WriteLine("Update accinfo starts...");
            var ctaccinfo = ct.AccountInfos.FirstOrDefault(ai => ai.Symbol == symbol);
            var taccinfo = t.AccountInfos.FirstOrDefault(ai => ai.Symbol == symbol);

            if (ctaccinfo == null || taccinfo == null || ctaccinfo.Leverage != taccinfo.Leverage || ctaccinfo.MarginType != taccinfo.MarginType)
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var tpos = await t.Client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol, timestamp);
                var tposdetails = tpos.Data.FirstOrDefault();
                if (tposdetails == null)
                {
                    return;
                }
                var newaccinfo = new AccountInfo()
                {
                    Symbol = symbol,
                    Leverage = tposdetails.Leverage,
                    MarginType = tposdetails.MarginType
                };

                if (ctaccinfo != null)
                {
                    ct.AccountInfos.Remove(ctaccinfo);
                }
                ct.AccountInfos.Add(newaccinfo);

                if (taccinfo != null)
                {
                    t.AccountInfos.Remove(taccinfo);
                }
                t.AccountInfos.Add(newaccinfo);

                _ = ct.Client.UsdFuturesApi.Account.ChangeMarginTypeAsync(newaccinfo.Symbol, newaccinfo.MarginType);
                _ = ct.Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(newaccinfo.Symbol, newaccinfo.Leverage);
                Console.WriteLine("Update accinfo ended.");
            }
        }

        public static async Task MarginTypeChangedAsync(BinanceRestClient client, string pSymbol, string pMarginType)
        {
            var lMarginType = FuturesMarginType.Isolated;
            if (pMarginType == "CROSS")
            {
                lMarginType = FuturesMarginType.Cross;
            }
            await client.UsdFuturesApi.Account.ChangeMarginTypeAsync(pSymbol, lMarginType);
           
            logger.Information($"Margin Type changed on {pSymbol}: {lMarginType}");
            Console.WriteLine($"Megváltozott a {pSymbol} piacnál a margin típus: {lMarginType}");
        }

        private static async Task LeverageChangedAsync(BinanceRestClient client, string pSymbol, int pLeverage, CopyTrader ct)
        {
            var result = await client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(pSymbol, pLeverage);
            logger.Information($"Leverage changed on {pSymbol}: {pLeverage}");
            Console.WriteLine($"Megváltozott a {pSymbol} piacnál a levarage: {pLeverage}");
        }
        #region JSONclasses
        private class AccountConfigUpdateEventData
        {
            public AccountConfigUpdateEvent? AC { get; set; }
        }

        private class AccountConfigUpdateEvent
        {
            public string? S { get; set; }
            public int L { get; set; }
        }
        // Define custom classes to represent the JSON structure
        private class AccountUpdateEventData
        {
            public AccountUpdateEvent? A { get; set; }
        }

        private class AccountUpdateEvent
        {
            public string? M { get; set; }
            public List<SymbolInfo>? P { get; set; }
        }

        private class SymbolInfo
        {
            public string? S { get; set; }
            public string? MT { get; set; }
        }

        private class OrderTradeUpdateEventData
        {
            public OrderTradeUpdateEvent? O { get; set; }
        }

        private class OrderTradeUpdateEvent
        {
            public string? s { get; set; } // ** Symbol
            public string c { get; set; } // Client order ID.
            public string? S { get; set; } // ** Side of the order, either "BUY" or "SELL".
            public string? o { get; set; } // ** Order type, ...
            public string? b { get; set; } // ** Bids notional value. - How much did you pay
            public string? f { get; set; } // Time in force. For example, "GTC" represents "Good 'Til Canceled".
            public decimal? q { get; set; } // ** Quantity of the order. - (ETHUSDT -> how much ETH you bought)
            public decimal? p { get; set; } // ** Price like ETH 1 - 1950 USDT --> this will be 1950
            public decimal? sp { get; set; } // ** Stop price. - ez a teljesülési ár
            public string? x { get; set; } // Current execution type, ... "x":"TRADE","X":"FILLED ! stop lossnál különbség jött.
            public string? X { get; set; } // Current order status 
            public long? i { get; set; } // Current order status 
            public string? ot { get; set; } // Order Type like "TRAILING_STOP_MARKET"
            public decimal? n { get; set; } // ** Commission asset - transaction fee
            public string? N { get; set; } // ** Commission asset name, like USDT - transaction fee currency
            public string? ps { get; set; } // Position Side like "BOTH"
            public string? wt { get; set; } // working type
            public decimal? AP { get; set; } // Activation Price
            public decimal? cr { get; set; } // Callback Rate like 1%
            public decimal? rp { get; set; } // ** Realized profit.
        }

        private class MessageEventData
        {
            public string? e { get; set; } // Event Type
            public string? E { get; set; } // Transaction time (timestamp) when the event occurred.
            public string? T { get; set; } // Event time (timestamp) when the event was received.
            public AccountConfigUpdateEvent? ac { get; set; } //
            public OrderTradeUpdateEvent? o { get; set; } // Object containing order and trade update information.
        }
        #endregion
    }
}