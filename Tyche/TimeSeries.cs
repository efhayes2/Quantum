using System;
using System.Collections.Generic;
using System.Linq;

namespace Tyche
{
    public class TimeSeries
    {
        public string Name;
        public Dictionary<DateTime, double> Numbers { get; }
        public Dictionary<DateTime, double> Returns { get; set; }
        public DateTime[] Dates { get; }


        public TimeSeries(string name, DateTime[] dates, Dictionary<DateTime, double> numbers)
        {
            Dates = dates;
            Numbers = numbers;
            Name = name;
        }

        public TimeSeries(string name, List<DateTime> dates, IEnumerable<double> price)
        {
            Numbers = dates.Zip(price, (k, v) => new { Key = k, Value = v })
                .ToDictionary(x => x.Key, x => x.Value);

            Dates = dates.ToArray();
            Name = name;
        }


        public void ComputeReturns()
        {
            var returns = new Dictionary<DateTime, double>();
            var sortedKeys = Numbers.Keys.ToList();
            sortedKeys.Sort();
            sortedKeys.Reverse();

            for (var i = 0; i < sortedKeys.Count - 1; i++)
            {
                var ret = Math.Log(Numbers[sortedKeys[i]] / Numbers[sortedKeys[i+1]]);
                returns[sortedKeys[i]] = ret;
            }

            Returns = returns;
        }
    }
}