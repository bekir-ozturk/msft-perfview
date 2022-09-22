using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace PerfView
{
    internal class TimelineFocusCanvas : PanAndZoomCanvas
    {
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
            const float FontSize = RowHeight - (2 * Padding);
            float width = (float)RenderSize.Width;
            float height = (float)RenderSize.Height;
            int threadsCount = visuals.VisualsPerThreadId.Count;
            float startingFrame = visuals.StartingFrame;
            float endingFrame = visuals.EndingFrame;
            float range = endingFrame - startingFrame;
            m_PixelsPerUnit = width / range;

            float fontSizeInPoints = new Font(
                Typeface.FontFamily.ToString(),
                FontSize,
                GraphicsUnit.Pixel
            )
                .SizeInPoints;

            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext context = visual.RenderOpen())
            {
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
                        float startX = (workVisual.StartingFrame - startingFrame) * m_PixelsPerUnit;
                        float safeStartX = MathExtensions.Clamp(startX, 0.0f, width);
                        float startY = i * RowHeight + i * RowGap;
                        float endX = (workVisual.EndingFrame - startingFrame) * m_PixelsPerUnit;
                        float safeEndX = MathExtensions.Clamp(endX, 0.0f, width);
                        float workWidth = safeEndX - safeStartX;

                        if (workWidth < 0.1f)
                        {
                            continue;
                        }

                        context.Rectangle(
                            brush,
                            pen,
                            safeStartX,
                            startY,
                            workWidth,
                            RowHeight,
                            Padding
                        );

                        double textWidth = workWidth - (2 * Padding);
                        if (textWidth < 0.1f)
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
                            safeStartX + Padding,
                            startY + Padding,
                            textWidth,
                            RowHeight
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
