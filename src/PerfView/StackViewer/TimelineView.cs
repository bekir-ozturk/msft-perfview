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
            InitalizationComplete = true;
            UpdateSummaryCanvas(_visuals);
            UpdateFocusCanvas(_visuals);
        }

        private void RepopulateVisualsData(CallTree callTree, TimelineVisuals visuals)
        {
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

                int firstSampleOfInterest = st.Value.TakeWhile(s => (int)s.sample.SampleIndex <= visuals.StartingFrame).Count();
                firstSampleOfInterest = Math.Max(0, firstSampleOfInterest - 1);

                int sampleAtTheEndOfInterest = st.Value.Skip(firstSampleOfInterest).TakeWhile(s => (int)s.sample.SampleIndex < visuals.EndingFrame).Count() + firstSampleOfInterest;
                sampleAtTheEndOfInterest = Math.Min(st.Value.Count, sampleAtTheEndOfInterest + 1);

                if (!FindWorkToDisplay(st.Value, firstSampleOfInterest, sampleAtTheEndOfInterest, 0, 40, 150, works))
                {
                    works.Add(new WorkVisual()
                    {
                        StartingFrame = (int)st.Value[firstSampleOfInterest].sample.SampleIndex,
                        EndingFrame = (int)(st.Value[sampleAtTheEndOfInterest - 1].sample.SampleIndex + 1),
                        DisplayColor = GetRandomColor(),
                        DisplayName = "Thread " + st.Key,
                        IsGroupingSmallWork = true
                    });
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="samples">All samples of the thread.</param>
        /// <param name="startIndex">First sample in the range of interest.</param>
        /// <param name="endIndex">The first sample after the range of interest has ended.</param>
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
            int groupedWorkStart = -1;
            int lastWorkStart = startIndex;
            StackSourceFrameIndex? trackedFrame = null;
            string trackedFrameName = "Self"; // If stack is not deep enough, this means time was spend in the function itself.
            for (int i = startIndex; i <= endIndex; i++)
            {
                (StackSourceSample sample, StackInfo stack) = (default, default);
                StackSourceFrameIndex? currentFrame = null;
                StackSourceSampleIndex currentSampleIndex;
                bool noMoreSamplesLeft = i == endIndex;

                if (!noMoreSamplesLeft)
                {
                    (sample, stack) = samples[i];
                    currentFrame = stack.Frames.Length > frameDepth
                        ? (StackSourceFrameIndex?)stack.Frames[frameDepth]
                        : null;
                    currentSampleIndex = sample.SampleIndex;
                }
                else
                {
                    // We are passed the last sample. We don't have any more samples to know the duration of this execution.
                    // So let's just assume that this thing has executed for only 1 frame.
                    currentSampleIndex = samples[i - 1].sample.SampleIndex + 1;
                }

                if (trackedFrame != currentFrame // The frame is changing. Decide whether to visualize the processed samples.
                    || noMoreSamplesLeft) // Or, there are no more frames left. Visualize all that was processed.
                {
                    // The frame has changed. The last chunk we were building is finished now.
                    int chunkSize = currentSampleIndex - samples[lastWorkStart].sample.SampleIndex;
                    if (chunkSize == 0)
                    {
                        // Evaluation has just started, nothing to visualize here.
                    }
                    else if (chunkSize < minimumVisualSampleCount) // this is too small to visualize. Continue accumulating.
                    {
                        if (groupedWorkStart == -1)
                            groupedWorkStart = lastWorkStart;
                        else
                            ; // Group had been started earlier. Keep on.

                        if (noMoreSamplesLeft)
                        {
                            // looks like there are on more samples. Visualize this last bit.
                            if (groupedWorkStart == startIndex)
                            {
                                // We have traversed the whole samples, but we have found nothing significant to visualize.
                                // Instead of putting a big "Other" visual, let the parent know
                                // so that we can use the caller frame name instead.
                                return false;
                            }

                            visuals.Add(new WorkVisual()
                            {
                                StartingFrame = (int)samples[groupedWorkStart].sample.SampleIndex,
                                EndingFrame = (int)currentSampleIndex,
                                DisplayColor = Colors.DimGray,
                                DisplayName = "Other",
                                IsGroupingSmallWork = true,
                            });
                            groupedWorkStart = -1;
                        }
                    }
                    else
                    {
                        // Work we accumulated so far is big enough to be displayed.
                        // Let's first check one thing though: did we end up finding a huge single chunk for this whole range?
                        // If yes, there was no point in splitting this further if we already reached the bottom of the call stack.
                        if (lastWorkStart == startIndex // this started at the beginning
                            && noMoreSamplesLeft // and went all the way to the end
                            && !trackedFrame.HasValue) // doesn't even have any frames this deep.
                        {
                            // We went to far. Let the caller know that we don't have anything valuable here to visualize.
                            return false;
                        }

                        // Some earlier work to be grouped has been accumulating as well. Visualise that first.
                        if (groupedWorkStart != -1)
                        {
                            visuals.Add(new WorkVisual()
                            {
                                StartingFrame = (int)samples[groupedWorkStart].sample.SampleIndex,
                                EndingFrame = (int)samples[lastWorkStart].sample.SampleIndex,
                                DisplayColor = Colors.DimGray,
                                DisplayName = "Other",
                                IsGroupingSmallWork = true,
                            });
                            groupedWorkStart = -1;
                        }

                        bool createdVisuals = false;
                        if (chunkSize > maximumVisualSampleCount)
                        {
                            // This work is too big to visualise alone. Try to split into multiple pieces.
                            if (FindWorkToDisplay(samples, lastWorkStart, i, frameDepth+1, minimumVisualSampleCount, maximumVisualSampleCount, visuals))
                            {
                                // We managed to split this work. Don't try again.
                                createdVisuals = true;
                            }
                        }

                        if (!createdVisuals)
                        {
                            // We haven't been able to split this further. We will just put this work as a single visual.
                            visuals.Add(new WorkVisual()
                            {
                                StartingFrame = (int)samples[lastWorkStart].sample.SampleIndex,
                                EndingFrame = (int)currentSampleIndex,
                                DisplayColor = GetRandomColor(),
                                DisplayName = trackedFrameName,
                                IsGroupingSmallWork = false,
                            });
                        }

                    }

                    // Frame changed, one work ended the other is starting.
                    lastWorkStart = i;

                    if (!noMoreSamplesLeft)
                    {
                        // Prepare for the next iteration.
                        trackedFrame = currentFrame;
                        trackedFrameName = stack.FrameNames.Length > frameDepth
                            ? stack.FrameNames[frameDepth]
                            : "Self";
                    }
                }
            }
            return true;
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
