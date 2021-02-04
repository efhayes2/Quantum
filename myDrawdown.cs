using System;
using System.Collections.Generic;

namespace Quantum
{
    public static class MyDrawdown
    {
        public static double[] ComputeTheDrawdown(double[] dates, double[] nav, double nav0)
        {
            var maxValue = nav0;
            var minValue = nav0;
            var currentDrawdown = minValue / maxValue - 1;

            var count = nav.Length;

            var drawDownReached = 0;
            var drawDownLengthInDays = 0;
            var maxLengthInDays = 0;

            for (var i = 0; i < count; i++)
            {
                
                if (nav[i] < minValue)
                {
                    drawDownLengthInDays++;
                    minValue = nav[i];
                    var drawdown = minValue / maxValue - 1;
                    if (drawdown < currentDrawdown)
                    {
                        currentDrawdown = drawdown;
                        drawDownReached = i;
                    }
                }
                if (!(nav[i] > maxValue)) continue;
                maxLengthInDays = drawDownLengthInDays;
                drawDownLengthInDays = 0;
                maxValue = nav[i];
                minValue = maxValue;
                //drawdown = minValue / maxValue - 1;
            }

            /*
            var retVals = new double[3];

            retVals[0] = dates[drawDownReached];
            retVals[1] = dates[drawDownEnds];
            retVals[2] = currentDrawdown;
            */

            var retVals = new double[3];

            retVals[0] = dates[drawDownReached];
            retVals[1] = maxLengthInDays;
            retVals[2] = currentDrawdown;

            return retVals;
        }
    }
}
