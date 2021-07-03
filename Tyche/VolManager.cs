using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Statistics.Distributions.Univariate;
using Connection;
using Utility;

namespace Tyche
{
    public class VolManager
    {
        public static RunStartup StartupCompleted = new();

        public static string CreatePortfolio(object[] tickers, object[] quantities)

        {
            var tList = Array.ConvertAll(tickers, element => (string) element).ToList();
            var qList = Array.ConvertAll(quantities, element => (double) element).ToList();
            var portfolio = new Portfolio(tList, qList);

            return portfolio.PortfolioId;
        }

        public static object[] ComputeRangeProbabilities(string ticker, double upper, double lower, double barrier,
            double days)

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
            returnValues[0] = gauss.DistributionFunction(upperZscore); // probUpper
            returnValues[1] = gauss.DistributionFunction(lowerZscore); // probLower
            returnValues[2] = (double) returnValues[0] - (double) returnValues[1]; // UpperMinusLower 

            returnValues[3] = gauss.DistributionFunction(upperZscoreWithBarrier) * expTermBarrier +
                              (double) returnValues[0]; // probUpperWithBarrier
            returnValues[4] = gauss.DistributionFunction(lowerZscoreWithBarrier) * expTermBarrier +
                              (double) returnValues[0]; // probLowerWithBarrier
            returnValues[5] = (double) returnValues[3] - (double) returnValues[4]; // UpperMinusLowerWithBarrier 

            return returnValues;
        }


        public static double GetPriceByTickerAndDate(string ticker, DateTime date)
        {
            return DataStore.PriceTimeSeries[ticker].Numbers.GetValueOrDefault(date);
        }

        public static double GetCurrentPriceByTicker(string ticker)
        {
            return DataStore.CurrentPrices[ticker];
        }

        // [ExcelFunction(Description = "Retrieve Returns Correlation Matrix from Ticker List")]
        public static double[,] GetCorrelationMatrixFromTickers(object[] tickers, bool useEwma)
        {
            return GetCorrelationMatrixFromTickers_(tickers, useEwma);
        }

        // [ExcelFunction(Description = "Retrieve Returns Correlation Matrix from Ticker List")]
        public static double[,] GetCovarianceMatrixFromTickers(object[] tickers, bool useEwma)
        {
            return GetCovarianceMatrixFromTickers_(tickers, useEwma);
        }


        // [ExcelFunction(Description = "Retrieve Return Volatilities Vector from Ticker List")]
        public static double[] GetVolsFromTickers(object[] tickers)
        {
            return GetVolsFromTickers_(tickers);
        }

        // [ExcelFunction(Description = "Retrieve Return VolatilitiesTicker")]
        public static double GetVolFromTicker(string ticker, bool useEwma)
        {
            var vols = useEwma ? DataStore.OneDayEwmaVols : DataStore.OneDayVolatilities;
            return vols[ticker];
        }


        // [ExcelFunction(Description = "Retrieve Return Variances Matrix from Ticker List")]
        public static double GetVarianceFromTicker(string ticker, bool useEwma)
        {
            var variances = useEwma ? DataStore.OneDayEwmaVariances : DataStore.OneDayVariances;
            return variances[ticker];
        }


        // [ExcelFunction(Description = "Retrieve Ewma Return Variances Matrix from Ticker List")]
        public static double GetEwmaVarianceFromTicker(string ticker)
        {
            return GetVarianceFromTicker(ticker, true);
        }


        //  [ExcelFunction(Description = "Retrieve Return Volatilities Matrix from Ticker")]
        public static double GetEwmaAverageReturnFromTicker(string ticker)
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
