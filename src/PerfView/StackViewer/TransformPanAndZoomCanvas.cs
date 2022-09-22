using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PerfView.StackViewer
{
    public class TransformPanAndZoomCanvas : PanAndZoomCanvas
    {
        public bool IsZoomed => _transform.Matrix.M11 != 1.0 || _transform.Matrix.M22 != 1.0;

        public TransformPanAndZoomCanvas()
            : base()
        {
        }

        protected readonly MatrixTransform _transform = new MatrixTransform();

        protected override Point GetTransformedPosition(Point point)
        {
            var inverse = _transform.Inverse;
            if (inverse == null)
            {
                return point;
            }

            return inverse.Transform(point);
        }

        protected override void OnReset()
        {
            _transform.Matrix = new TranslateTransform(0, 0).Value;
        }

        protected override void OnPan(Vector delta)
        {
            TranslateTransform translate = new TranslateTransform(delta.X, delta.Y);
            _transform.Matrix = translate.Value * _transform.Matrix;

            foreach (DrawingVisual child in Visuals.Items.Cast<DrawingVisual>())
            {
                child.Transform = _transform;
            }
        }

        protected override void OnZoom(Point center, Vector delta)
        {
            // TODO: Do absolute scaling.
            Matrix scaleMatrix = _transform.Matrix;
            scaleMatrix.ScaleAt(delta.X, delta.Y, center.X, center.Y);
            _transform.Matrix = scaleMatrix;
        }
    }
}
