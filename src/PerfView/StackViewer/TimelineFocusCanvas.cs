using PerfView.StackViewer;
using System.Drawing;
using System.Linq;
using System.Windows.Media;
using Pen = System.Windows.Media.Pen;

namespace PerfView
{
    internal class TimelineFocusCanvas : RenderPanAndZoomCanvas
    {
        public TimelineFocusCanvas()
            : base()
        {
            m_VisualsHost = new VisualCollectionHost(this);
            Children.Add(m_VisualsHost);
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
            double width = RenderSize.Width;
            double height = RenderSize.Height;
            int threadsCount = visuals.VisualsPerThreadId.Count;
            int startingFrame = visuals.StartingFrame;
            int endingFrame = visuals.EndingFrame;
            int range = endingFrame - startingFrame;
            double scale = width / range;

            double fontSizeInPoints = new Font(
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
                        double startX = (workVisual.StartingFrame - startingFrame) * scale;
                        double safeStartX = MathExtensions.Clamp(startX, 0, width);
                        double startY = i * RowHeight + i * RowGap;
                        double endX = (workVisual.EndingFrame - startingFrame) * scale;
                        double safeEndX = MathExtensions.Clamp(endX, 0, width);
                        double workWidth = safeEndX - safeStartX;
                        context.Rectangle(
                            brush,
                            pen,
                            safeStartX,
                            startY,
                            workWidth,
                            RowHeight,
                            Padding
                        );

                        if (workVisual.DisplayName != null)
                        {
                            context.Text(
                                workVisual.DisplayName,
                                Typeface,
                                FontSize,
                                safeStartX + Padding,
                                startY + Padding,
                                workWidth - (2 * Padding),
                                RowHeight
                            );
                        }
                    }
                }
            }

            m_VisualsHost.Replace(visual, 0);
        }

        private static readonly Typeface Typeface = new Typeface("Consolas");

        private readonly VisualCollectionHost m_VisualsHost;
    }
}
