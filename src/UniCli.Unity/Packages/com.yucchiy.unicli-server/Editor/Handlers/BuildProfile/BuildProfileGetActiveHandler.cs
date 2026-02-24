#if UNITY_6000_0_OR_NEWER
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEditor.Build.Profile;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class BuildProfileGetActiveHandler : CommandHandler<Unit, BuildProfileGetActiveResponse>
    {
        public override string CommandName => "BuildProfile.GetActive";
        public override string Description => "Get the active build profile";

        protected override bool TryWriteFormatted(BuildProfileGetActiveResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
            {
                writer.WriteLine("Failed to get active build profile");
                return true;
            }

            if (!response.hasActiveProfile)
            {
                writer.WriteLine("No active build profile (using platform profile)");
                return true;
            }

            writer.WriteLine($"{response.name} ({response.path})");
            if (response.scriptingDefines != null && response.scriptingDefines.Length > 0)
                writer.WriteLine($"  Scripting Defines: {string.Join(";", response.scriptingDefines)}");
            writer.WriteLine($"  Override Global Scenes: {response.overrideGlobalScenes}");
            if (response.scenes != null && response.scenes.Length > 0)
            {
                writer.WriteLine($"  Scenes ({response.scenes.Length}):");
                foreach (var scene in response.scenes)
                    writer.WriteLine($"    {scene}");
            }

            return true;
        }

        protected override ValueTask<BuildProfileGetActiveResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null)
            {
                return new ValueTask<BuildProfileGetActiveResponse>(new BuildProfileGetActiveResponse
                {
                    hasActiveProfile = false,
                });
            }

            var path = AssetDatabase.GetAssetPath(profile);
            var scenes = profile.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            return new ValueTask<BuildProfileGetActiveResponse>(new BuildProfileGetActiveResponse
            {
                hasActiveProfile = true,
                name = profile.name,
                path = path,
                scriptingDefines = profile.scriptingDefines,
                scenes = scenes,
                overrideGlobalScenes = profile.overrideGlobalScenes,
            });
        }
    }

    [Serializable]
    public class BuildProfileGetActiveResponse
    {
        public bool hasActiveProfile;
        public string name;
        public string path;
        public string[] scriptingDefines;
        public string[] scenes;
        public bool overrideGlobalScenes;
    }
}
#endif
