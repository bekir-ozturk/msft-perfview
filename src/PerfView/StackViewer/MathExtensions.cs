using System;

namespace PerfView
{
    public static class MathExtensions
    {
        public static float Clamp(float value, float min, float max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }
    }
}
