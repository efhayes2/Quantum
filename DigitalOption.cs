using System;
using Accord.Statistics.Distributions.Univariate;
using ExcelDna.Integration;

namespace Quantum
{
    class DigitalOption
    {
        [ExcelFunction(Description = "Digital Option Price")]
        public static double DigitalOptionPrice(
            [ExcelArgument("True if it is a call options")] bool isCallOption,
            [ExcelArgument("Spot Price")] double spot,
            [ExcelArgument("Strike Price")] double strike,
            [ExcelArgument("Time to maturity")] double t,
            [ExcelArgument("Continuous interest rate")] double r,
            [ExcelArgument("Continuous dividend yield")] double q,
            [ExcelArgument("Vol")] double vol,
            [ExcelArgument("Use RHS")] bool useRhs)

        {
            var df = Math.Exp(-r * t);
            var divDf = Math.Exp(-q * t);

            var forward = spot * divDf / df;

            var normal = new NormalDistribution();

            var d1 = (Math.Log(forward / strike) + (r + vol * vol / 2.0) * t) / (vol * Math.Sqrt(t));
            var d2 = d1 - vol * Math.Sqrt(t);

            var nd1 = normal.DistributionFunction(d1);
            var nd2 = normal.DistributionFunction(d2);

            var nNegatived1 = normal.DistributionFunction(-d1);
            var nNegatived2 = normal.DistributionFunction(-d2);

            var forwardPrice = isCallOption ? nd2 : nNegatived2;

            return df * (useRhs ? forwardPrice : forwardPrice / spot);
        }
    }
}
