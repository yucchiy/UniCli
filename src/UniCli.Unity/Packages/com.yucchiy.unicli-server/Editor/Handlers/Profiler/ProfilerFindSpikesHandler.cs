using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Profiler")]
    public sealed class ProfilerFindSpikesHandler : CommandHandler<ProfilerFindSpikesRequest, ProfilerFindSpikesResponse>
    {
        public override string CommandName => "Profiler.FindSpikes";
        public override string Description => "Find frames exceeding frame time or GC allocation thresholds";

        protected override bool TryWriteFormatted(ProfilerFindSpikesResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
            {
                writer.WriteLine("Failed to find spikes");
                return true;
            }

            writer.WriteLine($"Searched {response.searchedFrameCount} frames [{response.startFrame}..{response.endFrame}]");

            var showing = response.spikes != null ? response.spikes.Length : 0;
            writer.WriteLine($"Found {response.totalSpikeCount} spikes (showing top {showing})");

            if (response.spikes == null || response.spikes.Length == 0)
                return true;

            writer.WriteLine("");

            for (var i = 0; i < response.spikes.Length; i++)
            {
                var spike = response.spikes[i];
                writer.WriteLine(
                    $"#{i + 1}  Frame {spike.frameIndex}  {spike.frameTimeMs:F2}ms  " +
                    $"GC: {ProfilerFrameHelper.FormatBytes(spike.gcAllocBytes)}  [{spike.reason}]");

                if (spike.topSamples != null)
                {
                    foreach (var s in spike.topSamples)
                    {
                        var gcPart = s.gcAllocBytes > 0 ? $"  GC: {ProfilerFrameHelper.FormatBytes(s.gcAllocBytes)}" : "";
                        writer.WriteLine($"    {ProfilerFrameHelper.Truncate(s.name, 40),-40} {s.selfTimeMs,7:F2}ms self{gcPart}");
                    }
                }

                if (i < response.spikes.Length - 1)
                    writer.WriteLine("");
            }

            return true;
        }

        protected override ValueTask<ProfilerFindSpikesResponse> ExecuteAsync(ProfilerFindSpikesRequest request, CancellationToken cancellationToken)
        {
            if (request.frameTimeThresholdMs <= 0 && request.gcThresholdBytes <= 0)
                throw new ArgumentException("At least one threshold must be specified (frameTimeThresholdMs or gcThresholdBytes)");

            var (start, end) = ProfilerFrameHelper.ResolveFrameRange(request.startFrame, request.endFrame);
            var frameCount = end - start + 1;
            var limit = request.limit > 0 ? request.limit : 20;
            var samplesPerFrame = request.samplesPerFrame;

            var spikes = new List<SpikeFrame>();

            for (var i = 0; i < frameCount; i++)
            {
                var frameIndex = start + i;

                using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(
                    frameIndex, 0, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                    HierarchyFrameDataView.columnDontSort, false))
                {
                    var frameTimeMs = frameData.frameTimeMs;
                    var gcAlloc = ProfilerFrameHelper.GetFrameGcAlloc(frameData);

                    var isFrameTimeSpike = request.frameTimeThresholdMs > 0 && frameTimeMs > request.frameTimeThresholdMs;
                    var isGcSpike = request.gcThresholdBytes > 0 && gcAlloc > request.gcThresholdBytes;

                    if (!isFrameTimeSpike && !isGcSpike) continue;

                    string reason;
                    if (isFrameTimeSpike && isGcSpike) reason = "both";
                    else if (isFrameTimeSpike) reason = "frameTime";
                    else reason = "gcAlloc";

                    ProfilerSampleInfo[] topSamples = null;
                    if (samplesPerFrame > 0)
                    {
                        var allSamples = ProfilerFrameHelper.CollectAllSamples(frameData);
                        allSamples.Sort((a, b) => b.totalTimeMs.CompareTo(a.totalTimeMs));
                        if (allSamples.Count > samplesPerFrame)
                            allSamples.RemoveRange(samplesPerFrame, allSamples.Count - samplesPerFrame);
                        topSamples = allSamples.ToArray();
                    }

                    spikes.Add(new SpikeFrame
                    {
                        frameIndex = frameIndex,
                        frameTimeMs = frameTimeMs,
                        gcAllocBytes = gcAlloc,
                        totalSampleCount = frameData.sampleCount,
                        reason = reason,
                        topSamples = topSamples
                    });
                }
            }

            var totalSpikeCount = spikes.Count;

            spikes.Sort((a, b) => b.frameTimeMs.CompareTo(a.frameTimeMs));
            if (spikes.Count > limit)
                spikes.RemoveRange(limit, spikes.Count - limit);

            return new ValueTask<ProfilerFindSpikesResponse>(new ProfilerFindSpikesResponse
            {
                searchedFrameCount = frameCount,
                startFrame = start,
                endFrame = end,
                totalSpikeCount = totalSpikeCount,
                spikes = spikes.ToArray()
            });
        }
    }

    [Serializable]
    public class ProfilerFindSpikesRequest
    {
        public int startFrame = -1;
        public int endFrame = -1;
        public float frameTimeThresholdMs;
        public long gcThresholdBytes;
        public int limit = 20;
        public int samplesPerFrame = 5;
    }

    [Serializable]
    public class ProfilerFindSpikesResponse
    {
        public int searchedFrameCount;
        public int startFrame;
        public int endFrame;
        public int totalSpikeCount;
        public SpikeFrame[] spikes;
    }

    [Serializable]
    public class SpikeFrame
    {
        public int frameIndex;
        public float frameTimeMs;
        public long gcAllocBytes;
        public int totalSampleCount;
        public string reason;
        public ProfilerSampleInfo[] topSamples;
    }
}
