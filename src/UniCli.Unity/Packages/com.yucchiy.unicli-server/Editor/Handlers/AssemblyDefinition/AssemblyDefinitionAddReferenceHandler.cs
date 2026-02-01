using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AssemblyDefinitionAddReferenceHandler : CommandHandler<AssemblyDefinitionAddReferenceRequest, AssemblyDefinitionAddReferenceResponse>
    {
        public override string CommandName => CommandNames.AssemblyDefinition.AddReference;
        public override string Description => "Add an assembly reference to an existing assembly definition";

        protected override bool TryFormat(AssemblyDefinitionAddReferenceResponse response, bool success, out string formatted)
        {
            formatted = $"Added reference '{response.addedReference}' to {response.name} ({response.references.Length} references total)";
            return true;
        }

        protected override ValueTask<AssemblyDefinitionAddReferenceResponse> ExecuteAsync(AssemblyDefinitionAddReferenceRequest request)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("Assembly definition name is required");

            if (string.IsNullOrEmpty(request.reference))
                throw new ArgumentException("Reference assembly name is required");

            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(request.name);
            if (string.IsNullOrEmpty(asmdefPath))
                throw new ArgumentException($"Assembly definition not found: {request.name}");

            var json = File.ReadAllText(asmdefPath);
            var data = JsonUtility.FromJson<AssemblyDefinitionData>(json);

            var guidReference = ConvertToGuidReference(request.reference);

            var existingReferences = data.references ?? Array.Empty<string>();
            if (existingReferences.Contains(guidReference))
                throw new ArgumentException($"Reference '{request.reference}' already exists in {request.name}");

            // Also check by name in case existing refs use name format
            if (existingReferences.Contains(request.reference))
                throw new ArgumentException($"Reference '{request.reference}' already exists in {request.name}");

            var newReferences = new string[existingReferences.Length + 1];
            Array.Copy(existingReferences, newReferences, existingReferences.Length);
            newReferences[existingReferences.Length] = guidReference;
            data.references = newReferences;

            var updatedJson = JsonUtility.ToJson(data, true);
            File.WriteAllText(asmdefPath, updatedJson);

            AssetDatabase.ImportAsset(asmdefPath);

            var resolvedReferences = ResolveReferenceNames(newReferences);

            return new ValueTask<AssemblyDefinitionAddReferenceResponse>(new AssemblyDefinitionAddReferenceResponse
            {
                name = request.name,
                path = asmdefPath,
                addedReference = request.reference,
                references = resolvedReferences
            });
        }

        private static string ConvertToGuidReference(string assemblyName)
        {
            if (assemblyName.StartsWith("GUID:"))
                return assemblyName;

            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
            if (string.IsNullOrEmpty(asmdefPath))
                throw new ArgumentException($"Referenced assembly definition not found: {assemblyName}");

            var guid = AssetDatabase.AssetPathToGUID(asmdefPath);
            if (string.IsNullOrEmpty(guid))
                throw new ArgumentException($"Could not resolve GUID for assembly: {assemblyName}");

            return $"GUID:{guid}";
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
    public class AssemblyDefinitionAddReferenceRequest
    {
        public string name;
        public string reference;
    }

    [Serializable]
    public class AssemblyDefinitionAddReferenceResponse
    {
        public string name;
        public string path;
        public string addedReference;
        public string[] references;
    }
}
