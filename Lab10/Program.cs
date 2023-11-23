using System.Globalization;
using System.Net;
using Lab10.Structures;
using Lab10.Classes;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Lab10;

public class Program
{
    public static void Main(string[] args)
    {
        string[] tickers = File.ReadAllLines("C:/Users/Полина/source/repos/Lab10/Lab10/ticker.txt");

        using (var context = new TickersContext())
        {
            FillDb(context, tickers);
            UpdateTodayCondition(context);

            Console.WriteLine("Enter a ticker symbol:");
            var tickerSymbol = Console.ReadLine();

            while (tickerSymbol != "")
            {
                var ticker = context.Tickers.FirstOrDefault(t => t.TickerSymbol == tickerSymbol);
                if (ticker == null)
                {
                    Console.WriteLine("Ticker symbol not found.");
                    return;
                }

                var todayCondition = context.TodayCondition.FirstOrDefault(c => c.TickerId == ticker.Id);
                if (todayCondition == null)
                {
                    Console.WriteLine("No data available for today.");
                    return;
                }

                Console.WriteLine($"Price for {tickerSymbol} has {todayCondition.State} today.");

                Console.WriteLine("Enter new ticker symbol or press Enter for exit:");
                tickerSymbol = Console.ReadLine();
            }
        }
    }

    public static void UpdateTodayCondition(TickersContext context)
    {
        var tickerPrices = context.Prices.ToList();
        string state;
        foreach (var tickerPrice in tickerPrices)
        {
            if (tickerPrice.PriceBefore > tickerPrice.PriceAfter)
            {
                state = "decreased";
            }
            else
            {
                state = "increased";
            }

            var todayCondition = new TodayCondition
            {
                TickerId = tickerPrice.TickerId,
                State = state
            };

            context.TodayCondition.Add(todayCondition);
            context.SaveChanges();
        }
    }

    private static void FillDb(TickersContext context, string[] tickers)
    {
        for (int i = 0; i < tickers.Length; ++i)
        {
            var ticker = new Tickers
            {
                TickerSymbol = tickers[i]
            };
            context.Tickers.Add(ticker);
            context.SaveChanges();

            var arr = GetStockData(tickers[i]);

            var temp = new Prices
            {
                TickerId = context.Tickers.FirstOrDefault(t => t.TickerSymbol == ticker.TickerSymbol).Id,
                PriceBefore = arr[0],
                PriceAfter = arr[1],
                DateBefore = DateTimeOffset.Now.Date,
                DateAfter = DateTimeOffset.Now.AddDays(1)
            };
            context.Prices.Add(temp);
            context.SaveChanges();
        }
    }

    static double[] GetStockData(string ticker)
    {
        long startTimestamp = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds();
        long endTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string url = $"https://query1.finance.yahoo.com/v7/finance/download/{ticker}?period1={startTimestamp}&period2={endTimestamp}&interval=1d&events=history&includeAdjustedClose=true";

        using (WebClient client = new WebClient())
        {
            try
            {
                // download data
                string data = client.DownloadString(url);

                string[] lines = data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                double[] prices = new double[lines.Length - 1];
                for (int i = 1; i < lines.Length; i++)
                {
                    var columns = lines[i].Split(',');
                    var high = double.Parse(columns[2], CultureInfo.InvariantCulture);
                    var low = double.Parse(columns[3], CultureInfo.InvariantCulture);
                    prices[i - 1] = (high + low) / 2;
                }

                return prices;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Произошла ошибка при получении данных для акции {ticker}: {e.Message}");
                return null;
            }
        }
    }
}
