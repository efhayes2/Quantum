using System;
using System.Collections.Generic;
using System.Linq;

namespace Tyche
{
   
    public class Portfolio
    {
        public static Dictionary<string, Portfolio> Portfolios = new();
        public List<DateTime> Dates { get; set; }
        public List<string> Tickers { get; }
        public List<double> Quantities { get; }
        public Dictionary<string, double> Positions { get; }
        public Dictionary<DateTime, double> PortfolioValues { get; set; }
        public Dictionary<DateTime, double> PortfolioReturns { get; set; }
        public string PortfolioId;
        private static int _portfolioCounter = 0;

        public Portfolio(List<string> tickers, List<double> quantities)
        {
            Positions = new Dictionary<string, double>();
            Tickers = tickers;
            Quantities = quantities;
            for (var i = 0; i < tickers.Count; i++)
            {
                Positions[Tickers[i]] = Quantities[i];
            }

            ComputePortfolioPriceSeries();
            PortfolioId = "Portfolio_" +
                          "" +
                          "" + _portfolioCounter;
            _portfolioCounter++;
            DataStore.CreateTimeSeriesFromPrices(PortfolioId, Dates, PortfolioValues.Values.ToList());
            DataStore.ComputeOneEwmaAverageDailyReturn(PortfolioId);
            DataStore.ComputeOneEwmaVariance(PortfolioId);
            Portfolios[PortfolioId] = this;
        }

        public void ComputePortfolioPriceSeries()
        {
            var prices = DataStore.PriceTimeSeries.Values;

            Dates = prices.ToArray()[0].Dates.ToList();
            var portfolioValues = new Dictionary<DateTime, double>();

            foreach (var date in Dates)
            {
                portfolioValues[date] = Tickers.Sum(ticker => DataStore.PriceTimeSeries[ticker].Numbers[date] * Positions[ticker]);
            }

            PortfolioValues = portfolioValues;
        }
    }
}