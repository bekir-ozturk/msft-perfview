using Microsoft.Diagnostics.Tracing.Stacks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PerfView
{
    public partial class TimelineView : UserControl
    {
        private CallTree _callTree = null;

        private readonly TimelineVisuals _visuals = new TimelineVisuals();

        /// <summary>
        /// What is the first frame that is being drawn on our canvas?
        /// The last frame will depend on the width of the canvas, which may be resized by user.
        /// The first frame will always be fixed, unless user drags or uses the scroll bar.
        /// </summary>
        private int _startingFrameIndex = 0;

        /// <summary>
        /// Pixels per frame: how many pixels do we need to draw a single frame.
        /// This value will provide zooming in and out functinality.
        /// </summary>
        private double _pixelsPerFrame = 1d;

        public bool IsInitialized { get; private set; } = false;

        public TimelineView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(CallTree callTree)
        {
            _callTree = callTree;
            RepopulateVisualsData(_callTree, _visuals);
            UpdateCanvas(_visuals);
            IsInitialized = true;
        }

        private void RepopulateVisualsData(CallTree callTree, TimelineVisuals visuals)
        {
            // To be implemented by Bekir
        }

        private void UpdateCanvas(TimelineVisuals visuals)
        {
            // To be implemented by Radek Barton
        }
    }
}
