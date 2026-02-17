using System;
using System.Threading.Tasks;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ProfilerStartRecordingHandler : CommandHandler<ProfilerStartRecordingRequest, ProfilerStartRecordingResponse>
    {
        public override string CommandName => CommandNames.Profiler.StartRecording;
        public override string Description => "Start profiler recording";

        protected override bool TryWriteFormatted(ProfilerStartRecordingResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine("Profiler recording started");
                if (response.deepProfiling)
                    writer.WriteLine("  Deep Profiling: enabled");
                if (response.profileEditor)
                    writer.WriteLine("  Profile Editor: enabled");
            }
            else
            {
                writer.WriteLine("Failed to start profiler recording");
            }
            return true;
        }

        protected override ValueTask<ProfilerStartRecordingResponse> ExecuteAsync(ProfilerStartRecordingRequest request)
        {
            if (!request.keepFrames)
                ProfilerDriver.ClearAllFrames();

            ProfilerDriver.deepProfiling = request.deep;
            ProfilerDriver.profileEditor = request.editor;
            ProfilerDriver.enabled = true;

            return new ValueTask<ProfilerStartRecordingResponse>(new ProfilerStartRecordingResponse
            {
                enabled = ProfilerDriver.enabled,
                deepProfiling = ProfilerDriver.deepProfiling,
                profileEditor = ProfilerDriver.profileEditor
            });
        }
    }

    [Serializable]
    public class ProfilerStartRecordingRequest
    {
        public bool deep;
        public bool editor;
        public bool keepFrames;
    }

    [Serializable]
    public class ProfilerStartRecordingResponse
    {
        public bool enabled;
        public bool deepProfiling;
        public bool profileEditor;
    }
}
