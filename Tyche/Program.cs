using System;
using System.Collections.Generic;

namespace Tyche
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Running test of Tyche calculator ...");
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var startupCompleted = new RunStartup();

            // var tickers = new object[] {"AAPL", "FB"};
            // var tickersString = new List<string>() { "AAPL", "FB", "GOOG", "MA", "MSFT", "TER", "V" };

            //var tickers = new object[] {"A.US", "AA.US", "AAAU.US", "AACG.US", "AADR.US", "AAIC.US"};
            //var tickersString = new List<string>() { "A.US", "AA.US", "AAAU.US", "AACG.US", "AADR.US", "AAIC.US" };

            var tickers = new object[] { "A.US", "AA.US" }; //, "AAAU.US", "AACG.US", "AADR.US", "AAIC.US"};
            var tickersString = new List<string>() { "A.US", "AA.US" };


            var quantities = new List<double>() { 1000.0, 1000.0 }; //, 200.0, 800.0, 1000.0, 1000.0, 1000.0};
            var portfolio = new Portfolio(tickersString, quantities);

            var vols = VolManager.GetVolsFromTickers(tickers);
            var ewma = VolManager.GetCorrelationMatrixFromTickers(tickers, true);

            watch.Stop();
            Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");

            Console.WriteLine("Terminating the application...");
            Console.WriteLine("Hit any key to continue ...");
            
            Console.Read();
        }
    }
}