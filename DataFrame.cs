using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;

namespace Quantum
{
    public class DataFrame
    {
        public DataFrame(Dictionary<string, TimeSeries> frame)
        {
            Index = CreateIndexFromFrame(frame);
            Frame = MatchFrameToIndex(frame);
        }


        public Dictionary<string, TimeSeries> Frame { get; }
        public DateTime[] Index { get; }

        private Dictionary<string, TimeSeries> MatchFrameToIndex(Dictionary<string, TimeSeries> frame)
        {
            //Dictionary<DateTime, double> Numbers { get; }

            foreach (var ticker in frame.Keys)
            {
                var index = frame[ticker].Numbers;
                foreach (var dt in Index)
                {
                    if (index.ContainsKey(dt))
                        continue;
                    index[dt] = double.NaN;
                }
            }

            return frame;
        }

        private static DateTime[] CreateIndexFromFrame(Dictionary<string, TimeSeries> frame)
        {
            var dates = new HashSet<DateTime>();

            foreach (var ticker in frame.Keys)
            {
                dates.UnionWith(frame[ticker].Dates);
            }

            return dates.ToArray();

        }


    }
}