using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class BuildTargetGetActiveHandler : CommandHandler<Unit, BuildTargetGetActiveResponse>
    {
        public override string CommandName => "BuildTarget.GetActive";
        public override string Description => "Get the active build target and build target group via EditorUserBuildSettings";

        protected override bool TryWriteFormatted(BuildTargetGetActiveResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
                return false;

            writer.WriteLine($"Build Target: {response.buildTarget}");
            writer.WriteLine($"Target Group: {response.buildTargetGroup}");

            return true;
        }

        protected override ValueTask<BuildTargetGetActiveResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            return new ValueTask<BuildTargetGetActiveResponse>(new BuildTargetGetActiveResponse
            {
                buildTarget = target.ToString(),
                buildTargetGroup = group.ToString(),
            });
        }
    }

    [Serializable]
    public class BuildTargetGetActiveResponse
    {
        public string buildTarget;
        public string buildTargetGroup;
    }
}
