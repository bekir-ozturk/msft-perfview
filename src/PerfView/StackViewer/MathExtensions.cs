using System;

namespace PerfView
{
    public static class MathExtensions
    {
        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }
    }
}
