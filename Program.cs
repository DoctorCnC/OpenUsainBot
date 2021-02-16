using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCryptShot
{
  internal static class Program
  {
    public static void Main(string[] args)
    {
      Console.Title = "OpenUsainBot Alpha";
      Utilities.tag();
      Config config = Program.LoadOrCreateConfig();
      if (config == null)
      {
        Console.Read();
      }
      else
      {
        try
        {
          BinanceClientOptions options = new BinanceClientOptions();
          options.ApiCredentials = new ApiCredentials(config.apiKey, config.apiSecret);
          options.LogVerbosity = LogVerbosity.None;
          options.LogWriters = new List<TextWriter>()
          {
            Console.Out
          };
          BinanceClient.SetDefaultOptions(options);
        }
        catch (Exception ex)
        {
          Console.ForegroundColor = ConsoleColor.Red;
          Utilities.Write(ConsoleColor.DarkRed , "ERROR! Could not set Binance options. Error message: " + ex.Message );
          Console.Read();
          return;
        }
        Utilities.Write(ConsoleColor.Green, "Successfully logged in.");
        while (true)
        {
          string symbol;
          if (config.channel_id.Length > 0 && config.discord_token.Length > 0)
          {
            symbol = (string) null;
            Utilities.Write(ConsoleColor.Yellow, "Looking for ticker...");
            while (symbol == null)
            {
              symbol = "ETH";
              Thread.Sleep(1000);
            }
            Utilities.Write(ConsoleColor.Green, "Found ticker " + symbol);
            Console.ForegroundColor = ConsoleColor.White;
          }
          else
          {
            Utilities.Write(ConsoleColor.Yellow, "Input symbol: ");
            Console.ForegroundColor = ConsoleColor.White;
            symbol = Console.ReadLine();
          }
          if (!string.IsNullOrEmpty(symbol))
            Program.ExecuteOrder(symbol, config.quantity, config.strategyrisk, config.sellStrategy, config.maxsecondsbeforesell);
          else
            break;
        }
      }
    }

    private static Config LoadOrCreateConfig()
    {
      if (System.IO.File.Exists("config.json"))
        return JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json"));
      System.IO.File.WriteAllText("config.json", JsonConvert.SerializeObject((object) new Config()
      {
        apiKey = "",
        apiSecret = "",
        quantity = 0.00018M,
        strategyrisk = 20M,
        sellStrategy = 0.8M,
        maxsecondsbeforesell = 20M,
        discord_token = "",
        channel_id = ""
      }));
      Utilities.Write(ConsoleColor.Red, "config.json was missing and has been created. Please edit the file and restart the application.");
      return (Config) null;
    }

    private static void ExecuteOrder(
      string symbol,
      Decimal quantity,
      Decimal strategyrisk,
      Decimal sellStrategy,
      Decimal maxsecondsbeforesell)
    {
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      using (BinanceClient client = new BinanceClient())
      {
        string pair = symbol.ToUpper() + "BTC";
        WebCallResult<BinancePlacedOrder> webCallResult1 = client.Spot.Order.PlaceOrder(pair, OrderSide.Buy, OrderType.Market, quoteOrderQuantity: new Decimal?(quantity));
        if (!webCallResult1.Success)
        {
          Utilities.Write(ConsoleColor.Red, "ERROR! Could not place the Market order. Error code: " + webCallResult1.Error?.Message);
        }
        else
        {
          stopwatch.Stop();
          long timestamp = DateTime.Now.ToFileTime();
          TimeSpan elapsed = stopwatch.Elapsed;
          Console.WriteLine("RunTime " + string.Format("{0:00}:{1:00}:{2:00}.{3:00}", (object) elapsed.Hours, (object) elapsed.Minutes, (object) elapsed.Seconds, (object) (elapsed.Milliseconds / 10)));
          WebCallResult<BinanceExchangeInfo> exchangeInfo = client.Spot.System.GetExchangeInfo();
          if (!exchangeInfo.Success)
          {
            Utilities.Write(ConsoleColor.Red, "ERROR! Could not exchange informations. Error code: " + exchangeInfo.Error?.Message);
          }
          else
          {
            BinanceSymbol binanceSymbol = exchangeInfo.Data.Symbols.FirstOrDefault<BinanceSymbol>((Func<BinanceSymbol, bool>) (s => s.QuoteAsset == "BTC" && s.BaseAsset == symbol.ToUpper()));
            if (binanceSymbol == null)
            {
              Utilities.Write(ConsoleColor.Red, "ERROR! Could not get symbol informations.");
            }
            else
            {
              int symbolPrecision = 1;
              Decimal tickSize = binanceSymbol.PriceFilter.TickSize;
              while ((tickSize *= 10M) < 1M)
                ++symbolPrecision;
              Decimal num1 = 0M;
              if (webCallResult1.Data.Fills != null)
                num1 = webCallResult1.Data.Fills.Average<BinanceOrderTrade>((Func<BinanceOrderTrade, Decimal>) (trade => trade.Price));
              Decimal OrderQuantity = webCallResult1.Data.QuantityFilled;
              Utilities.Write(ConsoleColor.Green, string.Format("Order submitted, Got: {0} coins from {1} at {2}", (object) OrderQuantity, (object) pair, (object) num1));
              Decimal sellPriceRiskRatio = 0.95M;
              Decimal StartSellStrategy = sellStrategy;
              Decimal MaxSellStrategy = 1M - (1M - sellStrategy) / 5M;
              Decimal volasellmax = 1M;
              Decimal currentstoploss = 0M;
              List<Decimal> tab = new List<Decimal>();
              int count = -1;
              int x = 0;
              int usainsell = 0;
              int imincharge = 0;
              new Thread(new ThreadStart(NewThread)).Start();
              WebCallResult<BinanceBookPrice> priceResult3 = client.Spot.Market.GetBookPrice(pair);
              new Thread(new ThreadStart(NewThread2)).Start();
              WebCallResult<BinanceBookPrice> priceResult2 = client.Spot.Market.GetBookPrice(pair);
              while (sellStrategy <= MaxSellStrategy && sellStrategy >= StartSellStrategy && usainsell == 0)
              {
                if ((Decimal) timestamp + 10000000M * maxsecondsbeforesell > (Decimal) DateTime.Now.ToFileTime())
                {
                  priceResult2 = client.Spot.Market.GetBookPrice(pair);
                  Decimal num2 = Math.Round(priceResult2.Data.BestBidPrice * sellStrategy, symbolPrecision);
                  if (num2 > currentstoploss)
                  {
                    currentstoploss = num2;
                    Decimal num3 = Math.Round(num2 * sellPriceRiskRatio, symbolPrecision);
                    WebCallResult<IEnumerable<BinanceCancelledId>> webCallResult2 = client.Spot.Order.CancelAllOpenOrders(pair);
                    if (!webCallResult2.Success)
                    {
                      if (x == 0)
                      {
                        x = 1;
                        Utilities.Write(ConsoleColor.Red, "ERROR! Could not remove orders. Error code: " + webCallResult2.Error?.Message);
                      }
                      else
                      {
                        x = 2;
                        Utilities.Write(ConsoleColor.Red, "StopLoss executed : " + webCallResult2.Error?.Message);
                        break;
                      }
                    }
                    else
                      Utilities.Write(ConsoleColor.Cyan, "Orders successfully removed.");
                    if (usainsell == 0)
                    {
                      WebCallResult<BinancePlacedOrder> webCallResult3 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.StopLossLimit, new Decimal?(OrderQuantity), price: new Decimal?(num3), timeInForce: new TimeInForce?(TimeInForce.GoodTillCancel), stopPrice: new Decimal?(num2));
                      if (!webCallResult3.Success)
                      {
                        Utilities.Write(ConsoleColor.Red, "ERROR! Could not place the StopLimit order. Error code: " + webCallResult3.Error?.Message);
                        break;
                      }
                      Utilities.Write(ConsoleColor.DarkYellow, string.Format("StopLimit Order submitted, stop limit price: {0}, sell price: {1}", (object) num2, (object) num3));
                    }
                  }
                }
                else
                {
                  imincharge = 1;
                  client.Spot.Order.CancelAllOpenOrders(pair);
                  WebCallResult<BinancePlacedOrder> webCallResult2;
                  for (webCallResult2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, new Decimal?(OrderQuantity), price: new Decimal?(Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision)), timeInForce: new TimeInForce?(TimeInForce.GoodTillCancel)); !webCallResult2.Success; webCallResult2 = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, new Decimal?(OrderQuantity), price: new Decimal?(Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision)), timeInForce: new TimeInForce?(TimeInForce.GoodTillCancel)))
                    Utilities.Write(ConsoleColor.Red, "ERROR! Could not place the Market order sell, trying another time. Error code: " + webCallResult2.Error?.Message);
                  usainsell = 1;
                  Utilities.Write(ConsoleColor.Green, "UsainBot TIME SOLD successfully  " + OrderQuantity.ToString() + " " + webCallResult2.Data.Symbol + " sold at " + priceResult3.Data.BestBidPrice.ToString());
                  break;
                }
              }

              void NewThread()
              {
                while ((Decimal) timestamp + maxsecondsbeforesell * 10000000M > (Decimal) DateTime.Now.ToFileTime() && x != 2)
                {
                  ++count;
                  priceResult2 = client.Spot.Market.GetBookPrice(pair);
                  if (!priceResult2.Success)
                    break;
                  tab.Add(priceResult2.Data.BestBidPrice);
                  Decimal num1;
                  if (count % 10 == 0)
                  {
                    int index = count;
                    Decimal num2 = 0M;
                    int num3 = -1;
                    while (--index > 0 && ++num3 < 10)
                      num2 += (tab[index] - tab[index - 1]) / 10M;
                    Decimal num4 = num2 / 3M;
                    while (--index > 0 && ++num3 < 30)
                      num4 += (tab[index] - tab[index - 1]) / 30M;
                    Utilities.Write(ConsoleColor.Yellow, string.Format(" {0}", (object) Math.Round(num2 / priceResult2.Data.BestBidPrice * 100000M, 2)));
                    Utilities.Write(ConsoleColor.Magenta, string.Format(" {0}", (object) Math.Round(num4 / priceResult2.Data.BestBidPrice * 100000M, 2)));
                    Decimal d = (num4 - num2) / priceResult2.Data.BestBidPrice * 100000M;
                    if (d > strategyrisk / 4M)
                    {
                      Utilities.Write(ConsoleColor.Red, string.Format(" negative volatility detected at a {0} ratio", (object) Math.Round(d, 2)));
                      if (d < strategyrisk)
                      {
                        if (d > volasellmax)
                        {
                          volasellmax = d;
                          sellStrategy = Math.Round(StartSellStrategy + (Decimal) Math.Pow((double) d / (double) strategyrisk, 1.5) * (MaxSellStrategy - StartSellStrategy), 3);
                        }
                      }
                      else
                      {
                        if (usainsell != 0 || imincharge != 0)
                          break;
                        imincharge = 1;
                        client.Spot.Order.CancelAllOpenOrders(pair);
                        WebCallResult<BinancePlacedOrder> webCallResult;
                        for (webCallResult = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, new Decimal?(OrderQuantity), price: new Decimal?(Math.Round(priceResult2.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision)), timeInForce: new TimeInForce?(TimeInForce.GoodTillCancel)); !webCallResult.Success; webCallResult = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, new Decimal?(OrderQuantity), price: new Decimal?(Math.Round(priceResult2.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision)), timeInForce: new TimeInForce?(TimeInForce.GoodTillCancel)))
                          Utilities.Write(ConsoleColor.Red, "ERROR! Could not place the Market order sell, trying another time. Error code: " + webCallResult.Error?.Message);
                        usainsell = 1;
                        string[] strArray = new string[6]
                        {
                          "UsainBot PANIC SOLD successfully  ",
                          OrderQuantity.ToString(),
                          " ",
                          webCallResult.Data.Symbol,
                          " sold at ",
                          null
                        };
                        num1 = priceResult2.Data.BestBidPrice;
                        strArray[5] = num1.ToString();
                        Utilities.Write(ConsoleColor.Green, string.Concat(strArray));
                        break;
                      }
                    }
                  }
                  string[] strArray1 = new string[6]
                  {
                    string.Format("Price for {0} is {1} to {2} in iteration  ", (object) pair, (object) priceResult2.Data.BestBidPrice, (object) priceResult2.Data.BestAskPrice),
                    count.ToString(),
                    "  negative volatility ratio is ",
                    null,
                    null,
                    null
                  };
                  num1 = Math.Round(volasellmax, 2);
                  strArray1[3] = num1.ToString();
                  strArray1[4] = " stop limit is placed at ";
                  strArray1[5] = currentstoploss.ToString();
                  Console.Title = string.Concat(strArray1);
                }
              }

              void NewThread2()
              {
                while ((Decimal) timestamp + maxsecondsbeforesell * 10000000M > (Decimal) DateTime.Now.ToFileTime() && x != 2)
                {
                  ++count;
                  try
                  {
                    priceResult3 = client.Spot.Market.GetBookPrice(pair);
                    tab.Add(priceResult3.Data.BestBidPrice);
                  }
                  catch
                  {
                    tab.Add(tab[count - 1]);
                  }
                  string[] strArray1 = new string[6]
                  {
                    string.Format("Price for {0} is {1} to {2} in iteration  ", (object) pair, (object) priceResult3.Data.BestBidPrice, (object) priceResult3.Data.BestAskPrice),
                    count.ToString(),
                    "  negative volatility ratio is ",
                    null,
                    null,
                    null
                  };
                  Decimal num1 = Math.Round(volasellmax, 2);
                  strArray1[3] = num1.ToString();
                  strArray1[4] = " stop limit is placed at ";
                  strArray1[5] = currentstoploss.ToString();
                  Console.Title = string.Concat(strArray1);
                  if (count % 10 == 0)
                  {
                    int index = count;
                    Decimal num2 = 0M;
                    int num3 = -1;
                    while (--index > 0 && ++num3 < 10)
                      num2 += (tab[index] - tab[index - 1]) / 10M;
                    Decimal num4 = num2 / 3M;
                    while (--index > 0 && ++num3 < 30)
                      num4 += (tab[index] - tab[index - 1]) / 30M;
                    Utilities.Write(ConsoleColor.Green, string.Format(" {0}", (object) Math.Round(num2 / priceResult3.Data.BestBidPrice * 100000M, 2)));
                    Utilities.Write(ConsoleColor.Red, string.Format(" {0}", (object) Math.Round(num4 / priceResult3.Data.BestBidPrice * 100000M, 2)));
                    Decimal d = (num4 - num2) / priceResult3.Data.BestBidPrice * 100000M;
                    if (d > strategyrisk / 4M)
                    {
                      Utilities.Write(ConsoleColor.Red, string.Format(" negative volatility detected at a {0} ratio", (object) Math.Round(d, 2)));
                      if (d < strategyrisk)
                      {
                        if (d > volasellmax)
                        {
                          volasellmax = d;
                          sellStrategy = Math.Round(StartSellStrategy + (Decimal) Math.Pow((double) d / (double) strategyrisk, 1.5) * (MaxSellStrategy - StartSellStrategy), 3);
                        }
                      }
                      else
                      {
                        if (usainsell != 0 || imincharge != 0)
                          break;
                        imincharge = 1;
                        client.Spot.Order.CancelAllOpenOrders(pair);
                        WebCallResult<BinancePlacedOrder> webCallResult;
                        for (webCallResult = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, new Decimal?(OrderQuantity), price: new Decimal?(Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision)), timeInForce: new TimeInForce?(TimeInForce.GoodTillCancel)); !webCallResult.Success; webCallResult = client.Spot.Order.PlaceOrder(pair, OrderSide.Sell, OrderType.Limit, new Decimal?(OrderQuantity), price: new Decimal?(Math.Round(priceResult3.Data.BestBidPrice * sellPriceRiskRatio, symbolPrecision)), timeInForce: new TimeInForce?(TimeInForce.GoodTillCancel)))
                          Utilities.Write(ConsoleColor.Red, "ERROR! Could not place the Market order sell, trying another time. Error code: " + webCallResult.Error?.Message);
                        usainsell = 1;
                        string[] strArray2 = new string[6]
                        {
                          "UsainBot PANIC SOLD successfully  ",
                          OrderQuantity.ToString(),
                          " ",
                          webCallResult.Data.Symbol,
                          " sold at ",
                          null
                        };
                        num1 = priceResult3.Data.BestBidPrice;
                        strArray2[5] = num1.ToString();
                        Utilities.Write(ConsoleColor.Green, string.Concat(strArray2));
                        break;
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
