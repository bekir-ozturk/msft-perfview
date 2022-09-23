﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PerfView.FlameGraph;

namespace PerfView
{
    public class ColorLegendDrawingCanvas : Canvas
    {
        private static readonly Typeface Typeface = new Typeface("Consolas");

        public event EventHandler<string> CurrentFlameBoxChanged;

        private List<Visual> visuals = new List<Visual>();
        private FlameBoxesMap flameBoxesMap = new FlameBoxesMap();
        private ToolTip tooltip = new ToolTip() { FontSize = 20.0 };
        private ScaleTransform scaleTransform = new ScaleTransform(1.0f, 1.0f, 0.0f, 0.0f);
        private Cursor cursor;

        public ColorLegendDrawingCanvas()
        {
            MouseMove += OnMouseMove;
            MouseLeave += OnMouseLeave;
            PreviewMouseWheel += OnPreviewMouseWheel;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            PreviewKeyDown += OnPreviewKeyDown;
            Focusable = true;
        }

        public bool IsEmpty => visuals.Count == 0;

        protected override int VisualChildrenCount => visuals.Count;

        protected override Visual GetVisualChild(int index) => visuals[index];

        private bool IsZoomed => scaleTransform.ScaleX != 1.0;

        private Dictionary<FlameColor, Color> colorsMapping = new Dictionary<FlameColor, Color>() {
            {FlameColor.Grey,  Color.FromRgb(128, 128, 128)},
            {FlameColor.Brown,  Color.FromRgb(181, 101, 29)},
            {FlameColor.Blue,  Color.FromRgb(65, 105, 225)},
            {FlameColor.Red,  Color.FromRgb(255, 0, 0)}
        };

        private List<Color> greenShades = new List<Color>(){
                Color.FromRgb(0,255,0),
                Color.FromRgb(124,252,0),
                Color.FromRgb(127,255,0),
                Color.FromRgb(173,255,47)
            };

        public void Draw(double witdh, double height)
        {
            Clear();

            var visual = new DrawingVisual { Transform = scaleTransform }; // we have only one visual to provide best possible perf

            using (DrawingContext drawingContext = visual.RenderOpen())
            {

                DrawColorLegend(drawingContext, height/25, witdh/2);
                AddVisual(visual);

                flameBoxesMap.Sort();
            }
        }

        private void DrawColorLegend(DrawingContext drawingContext, double blockSize, double borderLine) {

           // var blockSize = 10;
            var gap = blockSize;

            var forSize = new System.Drawing.Font("Consolas", (int)blockSize*2, System.Drawing.GraphicsUnit.Pixel);

            foreach (var color in FlameBox.MappingToColor) {
                if (color.Value == FlameColor.Default)
                    continue;

              var rgbColor = colorsMapping[color.Value];
                drawingContext.DrawRectangle(
                    new SolidColorBrush(rgbColor),
                    null,
                    new Rect(blockSize, gap, blockSize, blockSize));

                var text = new FormattedText(
                        color.Key,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        Typeface,
                        Math.Min(forSize.SizeInPoints, 20),
                        Brushes.Black);

                drawingContext.DrawText(text, new Point(3*blockSize, gap));

                gap += (blockSize*2);
            }

            drawingContext.DrawRectangle(
                new SolidColorBrush(greenShades[0]),
                null,
                new Rect(blockSize, gap, blockSize, blockSize));

            var textModule = new FormattedText(
                "Module",
                 CultureInfo.InvariantCulture,
                 FlowDirection.LeftToRight,
                 Typeface,
                 Math.Min(forSize.SizeInPoints, 20),
                 Brushes.Black);

            drawingContext.DrawText(textModule, new Point(3 * blockSize, gap));

            drawingContext.DrawRectangle(
                 null,
                new Pen(new SolidColorBrush(Color.FromRgb(0, 0, 0)), 1),
                new Rect(0, 0, borderLine, gap + (blockSize * 2)));
        }

        /// <summary>
        /// DrawingVisual provides no tooltip support, so I had to implement it myself.. I feel bad for it.
        /// </summary>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsEmpty && e.LeftButton == MouseButtonState.Released)
            {
                var position = scaleTransform.Inverse.Transform(Mouse.GetPosition(this));
                var tooltipText = flameBoxesMap.Find(position);
                if (tooltipText != null)
                {
                    ShowTooltip(tooltipText);
                    CurrentFlameBoxChanged(this, tooltipText);
                    return;
                }
            }
            else if (!IsEmpty && e.LeftButton == MouseButtonState.Pressed && IsZoomed)
            {
                var relativeMousePosition = scaleTransform.Inverse.Transform(Mouse.GetPosition(this));
                MoveZoomingCenterPoint(relativeMousePosition.X, relativeMousePosition.Y);
            }

