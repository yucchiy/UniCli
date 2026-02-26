using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEditor.Compilation;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class CompileHandler : CommandHandler<Unit, CompileResponse>
    {
        private readonly EditorStateGuard _guard;

        public CompileHandler(EditorStateGuard guard)
        {
            _guard = guard;
        }

        public override string CommandName => "Compile";
        public override string Description => "Trigger script compilation and return results with error details";

        protected override bool TryWriteFormatted(CompileResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Compilation succeeded ({response.errorCount} errors, {response.warningCount} warnings)");
            else
                writer.WriteLine($"Compilation failed ({response.errorCount} errors, {response.warningCount} warnings)");

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

        protected override async ValueTask<CompileResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            using var scope = _guard.BeginScope(CommandName, GuardCondition.NotPlaying);

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

                await tcs.Task.WithCancellation(cancellationToken);
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
