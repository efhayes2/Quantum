using System;
using System.Collections.Generic;
using System.Linq;
using ExcelDna.Integration;
using Connection;
using Deedle.Math;
using Utility;
using Accord.Statistics.Distributions.Univariate;

namespace Quantum
{
    public static class ExcelInterface
    
    {
        public static MsSqlConnection Conn = new MsSqlConnection();
        public static RunStartup StartupCompleted = new RunStartup();

        [ExcelFunction(Description = "Create Portfolio")]
        public static string CreatePortfolio(
            [ExcelArgument("Tickers")] object[] tickers,
            [ExcelArgument("Quantities")] object[] quantities)

        {
            var tList = Array.ConvertAll(tickers, element => (string)element).ToList();
            var qList = Array.ConvertAll(quantities, element => (double)element).ToList();
            var portfolio = new Portfolio(tList, qList);

            return portfolio.PortfolioId;
        }


        [ExcelFunction(Description = "Compute Range Probabilities")]
        public static object[] ComputeRangeProbabilities(
            [ExcelArgument("Ticker")] string ticker,
            [ExcelArgument("Upper")] double upper,
            [ExcelArgument("Lower")] double lower,
            [ExcelArgument("Barrier")] double barrier,
            [ExcelArgument("Days")] double days)

        {
            var returnValues = new object[6];
            var muT = DataStore.OneDayEwmaAverageReturns[ticker] * days;
            var sigmaRootT = DataStore.OneDayEwmaVols[ticker] * Math.Sqrt(days);
            var upperZscore = (upper - muT) / sigmaRootT;
            var lowerZscore = (lower - muT) / sigmaRootT;
            var upperZscoreWithBarrier = (2.0 * barrier - upper + muT) / sigmaRootT;
            var lowerZscoreWithBarrier = (2.0 * barrier - lower + muT) / sigmaRootT;
            var expTermBarrier = Math.Exp(2.0 * muT * barrier / (sigmaRootT * sigmaRootT));
            var gauss = new NormalDistribution();
            returnValues[0] = gauss.DistributionFunction(upperZscore);  // probUpper
            returnValues[1] = gauss.DistributionFunction(lowerZscore);  // probLower
            returnValues[2] = (double) returnValues[0] - (double) returnValues[1];  // UpperMinusLower 

            returnValues[3] = gauss.DistributionFunction(upperZscoreWithBarrier) * expTermBarrier + (double) returnValues[0]; // probUpperWithBarrier
            returnValues[4] = gauss.DistributionFunction(lowerZscoreWithBarrier) * expTermBarrier + (double)returnValues[0]; // probLowerWithBarrier
            returnValues[5] = (double)returnValues[3] - (double)returnValues[4];  // UpperMinusLowerWithBarrier 

            return returnValues;
        }


        [ExcelFunction(Description = "Retrieve Returns Correlation Matrix from Ticker List")]
        public static double GetPriceByTickerAndDate(
            [ExcelArgument("Ticker")] string ticker,
            [ExcelArgument("Date")] DateTime date)
        {
            return DataStore.PriceTimeSeries[ticker].Numbers.GetValueOrDefault(date);
        }

        [ExcelFunction(Description = "Retrieve Returns Correlation Matrix from Ticker List")]
        public static double GetCurrentPriceByTicker([ExcelArgument("Ticker")] string ticker)
        {
            return DataStore.CurrentPrices[ticker];
        }

        [ExcelFunction(Description = "Retrieve Returns Correlation Matrix from Ticker List")]
        public static double[,] GetCorrelationMatrixFromTickers(
            [ExcelArgument("Tickers")] object[] tickers,
            [ExcelArgument("UseEwma")] bool useEwma)
        {
            return GetCorrelationMatrixFromTickers_(tickers, useEwma);
        }

        [ExcelFunction(Description = "Retrieve Returns Correlation Matrix from Ticker List")]
        public static double[,] GetCovarianceMatrixFromTickers(
            [ExcelArgument("Tickers")] object[] tickers,
            [ExcelArgument("UseEwma")] bool useEwma)
        {
            return GetCovarianceMatrixFromTickers_(tickers, useEwma);
        }


        [ExcelFunction(Description = "Retrieve Return Volatilities Vector from Ticker List")]
        public static double[] GetVolsFromTickers([ExcelArgument("Ticker")] object[] tickers)
        {
            return GetVolsFromTickers_(tickers);
        }

        [ExcelFunction(Description = "Retrieve Return VolatilitiesTicker")]
        public static double GetVolFromTicker(
            [ExcelArgument("Ticker")] string ticker,
            [ExcelArgument("UseEwma")] bool useEwma)
        {
            var vols = useEwma ? DataStore.OneDayEwmaVols : DataStore.OneDayVolatilities;
            return vols[ticker];
        }


        [ExcelFunction(Description = "Retrieve Return Variances Matrix from Ticker List")]
        public static double GetVarianceFromTicker(
            [ExcelArgument("Ticker")] string ticker,
            [ExcelArgument("UseEwma")] bool useEwma)
        {
            var variances = useEwma ? DataStore.OneDayEwmaVariances : DataStore.OneDayVariances;
            return variances[ticker];
        }


        [ExcelFunction(Description = "Retrieve Ewma Return Variances Matrix from Ticker List")]
        public static double GetEwmaVarianceFromTicker(
            [ExcelArgument("Ticker")] string ticker)
        {
            return GetVarianceFromTicker(ticker, true);
        }


        [ExcelFunction(Description = "Retrieve Return Volatilities Matrix from Ticker")]
        public static double GetEwmaAverageReturnFromTicker([ExcelArgument("Ticker")] string ticker)
        {
            return DataStore.OneDayEwmaAverageReturns[ticker];
        }



        private static double[,] GetCorrelationMatrixFromTickers_(IReadOnlyList<object> tickers, bool useEwma)
        {
            var dict = useEwma ? DataStore.OneDayEwmaCovarianceDict : DataStore.OneDayCorrelationsDict;

            var sz = tickers.Count;
            var result = new double[sz, sz];

            for (var j = 0; j < tickers.Count; j++)
            {
                var lst = new List<double>();
                for (var k = 0; k < tickers.Count; k++)
                {
                    var key = tickers[j] + "_" + tickers[k];
                    result[j, k] = dict[key];
                    result[k, j] = result[j, k];
                }
            }
            return result;
        }


        private static double[,] GetCovarianceMatrixFromTickers_(IReadOnlyList<object> tickers, bool useEwma)
        {
            var dict = useEwma ? DataStore.OneDayEwmaCovarianceDict : DataStore.OneDayCovarianceDict;

            var sz = tickers.Count;
            var result = new double[sz, sz];

            for (var j = 0; j < tickers.Count; j++)
            {
                var lst = new List<double>();
                for (var k = 0; k < tickers.Count; k++)
                {
                    var key = tickers[j] + "_" + tickers[k];
                    result[j, k] = dict[key];
                    result[k, j] = result[j, k];
                }
            }
            return result;
        }


        private static double[] GetVolsFromTickers_(IEnumerable<object> tickers)
        {
            return tickers.Select(ticker => DataStore.OneDayVolatilities[ticker.ToString()]).ToArray();
        }
    }


}

