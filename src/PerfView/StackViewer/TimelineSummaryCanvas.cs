using System.Windows.Controls;
using System.Windows.Media;

namespace PerfView
{
    internal class TimelineSummaryCanvas : Canvas
    {
        public TimelineSummaryCanvas()
            : base()
        {
            m_VisualsHost = new VisualCollectionHost(this);
            Children.Add(m_VisualsHost);
        }

        internal void Update()
        {
            double width = RenderSize.Width;
            double height = RenderSize.Height;

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.Rectangle(Brushes.Blue, null, 0, 0, width, height, 0);
            }

            m_VisualsHost.Replace(visual, 0);
        }

        protected override int VisualChildrenCount => m_VisualsHost.Items.Count;

        protected override Visual GetVisualChild(int index)
        {
            return m_VisualsHost.Items[index];
        }

        private readonly VisualCollectionHost m_VisualsHost;
    }
}
