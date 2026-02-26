using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class BuildTargetSwitchHandler : CommandHandler<BuildTargetSwitchRequest, BuildTargetSwitchResponse>
    {
        private readonly EditorStateGuard _guard;

        public BuildTargetSwitchHandler(EditorStateGuard guard)
        {
            _guard = guard;
        }

        public override string CommandName => "BuildTarget.Switch";
        public override string Description => "Switch the active build target via EditorUserBuildSettings.SwitchActiveBuildTarget";

        protected override bool TryWriteFormatted(BuildTargetSwitchResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
                return false;

            writer.WriteLine($"Switched to {response.buildTarget} ({response.buildTargetGroup})");

            return true;
        }

        protected override ValueTask<BuildTargetSwitchResponse> ExecuteAsync(BuildTargetSwitchRequest request, CancellationToken cancellationToken)
        {
            using var scope = _guard.BeginScope(CommandName, GuardCondition.NotPlaying);

            if (string.IsNullOrEmpty(request.target))
                throw new ArgumentException("target is required (e.g. Android, iOS, StandaloneWindows64, StandaloneOSX, WebGL)");

            if (!Enum.TryParse<BuildTarget>(request.target, true, out var target))
                throw new ArgumentException($"Invalid build target: '{request.target}'. Use a valid BuildTarget name (e.g. Android, iOS, StandaloneWindows64, StandaloneOSX, WebGL).");

            var group = BuildPipeline.GetBuildTargetGroup(target);

            var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            if (!switched)
                throw new CommandFailedException($"Failed to switch build target to {target}", new { target = target.ToString() });

            return new ValueTask<BuildTargetSwitchResponse>(new BuildTargetSwitchResponse
            {
                buildTarget = target.ToString(),
                buildTargetGroup = group.ToString(),
            });
        }
    }

    [Serializable]
    public class BuildTargetSwitchRequest
    {
        public string target;
    }

    [Serializable]
    public class BuildTargetSwitchResponse
    {
        public string buildTarget;
        public string buildTargetGroup;
    }
}
