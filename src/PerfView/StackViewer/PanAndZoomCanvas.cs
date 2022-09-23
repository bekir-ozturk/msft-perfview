using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System;

namespace PerfView
{
    public abstract class PanAndZoomCanvas : Canvas
    {
        public VisualCollectionHost Visuals
        {
            get { return m_VisualsHost; }
        }

        public bool IsPanningX
        {
            get { return m_PanXAxis.IsChecked.Value; }
        }

        public bool IsPanningY
        {
            get { return m_PanYAxis.IsChecked.Value; }
        }

        public bool IsZoomingX
        {
            get { return m_ZoomXAxis.IsChecked.Value; }
        }

        public bool IsZoomingY
        {
            get { return m_ZoomYAxis.IsChecked.Value; }
        }

        public PanAndZoomCanvas()
        {
            Focusable = true;

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseLeave += OnMouseLeave;
            MouseWheel += OnMouseWheel;

            m_VisualsHost = new VisualCollectionHost(this);
            Children.Add(m_VisualsHost);

            m_PanningGroupBox.Header = "Panning";
            m_PanningGroupBox.Background = Brushes.White;
            StackPanel panStackPanel = new StackPanel();
            AddCheckBox(m_PanXAxis, "X-Axis", panStackPanel);
            AddCheckBox(m_PanYAxis, "Y-Axis", panStackPanel);
            m_PanningGroupBox.Content = panStackPanel;
            m_ZoomGroupBox.Header = "Zoom";
            m_ZoomGroupBox.Background = Brushes.White;
            StackPanel zoomStackPanel = new StackPanel();
            AddCheckBox(m_ZoomXAxis, "X-Axis", zoomStackPanel);
            AddCheckBox(m_ZoomYAxis, "Y-Axis", zoomStackPanel);
            m_ZoomGroupBox.Content = zoomStackPanel;
            m_Controls.Children.Add(m_PanningGroupBox);
            m_Controls.Children.Add(m_ZoomGroupBox);

            var controlsBorder = new Border
            {
                Padding = new Thickness(5),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                Child = m_Controls
            };
            Children.Add(controlsBorder);

            SetZIndex(m_VisualsHost, 100);
            SetZIndex(controlsBorder, 1);
        }

        protected override int VisualChildrenCount => Visuals.Items.Count + 1;

        protected override Visual GetVisualChild(int index)
        {
            return index == VisualChildrenCount - 1  ? m_Controls : Visuals.Items[index];
        }

        protected abstract Point GetTransformedPosition(Point point);
        protected abstract void OnReset();
        protected abstract void OnPan(Vector delta);
        protected abstract void OnZoom(Point center, Vector scale);

        private readonly VisualCollectionHost m_VisualsHost;

        private readonly StackPanel m_Controls = new StackPanel();
        private readonly GroupBox m_PanningGroupBox = new GroupBox();
        private readonly CheckBox m_PanXAxis = new CheckBox();
        private readonly CheckBox m_PanYAxis = new CheckBox();
        private readonly GroupBox m_ZoomGroupBox = new GroupBox();
        private readonly CheckBox m_ZoomXAxis = new CheckBox();
        private readonly CheckBox m_ZoomYAxis = new CheckBox();

        private bool m_IsCtrlDown;
        private bool m_IsZooming;
        private bool m_IsDragging;
        private Point m_InitialMousePosition;
        private Point m_LastMousePosition;

        private static void AddCheckBox(CheckBox checkBox, string text, StackPanel stackPanel)
        {
            checkBox.Content = text;
            checkBox.IsChecked = true;
            checkBox.Margin = new Thickness(2);
            stackPanel.Children.Add(checkBox);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                OnReset();
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
                m_IsCtrlDown = true;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            m_IsCtrlDown = false;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                m_InitialMousePosition = e.GetPosition(this);
                m_LastMousePosition = m_InitialMousePosition;

                Cursor = Cursors.Hand;

                if (m_IsCtrlDown)
                {
                    m_IsZooming = true;
                }
                else
                {
                    m_IsDragging = true;
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && (m_IsDragging || m_IsZooming))
            {
                Point lastPosition = GetTransformedPosition(m_LastMousePosition);
                Point currentPosition = GetTransformedPosition(e.GetPosition(this));
                if (m_IsDragging)
                {
                    PanCanvas(currentPosition - lastPosition);
                }
                if (m_IsZooming)
                {
                    Vector delta = (currentPosition - lastPosition);

                    int zoomDelta = (int)delta.Length;
                    if (delta.X < 0)
                    {
                        zoomDelta *= -1;
                    }
                    ZoomCanvas(m_InitialMousePosition, zoomDelta);
                }

                m_LastMousePosition = e.GetPosition(this);
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            m_IsDragging = false;
            m_IsZooming = false;
            m_InitialMousePosition = default;
            Cursor = Cursors.Arrow;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            m_IsDragging = false;
            m_IsZooming = false;
            m_InitialMousePosition = default;
            Cursor = Cursors.Arrow;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (m_IsCtrlDown)
            {
                ZoomCanvas(e.GetPosition(this), e.Delta);
            }
            else
            {
                PanCanvas(IsPanningY ? new Vector(0, e.Delta) : new Vector(e.Delta, 0));
            }
        }

        private void PanCanvas(Vector delta)
        {
            if (!IsPanningX)
            {
                delta.X = 0;
            }

            if (!IsPanningY)
            {
                delta.Y = 0;
            }

            if (delta.X == 0 && delta.Y == 0)
            {
                return;
            }

            OnPan(delta);
        }

        private void ZoomCanvas(Point center, int delta)
        {
            if (delta == 0.0f)
            {
                return;
            }

            float scaleFactor = 1.0f + (0.001f * delta);
            var scale = new Vector(IsZoomingX ? scaleFactor : 1.0f, IsZoomingY ? scaleFactor : 1.0f);

            OnZoom(center, scale);
        }
    }
}
