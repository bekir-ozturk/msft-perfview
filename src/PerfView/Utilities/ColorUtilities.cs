using System;
using System.Windows.Media;

namespace PerfView.Utilities
{
    internal class ColorUtilities
    {
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = (byte)value;
            byte p = (byte)(value * (1 - saturation));
            byte q = (byte)(value * (1 - f * saturation));
            byte t = (byte)(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromRgb(v, t, p);
            else if (hi == 1)
                return Color.FromRgb(q, v, p);
            else if (hi == 2)
                return Color.FromRgb(p, v, t);
            else if (hi == 3)
                return Color.FromRgb(p, q, v);
            else if (hi == 4)
                return Color.FromRgb(t, p, v);
            else
                return Color.FromRgb(v, p, q);
        }
    }
}
