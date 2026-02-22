using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Profiler")]
    public sealed class ProfilerStopRecordingHandler : CommandHandler<Unit, ProfilerStopRecordingResponse>
    {
        public override string CommandName => "Profiler.StopRecording";
        public override string Description => "Stop profiler recording";

        protected override bool TryWriteFormatted(ProfilerStopRecordingResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine("Profiler recording stopped");
                if (response.frameCount > 0)
                    writer.WriteLine($"  Captured frames: {response.firstFrameIndex}..{response.lastFrameIndex} ({response.frameCount} frames)");
            }
            else
            {
                writer.WriteLine("Failed to stop profiler recording");
            }
            return true;
        }

        protected override ValueTask<ProfilerStopRecordingResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var firstFrame = ProfilerDriver.firstFrameIndex;
            var lastFrame = ProfilerDriver.lastFrameIndex;

            ProfilerDriver.enabled = false;

            return new ValueTask<ProfilerStopRecordingResponse>(new ProfilerStopRecordingResponse
            {
                firstFrameIndex = firstFrame,
                lastFrameIndex = lastFrame,
                frameCount = lastFrame >= firstFrame && firstFrame >= 0 ? lastFrame - firstFrame + 1 : 0
            });
        }
    }

    [Serializable]
    public class ProfilerStopRecordingResponse
    {
        public int firstFrameIndex;
        public int lastFrameIndex;
        public int frameCount;
    }
}
