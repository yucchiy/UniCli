using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ProfilerGetFrameDataHandler : CommandHandler<ProfilerGetFrameDataRequest, ProfilerGetFrameDataResponse>
    {
        public override string CommandName => CommandNames.Profiler.GetFrameData;
        public override string Description => "Get CPU profiler sample data for a specific frame";

        protected override bool TryWriteFormatted(ProfilerGetFrameDataResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
            {
                writer.WriteLine("Failed to get frame data");
                return true;
            }

            writer.WriteLine($"Frame {response.frameIndex} ({response.frameTimeMs:F2}ms, {response.totalSampleCount} total samples)");
            writer.WriteLine("");

            if (response.samples != null && response.samples.Length > 0)
            {
                writer.WriteLine($"{"Name",-50} {"Total",8} {"Self",8} {"Calls",6} {"GC Alloc",10}");
                writer.WriteLine(new string('-', 86));
                foreach (var s in response.samples)
                {
                    writer.WriteLine($"{ProfilerFrameHelper.Truncate(s.name, 50),-50} {s.totalTimeMs,7:F2}ms {s.selfTimeMs,7:F2}ms {s.calls,6} {ProfilerFrameHelper.FormatBytes(s.gcAllocBytes),10}");
                }
            }
            else
            {
                writer.WriteLine("  (no samples)");
            }

            return true;
        }

        protected override ValueTask<ProfilerGetFrameDataResponse> ExecuteAsync(ProfilerGetFrameDataRequest request, CancellationToken cancellationToken)
        {
            var firstFrame = ProfilerDriver.firstFrameIndex;
            var lastFrame = ProfilerDriver.lastFrameIndex;

            if (lastFrame < firstFrame)
                throw new InvalidOperationException("No profiler frames available. Start recording first.");

            var frameIndex = request.frame < 0 ? lastFrame : request.frame;

            if (frameIndex < firstFrame || frameIndex > lastFrame)
                throw new ArgumentException($"Frame {frameIndex} is out of range [{firstFrame}..{lastFrame}]");

            var limit = request.limit > 0 ? request.limit : 20;

            using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                HierarchyFrameDataView.columnDontSort, false))
            {
                var frameTimeMs = frameData.frameTimeMs;
                var totalSamples = frameData.sampleCount;

                var allSamples = ProfilerFrameHelper.CollectAllSamples(frameData);
                allSamples.Sort((a, b) => b.totalTimeMs.CompareTo(a.totalTimeMs));
                if (allSamples.Count > limit)
                    allSamples.RemoveRange(limit, allSamples.Count - limit);

                return new ValueTask<ProfilerGetFrameDataResponse>(new ProfilerGetFrameDataResponse
                {
                    frameIndex = frameIndex,
                    frameTimeMs = frameTimeMs,
                    totalSampleCount = totalSamples,
                    samples = allSamples.ToArray()
                });
            }
        }
    }

    [Serializable]
    public class ProfilerGetFrameDataRequest
    {
        public int frame = -1;
        public int limit;
    }

    [Serializable]
    public class ProfilerGetFrameDataResponse
    {
        public int frameIndex;
        public float frameTimeMs;
        public int totalSampleCount;
        public ProfilerSampleInfo[] samples;
    }

    [Serializable]
    public class ProfilerSampleInfo
    {
        public string name;
        public float totalTimeMs;
        public float selfTimeMs;
        public int calls;
        public long gcAllocBytes;
    }
}
