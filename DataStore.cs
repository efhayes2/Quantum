using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Accord.Math;
using Utility;
using Connection;
using Accord.Statistics;
using MathNet.Numerics.Statistics;

namespace Quantum
{

    public class RunStartup
    {
        public static bool Initiated = false;
        public RunStartup()
        {
            if (Initiated) return;
            DataStore.RetrieveTickers();
            DataStore.RetrievePrices();
            DataStore.ComputeCorrelationAndCovarianceMatrices();
            DataStore.ComputeCurrentPrices();
            DataStore.CreateDataFrames();

            var firstEwmaDate = new DateTime(2018, 12, 27);
            const int lookBackSize = 40;
            DataStore.ComputeAllEwmaVariances(firstEwmaDate, lookBackSize, lambda:0.94);
            DataStore.ComputeEwmaCovariances(firstEwmaDate, lookBackSize, lambda: 0.94);
            DataStore.ComputeAllEwmaAverageDailyReturns(firstEwmaDate, lookBackSize, lambda: 0.94);

            const int volLookbackSize = 504;
            DataStore.ComputeAllUnweightedVolsAndVariances(volLookbackSize);

        }
    }


    public static class DataStore

    {
        // public static MsSqlConnection Conn = new MsSqlConnection();

        public static QSqliteConnection Conn = new QSqliteConnection();
        public static List<string> Tickers = new List<string>();
        public static Dictionary<string, List<double>> Prices = new Dictionary<string, List<double>>();
        public static Dictionary<string, double> CurrentPrices = new Dictionary<string, double>();
        public static Dictionary<string, Dictionary<DateTime, double>> Returns =
            new Dictionary<string, Dictionary<DateTime, double>>();

        public static double[,] CorrMat;
        public static Dictionary<string, double> OneDayCorrelationsDict = new Dictionary<string, double>();
        public static double[,] CovMat;
        public static Dictionary<string, double> OneDayCovarianceDict = new Dictionary<string, double>();

        public static Dictionary<string, double> OneDayVolatilities = new Dictionary<string, double>();
        public static Dictionary<string, double> OneDayVariances = new Dictionary<string, double>();
        public static Dictionary<string, double> AnnualizedVolatilities = new Dictionary<string, double>();
        public static Dictionary<string, double> AnnualizedVariances = new Dictionary<string, double>();

        // public static Dictionary<string, Dictionary<DateTime, double>> EwmaVariances =
           // new Dictionary<string, Dictionary<DateTime, double>>();
        public static Dictionary<string, double> OneDayEwmaVariances = new Dictionary<string, double>();
        public static Dictionary<string, double> OneDayEwmaVols = new Dictionary<string, double>();
        public static Dictionary<string, double> OneDayEwmaAverageReturns = new Dictionary<string, double>();
        public static Dictionary<string, double> OneDayEwmaCovarianceDict = new Dictionary<string, double>();
        public static Dictionary<string, double> AnnualizedEwmaVariances = new Dictionary<string, double>();
        public static Dictionary<string, double> AnnualizedEwmaVols = new Dictionary<string, double>();

        public static Dictionary<string, TimeSeries> PriceTimeSeries = new Dictionary<string, TimeSeries>();
        public static Dictionary<string, TimeSeries> ReturnsTimeSeries = new Dictionary<string, TimeSeries>();
        public static DataFrame PricesDataFrame;
        public static List<DateTime> PriceDates;



        public static void RetrieveTickers()
        {
            const string query = "Select DISTINCT ticker from stock_prices";
            var dt = Conn.ExecuteQueryCommand(query);

            Tickers = dt.AsEnumerable().Select(p => p.Field<string>("ticker")).ToHashSet().ToList();
            Tickers.Sort();
        }


        public static void ComputeCurrentPrices()
        {
            //var 
            foreach (var ticker in Prices.Keys)
            {
              //  var last 
            }
        }


