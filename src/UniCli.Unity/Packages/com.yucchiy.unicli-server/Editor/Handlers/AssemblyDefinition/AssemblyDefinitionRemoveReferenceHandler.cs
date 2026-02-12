using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniCli.Server.Editor;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AssemblyDefinitionRemoveReferenceHandler : CommandHandler<AssemblyDefinitionRemoveReferenceRequest, AssemblyDefinitionRemoveReferenceResponse>
    {
        public override string CommandName => CommandNames.AssemblyDefinition.RemoveReference;
        public override string Description => "Remove an assembly reference from an existing assembly definition";

        protected override bool TryWriteFormatted(AssemblyDefinitionRemoveReferenceResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine($"Removed reference '{response.removedReference}' from {response.name} ({response.references.Length} references total)");
            return true;
        }

        protected override ValueTask<AssemblyDefinitionRemoveReferenceResponse> ExecuteAsync(AssemblyDefinitionRemoveReferenceRequest request)
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

            var existingReferences = data.references ?? Array.Empty<string>();

            var guidReference = TryConvertToGuidReference(request.reference);
            var indexToRemove = FindReferenceIndex(existingReferences, request.reference, guidReference);

            if (indexToRemove < 0)
                throw new ArgumentException($"Reference '{request.reference}' not found in {request.name}");

            var newReferences = new List<string>(existingReferences);
            newReferences.RemoveAt(indexToRemove);
            data.references = newReferences.ToArray();

            var updatedJson = JsonUtility.ToJson(data, true);
            File.WriteAllText(asmdefPath, updatedJson);

            AssetDatabase.ImportAsset(asmdefPath);

            var resolvedReferences = ResolveReferenceNames(data.references);

            return new ValueTask<AssemblyDefinitionRemoveReferenceResponse>(new AssemblyDefinitionRemoveReferenceResponse
            {
                name = request.name,
                path = asmdefPath,
                removedReference = request.reference,
                references = resolvedReferences
            });
        }

        private static int FindReferenceIndex(string[] references, string name, string guidReference)
        {
            for (var i = 0; i < references.Length; i++)
            {
                if (references[i] == name)
                    return i;

                if (guidReference != null && references[i] == guidReference)
                    return i;
            }

            return -1;
        }

        private static string TryConvertToGuidReference(string assemblyName)
        {
            if (assemblyName.StartsWith("GUID:"))
                return assemblyName;

            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
            if (string.IsNullOrEmpty(asmdefPath))
                return null;

            var guid = AssetDatabase.AssetPathToGUID(asmdefPath);
            if (string.IsNullOrEmpty(guid))
                return null;

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
    public class AssemblyDefinitionRemoveReferenceRequest
    {
        public string name;
        public string reference;
    }

    [Serializable]
    public class AssemblyDefinitionRemoveReferenceResponse
    {
        public string name;
        public string path;
        public string removedReference;
        public string[] references;
    }
}
