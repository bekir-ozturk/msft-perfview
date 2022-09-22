using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Diagnostics.Tracing.Stacks;

namespace PerfView
{
    public class HistogramView : Canvas
    {
        private readonly Line _startLine = new Line();
        private readonly Line _endLine = new Line();
        private bool _isDragging;

        public static readonly DependencyProperty HistogramProperty = DependencyProperty.Register(
            "Histogram",
            typeof(Histogram),
            typeof(HistogramView),
            new PropertyMetadata((s, e) => { ((HistogramView)s).Update(); })
        );

        public HistogramView()
        {
            m_VisualHost = new VisualHost(this);
            Children.Add(m_VisualHost);

            _startLine.Y1 = 0;
            _startLine.Stroke = Brushes.Yellow;
            Children.Add(_startLine);
            _endLine.Y1 = 0;
            _endLine.Stroke = Brushes.Yellow;
            Children.Add(_endLine);

            Update();

            SizeChanged += (s, e) => Update();
            MouseDown += HistogramView_MouseDown;
            MouseMove += HistogramView_MouseMove;
            MouseUp += HistogramView_MouseUp;
        }

        private void HistogramView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = true;

            var point = e.GetPosition(this);
            _startLine.X1 = point.X;
            _startLine.X2 = point.X;
            StartPoint = point.X / ActualWidth;
            _startLine.StrokeThickness = 3;
            _endLine.StrokeThickness = 3;
        }

        private void HistogramView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            var point = e.GetPosition(this);
            _endLine.X1 = point.X;
            _endLine.X2 = point.X;
            EndPoint = point.X / ActualWidth;
        }

        private void HistogramView_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDragging = false;
            _startLine.StrokeThickness = 0;
            _endLine.StrokeThickness = 0;

            if (StartPoint <= EndPoint)
                return;

            var temp = StartPoint;
            StartPoint = EndPoint;
            EndPoint = temp;
        }

        public double StartPoint { get; private set; }

        public double EndPoint { get; private set; }

        public Histogram Histogram
        {
            get { return (Histogram)GetValue(HistogramProperty); }
            set { SetValue(HistogramProperty, value); }
        }

        private class VisualHost : UIElement
        {
            public readonly VisualCollection Visuals;

            public VisualHost(Visual Parent) 
            {
               Visuals = new VisualCollection(this);
            }

            protected override int VisualChildrenCount
            {
                get { return Visuals.Count; }
            }

            protected override Visual GetVisualChild(int index)
            {
                return Visuals[index];
            }
        }

        private readonly VisualHost m_VisualHost;

        private void Update()
        {
            if (RenderSize.IsEmpty)
            {
                return;
            }

            if (Histogram == null || Histogram.Count == 0)
            {
                return;
            }

            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(new Point(0, RenderSize.Height), true, true);
                PointCollection points = new PointCollection();
                float step = (float)RenderSize.Width / (Histogram.Count - 1);
                float scale = (float)RenderSize.Height / Histogram.Max();
                for (int i = 0; i < Histogram.Count; ++i)
                {
                    float value = Histogram[i] * scale;
                    points.Add(new Point(i * step, RenderSize.Height - value));
                }
                points.Add(new Point(RenderSize.Width, RenderSize.Height));
                context.PolyLineTo(points, true, true);
            }

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawGeometry(Brushes.OrangeRed, null, geometry);
            }

            if (m_VisualHost.Visuals.Count == 0)
            {
                m_VisualHost.Visuals.Add(visual);
            }
            else
            {
                m_VisualHost.Visuals.RemoveAt(0);
                m_VisualHost.Visuals.Insert(0, visual);
            }            

            _startLine.Y2 = ActualHeight;
            _endLine.Y2 = ActualHeight;
        }
    }
}
