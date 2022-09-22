using System;
using System.Windows.Media;

namespace PerfView.Utilities
{
    internal class ColorUtilities
    {
        public static Color ColorFromHSV(double h, double s, double v)
        {
            double r = 0;
            double g = 0;
            double b = 0;

            int i = (int)Math.Floor(h * 6d);
            double f = h * 6d - i;
            double p = v * (1d - s);
            double q = v * (1d - f * s);
            double t = v * (1d - (1d - f) * s);

            switch (i % 6)
            {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;
                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;
                case 5:
                    r = v;
                    g = p;
                    b = q;
                    break;
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}
