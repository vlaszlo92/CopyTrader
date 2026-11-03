using Binance.Net;
using Binance.Net.Clients;
using CryptoExchange.Net.Authentication;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using System.Runtime.InteropServices;

namespace CopyTrader.Worker
{
    internal static class Program
    {
        static async Task Main()
        {
            using var shutdownSource = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdownSource.Cancel();
            };
            using var sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;
                shutdownSource.Cancel();
            });

            await Service1.RunUntilShutdownRequestedAsync(shutdownSource.Token);
        }

        public partial class Service1
        {
            static List<Trader> traders;
            static List<CopyTrader> copyTraders;
            public static Logger Logger;

            static string baseApiUrl = "https://fapi.binance.com"; //"https://testnet.binancefuture.com"
            static string baseWebSocketUrl = "wss://fstream.binance.com"; //"wss://stream.binancefuture.com"

            public static async Task RunUntilShutdownRequestedAsync(CancellationToken shutdownToken)
            {
                Logger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {UserName}] {Message}{NewLine}")
                    .CreateLogger();
                Log.Logger = Logger;
                Logger.Information("Service starting...");

                traders = new List<Trader>();
                copyTraders = new List<CopyTrader>();

                var updateLoop = Task.Run(() => UpdateTradersAndCopyTraders(shutdownToken));
                await Task.WhenAny(updateLoop, Task.Delay(Timeout.Infinite, shutdownToken));

                if (updateLoop.IsFaulted)
                {
                    Logger.Fatal(updateLoop.Exception!.GetBaseException(), "Update loop crashed, exiting.");
                    Log.CloseAndFlush();
                    Environment.ExitCode = 1;
                    return;
                }

                Logger.Information("Shutdown requested, stopping.");
                await Task.WhenAny(updateLoop, Task.Delay(TimeSpan.FromSeconds(10)));
                Log.CloseAndFlush();
            }

            private static async Task UpdateTradersAndCopyTraders(CancellationToken shutdownToken)
            {
                while (!shutdownToken.IsCancellationRequested)
                {
                    var newCopyTraders = new List<CopyTrader>();
                    using (var dbContext = new DatabaseContext(Constants.DB_CONNECTION_STRING))
                    {
                        var synchedTraders = dbContext.GetTraders(traders.Count > 0 ? traders.Max(t => t.Id) : 0);
                        var synchedCopyTraders = dbContext.GetCopyTraders(copyTraders.Count > 0 ? copyTraders.Max(ct => ct.Id) : 0);

                        foreach (var trader in traders)
                        {
                            if (trader.IsActive || synchedTraders.FirstOrDefault(t => t.UserApiId == trader.UserApiId) == null)
                            {
                                trader.IsActive = false;
                                Console.WriteLine(trader.Name + " set to inactive.");
                                Log.Information(trader.Name + " set to inactive.");
                                //traders.Remove(trader);
                            }
                        }
                        foreach (var trader in synchedTraders)
                        {
                            var t = traders.FirstOrDefault(t => t.UserApiId == trader.UserApiId);
                            if (t == null)
                            {
                                trader.CopyTraders = GetCopyTradersByGroupIds(copyTraders, trader.GroupIds);

                                baseApiUrl = trader.Name.Contains("TEST") ? "https://testnet.binancefuture.com" : "https://fapi.binance.com";
                                baseWebSocketUrl = trader.Name.Contains("TEST") ? "wss://stream.binancefuture.com" : "wss://fstream.binance.com";


                                //TODO : from here -- SetupTrader()
                                trader.ListenKey = await CreateListenKey(baseApiUrl, trader);

                                _ = Task.Run(() => KeepExtendingListenKeyValidity(trader, baseApiUrl, trader.ListenKey));

                                string websocketUrl = $"{baseWebSocketUrl}/ws/{trader.ListenKey}";
                                await Task.Run(async () =>
                                {
                                    LogContext.PushProperty("UserName", trader.Name);
                                    await ConnectToWebSocketStream(websocketUrl, trader);
                                });


                                BinanceRestClient.SetDefaultOptions(options =>
                                {
                                    options.ApiCredentials = new ApiCredentials(trader.ApiKey, trader.SecKey);
                                });
                                trader.Client = new BinanceRestClient(options => options.Environment = trader.Name.Contains("TEST") ? BinanceEnvironment.Testnet : BinanceEnvironment.Live); //TODO this is livenet

                                //TODO: uncomment below
                                //var tpos = await trader.Client.UsdFuturesApi.Account.GetPositionInformationAsync();
                                //var tactivepos = tpos.Data.Where(p => p.Quantity != 0).ToList();
                                //var tobook = await trader.Client.UsdFuturesApi.Trading.GetOpenOrdersAsync();

                                //TODO: sync db + log
                                //TODO: if a connection dead, reopen.

                                //TODO : to here -- do this also with copytrader. ---- ha kioffolódik egy T vagy CT, újra tudjduk setupolni.
                                traders.Add(trader);
                            }
                            else
                            {
                                var ind = traders.FindIndex(tr => tr.UserApiId == t.UserApiId);
                                traders[ind].Balance = trader.Balance;
                                traders[ind].GroupIds = trader.GroupIds;
                                traders[ind].IsActive = trader.IsActive;
                                traders[ind].CopyTraders = GetCopyTradersByGroupIds(copyTraders, trader.GroupIds);
                                //Console.WriteLine(traders[ind].Name + " módosítva");
                            }
                        }

                        foreach (var copytrader in copyTraders)
                        {
                            if (synchedCopyTraders.FirstOrDefault(ct => ct.UserApiId == copytrader.UserApiId) == null)
                            {
                                copytrader.IsActive = false;
                                Console.WriteLine(copytrader.Name + " set to inactive.");
                                Logger.Information(copytrader.Name + " set to inactive.");
                                //copyTraders.Remove(copytrader);
                            }
                        }
                        foreach (var ct in synchedCopyTraders)
                        {
                            var copyt = copyTraders.FirstOrDefault(copyt => copyt.UserApiId == ct.UserApiId);
                            if (copyt == null)
                            {
                                var t = traders.FirstOrDefault(t => t.GroupIds.Contains(ct.TraderGroupId));
                                if (t == null)
                                {
                                    continue; //TODO: itt nincs akit kopizzzon... kezeld le
                                }

                                BinanceRestClient.SetDefaultOptions(options =>
                                {
                                    options.ApiCredentials = new ApiCredentials(ct.ApiKey, ct.SecKey);
                                });
                                ct.Client = new BinanceRestClient(options => options.Environment = ct.Name.Contains("TEST") ? BinanceEnvironment.Testnet : BinanceEnvironment.Live); //TODO this is livenet

                                //TODO: uncomment below

                                //var ctpos = await ct.Client.UsdFuturesApi.Account.GetPositionInformationAsync();
                                //var ctactivepos = ctpos.Data.Where(p => p.Quantity != 0).ToList();
                                //var ctobook = await ct.Client.UsdFuturesApi.Trading.GetOpenOrdersAsync();

                                //TODO: sync db + log
                                //TODO: if a connection dead, reopen.
                                newCopyTraders.Add(ct);
                            }
                            else
                            {
                                var ind = copyTraders.FindIndex(cot => cot.UserApiId == copyt.UserApiId);
                                copyTraders[ind].Balance = ct.Balance;
                                copyTraders[ind].TraderGroupId = ct.TraderGroupId;
                                copyTraders[ind].IsActive = ct.IsActive;
                                //Console.WriteLine(copyTraders[ind].Name + " módosítva");
                            }
                        }
                    }
                    var cancellationTokenSource = new CancellationTokenSource();
                    _ = Task.Run(() => ProcessAllCopyTraders(newCopyTraders, cancellationTokenSource.Token));
                    copyTraders.AddRange(newCopyTraders);

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), shutdownToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            private static async Task<string> CreateListenKey(string baseUrl, Trader trader)
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var queryString = $"timestamp={timestamp}";
                var signature = GenerateSignature(queryString, trader.SecKey);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/fapi/v1/listenKey?{queryString}");
                request.Headers.Add("X-MBX-APIKEY", trader.ApiKey);
                request.Headers.Add("signature", signature);

                var response = await trader.HttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(content) ?? "";
                Logger.Information($"ListenKey created (ApiId: {trader.UserApiId}) - ListenKey: {data}.");

                return data?.listenKey ?? "";
            }

            private static async Task KeepExtendingListenKeyValidity(Trader trader, string baseUrl, string listenKey)
            {
                while (true)
                {
                    await ExtendListenKeyValidity(trader, baseUrl, listenKey);
                    await Task.Delay(TimeSpan.FromMinutes(30));
                }
            }

            private static async Task ExtendListenKeyValidity(Trader trader, string baseUrl, string listenKey)
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var queryString = $"listenKey={listenKey}&timestamp={timestamp}";
                var signature = GenerateSignature(queryString, trader.ApiKey);

                var request = new HttpRequestMessage(HttpMethod.Put, $"{baseUrl}/fapi/v1/listenKey?{queryString}");
                request.Headers.Add("X-MBX-APIKEY", trader.ApiKey);
                request.Headers.Add("signature", signature);

                var response = await trader.HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error extending listenKey validity.");
                    Logger.Error($"Error extending listenKey validity (ApiId: {trader.UserApiId}).");
                    //TODO: csinálld meg, hogy menjen a trader. új listenkey etc.
                }
            }

            private static async Task ConnectToWebSocketStream(string websocketUrl, Trader trader)
            {
                try
                {
                    await trader.ClientWebSocket.ConnectAsync(new Uri(websocketUrl), CancellationToken.None);

                    if (trader.ClientWebSocket.State == WebSocketState.Open)
                    {
                        Console.WriteLine($"WebSocket connection opened for Trader {trader.Id}.");
                        Logger.Information($"WebSocket connection opened for Trader {trader.Id}.");
                        _ = Task.Run(() => ReceiveWebSocketMessages(trader));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting WebSocket for Trader {trader.Id}: {ex.Message}");
                    Logger.Error($"Error connecting WebSocket for Trader {trader.Id}: {ex.Message}");
                    trader.ClientWebSocket.Dispose();
                }
            }

            private static async Task ReceiveWebSocketMessages(Trader trader)
            {
                Logger.Information($"Trader {trader.Name} (id: {trader.UserApiId}) created.");
                Console.WriteLine($"Trader {trader.Name} (id: {trader.UserApiId}) created.");
                var buffer = new byte[4096];
                while (trader.ClientWebSocket.State == WebSocketState.Open)
                {
                    if (!trader.IsActive)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1));
                        continue;
                    }
                    try
                    {
                        var result = await trader.ClientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            Console.WriteLine("Received message: " + message);
                            if (trader != null && trader.CopyTraders != null)
                            {
                                Logger.Information($"{message} - Trader name: {trader.Name} - UserApiId: {trader.UserApiId}");
                                foreach (var ct in trader.CopyTraders)
                                {
                                    ct.AddTransaction(message, trader.UserApiId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"WebSocket disconnected for Trader {trader.Id}: {ex.Message}");
                        Logger.Error($"WebSocket disconnected for Trader {trader.Id}: {ex.Message}");
                        break;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
                await ConnectToWebSocketStream($"{baseWebSocketUrl}/ws/{trader.ListenKey}", trader);

                if (trader.ClientWebSocket.State == WebSocketState.Open)
                {
                    await ReceiveWebSocketMessages(trader);
                }
            }

            private static string GenerateSignature(string queryString, string apiSecret)
            {
                var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
                var bytes = Encoding.UTF8.GetBytes(queryString);
                var hash = hmacsha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
            private static async Task ProcessAllCopyTraders(List<CopyTrader> CopyTraders, CancellationToken cancellationToken)
            {
                List<Task> copyTraderThreads = (from copyTrader in CopyTraders
                                                let thread = Task.Run(() =>
                                                {
                                                    LogContext.PushProperty("UserName", copyTrader.Name);
                                                    ProcessTraderTransactions(copyTrader, cancellationToken);
                                                })
                                                select thread).ToList();

                await Task.WhenAll(copyTraderThreads);
            }

            private static void ProcessTraderTransactions(CopyTrader copyTrader, CancellationToken cancellationToken)
            {
                Console.WriteLine($"CopyTrader {copyTrader.Name} (id: {copyTrader.UserApiId}) created.");
                Logger.Information($"CopyTrader {copyTrader.Name} (id: {copyTrader.UserApiId}) created.");

                while (true)
                {
                    try
                    {
                        if (copyTrader.TraderTransactions.TryTake(out var data, Timeout.Infinite, cancellationToken))
                        {
                            if (!copyTrader.IsActive)
                            {
                                Console.WriteLine("elment de inaktív");

                                _ = Task.Delay(TimeSpan.FromSeconds(10));
                                continue;
                            }
                            Console.WriteLine("elment");

                            var trader = traders.FirstOrDefault(t => t.UserApiId == data.TraderId);
                            copyTrader.QuantityRatio = CalcQuantityRatio(trader, copyTrader);
                            MessageHandler.ProcessMessageAsync(data.Transaction, copyTrader, trader);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message);
                    }
                }
            }
            private static decimal CalcQuantityRatio(Trader t, CopyTrader ct)
            {
                if (t == null || t.Balance == null || ct == null || ct.Balance == null)
                    return 1;

                return Math.Max((decimal)ct.Balance.MaxValue / (decimal)t.Balance.MaxValue, 1);
            }

            private static List<CopyTrader> GetCopyTradersByGroupIds(List<CopyTrader> copyTraders, List<int> groupIds)
            {
                var filteredCopyTraders = copyTraders.Where(ct => groupIds.Contains(ct.TraderGroupId) && ct.IsActive);
                return filteredCopyTraders.DistinctBy(ct => ct.UserApiId).ToList();
            }
        }
    }
}