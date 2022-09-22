using System.Windows.Media;

namespace PerfView
{
    public static class ColorExtensions
    {
        public static Color Scale(this Color color, double coeficient)
        {
            return Color.FromRgb(
                (byte)MathExtensions.Clamp(color.R * coeficient, byte.MinValue, byte.MaxValue),
                (byte)MathExtensions.Clamp(color.G * coeficient, byte.MinValue, byte.MaxValue),
                (byte)MathExtensions.Clamp(color.B * coeficient, byte.MinValue, byte.MaxValue)
            );
        }
    }
}
