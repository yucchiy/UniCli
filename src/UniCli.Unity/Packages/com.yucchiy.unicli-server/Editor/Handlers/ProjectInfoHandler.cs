using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ProjectInfoHandler : CommandHandler<Unit, ProjectInfoResponse>
    {
        public override string CommandName => CommandNames.Project.Inspect;
        public override string Description => "Get Unity project information";

        protected override bool TryFormat(ProjectInfoResponse response, bool success, out string formatted)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Unity:   {response.unityVersion}");
            sb.AppendLine($"Project: {response.productName}");
            sb.AppendLine($"Company: {response.companyName}");
            sb.AppendLine($"Path:    {response.projectPath}");
            sb.AppendLine($"Target:  {response.buildTarget}");
            sb.AppendLine($"Playing: {(response.isPlaying ? "Yes" : "No")}");
            sb.Append($"PID:     {response.processId}");

            formatted = sb.ToString();
            return true;
        }

        protected override ValueTask<ProjectInfoResponse> ExecuteAsync(Unit request)
        {
            var response = new ProjectInfoResponse
            {
                unityVersion = Application.unityVersion,
                projectPath = Application.dataPath,
                productName = PlayerSettings.productName,
                companyName = PlayerSettings.companyName,
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                isPlaying = EditorApplication.isPlaying,
                processId = Process.GetCurrentProcess().Id
            };

            return new ValueTask<ProjectInfoResponse>(response);
        }
    }

    [Serializable]
    public class ProjectInfoResponse
    {
        public string unityVersion;
        public string projectPath;
        public string productName;
        public string companyName;
        public string buildTarget;
        public bool isPlaying;
        public int processId;
    }
}
