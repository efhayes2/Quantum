using System.Collections.Generic;
using Quantum;


namespace TestHarness
{
    internal class Program
    {
        

        private static void Main(string[] args)
        {
            var StartupCompleted = new RunStartup();
        //Console.WriteLine("Hello, World!");
            var tickers = new object[] {"AAPL", "FB"};
            var tickersString = new List<string>() { "AAPL", "FB", "GOOG", "MA", "MSFT", "TER", "V" };
            var quantities = new List<double>() {1000.0, 1000.0, 200.0, 800.0, 1000.0, 1000.0, 1000.0};
            var portfolio = new Portfolio(tickersString, quantities);

            var vols = ExcelInterface.GetVolsFromTickers(tickers);
            var ewma = ExcelInterface.GetCorrelationMatrixFromTickers(tickers, true);
        }
    }
}
