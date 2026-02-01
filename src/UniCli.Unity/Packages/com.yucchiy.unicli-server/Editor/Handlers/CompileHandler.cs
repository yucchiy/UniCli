using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEditor.Compilation;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class CompileHandler : CommandHandler<Unit, CompileResponse>
    {
        public override string CommandName => CommandNames.Compile;
        public override string Description => "Trigger script compilation and return results with error details";

        protected override bool TryFormat(CompileResponse response, bool success, out string formatted)
        {
            var sb = new StringBuilder();

            if (success)
                sb.AppendLine($"Compilation succeeded ({response.errorCount} errors, {response.warningCount} warnings)");
            else
                sb.AppendLine($"Compilation failed ({response.errorCount} errors, {response.warningCount} warnings)");

            AppendIssues(sb, response.errors);
            AppendIssues(sb, response.warnings);

            formatted = sb.ToString().TrimEnd();
            return true;
        }

        private static void AppendIssues(StringBuilder sb, CompileIssue[] issues)
        {
            if (issues == null) return;

            foreach (var issue in issues)
            {
                if (!string.IsNullOrEmpty(issue.file))
                    sb.AppendLine($"  {issue.file}({issue.line}): {issue.message}");
                else
                    sb.AppendLine($"  {issue.message}");
            }
        }

        protected override async ValueTask<CompileResponse> ExecuteAsync(Unit request)
        {
            var errors = new List<CompileIssue>();
            var warnings = new List<CompileIssue>();
            var tcs = new TaskCompletionSource<bool>();

            void OnCompilationFinished(object obj)
            {
                CompilationPipeline.compilationFinished -= OnCompilationFinished;
                tcs.SetResult(true);
            }

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

            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            try
            {
                if (!EditorApplication.isCompiling)
                {
                    CompilationPipeline.RequestScriptCompilation();
                }

                await tcs.Task;
            }
            finally
            {
                CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
            }

            var response = new CompileResponse
            {
                errorCount = errors.Count,
                warningCount = warnings.Count,
                errors = errors.ToArray(),
                warnings = warnings.ToArray()
            };

            if (errors.Count > 0)
                throw new CommandFailedException($"Compilation failed with {errors.Count} error(s)", response);

            return response;
        }
    }

    [Serializable]
    public class CompileResponse
    {
        public int errorCount;
        public int warningCount;
        public CompileIssue[] errors;
        public CompileIssue[] warnings;
    }

    [Serializable]
    public class CompileIssue
    {
        public string message;
        public string file;
        public int line;
    }
}
