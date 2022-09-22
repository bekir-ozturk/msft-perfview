using System.Windows;

namespace PerfView
{
    internal class RenderPanAndZoomCanvas : PanAndZoomCanvas
    {
        protected override Point GetTransformedPosition(Point point)
        {
            return point;
        }

        protected override void OnPan(Vector delta)
        {
            
        }

        protected override void OnReset()
        {
            
        }

        protected override void OnZoom(Point center, Vector delta)
        {
            
        }
    }
}
