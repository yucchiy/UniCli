using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Assets")]
    public sealed class MaterialSetFloatHandler : CommandHandler<MaterialSetFloatRequest, MaterialSetFloatResponse>
    {
        public override string CommandName => "Material.SetFloat";
        public override string Description => "Set a float property on a material (Material.SetFloat)";

        protected override ValueTask<MaterialSetFloatResponse> ExecuteAsync(MaterialSetFloatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.guid))
                throw new ArgumentException("guid is required");
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name (shader property name) is required");

            var assetPath = AssetDatabase.GUIDToAssetPath(request.guid);
            if (string.IsNullOrEmpty(assetPath))
                throw new CommandFailedException($"Asset not found for GUID: {request.guid}", new MaterialSetFloatResponse());

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
                throw new CommandFailedException($"Failed to load material at: {assetPath}", new MaterialSetFloatResponse());

            if (!material.HasProperty(request.name))
                throw new CommandFailedException($"Material does not have property: {request.name}", new MaterialSetFloatResponse());

            material.SetFloat(request.name, request.value);
            EditorUtility.SetDirty(material);

            return new ValueTask<MaterialSetFloatResponse>(new MaterialSetFloatResponse
            {
                guid = request.guid,
                name = request.name,
                value = request.value
            });
        }
    }

    [Serializable]
    public class MaterialSetFloatRequest
    {
        public string guid;
        public string name;
        public float value;
    }

    [Serializable]
    public class MaterialSetFloatResponse
    {
        public string guid;
        public string name;
        public float value;
    }
}
