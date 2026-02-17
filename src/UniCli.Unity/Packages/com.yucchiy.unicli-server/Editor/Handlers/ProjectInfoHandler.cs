using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ProjectInfoHandler : CommandHandler<Unit, ProjectInfoResponse>
    {
        private readonly ServerContext _context;

        public ProjectInfoHandler(ServerContext context)
        {
            _context = context;
        }

        public override string CommandName => CommandNames.Project.Inspect;
        public override string Description => "Get Unity project information";

        protected override bool TryWriteFormatted(ProjectInfoResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine($"Unity:     {response.unityVersion}");
            writer.WriteLine($"Project:   {response.productName}");
            writer.WriteLine($"Company:   {response.companyName}");
            writer.WriteLine($"Path:      {response.projectPath}");
            writer.WriteLine($"Target:    {response.buildTarget}");
            writer.WriteLine($"Playing:   {(response.isPlaying ? "Yes" : "No")}");
            writer.WriteLine($"PID:       {response.processId}");
            writer.WriteLine($"Server ID: {response.serverId}");
            writer.WriteLine($"Version:   {response.serverVersion}");
            writer.WriteLine($"Started:   {response.startedAt}");
            writer.WriteLine($"Uptime:    {response.uptimeSeconds:F1}s");

            return true;
        }

        protected override ValueTask<ProjectInfoResponse> ExecuteAsync(Unit request)
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectInfoHandler).Assembly);

            var response = new ProjectInfoResponse
            {
                unityVersion = Application.unityVersion,
                projectPath = Application.dataPath,
                productName = PlayerSettings.productName,
                companyName = PlayerSettings.companyName,
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                isPlaying = EditorApplication.isPlaying,
                processId = Process.GetCurrentProcess().Id,
                serverId = _context.ServerId,
                serverVersion = packageInfo?.version ?? "unknown",
                startedAt = _context.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                uptimeSeconds = (DateTime.Now - _context.StartedAt).TotalSeconds
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
        public string serverId;
        public string serverVersion;
        public string startedAt;
        public double uptimeSeconds;
    }
}
