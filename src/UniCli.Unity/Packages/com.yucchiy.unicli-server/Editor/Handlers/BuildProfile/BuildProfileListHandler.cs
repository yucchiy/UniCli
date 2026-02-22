#if UNITY_6000_0_OR_NEWER
using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEditor.Build.Profile;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Build")]
    public sealed class BuildProfileListHandler : CommandHandler<Unit, BuildProfileListResponse>
    {
        public override string CommandName => "BuildProfile.List";
        public override string Description => "List all build profiles";

        protected override bool TryWriteFormatted(BuildProfileListResponse response, bool success, IFormatWriter writer)
        {
            if (!success || response.profiles == null || response.profiles.Length == 0)
            {
                writer.WriteLine("No build profiles found.");
                return true;
            }

            writer.WriteLine($"Build profiles ({response.profiles.Length}):");
            foreach (var profile in response.profiles)
            {
                var active = profile.isActive ? " [active]" : "";
                writer.WriteLine($"  {profile.name} ({profile.path}){active}");
            }

            return true;
        }

        protected override ValueTask<BuildProfileListResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var guids = AssetDatabase.FindAssets("t:BuildProfile");
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            var profiles = new BuildProfileEntry[guids.Length];

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
                profiles[i] = new BuildProfileEntry
                {
                    name = profile != null ? profile.name : System.IO.Path.GetFileNameWithoutExtension(path),
                    path = path,
                    isActive = activeProfile != null && profile == activeProfile,
                };
            }

            return new ValueTask<BuildProfileListResponse>(new BuildProfileListResponse
            {
                profiles = profiles,
            });
        }
    }

    [Serializable]
    public class BuildProfileListResponse
    {
        public BuildProfileEntry[] profiles;
    }

    [Serializable]
    public class BuildProfileEntry
    {
        public string name;
        public string path;
        public bool isActive;
    }
}
#endif
