using System.Collections.Generic;
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
        private readonly GroupBox _panningGroupBox = new GroupBox();
        private readonly CheckBox _panXAxis = new CheckBox();
        private readonly CheckBox _panYAxis = new CheckBox();
        protected readonly List<Visual> visuals = new List<Visual>();
        private Point _initialMousePosition;

        public PanAndZoomCanvas()
        {
            _panningGroupBox.Header = "Panning";
            StackPanel stackPanel = new StackPanel();
            AddCheckBox(_panXAxis, "X-Axis", stackPanel);
            AddCheckBox(_panYAxis, "Y-Axis", stackPanel);
            _panningGroupBox.Content = stackPanel;
            _ = Children.Add(_panningGroupBox);

            KeyDown += PanAndZoomCanvas_KeyDown;
            MouseDown += PanAndZoomCanvas_MouseDown;
            MouseMove += PanAndZoomCanvas_MouseMove;
            MouseUp += PanAndZoomCanvas_MouseUp;
            MouseWheel += PanAndZoomCanvas_MouseWheel;
        }

        public bool IsZoomed => _transform.Matrix.M11 != 1.0 || _transform.Matrix.M22 != 1.0;

        protected override int VisualChildrenCount => visuals.Count + 1;

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
        }

        protected override Visual GetVisualChild(int index)
        {
            return index == 0 ? _panningGroupBox : visuals[index - 1];
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
            }
        }

        protected void PanAndZoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePosition = _transform.Inverse.Transform(e.GetPosition(this));
                Vector delta = Point.Subtract(mousePosition, _initialMousePosition);
                PanCanvas(delta);
            }
        }

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

            foreach (DrawingVisual child in visuals.Cast<DrawingVisual>())
            {
                child.Transform = _transform;
            }
        }

        private void PanAndZoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _initialMousePosition = default;
            Cursor = Cursors.Arrow;
        }

        private void PanAndZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            float scaleFactor = _zoomfactor;
            if (e.Delta < 0)
            {
                scaleFactor = 1f / scaleFactor;
            }

            Point mousePostion = e.GetPosition(this);

            Matrix scaleMatrix = _transform.Matrix;
            scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePostion.X, mousePostion.Y);
            _transform.Matrix = scaleMatrix;
        }
    }
}
