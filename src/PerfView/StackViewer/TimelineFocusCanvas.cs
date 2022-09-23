using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace PerfView
{
    internal class TimelineFocusCanvas : PanAndZoomCanvas
    {
        private static readonly Pen _gridLinePen = new Pen(new SolidColorBrush(Color.FromRgb(55,55,55)), 1);
        private static readonly Pen _gridMinorLinePen = new Pen(new SolidColorBrush(Color.FromRgb(125,125,125)), 1);

        public class PanEventArgs : EventArgs
        {
            public float TimeDelta { get; set; }
        }

        public class ZoomEventArgs : EventArgs
        {
            public float TimeOffset { get; set; }
            public float Scale { get; set; }
        }

        public event EventHandler<EventArgs> Reset;
        public event EventHandler<PanEventArgs> Pan;
        public event EventHandler<ZoomEventArgs> Zoom;

        public TimelineFocusCanvas()
            : base()
        {
        }

        internal void Update(TimelineVisuals visuals)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (RenderSize.IsEmpty)
            {
                return;
            }

            const int RowGap = 5;
            const int RowHeight = 20;
            const int Padding = 5;
            const int TimeRibbonHeight = 20;
            const float FontSize = RowHeight - (2 * Padding);
            float width = (float)RenderSize.Width;
            float height = (float)RenderSize.Height;
            int threadsCount = visuals.VisualsPerThreadId.Count;
            float startingFrame = visuals.StartingFrame;
            float endingFrame = visuals.EndingFrame;
            float range = endingFrame - startingFrame;
            m_PixelsPerUnit = width / range;

            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
                int visibleFrames = (int)(visuals.EndingFrame - visuals.StartingFrame);
                double gridPeriod = Math.Pow(10, (int)Math.Log10(visibleFrames));

                double gridStartFrame = visuals.StartingFrame - (visuals.StartingFrame % gridPeriod);
                double steps = gridPeriod / 10;
                double minorLineOpacity = 1 - (visibleFrames - gridPeriod) / gridPeriod;
                ((SolidColorBrush)_gridMinorLinePen.Brush).Opacity = minorLineOpacity;

                int iteration = 0;
                while (gridStartFrame < visuals.EndingFrame)
                {
                    double offsetX = (gridStartFrame - startingFrame) * m_PixelsPerUnit;
                    context.DrawLine(
                        iteration % 10 == 0 ? _gridLinePen : _gridMinorLinePen,
                        new Point(offsetX, TimeRibbonHeight),
                        new Point(offsetX, 1000));

                    if (iteration % 10 == 0 || minorLineOpacity > 0) 
                    {
                        context.Text(
                            gridStartFrame.ToString(),
                            Typeface,
                            FontSize,
                            offsetX - 100,
                            0,
                            200,
                            TimeRibbonHeight,
                            iteration % 10 == 0 ? _gridLinePen.Brush : _gridMinorLinePen.Brush,
                            TextAlignment.Center);
                    }

                    iteration++;
                    gridStartFrame += steps;
                }

                var threads = visuals.VisualsPerThreadId.Select(
                    (Entry, Index) => new { Index, Entry.Key, Entry.Value }
                );
                foreach (var thread in threads)
                {
                    var i = thread.Index;
                    var threadId = thread.Key;
                    var workVisuals = thread.Value;

                    foreach (var workVisual in workVisuals)
                    {
                        var brush = new SolidColorBrush(workVisual.DisplayColor);
                        var pen = new Pen(
                            new SolidColorBrush(workVisual.DisplayColor.Scale(0.8)),
                            1.0
                        );
                        double startX = (workVisual.StartingFrame - startingFrame) * m_PixelsPerUnit;
                        double startY = TimeRibbonHeight + i * RowHeight + i * RowGap;
                        double endX = (workVisual.EndingFrame - startingFrame) * m_PixelsPerUnit;
                        startX = MathExtensions.Clamp(startX, -5, ActualWidth + 5);
                        endX = MathExtensions.Clamp(endX, -5, ActualWidth + 5);
                        double workWidth = endX - startX;

                        if (workWidth < 0.1f)
                        {
                            continue;
                        }

                        context.Rectangle(
                            brush,
                            pen,
                            startX,
                            startY,
                            workWidth,
                            RowHeight,
                            Padding
                        );

                        double textWidth = workWidth - (2 * Padding);
                        if (textWidth < 5f)
                        {
                            continue;
                        }

                        if (workVisual.DisplayName == null)
                        {
                            continue;
                        }

                        context.Text(
                            workVisual.DisplayName,
                            Typeface,
                            FontSize,
                            startX + Padding,
                            startY + Padding,
                            textWidth,
                            RowHeight,
                            Brushes.Black
                        );
                    }
                }
            }

            Visuals.Replace(visual, 0);
        }

        protected override Point GetTransformedPosition(Point point)
        {
            return point;
        }

        protected override void OnPan(Vector delta)
        {
            float timeDelta = (float)delta.X / m_PixelsPerUnit;
            Pan.Invoke(this, new PanEventArgs { TimeDelta = timeDelta });
        }

        protected override void OnReset()
        {
            Reset.Invoke(this, new EventArgs());
        }

        protected override void OnZoom(Point center, Vector delta)
        {
            float timeOffset = (float)center.X / m_PixelsPerUnit;
            float scale = (float)delta.X;
            Zoom.Invoke(this, new ZoomEventArgs { TimeOffset = timeOffset, Scale = scale });
        }

        private static readonly Typeface Typeface = new Typeface("Consolas");

        private float m_PixelsPerUnit = 1.0f;
    }
}
