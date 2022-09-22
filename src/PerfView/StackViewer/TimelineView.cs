using Microsoft.Diagnostics.Tracing.Stacks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace PerfView
{
    public partial class TimelineView : UserControl
    {
        private CallTree _callTree = null;

        private readonly TimelineVisuals _visuals = new TimelineVisuals
        {
            StartingFrame = 200,
            EndingFrame = 2000,
            VisualsPerThreadId = new Dictionary<int, List<WorkVisual>>{
                {
                    125,
                    new List<WorkVisual>{
                        new WorkVisual
                        {
                            StartingFrame = 100,
                            EndingFrame = 325,
                            DisplayName = "Thread 125 A",
                            DisplayColor = Colors.Yellow,
                            IsGroupingSmallWork = false
                        },
                        new WorkVisual
                        {
                            StartingFrame = 650,
                            EndingFrame = 700,
                            DisplayName = "Thread 125 B",
                            DisplayColor = Colors.YellowGreen,
                            IsGroupingSmallWork = true
                        }
                    }
                },
                {
                    29125,
                    new List<WorkVisual>{
                        new WorkVisual
                        {
                            StartingFrame = 500,
                            EndingFrame = 2150,
                            DisplayName = "Thread 29125",
                            DisplayColor = Colors.Turquoise,
                            IsGroupingSmallWork = false
                        }
                    }
                }
            }
        };

        public bool IsInitialized { get; private set; } = false;

        public TimelineView()
        {
            InitializeComponent();

            SummaryCanvas.SizeChanged += (s, e) => { UpdateSummaryCanvas(_visuals); };
            FocusCanvas.SizeChanged += (s, e) => { UpdateFocusCanvas(_visuals); };
            FocusCanvas.Reset += (s, e) => { OnFocusCanvasReset(e); };
            FocusCanvas.Pan += (s, e) => { OnFocusCanvasPan(e.TimeDelta); };
            FocusCanvas.Zoom += (s, e) => { OnFocusCanvasZoom(e.TimeOffset, e.Scale); };
        }

        public async Task InitializeAsync(CallTree callTree)
        {
            _callTree = callTree;
            RepopulateVisualsData(_callTree, _visuals);
            UpdateSummaryCanvas(_visuals);
            UpdateFocusCanvas(_visuals);
            IsInitialized = true;
        }

        private void RepopulateVisualsData(CallTree callTree, TimelineVisuals visuals)
        {
            // To be implemented by Bekir
        }

        private void UpdateSummaryCanvas(TimelineVisuals visuals)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (RenderSize.IsEmpty)
            {
                return;
            }

            SummaryCanvas.Update();
        }

        private void UpdateFocusCanvas(TimelineVisuals visuals)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (RenderSize.IsEmpty)
            {
                return;
            }

            FocusCanvas.Update(visuals);
        }

        private void OnFocusCanvasReset(EventArgs e)
        {
            // TODO: Proper reset.
            _visuals.StartingFrame = 200;
            _visuals.EndingFrame = 2000;

            FocusCanvas.Update(_visuals);
        }

        private void OnFocusCanvasPan(float timeDelta)
        {
            _visuals.StartingFrame -= timeDelta;
            _visuals.EndingFrame -= timeDelta;

            // TODO: Update the model here.

            FocusCanvas.Update(_visuals);
        }

        private void OnFocusCanvasZoom(float timeOffset, float scale)
        {
            float scaleCenter = _visuals.StartingFrame + timeOffset;
            _visuals.StartingFrame = scaleCenter - ((scaleCenter - _visuals.StartingFrame) / scale);
            _visuals.EndingFrame = scaleCenter + ((_visuals.EndingFrame - scaleCenter) / scale);

            // TODO: Update the model here.

            FocusCanvas.Update(_visuals);
        }
    }
}
