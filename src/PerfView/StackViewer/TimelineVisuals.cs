using System.Collections.Generic;
using System.Windows.Media;

namespace PerfView
{
    internal class TimelineVisuals
    {
        public int StartingFrame { get; set; }

        public int EndingFrame { get; set; }

        public Dictionary<int, List<WorkVisual>> VisualsPerThreadId { get; set; } = new Dictionary<int, List<WorkVisual>>();
    }

    /// <summary>
    /// Represents the smallest unit of work that we can show on timeline.
    /// </summary>
    internal class WorkVisual
    {
        public int StartingFrame { get; set; }
        public int EndingFrame { get; set; }
        public string DisplayName { get; set; }
        public Color DisplayColor { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether this visual is a representation
        /// of a single work or a combination of many small work.
        /// Small work will not be individually listed. Instead, they will be combined with the
        /// others close by into a single WorkVisual.
        /// </summary>
        public bool IsGroupingSmallWork { get; set; }
    }
}
