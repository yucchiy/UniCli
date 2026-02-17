using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AssemblyDefinitionCreateHandler : CommandHandler<AssemblyDefinitionCreateRequest, AssemblyDefinitionCreateResponse>
    {
        public override string CommandName => CommandNames.AssemblyDefinition.Create;
        public override string Description => "Create a new assembly definition file";

        protected override bool TryWriteFormatted(AssemblyDefinitionCreateResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine($"Created {response.path}");
            return true;
        }

        protected override ValueTask<AssemblyDefinitionCreateResponse> ExecuteAsync(AssemblyDefinitionCreateRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("Assembly definition name is required");

            if (string.IsNullOrEmpty(request.directory))
                throw new ArgumentException("Directory is required");

            var references = ConvertToGuidReferences(request.references ?? Array.Empty<string>());

            var data = new AssemblyDefinitionData
            {
                name = request.name,
                rootNamespace = request.rootNamespace ?? "",
                references = references,
                includePlatforms = request.includePlatforms ?? Array.Empty<string>(),
                excludePlatforms = request.excludePlatforms ?? Array.Empty<string>(),
                allowUnsafeCode = request.allowUnsafeCode,
                autoReferenced = request.autoReferenced,
                defineConstraints = request.defineConstraints ?? Array.Empty<string>(),
                noEngineReferences = request.noEngineReferences
            };

            Directory.CreateDirectory(request.directory);

            var filePath = Path.Combine(request.directory, $"{request.name}.asmdef");
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);

            AssetDatabase.ImportAsset(filePath);

            return new ValueTask<AssemblyDefinitionCreateResponse>(new AssemblyDefinitionCreateResponse
            {
                name = request.name,
                path = filePath
            });
        }

        private static string[] ConvertToGuidReferences(string[] assemblyNames)
        {
            var result = new List<string>(assemblyNames.Length);
            foreach (var name in assemblyNames)
            {
                if (name.StartsWith("GUID:"))
                {
                    result.Add(name);
                    continue;
                }

                var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(name);
                if (string.IsNullOrEmpty(asmdefPath))
                    throw new ArgumentException($"Referenced assembly definition not found: {name}");

                var guid = AssetDatabase.AssetPathToGUID(asmdefPath);
                if (string.IsNullOrEmpty(guid))
                    throw new ArgumentException($"Could not resolve GUID for assembly: {name}");

                result.Add($"GUID:{guid}");
            }

            return result.ToArray();
        }
    }

    [Serializable]
    public class AssemblyDefinitionCreateRequest
    {
        public string name;
        public string directory;
        public string rootNamespace = "";
        public string[] references;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool autoReferenced = true;
        public string[] defineConstraints;
        public bool noEngineReferences;
    }

    [Serializable]
    public class AssemblyDefinitionCreateResponse
    {
        public string name;
        public string path;
    }
}
