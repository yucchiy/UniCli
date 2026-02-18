#if UNITY_6000_0_OR_NEWER
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Profile;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class BuildProfileSetActiveHandler : CommandHandler<BuildProfileSetActiveRequest, BuildProfileSetActiveResponse>
    {
        public override string CommandName => CommandNames.BuildProfile.SetActive;
        public override string Description => "Set the active build profile";

        protected override bool TryWriteFormatted(BuildProfileSetActiveResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                if (string.IsNullOrEmpty(response.path))
                    writer.WriteLine("Switched to platform profile");
                else
                    writer.WriteLine($"Active build profile: {response.name} ({response.path})");
            }
            else
            {
                writer.WriteLine("Failed to set active build profile");
            }

            return true;
        }

        protected override ValueTask<BuildProfileSetActiveResponse> ExecuteAsync(BuildProfileSetActiveRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.path) || request.path == "none")
            {
                BuildProfile.SetActiveBuildProfile(null);
                return new ValueTask<BuildProfileSetActiveResponse>(new BuildProfileSetActiveResponse
                {
                    name = "",
                    path = "",
                });
            }

            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(request.path);
            if (profile == null)
                throw new CommandFailedException($"Build profile not found at '{request.path}'");

            BuildProfile.SetActiveBuildProfile(profile);

            return new ValueTask<BuildProfileSetActiveResponse>(new BuildProfileSetActiveResponse
            {
                name = profile.name,
                path = request.path,
            });
        }
    }

    [Serializable]
    public class BuildProfileSetActiveRequest
    {
        public string path;
    }

    [Serializable]
    public class BuildProfileSetActiveResponse
    {
        public string name;
        public string path;
    }
}
#endif
