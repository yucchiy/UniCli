using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class BuildHandler : CommandHandler<BuildRequest, BuildResponse>
    {
        public override string CommandName => CommandNames.BuildPlayer.Build;
        public override string Description => "Build the player using BuildPipeline.BuildPlayer";

        protected override bool TryWriteFormatted(BuildResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Build succeeded for {response.target} → {response.locationPathName}");
            else
                writer.WriteLine($"Build failed for {response.target} ({response.totalErrorCount} errors, {response.totalWarningCount} warnings)");

            if (response.totalBuildTimeSec > 0)
                writer.WriteLine($"  Total time: {response.totalBuildTimeSec:F1}s");

            if (response.totalSizeBytes > 0)
                writer.WriteLine($"  Total size: {FormatBytes(response.totalSizeBytes)}");

            WriteMessages(writer, "Errors", response.errors);
            WriteMessages(writer, "Warnings", response.warnings);

            return true;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }

        private static void WriteMessages(IFormatWriter writer, string label, BuildMessageInfo[] messages)
        {
            if (messages == null || messages.Length == 0) return;

            writer.WriteLine($"  {label}:");
            foreach (var msg in messages)
                writer.WriteLine($"    {msg.message}");
        }

        protected override ValueTask<BuildResponse> ExecuteAsync(BuildRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.locationPathName))
                throw new ArgumentException("locationPathName is required.");

            var target = ResolveTarget(request.target);
            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var scenes = ResolveScenes(request.scenes);
            var options = ResolveOptions(request.options);

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = request.locationPathName,
                target = target,
                targetGroup = targetGroup,
                options = options,
                extraScriptingDefines = request.extraScriptingDefines ?? Array.Empty<string>()
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var response = MapResponse(report, target, targetGroup, request.locationPathName);

            if (report.summary.result != BuildResult.Succeeded)
                throw new CommandFailedException($"Build failed for {target}: {report.summary.result}", response);

            return new ValueTask<BuildResponse>(response);
        }

        private static BuildTarget ResolveTarget(string target)
        {
            if (string.IsNullOrEmpty(target))
                return EditorUserBuildSettings.activeBuildTarget;

            if (Enum.TryParse<BuildTarget>(target, true, out var parsed))
                return parsed;

            throw new ArgumentException($"Invalid build target: '{target}'. Use a valid BuildTarget name (e.g. Android, iOS, StandaloneWindows64).");
        }

        private static string[] ResolveScenes(string[] scenes)
        {
            if (scenes != null && scenes.Length > 0)
                return scenes;

            return EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
        }

        private static BuildOptions ResolveOptions(string[] options)
        {
            if (options == null || options.Length == 0)
                return BuildOptions.None;

            var result = BuildOptions.None;
            foreach (var option in options)
            {
                if (Enum.TryParse<BuildOptions>(option, true, out var parsed))
                    result |= parsed;
                else
                    throw new ArgumentException($"Invalid build option: '{option}'. Use a valid BuildOptions name (e.g. Development, ConnectWithProfiler).");
            }

            return result;
        }

        private static BuildResponse MapResponse(BuildReport report, BuildTarget target, BuildTargetGroup targetGroup, string locationPathName)
        {
            var errors = new List<BuildMessageInfo>();
            var warnings = new List<BuildMessageInfo>();

            foreach (var step in report.steps)
            {
                foreach (var msg in step.messages)
                {
                    var info = new BuildMessageInfo
                    {
                        message = msg.content
                    };

                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        errors.Add(info);
                    else if (msg.type == LogType.Warning)
                        warnings.Add(info);
                }
            }

            var steps = new List<BuildStepInfo>();
            foreach (var step in report.steps)
            {
                steps.Add(new BuildStepInfo
                {
                    name = step.name,
                    durationSec = step.duration.TotalSeconds,
                    depth = step.depth
                });
            }

            return new BuildResponse
            {
                target = target.ToString(),
                targetGroup = targetGroup.ToString(),
                locationPathName = locationPathName,
                result = report.summary.result.ToString(),
                totalErrorCount = report.summary.totalErrors,
                totalWarningCount = report.summary.totalWarnings,
                totalBuildTimeSec = report.summary.totalTime.TotalSeconds,
                totalSizeBytes = (long)report.summary.totalSize,
                steps = steps.ToArray(),
                errors = errors.ToArray(),
                warnings = warnings.ToArray()
            };
        }
    }

    [Serializable]
    public class BuildRequest
    {
        public string target = "";
        public string locationPathName = "";
        public string[] scenes;
        public string[] options;
        public string[] extraScriptingDefines;
    }

    [Serializable]
    public class BuildResponse
    {
        public string target;
        public string targetGroup;
        public string locationPathName;
        public string result;
        public int totalErrorCount;
        public int totalWarningCount;
        public double totalBuildTimeSec;
        public long totalSizeBytes;
        public BuildStepInfo[] steps;
        public BuildMessageInfo[] errors;
        public BuildMessageInfo[] warnings;
    }

    [Serializable]
    public class BuildStepInfo
    {
        public string name;
        public double durationSec;
        public int depth;
    }

    [Serializable]
    public class BuildMessageInfo
    {
        public string message;
    }
}
