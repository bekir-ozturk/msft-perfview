using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PerfView.FlameGraph;

namespace PerfView.StackViewer
{
    public class FlameGraphDrawingCanvas : TransformPanAndZoomCanvas
    {
        private static readonly Typeface Typeface = new Typeface("Consolas");
        public event EventHandler<string> CurrentFlameBoxChanged;
        private readonly FlameBoxesMap flameBoxesMap = new FlameBoxesMap();
        private readonly ToolTip tooltip = new ToolTip() { FontSize = 20.0 };

        public FlameGraphDrawingCanvas()
            : base()
        {
            MouseMove += OnMouseMove;
            MouseLeave += OnMouseLeave;
        }

        public bool IsEmpty => Visuals.Items.Count == 0;

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

        public void Draw(IEnumerable<FlameBox> boxes)
        {
            Clear();

            var random = new Random(11);
            var visual = new DrawingVisual { Transform = _transform }; // we have only one visual to provide best possible perf

            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                System.Drawing.Font forSize = null;

                foreach (FlameBox box in boxes)
                {
                    SolidColorBrush brush;
                    if (box.Color == FlameColor.Default)
                    {
                        var id = random.Next(3);
                        brush = new SolidColorBrush(greenShades[id]);
                    }
                    else {
                        brush = new SolidColorBrush(colorsMapping[box.Color]);
                    }

                    drawingContext.DrawRectangle(
                        brush,
                        null,  // no Pen is crucial for performance
                        new Rect(box.X, box.Y, box.Width, box.Height));

                    if (box.Width * _transform.Matrix.M11 > 50 && box.Height * _transform.Matrix.M22 >= 6) // we draw the text only if humans can see something
                    {
                        if (forSize == null)
                        {
                            forSize = new System.Drawing.Font("Consolas", (float)box.Height, System.Drawing.GraphicsUnit.Pixel);
                        }

                        FormattedText text = new FormattedText(
                                box.Text,
                                CultureInfo.InvariantCulture,
                                FlowDirection.LeftToRight,
                                Typeface,
                                Math.Min(forSize.SizeInPoints, 20),
                                System.Windows.Media.Brushes.Black)
                        {
                            MaxTextWidth = box.Width,
                            MaxTextHeight = box.Height
                        };

                        drawingContext.DrawText(text, new Point(box.X, box.Y));
                    }

                    flameBoxesMap.Add(box);
                }

                AddVisual(visual);

                flameBoxesMap.Sort();
            }
        }

        /// <summary>
        /// DrawingVisual provides no tooltip support, so I had to implement it myself.. I feel bad for it.
        /// </summary>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsEmpty && e.LeftButton == MouseButtonState.Released)
            {
                Point position = _transform.Inverse.Transform(Mouse.GetPosition(this));
                string tooltipText = flameBoxesMap.Find(position);
                if (tooltipText != null)
                {
                    ShowTooltip(tooltipText);
                    CurrentFlameBoxChanged(this, tooltipText);
                    return;
                }
            }

            HideTooltip();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            HideTooltip();
        }

        private void ShowTooltip(string text)
        {
            if (ReferenceEquals(tooltip.Content, text) && tooltip.IsOpen)
            {
                return;
            }

            tooltip.IsOpen = false; // by closing and opening it again we restart it's position to the current mouse position..
            tooltip.Content = text;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            tooltip.IsOpen = true;
            tooltip.PlacementTarget = this;
        }

        private void HideTooltip()
        {
            tooltip.IsOpen = false;
        }

        private void Clear()
        {
            for (int i = Visuals.Items.Count - 1; i >= 0; i--)
            {
                DeleteVisual(Visuals.Items[i]);
            }

            flameBoxesMap.Clear();
        }

        private void AddVisual(Visual visual)
        {
            Visuals.Items.Add(visual);

            AddLogicalChild(visual);
        }

        private void DeleteVisual(Visual visual)
        {
            Visuals.Items.Remove(visual);

            RemoveLogicalChild(visual);
        }

        private class FlameBoxesMap
        {
            private readonly SortedDictionary<Range, List<FlameBox>> boxesMap = new SortedDictionary<Range, List<FlameBox>>();

            internal void Clear()
            {
                boxesMap.Clear();
            }

            internal void Add(FlameBox flameBox)
            {
                Range row = new Range(flameBox.Y, flameBox.Y + flameBox.Height);

                if (!boxesMap.TryGetValue(row, out List<FlameBox> list))
                {
                    boxesMap.Add(row, list = new List<FlameBox>());
                }

                list.Add(flameBox);
            }

            internal void Sort()
            {
                foreach (List<FlameBox> row in boxesMap.Values)
                {
                    row.Sort(CompareByX); // sort the boxes from left to the right
                }
            }

            internal string Find(Point point)
            {
                foreach (KeyValuePair<Range, List<FlameBox>> rowData in boxesMap)
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
                            else if (rowData.Value[mid].X + rowData.Value[mid].Width < point.X)
                            {
                                low = mid + 1;
                            }
                            else
                            {
                                break;
                            }
                        }

                        return rowData.Value[mid].X <= point.X && point.X <= rowData.Value[mid].X + rowData.Value[mid].Width
                            ? rowData.Value[mid].TooltipText
                            : null;
                    }
                }

                return null;
            }

            private static int CompareByX(FlameBox left, FlameBox right)
            {
                return left.X.CompareTo(right.X);
            }

            private readonly struct Range : IEquatable<Range>, IComparable<Range>
            {
                private readonly double Start, End;

                internal Range(double start, double end)
                {
                    Start = start;
                    End = end;
                }

                internal bool Contains(double y)
                {
                    return Start <= y && y <= End;
                }

                public override bool Equals(object obj)
                {
                    throw new InvalidOperationException("No boxing");
                }

                public bool Equals(Range other)
                {
                    return other.Start == Start && other.End == End;
                }

                public int CompareTo(Range other)
                {
                    return other.Start.CompareTo(Start);
                }

                public override int GetHashCode()
                {
                    return (Start * End).GetHashCode();
                }
            }
        }
    }
}
