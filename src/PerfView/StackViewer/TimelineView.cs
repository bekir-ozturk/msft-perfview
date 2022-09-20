using Microsoft.Diagnostics.Tracing.Stacks;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PerfView
{
    public partial class TimelineView : UserControl
    {
        public bool IsInitialized { get; private set; } = false;

        public TimelineView()
        {
            InitializeComponent();
        }

        public async Task InitializeAsync(CallTree callTree)
        {
            if (IsInitialized)
            {
                return;
            }

            if (callTree == null)
            {
                throw new ArgumentNullException(nameof(callTree));
            }

            if (callTree.Root == null)
            {
                throw new ArgumentException($"Argument {nameof(callTree)} must have a valid root.");
            }

            DataView.Visibility = System.Windows.Visibility.Collapsed;
            InfoLabel.Visibility = System.Windows.Visibility.Visible;
            InfoLabel.Content = "Timeline is loading...";

            var root = callTree.Root;
            var calleeCount = root.Callees.Count;
            var firstCallee = root.Callees[0];
            var firstCalleeName = firstCallee.Name;

            IsInitialized = true;
        }
    }
}
