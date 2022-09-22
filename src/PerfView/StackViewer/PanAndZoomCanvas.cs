using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PerfView.StackViewer
{
    public class PanAndZoomCanvas : Canvas
    {
        private const float _zoomfactor = 1.1f;
        protected readonly MatrixTransform _transform = new MatrixTransform();
        private readonly StackPanel _controls = new StackPanel();
        private readonly GroupBox _panningGroupBox = new GroupBox();
        private readonly CheckBox _panXAxis = new CheckBox();
        private readonly CheckBox _panYAxis = new CheckBox();
        private readonly GroupBox _zoomGroupBox = new GroupBox();
        private readonly CheckBox _zoomXAxis = new CheckBox();
        private readonly CheckBox _zoomYAxis = new CheckBox();
        private Point _initialMousePosition;
        private bool _isDragging;
        private bool _isZooming;

        public VisualCollectionHost Visuals
        {
            get { return m_VisualsHost; }
        }

        public PanAndZoomCanvas()
        {
            m_VisualsHost = new VisualCollectionHost(this);
            Children.Add(m_VisualsHost);

            _panningGroupBox.Header = "Panning";
            StackPanel panStackPanel = new StackPanel();
            AddCheckBox(_panXAxis, "X-Axis", panStackPanel);
            AddCheckBox(_panYAxis, "Y-Axis", panStackPanel);
            _panningGroupBox.Content = panStackPanel;
            _zoomGroupBox.Header = "Zoom";
            StackPanel zoomStackPanel = new StackPanel();
            AddCheckBox(_zoomXAxis, "X-Axis", zoomStackPanel);
            AddCheckBox(_zoomYAxis, "Y-Axis", zoomStackPanel);
            _zoomGroupBox.Content = zoomStackPanel;
            _ = _controls.Children.Add(_panningGroupBox);
            _ = _controls.Children.Add(_zoomGroupBox);
            _ = Children.Add(_controls);

            KeyDown += PanAndZoomCanvas_KeyDown;
            KeyUp += PanAndZoomCanvas_KeyUp;
            MouseDown += PanAndZoomCanvas_MouseDown;
            MouseMove += PanAndZoomCanvas_MouseMove;
            MouseUp += PanAndZoomCanvas_MouseUp;
            MouseWheel += PanAndZoomCanvas_MouseWheel;
        }

        public bool IsZoomed => _transform.Matrix.M11 != 1.0 || _transform.Matrix.M22 != 1.0;

        private void PanAndZoomCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _transform.Matrix = new TranslateTransform(0, 0).Value;
            }
            else if (e.Key == Key.Left)
            {
                PanCanvas(new Vector(-10, 0));
            }
            else if (e.Key == Key.Right)
            {
                PanCanvas(new Vector(10, 0));
            }
            else if (e.Key == Key.Up)
            {
                PanCanvas(new Vector(0, -10));
            }
            else if (e.Key == Key.Down)
            {
                PanCanvas(new Vector(0, 10));
            }
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
            {
                _isZooming = true;
            }
        }

        private void PanAndZoomCanvas_KeyUp(object sender, KeyEventArgs e)
        {
            _isZooming = false;
        }

        private static void AddCheckBox(CheckBox checkBox, string text, StackPanel stackPanel)
        {
            checkBox.Content = text;
            checkBox.IsChecked = true;
            checkBox.Margin = new Thickness(2);
            _ = stackPanel.Children.Add(checkBox);
        }

        private void PanAndZoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _initialMousePosition = _transform.Inverse.Transform(e.GetPosition(this));
                Cursor = Cursors.Hand;
                _isDragging = true;
            }
        }

        protected void PanAndZoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _isDragging)
            {
                Point mousePosition = _transform.Inverse.Transform(e.GetPosition(this));
                Vector delta = Point.Subtract(mousePosition, _initialMousePosition);
                PanCanvas(delta);
            }
        }

        protected void PanAndZoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _initialMousePosition = default;
            Cursor = Cursors.Arrow;
        }

        private readonly VisualCollectionHost m_VisualsHost;

        private void PanCanvas(Vector delta)
        {
            if (!_panXAxis.IsChecked.Value)
            {
                delta.X = 0;
            }

            if (!_panYAxis.IsChecked.Value)
            {
                delta.Y = 0;
            }

            TranslateTransform translate = new TranslateTransform(delta.X, delta.Y);
            _transform.Matrix = translate.Value * _transform.Matrix;

            foreach (DrawingVisual child in Visuals.Items.Cast<DrawingVisual>())
            {
                child.Transform = _transform;
            }
        }

        private void PanAndZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_isZooming)
            {
                float scaleFactor = _zoomfactor;
                if (e.Delta < 0)
                {
                    scaleFactor = 1f / scaleFactor;
                }

                Point mousePostion = e.GetPosition(this);

                Matrix scaleMatrix = _transform.Matrix;
                float scaleX = _zoomXAxis.IsChecked.Value ? scaleFactor : 1f;
                float scaleY = _zoomYAxis.IsChecked.Value ? scaleFactor : 1f;

                scaleMatrix.ScaleAt(scaleX, scaleY, mousePostion.X, mousePostion.Y);
                _transform.Matrix = scaleMatrix;
            }
            else
            {
                PanCanvas(new Vector(0, e.Delta));
            }
        }
    }
}
