using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class CompilePlayerHandler : CommandHandler<CompilePlayerRequest, CompilePlayerResponse>
    {
        private readonly EditorStateGuard _guard;

        public CompilePlayerHandler(EditorStateGuard guard)
        {
            _guard = guard;
        }

        public override string CommandName => "BuildPlayer.Compile";
        public override string Description => "Compile player scripts for a specific build target";

        protected override bool TryWriteFormatted(CompilePlayerResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Player compilation succeeded for {response.target} ({response.errorCount} errors, {response.warningCount} warnings, {response.assemblyCount} assemblies)");
            else
                writer.WriteLine($"Player compilation failed for {response.target} ({response.errorCount} errors, {response.warningCount} warnings)");

            WriteIssues(writer, response.errors);
            WriteIssues(writer, response.warnings);

            return true;
        }

        private static void WriteIssues(IFormatWriter writer, CompileIssue[] issues)
        {
            if (issues == null) return;

            foreach (var issue in issues)
            {
                if (!string.IsNullOrEmpty(issue.file))
                    writer.WriteLine($"  {issue.file}({issue.line}): {issue.message}");
                else
                    writer.WriteLine($"  {issue.message}");
            }
        }

        protected override ValueTask<CompilePlayerResponse> ExecuteAsync(CompilePlayerRequest request, CancellationToken cancellationToken)
        {
            using var scope = _guard.BeginScope(CommandName, GuardCondition.NotPlayingOrCompiling);

            var target = ResolveTarget(request.target);
            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);

            var settings = new ScriptCompilationSettings
            {
                target = target,
                group = targetGroup,
                extraScriptingDefines = request.extraScriptingDefines
            };

            var tempDir = Path.Combine(Path.GetTempPath(), $"unicli-compile-player-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);

            var errors = new List<CompileIssue>();
            var warnings = new List<CompileIssue>();

            void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
            {
                foreach (var msg in messages)
                {
                    var issue = new CompileIssue
                    {
                        message = msg.message,
                        file = msg.file ?? "",
                        line = msg.line
                    };

                    if (msg.type == CompilerMessageType.Error)
                        errors.Add(issue);
                    else if (msg.type == CompilerMessageType.Warning)
                        warnings.Add(issue);
                }
            }

            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            ScriptCompilationResult result;
            try
            {
                result = PlayerBuildInterface.CompilePlayerScripts(settings, tempDir);
            }
            catch (Exception ex)
            {
                errors.Add(new CompileIssue { message = ex.Message, file = "", line = 0 });
                result = default;
            }
            finally
            {
                CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;

                try { Directory.Delete(tempDir, true); }
                catch { /* best-effort cleanup */ }
            }

            var assemblies = result.assemblies != null
                ? new List<string>(result.assemblies).ToArray()
                : Array.Empty<string>();
            var response = new CompilePlayerResponse
            {
                target = target.ToString(),
                targetGroup = targetGroup.ToString(),
                assemblyCount = assemblies.Length,
                assemblies = assemblies,
                errorCount = errors.Count,
                warningCount = warnings.Count,
                errors = errors.ToArray(),
                warnings = warnings.ToArray()
            };

            if (errors.Count > 0)
                throw new CommandFailedException($"Player compilation failed for {target} with {errors.Count} error(s)", response);

            return new ValueTask<CompilePlayerResponse>(response);
        }

        private static BuildTarget ResolveTarget(string target)
        {
            if (string.IsNullOrEmpty(target))
                return EditorUserBuildSettings.activeBuildTarget;

            if (Enum.TryParse<BuildTarget>(target, true, out var parsed))
                return parsed;

            throw new ArgumentException($"Invalid build target: '{target}'. Use a valid BuildTarget name (e.g. Android, iOS, StandaloneWindows64).");
        }
    }

    [Serializable]
    public class CompilePlayerRequest
    {
        public string target = "";
        public string[] extraScriptingDefines;
    }

    [Serializable]
    public class CompilePlayerResponse
    {
        public string target;
        public string targetGroup;
        public int assemblyCount;
        public string[] assemblies;
        public int errorCount;
        public int warningCount;
        public CompileIssue[] errors;
        public CompileIssue[] warnings;
    }
}
