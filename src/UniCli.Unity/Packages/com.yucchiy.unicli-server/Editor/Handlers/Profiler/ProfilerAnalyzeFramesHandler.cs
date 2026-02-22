using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Profiler")]
    public sealed class ProfilerAnalyzeFramesHandler : CommandHandler<ProfilerAnalyzeFramesRequest, ProfilerAnalyzeFramesResponse>
    {
        public override string CommandName => "Profiler.AnalyzeFrames";
        public override string Description => "Analyze recorded frames and return aggregate statistics";

        protected override bool TryWriteFormatted(ProfilerAnalyzeFramesResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
            {
                writer.WriteLine("Failed to analyze frames");
                return true;
            }

            writer.WriteLine($"Analyzed {response.analyzedFrameCount} frames [{response.startFrame}..{response.endFrame}]");
            writer.WriteLine("");

            var ft = response.frameTime;
            writer.WriteLine("Frame Time:");
            writer.WriteLine($"  Avg: {ft.avgMs:F2}ms  Min: {ft.minMs:F2}ms  Max: {ft.maxMs:F2}ms  Median: {ft.medianMs:F2}ms  P95: {ft.p95Ms:F2}ms  P99: {ft.p99Ms:F2}ms");
            writer.WriteLine($"  Worst frame: #{ft.maxFrameIndex}");
            writer.WriteLine("");

            var gc = response.gcAlloc;
            var maxGcFrameStr = gc.maxBytesInFrame > 0 ? $"frame #{gc.maxFrameIndex}" : "frame #-";
            writer.WriteLine("GC Allocation:");
            writer.WriteLine($"  Total: {ProfilerFrameHelper.FormatBytes(gc.totalBytes)}  Avg/frame: {ProfilerFrameHelper.FormatBytes((long)gc.avgBytesPerFrame)}  Max/frame: {ProfilerFrameHelper.FormatBytes(gc.maxBytesInFrame)} ({maxGcFrameStr})");
            writer.WriteLine($"  Frames with GC: {gc.framesWithGc} / {response.analyzedFrameCount}");
            writer.WriteLine("");

            if (response.topSamples != null && response.topSamples.Length > 0)
            {
                writer.WriteLine("Top Samples (by avg self time):");
                writer.WriteLine($"{"Name",-50} {"Avg Self",10} {"Max Self",10} {"Avg Total",10} {"Avg Calls",10} {"Avg GC",10} {"Frames",8}");
                writer.WriteLine(new string('-', 112));
                foreach (var s in response.topSamples)
                {
                    writer.WriteLine(
                        $"{ProfilerFrameHelper.Truncate(s.name, 50),-50} " +
                        $"{s.avgSelfMs,8:F3}ms " +
                        $"{s.maxSelfMs,8:F3}ms " +
                        $"{s.avgTotalMs,8:F3}ms " +
                        $"{s.avgCalls,10:F1} " +
                        $"{ProfilerFrameHelper.FormatBytes((long)s.avgGcAllocBytes),10} " +
                        $"{s.presentInFrames,8}");
                }
            }

            return true;
        }

        protected override ValueTask<ProfilerAnalyzeFramesResponse> ExecuteAsync(ProfilerAnalyzeFramesRequest request, CancellationToken cancellationToken)
        {
            var (start, end) = ProfilerFrameHelper.ResolveFrameRange(request.startFrame, request.endFrame);
            var frameCount = end - start + 1;
            var topSampleCount = request.topSampleCount > 0 ? request.topSampleCount : 10;

            var frameTimes = new float[frameCount];
            var frameGcAllocs = new long[frameCount];
            var frameIndices = new int[frameCount];

            var sampleAccumulators = new Dictionary<string, SampleAccumulator>();

            for (var i = 0; i < frameCount; i++)
            {
                var frameIndex = start + i;
                frameIndices[i] = frameIndex;

                using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(
                    frameIndex, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                    HierarchyFrameDataView.columnDontSort, false))
                {
                    frameTimes[i] = frameData.frameTimeMs;

                    var samples = ProfilerFrameHelper.CollectAllSamples(frameData);

                    long gcTotal = 0;
                    foreach (var s in samples)
                    {
                        gcTotal += s.gcAllocBytes;

                        if (!sampleAccumulators.TryGetValue(s.name, out var acc))
                        {
                            acc = new SampleAccumulator { name = s.name };
                            sampleAccumulators[s.name] = acc;
                        }
                        acc.totalSelfMs += s.selfTimeMs;
                        acc.totalTotalMs += s.totalTimeMs;
                        acc.totalCalls += s.calls;
                        acc.totalGcAllocBytes += s.gcAllocBytes;
                        if (s.selfTimeMs > acc.maxSelfMs) acc.maxSelfMs = s.selfTimeMs;
                        acc.presentInFrames++;
                    }

                    frameGcAllocs[i] = gcTotal;
                }
            }

            var frameTime = BuildFrameTimeStats(frameTimes, frameIndices);
            var gcAlloc = BuildGcAllocStats(frameGcAllocs, frameIndices, frameCount);
            var topSamples = BuildTopSamples(sampleAccumulators, frameCount, topSampleCount, request.sampleNameFilter);

            return new ValueTask<ProfilerAnalyzeFramesResponse>(new ProfilerAnalyzeFramesResponse
            {
                analyzedFrameCount = frameCount,
                startFrame = start,
                endFrame = end,
                frameTime = frameTime,
                gcAlloc = gcAlloc,
                topSamples = topSamples
            });
        }

        private static FrameTimeStats BuildFrameTimeStats(float[] frameTimes, int[] frameIndices)
        {
            var sorted = new float[frameTimes.Length];
            Array.Copy(frameTimes, sorted, frameTimes.Length);
            Array.Sort(sorted);

            float min = sorted[0];
            float max = sorted[sorted.Length - 1];
            float sum = 0;
            int maxFrameIndex = frameIndices[0];
            float maxVal = frameTimes[0];

            for (var i = 0; i < frameTimes.Length; i++)
            {
                sum += frameTimes[i];
                if (frameTimes[i] > maxVal)
                {
                    maxVal = frameTimes[i];
                    maxFrameIndex = frameIndices[i];
                }
            }

            return new FrameTimeStats
            {
                avgMs = sum / frameTimes.Length,
                minMs = min,
                maxMs = max,
                medianMs = GetPercentile(sorted, 0.5f),
                p95Ms = GetPercentile(sorted, 0.95f),
                p99Ms = GetPercentile(sorted, 0.99f),
                maxFrameIndex = maxFrameIndex
            };
        }

        private static float GetPercentile(float[] sorted, float percentile)
        {
            if (sorted.Length == 1) return sorted[0];
            var index = percentile * (sorted.Length - 1);
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);
            if (lower == upper) return sorted[lower];
            var fraction = index - lower;
            return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
        }

        private static GcAllocStats BuildGcAllocStats(long[] gcAllocs, int[] frameIndices, int frameCount)
        {
            long total = 0;
            long maxBytes = 0;
            int maxFrameIndex = -1;
            int framesWithGc = 0;

            for (var i = 0; i < gcAllocs.Length; i++)
            {
                total += gcAllocs[i];
                if (gcAllocs[i] > 0) framesWithGc++;
                if (gcAllocs[i] > maxBytes)
                {
                    maxBytes = gcAllocs[i];
                    maxFrameIndex = frameIndices[i];
                }
            }

            return new GcAllocStats
            {
                totalBytes = total,
                avgBytesPerFrame = frameCount > 0 ? (float)total / frameCount : 0,
                maxBytesInFrame = maxBytes,
                maxFrameIndex = maxFrameIndex,
                framesWithGc = framesWithGc
            };
        }

        private static SampleStats[] BuildTopSamples(Dictionary<string, SampleAccumulator> accumulators, int frameCount, int topCount, string filter)
        {
            var list = new List<SampleStats>();
            foreach (var kvp in accumulators)
            {
                var acc = kvp.Value;

                if (!string.IsNullOrEmpty(filter) &&
                    acc.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                list.Add(new SampleStats
                {
                    name = acc.name,
                    avgSelfMs = acc.totalSelfMs / frameCount,
                    maxSelfMs = acc.maxSelfMs,
                    avgTotalMs = acc.totalTotalMs / frameCount,
                    avgCalls = (float)acc.totalCalls / frameCount,
                    avgGcAllocBytes = (float)acc.totalGcAllocBytes / frameCount,
                    presentInFrames = acc.presentInFrames
                });
            }

            list.Sort((a, b) => b.avgSelfMs.CompareTo(a.avgSelfMs));
            if (list.Count > topCount)
                list.RemoveRange(topCount, list.Count - topCount);

            return list.ToArray();
        }

        private class SampleAccumulator
        {
            public string name;
            public float totalSelfMs;
            public float maxSelfMs;
            public float totalTotalMs;
            public long totalCalls;
            public long totalGcAllocBytes;
            public int presentInFrames;
        }
    }

    [Serializable]
    public class ProfilerAnalyzeFramesRequest
    {
        public int startFrame = -1;
        public int endFrame = -1;
        public int topSampleCount = 10;
        public string sampleNameFilter = "";
    }

    [Serializable]
    public class ProfilerAnalyzeFramesResponse
    {
        public int analyzedFrameCount;
        public int startFrame;
        public int endFrame;
        public FrameTimeStats frameTime;
        public GcAllocStats gcAlloc;
        public SampleStats[] topSamples;
    }

    [Serializable]
    public class FrameTimeStats
    {
        public float avgMs;
        public float minMs;
        public float maxMs;
        public float medianMs;
        public float p95Ms;
        public float p99Ms;
        public int maxFrameIndex;
    }

    [Serializable]
    public class GcAllocStats
    {
        public long totalBytes;
        public float avgBytesPerFrame;
        public long maxBytesInFrame;
        public int maxFrameIndex;
        public int framesWithGc;
    }

    [Serializable]
    public class SampleStats
    {
        public string name;
        public float avgSelfMs;
        public float maxSelfMs;
        public float avgTotalMs;
        public float avgCalls;
        public float avgGcAllocBytes;
        public int presentInFrames;
    }
}
