using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Profiler")]
    public sealed class ProfilerLoadProfileHandler : CommandHandler<ProfilerLoadProfileRequest, ProfilerLoadProfileResponse>
    {
        public override string CommandName => "Profiler.LoadProfile";
        public override string Description => "Load profiler data from a .raw file";

        protected override bool TryWriteFormatted(ProfilerLoadProfileResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"Profile loaded from: {response.path}");
                writer.WriteLine($"Frames: [{response.firstFrameIndex}..{response.lastFrameIndex}] ({response.frameCount} frames)");
            }
            else
            {
                writer.WriteLine("Failed to load profile");
            }
            return true;
        }

        protected override ValueTask<ProfilerLoadProfileResponse> ExecuteAsync(ProfilerLoadProfileRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.path))
                throw new ArgumentException("path is required");

            if (!File.Exists(request.path))
                throw new FileNotFoundException($"Profile file not found: {request.path}");

            var result = ProfilerDriver.LoadProfile(request.path, request.keepExistingData);
            if (!result)
                throw new InvalidOperationException($"Failed to load profile from: {request.path}");

            var first = ProfilerDriver.firstFrameIndex;
            var last = ProfilerDriver.lastFrameIndex;

            return new ValueTask<ProfilerLoadProfileResponse>(new ProfilerLoadProfileResponse
            {
                path = request.path,
                firstFrameIndex = first,
                lastFrameIndex = last,
                frameCount = last - first + 1
            });
        }
    }

    [Serializable]
    public class ProfilerLoadProfileRequest
    {
        public string path;
        public bool keepExistingData;
    }

    [Serializable]
    public class ProfilerLoadProfileResponse
    {
        public string path;
        public int firstFrameIndex;
        public int lastFrameIndex;
        public int frameCount;
    }
}
