﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace PerfView
{
    public static class DrawingContextExtensions
    {
        public static void Rectangle(
            this DrawingContext context,
            Brush brush,
            Pen pen,
            double x,
            double y,
            double width,
            double height,
            double rounding
        )
        {
            var rectangle = new Rect(x, y, width, height);
            context.DrawRoundedRectangle(brush, pen, rectangle, rounding, rounding);
        }

        public static void Text(
            this DrawingContext context,
            string text,
            Typeface typeface,
            double fontSize,
            double x,
            double y,
            double width,
            double height
        )
        {
            var formattedText = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                Math.Min(fontSize, 20.0),
                Brushes.Black
            )
            {
                MaxTextWidth = width,
                MaxTextHeight = height
            };
            context.DrawText(formattedText, new Point(x, y));
        }
    }
}
