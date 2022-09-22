using System.Windows;
using System.Windows.Media;

namespace PerfView
{
    public class VisualCollectionHost : UIElement
    {
        public readonly VisualCollection Items;

        public VisualCollectionHost(Visual parent)
        {
            Items = new VisualCollection(parent);
        }

        public void Replace(DrawingVisual visual, int index)
        {
            if (Items.Count <= index)
            {
                Items.Add(visual);
            }
            else
            {
                Items.RemoveAt(index);
                Items.Insert(index, visual);
            }
        }

        protected override int VisualChildrenCount
        {
            get { return Items.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return Items[index];
        }
    }
}