        public static void ComputeAllUnweightedVolsAndVariances(int lookBackSize, double annualizationFactor = 252)
        {

            var dateCount = PriceDates.Count;
            var dates = PriceDates.GetRange(dateCount - lookBackSize,lookBackSize);

            foreach (var ticker in Tickers)
            {
                var returns = ReturnsTimeSeries[ticker].Returns;
                var returnsToUse = dates.Select(date => returns[date]).ToList();
                ComputeUnweightedVolsAndVariances(ticker, returnsToUse, annualizationFactor);
            }
        }

        public static void ComputeUnweightedVolsAndVariances(string ticker, List<double> returns, double annualizationDayCount = 252.0)
        {
            OneDayVolatilities[ticker] = returns.StandardDeviation();
            OneDayVariances[ticker] = Math.Pow(OneDayVolatilities[ticker], 2.0);
            AnnualizedVolatilities[ticker] = returns.StandardDeviation() * Math.Sqrt(annualizationDayCount);
            AnnualizedVariances[ticker] = Math.Pow(AnnualizedVolatilities[ticker], 2.0);
        }

        public static void CreateTimeSeriesFromPrices(string ticker, List<DateTime> dates, List<double> price, double annualizationFactor = 252.0)
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


        public static void ComputeAllEwmaAverageDailyReturns(DateTime startDate, int lookBackSize = 40, double lambda=0.94)
        {
            foreach (var ticker in Tickers)
            {
                ComputeOneEwmaAverageDailyReturn(ticker, startDate, lookBackSize, lambda);
            }
        }

        public static void ComputeOneEwmaAverageDailyReturn(string ticker, DateTime? startDate, int lookBackSize = 40, double lambda = 0.94)
        {
            var firstEwmaDate = startDate ?? new DateTime(2018, 12, 27);

            var lambdaPrime = 1.0 - lambda;
            var startInteger = PriceDates.IndexOf(firstEwmaDate);
            var seedDates = PriceDates.GetRange(startInteger - lookBackSize + 1, lookBackSize);
            var ewmaDates = PriceDates.GetRange(startInteger + 1, PriceDates.Count - startInteger - 1);

            var returns = ReturnsTimeSeries[ticker].Returns;
            var seedReturns = seedDates.Select(dt => returns[dt]).ToArray();
            var v1 = seedReturns.Mean();

            /*foreach (var dt in ewmaDates){
             
                v1 = lambda * v1 + lambdaPrime * returns[dt];
            }//*/
            v1 = ewmaDates.Aggregate(v1, (current, dt) => lambda * current + lambdaPrime * returns[dt]);
            OneDayEwmaAverageReturns[ticker] = v1;
        }


        public static void ComputeAllEwmaVariances(DateTime startDate, int lookBackSize, double lambda,
            double annualizationFactor = 252.0)
        {
            foreach (var ticker in Tickers)
            {
                ComputeOneEwmaVariance(ticker, startDate, lookBackSize, lambda, annualizationFactor);
            }
        }

        public static void ComputeOneEwmaVariance(string ticker, DateTime? startDate, int lookBackSize = 40, double lambda = 0.94, 
            double annualizationFactor = 252.0)
        {
            
            var firstEwmaDate = startDate ?? new DateTime(2018, 12, 27);
            var lambdaPrime = 1.0 - lambda;
            var startInteger = PriceDates.IndexOf(firstEwmaDate);
            var seedDates = PriceDates.GetRange(startInteger - lookBackSize + 1, lookBackSize);
            var ewmaDates = PriceDates.GetRange(startInteger + 1, PriceDates.Count - startInteger -1 );

            var returns = ReturnsTimeSeries[ticker].Returns;
            var seedReturns = seedDates.Select(dt => returns[dt]).ToArray();
            var v1 = seedReturns.Variance();

            /* if we need a time series of ewmaVariances, which is not likely
            var tickerEwmaVols = new Dictionary<DateTime, double>();
            tickerEwmaVols[startDate] = v1;
            foreach (var dt in ewmaDates)
            {
                v1 = lambda * v1 + lambdaPrime * returns[dt] * returns[dt];
                tickerEwmaVols[dt] = v1;
            }

            EwmaVariances[ticker] = tickerEwmaVols; */

            v1 = ewmaDates.Aggregate(v1, (current, dt) => lambda * current + lambdaPrime * returns[dt] * returns[dt]);
            OneDayEwmaVariances[ticker] = v1;
            OneDayEwmaVols[ticker] = Math.Sqrt(v1);
            AnnualizedEwmaVariances[ticker] = v1 * annualizationFactor;
            AnnualizedEwmaVols[ticker] = Math.Sqrt(AnnualizedEwmaVariances[ticker]);

        }

