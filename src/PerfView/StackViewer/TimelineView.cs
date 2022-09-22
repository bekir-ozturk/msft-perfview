using Microsoft.Diagnostics.Tracing.Stacks;
using PerfView.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace PerfView
{
    public partial class TimelineView : UserControl
    {
        private static readonly Random _random = new Random();
        private readonly Dictionary<StackSourceCallStackIndex, StackInfo> _stackInfoCache = new Dictionary<StackSourceCallStackIndex, StackInfo>();
        private CallTree _callTree = null;

        private readonly TimelineVisuals _visuals = new TimelineVisuals
        {
            StartingFrame = 89975,
            EndingFrame = 90875,
        };

        public bool InitalizationComplete { get; private set; } = false;

        public TimelineView()
        {
            InitializeComponent();

            SummaryCanvas.SizeChanged += (s, e) => { UpdateSummaryCanvas(_visuals); };
            FocusCanvas.SizeChanged += (s, e) => { UpdateFocusCanvas(_visuals); };
        }

        public void InitializeAsync(CallTree callTree)
        {
            _callTree = callTree;
            RepopulateVisualsData(_callTree, _visuals);
            UpdateSummaryCanvas(_visuals);
            UpdateFocusCanvas(_visuals);
            InitalizationComplete = true;
        }

        private void RepopulateVisualsData(CallTree callTree, TimelineVisuals visuals)
        {
            int minimumBlockSizeInFrames = 30;
            Dictionary<int, List<(StackSourceSample sample, StackInfo info)>> stacksPerThread = new Dictionary<int, List<(StackSourceSample, StackInfo)>>();

            callTree.StackSource.ForEach((s) =>
            {
                var treeNode = callTree.FindTreeNode(s.StackIndex);
                var threadNode = treeNode;
                while (threadNode != null && !threadNode.Name.StartsWith("Thread ("))
                {
                    threadNode = threadNode.Caller;
                }

                StackInfo sInfo;
                if (!_stackInfoCache.TryGetValue(s.StackIndex, out sInfo))
                {
                    int stackDepth = callTree.StackSource.StackDepth(s.StackIndex);
                    _stackInfoCache[s.StackIndex] = sInfo = new StackInfo()
                    {
                        Frames = new StackSourceFrameIndex[stackDepth],
                        FrameNames = new string[stackDepth]
                    };

                    StackSourceCallStackIndex stackIndex = s.StackIndex;
                    for (int i = stackDepth - 1; i >= 0; i--)
                    {
                        sInfo.Frames[i] = callTree.StackSource.GetFrameIndex(stackIndex);
                        string frameName = sInfo.FrameNames[i] = callTree.StackSource.GetFrameName(sInfo.Frames[i], true);

                        if (frameName.StartsWith("Thread ("))
                        {
                            sInfo.ThreadId = GetThreadIdFromFrameName(frameName);
                        }
                        stackIndex = callTree.StackSource.GetCallerIndex(stackIndex);
                    }
                }

                if (sInfo.ThreadId != 0)
                {
                    List<(StackSourceSample, StackInfo)> stacks;
                    if (!stacksPerThread.TryGetValue(sInfo.ThreadId, out stacks))
                    {
                        stacksPerThread[sInfo.ThreadId] = stacks = new List<(StackSourceSample, StackInfo)>();
                    }
                    stacks.Add((s, sInfo));
                }
            });

            foreach (var st in stacksPerThread)
            {
                if (!visuals.VisualsPerThreadId.TryGetValue(st.Key, out List<WorkVisual> works))
                {
                    visuals.VisualsPerThreadId[st.Key] = works = new List<WorkVisual>();
                }

                int nextVisualToWrite = 0;
                for (int i = 0; i < st.Value.Count; i++)
                {
                    StackSourceSample stackSample = st.Value[i].sample;

                    if ((int)stackSample.SampleIndex < visuals.StartingFrame)
                    {
                        continue;
                    }
                    if ((int)stackSample.SampleIndex > visuals.EndingFrame)
                    {
                        break;
                    }

                    if (works.Count == nextVisualToWrite)
                    {
                        works.Add(new WorkVisual());
                    }

                    var visual = works[nextVisualToWrite];
                    visual.StartingFrame = (int)stackSample.SampleIndex;
                    visual.EndingFrame = visual.StartingFrame + 1;
                    visual.IsGroupingSmallWork = false;
                    visual.DisplayColor = GetRandomColor();

                    nextVisualToWrite++;
                    if (nextVisualToWrite == 1000)
                        break; // Don't draw more than 1000 for now.
                }

                while (works.Count > st.Value.Count)
                {
                    works.RemoveAt(works.Count - 1);
                }
            }
            // callTree.FindTreeNode(callTree.StackSource.BaseStackSource.ForEach)
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="samples">All samples of the thread.</param>
        /// <param name="startIndex">First sample in the range of interest.</param>
        /// <param name="endIndex">Last samples in the range of interest (inclusive).</param>
        /// <param name="frameDepth">The depth that we should start from when iterating over the call stack.</param>
        /// <param name="minimumVisualSampleCount">How small can a WorkVisual be? Represented in # of samples.</param>
        /// <param name="maximumVisualSampleCount">How big a WorkVisual can be before we decide to split it into multiple smaller ones.</param>
        /// <param name="visuals">The list that we will put the generated visuals into.</param>
        /// <returns>True if some visuals have been generated. False if the chunks were to small to be displayed.</returns>
        private static bool FindWorkToDisplay(
            List<(StackSourceSample sample, StackInfo stack)> samples,
            int startIndex,
            int endIndex,
            int frameDepth,
            int minimumVisualSampleCount,
            int maximumVisualSampleCount,
            List<WorkVisual> visuals)
        {
            if (startIndex == endIndex)
            {
                if (minimumVisualSampleCount > 1)
                    return false; // We are too small to be drawn on our own.

                // We have zoomed in so much that even 1 frame can be displayed on the screen.
                visuals.Add(new WorkVisual()
                {
                    StartingFrame = startIndex,
                    EndingFrame = startIndex + 1,
                    DisplayColor = GetRandomColor(),
                    // Let's go to the lowest level frame because that's the code that was actually running
                    DisplayName = samples[startIndex].stack.FrameNames.Last(),
                    IsGroupingSmallWork = false
                });
            }

            for (int i = startIndex; i <= endIndex; i++)
            {
                // WIP
            }
            return false;
        }

        private void UpdateSummaryCanvas(TimelineVisuals visuals)
        {
            if (!InitalizationComplete)
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
            if (!InitalizationComplete)
            {
                return;
            }

            if (RenderSize.IsEmpty)
            {
                return;
            }

            FocusCanvas.Update(visuals);
        }

        private static int GetThreadIdFromFrameName(string frameName)
        {
            int indexOfClosingParanthesis = frameName.IndexOf(')');
            if (indexOfClosingParanthesis == -1)
            {
                throw new Exception("Invalid thread name: " + frameName);
            }

            int offset = "Thread (".Length;
            if (!int.TryParse(frameName.Substring(offset, indexOfClosingParanthesis - offset), out int threadId))
            {
                throw new Exception("Invalid thread Id: " + frameName.Substring(offset));
            }
            return threadId;
        }

        private static Color GetRandomColor()
        {
            return ColorUtilities.ColorFromHSV(270, 0.4, .7 + _random.NextDouble() / 3d);
        }
        internal class StackInfo
        {
            public int ThreadId { get; set; }

            public StackSourceFrameIndex[] Frames { get; set; }

            public string[] FrameNames { get; set; }
        }
    }
}
