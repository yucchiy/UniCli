using System;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditorInternal;
using UnityEngine.Profiling;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ProfilerInspectHandler : CommandHandler<Unit, ProfilerInspectResponse>
    {
        public override string CommandName => CommandNames.Profiler.Inspect;
        public override string Description => "Get profiler status and memory statistics";

        protected override bool TryWriteFormatted(ProfilerInspectResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine($"Profiler: {(response.enabled ? "Recording" : "Idle")}");
            writer.WriteLine($"  Deep Profiling: {response.deepProfiling}");
            writer.WriteLine($"  Profile Editor: {response.profileEditor}");
            if (response.frameCount > 0)
                writer.WriteLine($"  Frames: {response.firstFrameIndex}..{response.lastFrameIndex} ({response.frameCount} frames)");
            else
                writer.WriteLine("  Frames: (none)");
            writer.WriteLine("");
            writer.WriteLine("Memory:");
            writer.WriteLine($"  Total Allocated: {FormatBytes(response.totalAllocatedMemory)}");
            writer.WriteLine($"  Total Reserved:  {FormatBytes(response.totalReservedMemory)}");
            writer.WriteLine($"  Mono Heap Size:  {FormatBytes(response.monoHeapSize)}");
            writer.WriteLine($"  Mono Used Size:  {FormatBytes(response.monoUsedSize)}");
            writer.WriteLine($"  Graphics Memory: {FormatBytes(response.graphicsMemory)}");
            return true;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024L) return $"{bytes} B";
            if (bytes < 1024L * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        protected override ValueTask<ProfilerInspectResponse> ExecuteAsync(Unit request)
        {
            var firstFrame = ProfilerDriver.firstFrameIndex;
            var lastFrame = ProfilerDriver.lastFrameIndex;

            return new ValueTask<ProfilerInspectResponse>(new ProfilerInspectResponse
            {
                enabled = ProfilerDriver.enabled,
                deepProfiling = ProfilerDriver.deepProfiling,
                profileEditor = ProfilerDriver.profileEditor,
                firstFrameIndex = firstFrame,
                lastFrameIndex = lastFrame,
                frameCount = lastFrame >= firstFrame && firstFrame >= 0 ? lastFrame - firstFrame + 1 : 0,
                totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong(),
                totalReservedMemory = Profiler.GetTotalReservedMemoryLong(),
                monoHeapSize = Profiler.GetMonoHeapSizeLong(),
                monoUsedSize = Profiler.GetMonoUsedSizeLong(),
                graphicsMemory = Profiler.GetAllocatedMemoryForGraphicsDriver()
            });
        }
    }

    [Serializable]
    public class ProfilerInspectResponse
    {
        public bool enabled;
        public bool deepProfiling;
        public bool profileEditor;
        public int firstFrameIndex;
        public int lastFrameIndex;
        public int frameCount;
        public long totalAllocatedMemory;
        public long totalReservedMemory;
        public long monoHeapSize;
        public long monoUsedSize;
        public long graphicsMemory;
    }
}
