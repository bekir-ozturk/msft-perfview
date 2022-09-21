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