        public static void ComputeEwmaCovariances(DateTime startDate, int lookBackSize, double lambda)
        {
            var lambdaPrime = 1.0 - lambda;
            var startInteger = PriceDates.IndexOf(startDate);
            var seedDates = PriceDates.GetRange(startInteger - lookBackSize + 1, lookBackSize);
            var ewmaDates = PriceDates.GetRange(startInteger + 1, PriceDates.Count - startInteger - 1);

            var sz = Tickers.Count;
            var tickerArray = Tickers.ToArray();
            
            for (var i = 0; i < tickerArray.Length; i++)
            {
                var returns1 = ReturnsTimeSeries[tickerArray[i]].Returns;
                var seedReturns1 = seedDates.Select(dt => returns1[dt]).ToArray();
                for (var j = i; j < tickerArray.Length; j++)
                {
                    var returns2 = ReturnsTimeSeries[tickerArray[j]].Returns;
                    var seedReturns2 = seedDates.Select(dt => returns2[dt]).ToArray();
                    var v1 = seedReturns1.Covariance(seedReturns2);

                    //foreach (var dt in ewmaDates)
                    //{ v1 = lambda * v1 + lambdaPrime * returns1[dt] * returns2[dt];}
                    v1 = ewmaDates.Aggregate(v1, (current, dt) => lambda * current + lambdaPrime * returns1[dt] * returns2[dt]);

                    //var diff = v1 - v2;

                    var key = Tickers[i] + "_" + Tickers[j];
                    OneDayEwmaCovarianceDict[key] = v1;
                    key = Tickers[j] + "_" + Tickers[i];
                    OneDayEwmaCovarianceDict[key] = v1;
                }
            }
        }



        public static void RetrievePrices()
        {
            var tickerCount = Tickers.Count;
            var returnsMatrix = new double[tickerCount][];
            var i = 0;

            const string query = "Select date, ticker, price from stock_prices order by date desc";
            var dt = Conn.ExecuteQueryCommand(query);

            dt.ConvertColumnType("date", typeof(DateTime));
            dt.ConvertColumnType("ticker", typeof(string));
            dt.ConvertColumnType("price", typeof(double));

            foreach (var ticker in Tickers)
            {
                var price = dt.AsEnumerable()
                    .Where(s1 => s1.Field<string>("ticker") == ticker)
                    .Select(s1 => s1.Field<double>("price")).ToList();

                var dates = dt.AsEnumerable()
                    .Where(s1 => s1.Field<string>("ticker") == ticker)
                    .Select(s1 => s1.Field<DateTime>("date")).ToList();

                CreateTimeSeriesFromPrices(ticker, dates, price);
            }
        }

        public static void ComputeCorrelationAndCovarianceMatrices()
        {
            var tickerCount = Tickers.Count;
            var returnsMatrix = new double[tickerCount][];
            var i = 0;

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
            PricesDataFrame = new DataFrame(PriceTimeSeries);
            PriceDates = PricesDataFrame.Index.ToList();
            PriceDates.Sort();

            //   ReturnsDataFrame = new DataFrame(Returns);
        }
    }
}


