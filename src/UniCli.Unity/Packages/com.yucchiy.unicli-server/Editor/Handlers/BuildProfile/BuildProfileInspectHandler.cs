#if UNITY_6000_0_OR_NEWER
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Profile;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Build")]
    public sealed class BuildProfileInspectHandler : CommandHandler<BuildProfileInspectRequest, BuildProfileInspectResponse>
    {
        public override string CommandName => "BuildProfile.Inspect";
        public override string Description => "Inspect a build profile's details";

        protected override bool TryWriteFormatted(BuildProfileInspectResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
            {
                writer.WriteLine("Failed to inspect build profile");
                return true;
            }

            var active = response.isActive ? " [active]" : "";
            writer.WriteLine($"{response.name} ({response.path}){active}");
            writer.WriteLine($"  Override Global Scenes: {response.overrideGlobalScenes}");

            if (response.scriptingDefines != null && response.scriptingDefines.Length > 0)
                writer.WriteLine($"  Scripting Defines: {string.Join(";", response.scriptingDefines)}");

            if (response.scenes != null && response.scenes.Length > 0)
            {
                writer.WriteLine($"  Scenes ({response.scenes.Length}):");
                foreach (var scene in response.scenes)
                {
                    var enabled = scene.enabled ? "" : " [disabled]";
                    writer.WriteLine($"    {scene.path}{enabled}");
                }
            }

            if (response.scenesForBuild != null && response.scenesForBuild.Length > 0)
            {
                writer.WriteLine($"  Scenes for Build ({response.scenesForBuild.Length}):");
                foreach (var scene in response.scenesForBuild)
                    writer.WriteLine($"    {scene.path}");
            }

            return true;
        }

        protected override ValueTask<BuildProfileInspectResponse> ExecuteAsync(BuildProfileInspectRequest request, CancellationToken cancellationToken)
        {
            BuildProfile profile;
            string path;

            if (string.IsNullOrEmpty(request.path))
            {
                profile = BuildProfile.GetActiveBuildProfile();
                if (profile == null)
                    throw new CommandFailedException("No active build profile. Specify a path or set an active profile.", new BuildProfileInspectResponse());
                path = AssetDatabase.GetAssetPath(profile);
            }
            else
            {
                path = request.path;
                profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
                if (profile == null)
                    throw new CommandFailedException($"Build profile not found at '{path}'", new BuildProfileInspectResponse { path = path });
            }

            var activeProfile = BuildProfile.GetActiveBuildProfile();

            var scenes = profile.scenes
                .Select(s => new BuildProfileSceneEntry { path = s.path, enabled = s.enabled })
                .ToArray();

            var scenesForBuild = profile.GetScenesForBuild()
                .Where(s => s.enabled)
                .Select(s => new BuildProfileSceneEntry { path = s.path, enabled = s.enabled })
                .ToArray();

            return new ValueTask<BuildProfileInspectResponse>(new BuildProfileInspectResponse
            {
                name = profile.name,
                path = path,
                isActive = activeProfile != null && profile == activeProfile,
                scriptingDefines = profile.scriptingDefines,
                scenes = scenes,
                overrideGlobalScenes = profile.overrideGlobalScenes,
                scenesForBuild = scenesForBuild,
            });
        }
    }

    [Serializable]
    public class BuildProfileInspectRequest
    {
        public string path;
    }

    [Serializable]
    public class BuildProfileInspectResponse
    {
        public string name;
        public string path;
        public bool isActive;
        public string[] scriptingDefines;
        public BuildProfileSceneEntry[] scenes;
        public bool overrideGlobalScenes;
        public BuildProfileSceneEntry[] scenesForBuild;
    }

    [Serializable]
    public class BuildProfileSceneEntry
    {
        public string path;
        public bool enabled;
    }
}
#endif
