using System;
using System.Collections.Generic;
using System.Linq;

namespace PerformanceTester
{
    public static class Util
    {
        internal static double Percentile(this IEnumerable<double> seq, double percentile)
        {
            var elements = seq.ToArray();
            Array.Sort(elements);
            var realIndex = percentile * (elements.Length - 1);
            var index = (int) realIndex;
            var frac = realIndex - index;
            if (index + 1 < elements.Length)
            {
                return elements[index] * (1 - frac) + elements[index + 1] * frac;
            }

            return elements[index];
        }
    }
}