using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Connection;
using Utility;
using Accord.Math;
using Accord.Statistics;
using MathNet.Numerics.Statistics;
using System.Diagnostics;


namespace Tyche
{

    public class RunStartup
    {
        private static bool Initiated = false;
        
        private void PrintTime(Stopwatch watch)
        {
            var elapsedTime = watch.ElapsedMilliseconds / 1000.0;
            Console.WriteLine(elapsedTime < 60
                ? $"Execution Time: {elapsedTime} s"
                : $"Execution Time: {elapsedTime / 60.0} m");
        }
        
        public RunStartup(Stopwatch watch)
        {
            Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds / 1000.0 / 60.0} m");

            if (Initiated) return;
            DataStore.RetrieveTickers();
            PrintTime(watch);
            DataStore.RetrievePrices();
            PrintTime(watch);
            DataStore.ComputeCorrelationAndCovarianceMatrices();
            PrintTime(watch);
            DataStore.CreateDataFrames();
            PrintTime(watch);
            DataStore.SetDates();

            
            DataStore.ComputeAllEwmaVariances();
            DataStore.ComputeEwmaCovariances();
            DataStore.ComputeAllEwmaAverageDailyReturns();

            const int volLookbackSize = 100;
            DataStore.ComputeAllUnweightedVolsAndVariances(volLookbackSize);
            Initiated = true;
            
        }
    }

    

    public static class DataStore

    {
        private const string DefaultTable = "current_prices";

        private static readonly DateTime FirstEwmaDate = new DateTime(2019, 6, 4);
        private const int LookBackSize = 40;
        private const double Lambda = 0.94;
        private const double LambdaPrime = 1.0 - Lambda;
        private const double AnnualizationFactor = 252.0;

        // public static MsSqlConnection Conn = new MsSqlConnection();

        public static QSqliteConnection Conn3 = new();
        private static readonly MsSqlConnection Conn2 = new();
        private static readonly PostgreSqlConnection Conn = new ();
        private static List<string> Tickers = new();
        private static readonly ConcurrentDictionary<string, List<double>> Prices = new();
        public static readonly ConcurrentDictionary<string, double> CurrentPrices = new();

        private static readonly ConcurrentDictionary<string, Dictionary<DateTime, double>> Returns =
            new();

        private static double[,] CorrMat;
        public static readonly ConcurrentDictionary<string, double> OneDayCorrelationsDict = new();
        private static double[,] CovMat;
        public static readonly ConcurrentDictionary<string, double> OneDayCovarianceDict = new();

        public static readonly ConcurrentDictionary<string, double> OneDayVolatilities = new();
        public static readonly ConcurrentDictionary<string, double> OneDayVariances = new();
        private static readonly ConcurrentDictionary<string, double> AnnualizedVolatilities = new();
        private static readonly ConcurrentDictionary<string, double> AnnualizedVariances = new();

        public static readonly ConcurrentDictionary<string, double> OneDayEwmaVariances = new();
        public static readonly ConcurrentDictionary<string, double> OneDayEwmaVols = new();
        public static readonly ConcurrentDictionary<string, double> OneDayEwmaAverageReturns = new();
        public static readonly ConcurrentDictionary<string, double> OneDayEwmaCovarianceDict = new();
        private static readonly ConcurrentDictionary<string, double> AnnualizedEwmaVariances = new();
        private static readonly ConcurrentDictionary<string, double> AnnualizedEwmaVols = new();

        public static readonly ConcurrentDictionary<string, TimeSeries> PriceTimeSeries = new();
        private static readonly ConcurrentDictionary<string, TimeSeries> ReturnsTimeSeries = new();
        private static DataFrame PricesDataFrame;
        private static List<DateTime> PriceDates;
        private static List<DateTime> SeedDates;
        private static List<DateTime> EwmaDates;
        

        public static void SetDates()
        {
            SeedDates = PriceDates.GetRange(1, LookBackSize + 1);
            EwmaDates = PriceDates.GetRange(LookBackSize, PriceDates.Count - LookBackSize );
        }


        public static void RetrieveTickers(string table = null)
        {
            table ??= DefaultTable;

            var query = $"Select DISTINCT ticker from {table}";
            var dt = Conn.ExecuteQueryCommand(query);

            Tickers = dt.AsEnumerable().Select(p => p.Field<string>("ticker")).ToHashSet().ToList();
            Tickers.Sort();
        }

        public static void RetrievePrices(string table = null)
        {
            table ??= DefaultTable;
            var query = $"select price_date as date, ticker, close as price from {table} order by date desc";
            var dt = Conn.ExecuteQueryCommand(query);

            dt.ConvertColumnType("date", typeof(DateTime));
            dt.ConvertColumnType("ticker", typeof(string));
            dt.ConvertColumnType("price", typeof(double));

            Parallel.ForEach(Tickers, ticker =>
            {
                var price = dt.AsEnumerable()
                    .Where(s1 => s1.Field<string>("ticker") == ticker)
                    .Select(s1 => s1.Field<double>("price")).ToList();

                var dates = dt.AsEnumerable()
                    .Where(s1 => s1.Field<string>("ticker") == ticker)
                    .Select(s1 => s1.Field<DateTime>("date")).ToList();

                CreateTimeSeriesFromPrices(ticker, dates, price);
            });
        }



        public static void ComputeAllUnweightedVolsAndVariances(int lookBackSize, double annualizationFactor = 252)
        {

            var dateCount = PriceDates.Count;
            var dates = PriceDates.GetRange(dateCount - lookBackSize,lookBackSize);

            Parallel.ForEach(Tickers, ticker =>
            {
                var returns = ReturnsTimeSeries[ticker].Returns;
                var returnsToUse = dates.Select(date => returns[date]).ToList();
                ComputeUnweightedVolsAndVariances(ticker, returnsToUse, annualizationFactor);
            });
        }

        private static void ComputeUnweightedVolsAndVariances(string ticker, 
            IReadOnlyCollection<double> returns, 
            double annualizationDayCount = 252.0)
        {
            OneDayVolatilities[ticker] = returns.StandardDeviation();
            OneDayVariances[ticker] = Math.Pow(OneDayVolatilities[ticker], 2.0);
            AnnualizedVolatilities[ticker] = returns.StandardDeviation() * Math.Sqrt(annualizationDayCount);
            AnnualizedVariances[ticker] = Math.Pow(AnnualizedVolatilities[ticker], 2.0);
        }

        public static void CreateTimeSeriesFromPrices(string ticker, 
            List<DateTime> dates, 
            List<double> price, 
            double annualizationFactor = 252.0)
        {
            var priceTimeSeries = new TimeSeries(ticker, dates, price);

            PriceTimeSeries[ticker] = priceTimeSeries;
            Prices[ticker] = price;
            priceTimeSeries.ComputeReturns();
            Returns[ticker] = priceTimeSeries.Returns;
            ReturnsTimeSeries[ticker] = priceTimeSeries;
            CurrentPrices[ticker] = priceTimeSeries.Numbers[dates.Max()];

            var returns = Returns[ticker].Values.ToList();
            ComputeUnweightedVolsAndVariances(ticker, returns, annualizationFactor);
        }


        public static void ComputeAllEwmaAverageDailyReturns(double lambda=0.94)
        {
            Parallel.ForEach(Tickers, ComputeOneEwmaAverageDailyReturn);
        }

        public static void ComputeOneEwmaAverageDailyReturn(string ticker)
        {
            
            var startInteger = PriceDates.IndexOf(FirstEwmaDate);

            var returns = ReturnsTimeSeries[ticker].Returns;
            var seedReturns = SeedDates.Select(dt => returns[dt]).ToArray();
            var v1 = seedReturns.Mean();

            /*foreach (var dt in EwmaDates){             
                v1 = lambda * v1 + lambdaPrime * returns[dt];
            }*/

            v1 = EwmaDates.Aggregate(v1, (current, dt) => Lambda * current + LambdaPrime * returns[dt]);
            OneDayEwmaAverageReturns[ticker] = v1;
        }


        public static void ComputeAllEwmaVariances()
        {

            Parallel.ForEach(Tickers, ComputeOneEwmaVariance);

            /*foreach (var ticker in Tickers)
            {
                ComputeOneEwmaVariance(ticker);
            }//*/
        }

        public static void ComputeOneEwmaVariance(string ticker)
        {

            var returns = ReturnsTimeSeries[ticker].Returns;
            var seedReturns = SeedDates.Select(dt => returns[dt]).ToArray();
            var v1 = seedReturns.Variance();


            v1 = EwmaDates.Aggregate(v1, (current, dt) => Lambda * current + LambdaPrime * returns[dt] * returns[dt]);
            OneDayEwmaVariances[ticker] = v1;
            OneDayEwmaVols[ticker] = Math.Sqrt(v1);
            AnnualizedEwmaVariances[ticker] = v1 * AnnualizationFactor;
            AnnualizedEwmaVols[ticker] = Math.Sqrt(AnnualizedEwmaVariances[ticker]);

        }

        public static void ComputeEwmaCovariances()
        {
            var tickerArray = Tickers.ToArray();
            
            for (var i = 0; i < tickerArray.Length; i++)
            {
                var returns1 = ReturnsTimeSeries[tickerArray[i]].Returns;
                var seedReturns1 = SeedDates.Select(dt => returns1[dt]).ToArray();
                for (var j = i; j < tickerArray.Length; j++)
                {
                    var returns2 = ReturnsTimeSeries[tickerArray[j]].Returns;
                    var seedReturns2 = SeedDates.Select(dt => returns2[dt]).ToArray();
                    var v1 = seedReturns1.Covariance(seedReturns2);

                    //foreach (var dt in EwmaDates)
                    //{ v1 = lambda * v1 + lambdaPrime * returns1[dt] * returns2[dt];}
                    v1 = EwmaDates.Aggregate(v1, (current, dt) => Lambda * current + LambdaPrime * returns1[dt] * returns2[dt]);

                    //var diff = v1 - v2;

                    var key = Tickers[i] + "_" + Tickers[j];
                    OneDayEwmaCovarianceDict[key] = v1;
                    key = Tickers[j] + "_" + Tickers[i];
                    OneDayEwmaCovarianceDict[key] = v1;
                }
            }
        }


        public static void ComputeCorrelationAndCovarianceMatrices()
        {
            var tickerCount = Tickers.Count;
            var returnsMatrix = new double[tickerCount][];
            var i = 0;

            // parallel foreach here
            foreach (var ticker in Tickers)
            {
                returnsMatrix[i++] = Returns[ticker].Values.ToArray();
            }

            var returnsMatrix2 = returnsMatrix.ArraysTo2DArray(tickerCount, returnsMatrix[0].Length).Transpose();
            CorrMat = returnsMatrix2.Correlation();
            CovMat = returnsMatrix2.Covariance();

            for (var j = 0; j < tickerCount; j++)
            {
                OneDayCorrelationsDict[Tickers[j] + "_" + Tickers[j]] = 1.0;
                for (var k = j; k < tickerCount; k++)
                {
                    OneDayCorrelationsDict[Tickers[j] + "_" + Tickers[k]] = CorrMat[j, k];
                    OneDayCorrelationsDict[Tickers[k] + "_" + Tickers[j]] = CorrMat[k, j];
                    OneDayCovarianceDict[Tickers[j] + "_" + Tickers[k]] = CovMat[j, k];
                    OneDayCovarianceDict[Tickers[k] + "_" + Tickers[j]] = CovMat[k, j];
                }
            }
        }

        public static void CreateDataFrames()
        {
            var dict = new Dictionary<string, TimeSeries>(PriceTimeSeries);
            PricesDataFrame = new DataFrame(dict);
            PriceDates = PricesDataFrame.Index.ToList();
            PriceDates.Sort();
        }

    }
}


