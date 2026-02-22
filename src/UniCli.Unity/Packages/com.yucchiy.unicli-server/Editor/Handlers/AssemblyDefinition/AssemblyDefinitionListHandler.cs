using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AssemblyDefinitionListHandler : CommandHandler<Unit, AssemblyDefinitionListResponse>
    {
        public override string CommandName => "AssemblyDefinition.List";
        public override string Description => "List all assembly definitions in the project";

        protected override bool TryWriteFormatted(AssemblyDefinitionListResponse response, bool success, IFormatWriter writer)
        {
            var nameWidth = "Name".Length;
            var pathWidth = "Path".Length;
            var nsWidth = "Namespace".Length;
            var srcWidth = "Sources".Length;
            var refWidth = "Refs".Length;

            foreach (var entry in response.assemblies)
            {
                nameWidth = Math.Max(nameWidth, entry.name.Length);
                pathWidth = Math.Max(pathWidth, entry.path.Length);
                nsWidth = Math.Max(nsWidth, entry.rootNamespace.Length);
                srcWidth = Math.Max(srcWidth, entry.sourceFileCount.ToString().Length);
                refWidth = Math.Max(refWidth, entry.referenceCount.ToString().Length);
            }

            writer.WriteLine(
                $"{"Name".PadRight(nameWidth)}  {"Path".PadRight(pathWidth)}  {"Namespace".PadRight(nsWidth)}  {"Sources".PadRight(srcWidth)}  {"Refs".PadRight(refWidth)}");

            foreach (var entry in response.assemblies)
            {
                writer.WriteLine(
                    $"{entry.name.PadRight(nameWidth)}  {entry.path.PadRight(pathWidth)}  {entry.rootNamespace.PadRight(nsWidth)}  {entry.sourceFileCount.ToString().PadRight(srcWidth)}  {entry.referenceCount.ToString().PadRight(refWidth)}");
            }

            writer.WriteLine($"{response.totalCount} assembly definition(s)");

            return true;
        }

        protected override ValueTask<AssemblyDefinitionListResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            var entries = new List<AssemblyDefinitionEntry>();

            foreach (var assembly in assemblies)
            {
                var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.name);
                if (string.IsNullOrEmpty(asmdefPath))
                    continue;

                var rootNamespace = "";
                var referenceCount = 0;

                try
                {
                    var json = File.ReadAllText(asmdefPath);
                    var data = JsonUtility.FromJson<AssemblyDefinitionData>(json);
                    if (data != null)
                    {
                        rootNamespace = data.rootNamespace ?? "";
                        referenceCount = data.references?.Length ?? 0;
                    }
                }
                catch
                {
                    // JSON parse failure — use defaults
                }

                entries.Add(new AssemblyDefinitionEntry
                {
                    name = assembly.name,
                    path = asmdefPath,
                    rootNamespace = rootNamespace,
                    sourceFileCount = assembly.sourceFiles?.Length ?? 0,
                    referenceCount = referenceCount
                });
            }

            var sorted = entries.OrderBy(e => e.name).ToArray();

            return new ValueTask<AssemblyDefinitionListResponse>(new AssemblyDefinitionListResponse
            {
                assemblies = sorted,
                totalCount = sorted.Length
            });
        }
    }

    [Serializable]
    public class AssemblyDefinitionListResponse
    {
        public AssemblyDefinitionEntry[] assemblies;
        public int totalCount;
    }

    [Serializable]
    public class AssemblyDefinitionEntry
    {
        public string name;
        public string path;
        public string rootNamespace;
        public int sourceFileCount;
        public int referenceCount;
    }
}
