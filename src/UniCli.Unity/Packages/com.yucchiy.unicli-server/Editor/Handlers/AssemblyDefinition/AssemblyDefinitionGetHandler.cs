using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AssemblyDefinitionGetHandler : CommandHandler<AssemblyDefinitionGetRequest, AssemblyDefinitionGetResponse>
    {
        public override string CommandName => CommandNames.AssemblyDefinition.Get;
        public override string Description => "Get detailed information about a specific assembly definition";

        protected override bool TryWriteFormatted(AssemblyDefinitionGetResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine($"Name:             {response.name}");
            writer.WriteLine($"Path:             {response.path}");
            writer.WriteLine($"Root Namespace:   {response.rootNamespace}");
            writer.WriteLine($"Allow Unsafe:     {response.allowUnsafeCode}");
            writer.WriteLine($"Auto Referenced:  {response.autoReferenced}");
            writer.WriteLine($"No Engine Refs:   {response.noEngineReferences}");

            if (response.references.Length > 0)
            {
                writer.WriteLine($"References ({response.references.Length}):");
                foreach (var r in response.references)
                    writer.WriteLine($"  - {r}");
            }

            if (response.includePlatforms.Length > 0)
            {
                writer.WriteLine($"Include Platforms ({response.includePlatforms.Length}):");
                foreach (var p in response.includePlatforms)
                    writer.WriteLine($"  - {p}");
            }

            if (response.excludePlatforms.Length > 0)
            {
                writer.WriteLine($"Exclude Platforms ({response.excludePlatforms.Length}):");
                foreach (var p in response.excludePlatforms)
                    writer.WriteLine($"  - {p}");
            }

            if (response.defineConstraints.Length > 0)
            {
                writer.WriteLine($"Define Constraints ({response.defineConstraints.Length}):");
                foreach (var d in response.defineConstraints)
                    writer.WriteLine($"  - {d}");
            }

            if (response.defines.Length > 0)
            {
                writer.WriteLine($"Defines ({response.defines.Length}):");
                foreach (var d in response.defines)
                    writer.WriteLine($"  - {d}");
            }

            writer.WriteLine($"Source Files:     {response.sourceFiles.Length}");

            return true;
        }

        protected override ValueTask<AssemblyDefinitionGetResponse> ExecuteAsync(AssemblyDefinitionGetRequest request)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("Assembly definition name is required");

            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(request.name);
            if (string.IsNullOrEmpty(asmdefPath))
                throw new ArgumentException($"Assembly definition not found: {request.name}");

            var json = File.ReadAllText(asmdefPath);
            var data = JsonUtility.FromJson<AssemblyDefinitionData>(json);

            var references = ResolveReferenceNames(data.references ?? Array.Empty<string>());

            var sourceFiles = Array.Empty<string>();
            var defines = Array.Empty<string>();

            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);
            var targetAssembly = assemblies.FirstOrDefault(a => a.name == request.name);
            if (targetAssembly != null)
            {
                sourceFiles = targetAssembly.sourceFiles ?? Array.Empty<string>();
                defines = targetAssembly.defines ?? Array.Empty<string>();
            }

            return new ValueTask<AssemblyDefinitionGetResponse>(new AssemblyDefinitionGetResponse
            {
                name = data.name,
                path = asmdefPath,
                rootNamespace = data.rootNamespace ?? "",
                references = references,
                includePlatforms = data.includePlatforms ?? Array.Empty<string>(),
                excludePlatforms = data.excludePlatforms ?? Array.Empty<string>(),
                allowUnsafeCode = data.allowUnsafeCode,
                autoReferenced = data.autoReferenced,
                defineConstraints = data.defineConstraints ?? Array.Empty<string>(),
                noEngineReferences = data.noEngineReferences,
                sourceFiles = sourceFiles,
                defines = defines
            });
        }

        private static string[] ResolveReferenceNames(string[] references)
        {
            var resolved = new string[references.Length];
            for (var i = 0; i < references.Length; i++)
            {
                var r = references[i];
                if (r.StartsWith("GUID:"))
                {
                    var guid = r.Substring(5);
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            var refJson = File.ReadAllText(path);
                            var refData = JsonUtility.FromJson<AssemblyDefinitionData>(refJson);
                            if (refData != null && !string.IsNullOrEmpty(refData.name))
                            {
                                resolved[i] = refData.name;
                                continue;
                            }
                        }
                        catch
                        {
                            // Fall through to raw value
                        }
                    }
                }

                resolved[i] = r;
            }

            return resolved;
        }
    }

    [Serializable]
    public class AssemblyDefinitionGetRequest
    {
        public string name;
    }

    [Serializable]
    public class AssemblyDefinitionGetResponse
    {
        public string name;
        public string path;
        public string rootNamespace;
        public string[] references;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool autoReferenced;
        public string[] defineConstraints;
        public bool noEngineReferences;
        public string[] sourceFiles;
        public string[] defines;
    }
}
