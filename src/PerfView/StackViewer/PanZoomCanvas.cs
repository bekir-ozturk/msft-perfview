using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PerfView.StackViewer
{
    public class PanZoomCanvas : Canvas
    {
        protected readonly TransformGroup transformGroup = new TransformGroup();
        private readonly TranslateTransform translateTransform = new TranslateTransform();
        protected readonly ScaleTransform scaleTransform = new ScaleTransform(1.0f, 1.0f);
        private readonly double _defaultZoomFactor = 1.4;
        private readonly double _minZoom = double.MinValue;
        private readonly double _maxZoom = double.MaxValue;
        private bool _isZooming;

        public PanZoomCanvas()
        {
            transformGroup.Children.Add(translateTransform);
            transformGroup.Children.Add(scaleTransform);
            KeyDown += PanZoomCanvas_KeyDown;
            MouseWheel += PanZoomCanvas_MouseWheel;
            MouseDown += PanZoomCanvas_MouseDown;
            MouseMove += PanZoomCanvas_MouseMove;
            MouseUp += PanZoomCanvas_MouseUp;
        }

        private void PanZoomCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            _isZooming = e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl;

            if (e.Key == Key.Escape)
            {
                Reset();
            }
        }

        private void PanZoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_isZooming)
            {
                return;
            }

            double zoomFactor = _defaultZoomFactor;
            if (e.Delta <= 0)
            {
                zoomFactor = 1.0 / _defaultZoomFactor;
            }

            Point physicalPosition = e.GetPosition(this);
            Zoom(zoomFactor, transformGroup.Inverse.Transform(physicalPosition), physicalPosition);
        }

        private void Zoom(double zoomFactor, Point mousePosition, Point physicalPosition)
        {
            double currentZoom = scaleTransform.ScaleX;
            currentZoom *= zoomFactor;

            if (currentZoom < _minZoom)
            {
                currentZoom = _minZoom;
            }
            else if (currentZoom > _maxZoom)
            {
                currentZoom = _maxZoom;
            }

            if (currentZoom == 1)
            {
                Reset();
                return;
            }

            translateTransform.BeginAnimation(TranslateTransform.XProperty, CreateAnimation((-1 * mousePosition.X * currentZoom) - physicalPosition.X));
            translateTransform.BeginAnimation(TranslateTransform.YProperty, CreateAnimation((-1 * mousePosition.Y * currentZoom) - physicalPosition.Y));
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, CreateAnimation(currentZoom));
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, CreateAnimation(currentZoom));
        }

        private void Reset()
        {
            translateTransform.BeginAnimation(TranslateTransform.XProperty, CreateAnimation(0));
            translateTransform.BeginAnimation(TranslateTransform.YProperty, CreateAnimation(0));
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, CreateAnimation(1));
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, CreateAnimation(1));
        }

        private Point moveStartPoint;
        private Point startOffset;

        private DoubleAnimation CreateAnimation(double toValue)
        {
            DoubleAnimation animation = new DoubleAnimation(toValue, new Duration(TimeSpan.FromMilliseconds(500)))
            {
                AccelerationRatio = 0.1,
                DecelerationRatio = 0.9,
                FillBehavior = FillBehavior.HoldEnd
            };
            animation.Freeze();
            return animation;
        }

        private void PanZoomCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                _ = scaleTransform.Inverse.Transform(e.GetPosition(this));
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            moveStartPoint = e.GetPosition(this);
            startOffset = new Point(translateTransform.X, translateTransform.Y);
            _ = CaptureMouse();
            Cursor = Cursors.Hand;
        }

        private void PanZoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsMouseCaptured)
            {
                return;
            }

            Point physicalPoint = e.GetPosition(this);
            double nextX = physicalPoint.X - moveStartPoint.X + startOffset.X;
            double nextY = physicalPoint.Y - moveStartPoint.Y + startOffset.Y;
            double scaleValue = scaleTransform.ScaleX;
            double minXValue = Width - (Width * scaleValue);
            double minYValue = Height - (Height * scaleValue);
            double maxXValue = ActualWidth - (ActualWidth * scaleValue);
            double maxYValue = ActualHeight - (ActualHeight * scaleValue);

            if (nextX > 0)
            {
                nextX = 0;
            }
            else if (nextX < minXValue)
            {
                nextX = minXValue;
            }
            else if (nextX > maxXValue)
            {
                nextX = maxXValue;
            }

            if (nextY > 0)
            {
                nextY = 0;
            }
            else if (nextY < minYValue)
            {
                nextY = minYValue;
            }
            else if (nextY > maxYValue)
            {
                nextY = maxYValue;
            }
            translateTransform.BeginAnimation(TranslateTransform.XProperty, CreatePanAnimation(nextX), HandoffBehavior.Compose);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, CreatePanAnimation(nextY), HandoffBehavior.Compose);
        }

        private static DoubleAnimation CreatePanAnimation(double toValue)
        {
            DoubleAnimation animation = new DoubleAnimation(toValue, new Duration(TimeSpan.FromMilliseconds(300)))
            {
                AccelerationRatio = 0.1,
                DecelerationRatio = 0.9,
                FillBehavior = FillBehavior.HoldEnd
            };
            animation.Freeze();
            return animation;
        }

        private void PanZoomCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                Cursor = Cursors.Arrow;
                ReleaseMouseCapture();
            }
        }
    }
}
