﻿using Microsoft.Diagnostics.Tracing.Stacks;
using PerfView.Utilities;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace PerfView
{
    public partial class TimelineView : UserControl
    {
        private static readonly Random _random = new Random();
        private readonly Dictionary<StackSourceCallStackIndex, StackInfo> _stackInfoCache = new Dictionary<StackSourceCallStackIndex, StackInfo>();
        private Dictionary<int, List<(StackSourceSample sample, StackInfo info)>> stacksPerThread = null;

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
            FocusCanvas.Reset += (s, e) => { OnFocusCanvasReset(e); };
            FocusCanvas.Pan += (s, e) => { OnFocusCanvasPan(e.TimeDelta); };
            FocusCanvas.Zoom += (s, e) => { OnFocusCanvasZoom(e.TimeOffset, e.Scale); };
        }

        public void InitializeAsync(CallTree callTree)
        {
            _callTree = callTree;
            RepopulateVisualsData(_callTree, _visuals);
            InitalizationComplete = true;
            UpdateSummaryCanvas(_visuals);
        }

        private void RepopulateVisualsData(CallTree callTree, TimelineVisuals visuals)
        {
            if (stacksPerThread == null)
            {
                // This data was never calculated. Calculate once and then reuse.
                stacksPerThread = new Dictionary<int, List<(StackSourceSample, StackInfo)>>();
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
                            string frameName = sInfo.FrameNames[i] = callTree.StackSource.GetFrameName(sInfo.Frames[i], false);

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
            }

            visuals.VisualsPerThreadId.Clear();
            foreach (var st in stacksPerThread)
            {
                if (!visuals.VisualsPerThreadId.TryGetValue(st.Key, out List<WorkVisual> works))
                {
                    visuals.VisualsPerThreadId[st.Key] = works = new List<WorkVisual>();
                }

                int firstSampleOfInterest = 0;
                int sampleAtTheEndOfInterest = st.Value.Count;
                for (int i = 0; i < st.Value.Count; i++)
                {
                    // This can be done much quicker with binary search.
                    int sampleIndex = (int)st.Value[i].sample.SampleIndex;
                    if (sampleIndex < visuals.StartingFrame)
                        firstSampleOfInterest = i;
                    else if (sampleIndex > visuals.EndingFrame)
                    {
                        sampleAtTheEndOfInterest = i;
                        break;
                    }
                }

                if (firstSampleOfInterest == sampleAtTheEndOfInterest)
                    return; // Nothing to display.

                const double minimumVisualSizeInPixels = 150;
                const double maximumVisualSizeInPixels = 350;
                int minimumVisualSampleCount = (int)(minimumVisualSizeInPixels / FocusCanvas.ActualWidth * (visuals.EndingFrame - visuals.StartingFrame));
                int maximumVisualSampleCount = (int)(maximumVisualSizeInPixels / FocusCanvas.ActualWidth * (visuals.EndingFrame - visuals.StartingFrame));
                if (!FindWorkToDisplay(st.Value, firstSampleOfInterest, sampleAtTheEndOfInterest, 1, minimumVisualSampleCount, maximumVisualSampleCount, works))
                {
                    works.Add(new WorkVisual()
                    {
                        StartingFrame = (int)st.Value[firstSampleOfInterest].sample.SampleIndex,
                        EndingFrame = (int)(st.Value[sampleAtTheEndOfInterest - 1].sample.SampleIndex + 1),
                        DisplayColor = GetRandomColor(0, (int)st.Value[firstSampleOfInterest].sample.SampleIndex, 0, true),
                        DisplayName = "Thread " + st.Key,
                        IsGroupingSmallWork = true
                    });
                }
            }

            UpdateFocusCanvas(_visuals);
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
                    // We are passed the last sample.
                    // If there is a valid frame after this, assume that duration is up to that next sample.
                    // If not, assume that this thing has executed for only 1 frame.
                    currentSampleIndex = samples.Count > i 
                        ? samples[i].sample.SampleIndex 
                        : (samples[i - 1].sample.SampleIndex + 1);
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
                                DisplayColor = GetRandomColor(666, (int)samples[groupedWorkStart].sample.SampleIndex, frameDepth, true),
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
                                DisplayColor = GetRandomColor(666, (int)samples[groupedWorkStart].sample.SampleIndex, frameDepth, true),
                                DisplayName = "Other",
                                IsGroupingSmallWork = true,
                            });
                            groupedWorkStart = -1;
                        }

                        bool createdVisuals = false;
                        if (chunkSize > maximumVisualSampleCount)
                        {
                            // This work is too big to visualise alone. Try to split into multiple pieces.
                            if (FindWorkToDisplay(samples, lastWorkStart, i, frameDepth + 1, minimumVisualSampleCount, maximumVisualSampleCount, visuals))
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
                                DisplayColor = GetRandomColor((int)(trackedFrame ?? 0), (int)samples[lastWorkStart].sample.SampleIndex, frameDepth, false),
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
                        if (trackedFrameName == "BLOCKED_TIME" || trackedFrameName == "CPU_TIME")
                        {
                            if (frameDepth > 0)
                            {
                                trackedFrameName += " " + stack.FrameNames[frameDepth - 1];
                            }

                        }
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

        private void OnFocusCanvasReset(EventArgs e)
        {
            RepopulateVisualsData(_callTree, _visuals);

            FocusCanvas.Update(_visuals);
        }

        private void OnFocusCanvasPan(float timeDelta)
        {
            _visuals.StartingFrame -= timeDelta;
            _visuals.EndingFrame -= timeDelta;

            RepopulateVisualsData(_callTree, _visuals);

            FocusCanvas.Update(_visuals);
        }

        private void OnFocusCanvasZoom(float timeOffset, float scale)
        {
            float scaleCenter = _visuals.StartingFrame + timeOffset;
            _visuals.StartingFrame = scaleCenter - ((scaleCenter - _visuals.StartingFrame) / scale);
            _visuals.EndingFrame = scaleCenter + ((_visuals.EndingFrame - scaleCenter) / scale);

            RepopulateVisualsData(_callTree, _visuals);

            FocusCanvas.Update(_visuals);
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

        private static Color GetRandomColor(int frame, int startingFrameIndex, int frameDepth, bool group)
        {
            int value = (777777 + frame * 13 + startingFrameIndex * 17) % 1000;
            return ColorUtilities.ColorFromHSV(0.1 + frameDepth * 0.02, 0.1 + (value / 1000d) / 15d, group ? 0.8 : 0.85);
        }

        internal class StackInfo
        {
            public int ThreadId { get; set; }

            public StackSourceFrameIndex[] Frames { get; set; }

            public string[] FrameNames { get; set; }
        }
    }
}
