using PerfView.StackViewer;
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using Pen = System.Windows.Media.Pen;
using Size = System.Windows.Size;

namespace PerfView
{
    internal class TimelineFocusCanvas : PanAndZoomCanvas
    {
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

            float labelsWidth = GetTextWidth("Thread 999999") + (2 * Padding);
            float width = (float)RenderSize.Width;
            float height = (float)RenderSize.Height;
            int threadsCount = visuals.VisualsPerThreadId.Count;
            float startingFrame = visuals.StartingFrame;
            float endingFrame = visuals.EndingFrame;
            float range = endingFrame - startingFrame;
            float scale = width / range;

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

                    float startY = i * RowHeight + i * RowGap;

                    context.Text(
                        "Thread " + threadId,
                        Typeface,
                        FontSize,
                        Padding,
                        startY + Padding,
                        labelsWidth,
                        RowHeight
                    );

                    foreach (var workVisual in workVisuals)
                    {
                        var brush = new SolidColorBrush(workVisual.DisplayColor);
                        var pen = new Pen(
                            new SolidColorBrush(workVisual.DisplayColor.Scale(0.8)),
                            1.0
                        );
                        float startX = (workVisual.StartingFrame - startingFrame) * scale;
                        float safeStartX = MathExtensions.Clamp(startX, 0.0f, width);
                        float endX = (workVisual.EndingFrame - startingFrame) * scale;
                        float safeEndX = MathExtensions.Clamp(endX, 0.0f, width);
                        float workWidth = safeEndX - safeStartX;

                        if (workWidth < 0.1f)
                        {
                            continue;
                        }

                        context.Rectangle(
                            brush,
                            pen,
                            labelsWidth + safeStartX,
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
                            labelsWidth + safeStartX + Padding,
                            startY + Padding,
                            textWidth,
                            RowHeight
                        );
                    }
                }
            }

            Visuals.Replace(visual, 0);
        }

        private float GetTextWidth(string text)
        {
            var textBlock = new TextBlock { Text = text };
            textBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            textBlock.Arrange(new Rect(textBlock.DesiredSize));
            return (float)textBlock.ActualWidth;
        }

        private static readonly Typeface Typeface = new Typeface("Consolas");
    }
}
