using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Profiler")]
    public sealed class ProfilerSaveProfileHandler : CommandHandler<ProfilerSaveProfileRequest, ProfilerSaveProfileResponse>
    {
        public override string CommandName => "Profiler.SaveProfile";
        public override string Description => "Save profiler data to a .raw file";

        protected override bool TryWriteFormatted(ProfilerSaveProfileResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Profile saved to: {response.path}");
            else
                writer.WriteLine("Failed to save profile");
            return true;
        }

        protected override ValueTask<ProfilerSaveProfileResponse> ExecuteAsync(ProfilerSaveProfileRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.path))
                throw new ArgumentException("path is required");

            var directory = Path.GetDirectoryName(request.path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            ProfilerDriver.SaveProfile(request.path);

            if (!File.Exists(request.path))
                throw new InvalidOperationException($"Failed to save profile to: {request.path}");

            return new ValueTask<ProfilerSaveProfileResponse>(new ProfilerSaveProfileResponse
            {
                path = request.path,
                size = new FileInfo(request.path).Length
            });
        }
    }

    [Serializable]
    public class ProfilerSaveProfileRequest
    {
        public string path;
    }

    [Serializable]
    public class ProfilerSaveProfileResponse
    {
        public string path;
        public long size;
    }
}