            HideTooltip();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            HideTooltip();
            ResetCursor(); // leaving the control while still zooming and OnMouseLeftButtonUp won't fire
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            float modifier = e.Delta > 0 ? 1.1f : 0.9f;

            var relativeMousePosition = scaleTransform.Inverse.Transform(Mouse.GetPosition(this));

            scaleTransform.ScaleX = Math.Max(1.0, scaleTransform.ScaleX * modifier);
            scaleTransform.ScaleY = Math.Max(1.0, scaleTransform.ScaleY * modifier);
            scaleTransform.CenterX = relativeMousePosition.X;
            scaleTransform.CenterY = relativeMousePosition.Y;

            Keyboard.Focus(this); // make it possible to handle Arrow keys and move CenterX & Y scaling points
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsZoomed)
            {
                cursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = Cursors.Hand; // emulate drag&drop cursor style
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsZoomed)
            {
                ResetCursor();
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsZoomed)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Left:
                    MoveZoomingCenterPoint(scaleTransform.CenterX * 0.9, scaleTransform.CenterY);
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveZoomingCenterPoint(scaleTransform.CenterX * 1.1, scaleTransform.CenterY);
                    e.Handled = true;
                    break;
                case Key.Up:
                    MoveZoomingCenterPoint(scaleTransform.CenterX, scaleTransform.CenterY * 0.9);
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveZoomingCenterPoint(scaleTransform.CenterX, scaleTransform.CenterY * 1.1);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void ShowTooltip(string text)
        {
            if (object.ReferenceEquals(tooltip.Content, text) && tooltip.IsOpen)
            {
                return;
            }

            tooltip.IsOpen = false; // by closing and opening it again we restart it's position to the current mouse position..
            tooltip.Content = text;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            tooltip.IsOpen = true;
            tooltip.PlacementTarget = this;
        }

        private void HideTooltip() => tooltip.IsOpen = false;

        private void Clear()
        {
            for (int i = visuals.Count - 1; i >= 0; i--)
            {
                DeleteVisual(visuals[i]);
                visuals.RemoveAt(i);
            }

            flameBoxesMap.Clear();
        }

        private void AddVisual(Visual visual)
        {
            visuals.Add(visual);

            base.AddVisualChild(visual);
            base.AddLogicalChild(visual);
        }

        private void DeleteVisual(Visual visual)
        {
            base.RemoveVisualChild(visual);
            base.RemoveLogicalChild(visual);
        }

        private void MoveZoomingCenterPoint(double x, double y)
        {
            if (IsZoomed)
            {
                scaleTransform.CenterX = Math.Min(x, ActualWidth);
                scaleTransform.CenterY = Math.Min(y, ActualHeight);
            }
        }

        private void ResetCursor() => Mouse.OverrideCursor = cursor;

        private class FlameBoxesMap
        {
            private SortedDictionary<Range, List<FlameBox>> boxesMap = new SortedDictionary<Range, List<FlameBox>>();

            internal void Clear() => boxesMap.Clear();

            internal void Add(FlameBox flameBox)
            {
                var row = new Range(flameBox.Y, flameBox.Y + flameBox.Height);

                if (!boxesMap.TryGetValue(row, out var list))
                {
                    boxesMap.Add(row, list = new List<FlameBox>());
                }

                list.Add(flameBox);
            }

            internal void Sort()
            {
                foreach (var row in boxesMap.Values)
                {
                    row.Sort(CompareByX); // sort the boxes from left to the right
                }
            }

            internal string Find(Point point)
            {
                foreach (var rowData in boxesMap)
                {
                    if (rowData.Key.Contains(point.Y))
                    {
                        int low = 0, high = rowData.Value.Count - 1, mid = 0;

                        while (low <= high)
                        {
                            mid = (low + high) / 2;

                            if (rowData.Value[mid].X > point.X)
                            {
                                high = mid - 1;
                            }
                            else if ((rowData.Value[mid].X + rowData.Value[mid].Width) < point.X)
                            {
                                low = mid + 1;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (rowData.Value[mid].X <= point.X && point.X <= (rowData.Value[mid].X + rowData.Value[mid].Width))
                        {
                            return rowData.Value[mid].TooltipText;
                        }

                        return null;
                    }
                }

                return null;
            }

            private static int CompareByX(FlameBox left, FlameBox right) => left.X.CompareTo(right.X);

            private struct Range : IEquatable<Range>, IComparable<Range>
            {
                private readonly double Start, End;

                internal Range(double start, double end)
                {
                    Start = start;
                    End = end;
                }

                internal bool Contains(double y) => Start <= y && y <= End;

                public override bool Equals(object obj) => throw new InvalidOperationException("No boxing");

                public bool Equals(Range other) => other.Start == Start && other.End == End;

                public int CompareTo(Range other) => other.Start.CompareTo(Start);

                public override int GetHashCode() => (Start * End).GetHashCode();
            }
        }
    }
}