using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quantum
{
    public class SimpleDrawdown
    {
        public double Peak { get; set; }
        public double Trough { get; set; }
        public double MaxDrawDown { get; set; }

        public SimpleDrawdown()
        {
            Peak = double.NegativeInfinity;
            Trough = double.PositiveInfinity;
            MaxDrawDown = 0;
        }

        public void Calculate(double newValue)
        {
            if (newValue > Peak)
            {
                Peak = newValue;
                Trough = Peak;
            }
            else if (newValue < Trough)
            {
                Trough = newValue;
                var tmpDrawDown = Peak - Trough;
                if (tmpDrawDown > MaxDrawDown)
                    MaxDrawDown = tmpDrawDown;
            }
        }
    }
}
