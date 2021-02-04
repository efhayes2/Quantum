using System;
using Accord.Statistics;
using Accord.Statistics.Distributions.Univariate;
using ExcelDna.Integration;

namespace Quantum

{ 
    /// <summary>
    /// Summary description for BlackSholes.
    /// </summary>
    public static class BlackScholes
    {
        /* The Black and Scholes (1973) Stock option formula
		 * C# Implementation
		 * uses the C# Math.PI field rather than a constant as in the C++ implementaion
		 * the value of Pi is 3.14159265358979323846
		*/


        public static double StraddlePrice(double S, double K, double T, double r, double q, double vol, bool useRhs)
        {

            var call = Price(true, S, K, T, r, q, vol, useRhs);
            var put = Price(false, S, K, T, r, q, vol, useRhs);
            return call + put;
        }

        public static double StraddleDelta(double S, double K, double T, double r, double q, double vol, bool useRhs)
        {

            var call = Delta(true, S, K, T, r, q, vol, useRhs);
            var put = Delta(false, S, K, T, r, q, vol, useRhs);
            return call + put;
        }



        public static double Price(bool isCallOption, double S, double K, double T, double r, double q, double vol, bool useRhs)
        {
            
            var df = Math.Exp(-r * T);
            var divDf = Math.Exp(-q * T);

            var F = S * divDf / df;

            var normal = new NormalDistribution();

            var d1 = (Math.Log(F / K) + (r + vol * vol / 2.0) * T) / (vol * Math.Sqrt(T));
            var d2 = d1 - vol * Math.Sqrt(T);

            var nd1 = normal.DistributionFunction(d1);
            var nd2 = normal.DistributionFunction(d2);

            var nNegatived1 = normal.DistributionFunction(-d1);
            var nNegatived2 = normal.DistributionFunction(-d2);

            var price = df * (isCallOption ? F * nd1 - K * nd2 : K * nNegatived2 - F * nNegatived1);

            return (useRhs ? price : price / S);                        
        }


        public static double Delta(bool isCallOption, double S, double K, double T, double r, double q, double vol, bool useRhs)
        {
            var df = Math.Exp(-r * T);
            var divDf = Math.Exp(-q * T);
            var F = S * divDf / df;

            var normal = new NormalDistribution();

            var d1 = (Math.Log(F / K) + (r + vol * vol / 2.0) * T) / (vol * Math.Sqrt(T));            
            var nd1 = normal.DistributionFunction(d1);
            
            var delta = divDf * (isCallOption ? nd1 : nd1 - 1.0);

            if (useRhs)
                return delta;

            var price = Price(isCallOption, S, K, T, r, q, vol, true);
            return delta - price / S;
        }

        [ExcelFunction(Description = "Black-Scholes Price")]
        public static double BlackScholesPrice(
    [ExcelArgument("True if it is a call options")] bool isCallOption,
    [ExcelArgument("Spot Price")] double spot,
    [ExcelArgument("Strike Price")] double strike,
    [ExcelArgument("Time to maturity")] double t,
    [ExcelArgument("Continuous interest rate")] double r,
    [ExcelArgument("Continuous dividend yield")] double q,
    [ExcelArgument("Vol")] double vol,
    [ExcelArgument("Use RHS")] bool useRhs)

        {
            return BlackScholes.Price(isCallOption, spot, strike, t, r, q, vol, useRhs);
        }



        [ExcelFunction(Description = "Black-Scholes Delta")]
        public static double BlackScholesDelta(
            [ExcelArgument("True if it is a call options")] bool isCallOption,
            [ExcelArgument("Spot Price")] double spot,
            [ExcelArgument("Strike Price")] double strike,
            [ExcelArgument("Time to maturity")] double t,
            [ExcelArgument("Continuous interest rate")] double r,
            [ExcelArgument("Continuous dividend yield")] double q,
            [ExcelArgument("Vol")] double vol,
            [ExcelArgument("Use RHS")] bool useRhs)
        {
            return BlackScholes.Delta(isCallOption, spot, strike, t, r, q, vol, useRhs);
        }


        [ExcelFunction(Description = "Black-Scholes Price")]
        public static double BlackScholesStraddlePrice(
            [ExcelArgument("Spot Price")] double spot,
            [ExcelArgument("Strike Price")] double strike,
            [ExcelArgument("Time to maturity")] double t,
            [ExcelArgument("Continuous interest rate")] double r,
            [ExcelArgument("Continuous dividend yield")] double q,
            [ExcelArgument("Vol")] double vol,
            [ExcelArgument("Use RHS")] bool useRhs)

        {
            return BlackScholes.StraddlePrice(spot, strike, t, r, q, vol, useRhs);
        }



        [ExcelFunction(Description = "Black-Scholes Straddle Price")]
        public static double BlackScholesStraddleDelta(
            [ExcelArgument("Spot Price")] double spot,
            [ExcelArgument("Strike Price")] double strike,
            [ExcelArgument("Time to maturity")] double t,
            [ExcelArgument("Continuous interest rate")] double r,
            [ExcelArgument("Continuous dividend yield")] double q,
            [ExcelArgument("Vol")] double vol,
            [ExcelArgument("Use RHS")] bool useRhs)

        {
            return BlackScholes.StraddleDelta(spot, strike, t, r, q, vol, useRhs);
        }



        [ExcelFunction(Description = "Compute the drawdown of an NAV series")]
        public static object ComputeDrawdown(double[] dates, double[] nav, double nav0)
        {
            return MyDrawdown.ComputeTheDrawdown(dates, nav, nav0);
        }



    }
}