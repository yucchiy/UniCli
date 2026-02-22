using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Assets")]
    public sealed class MaterialGetFloatHandler : CommandHandler<MaterialGetFloatRequest, MaterialGetFloatResponse>
    {
        public override string CommandName => "Material.GetFloat";
        public override string Description => "Get a float property from a material (Material.GetFloat)";

        protected override ValueTask<MaterialGetFloatResponse> ExecuteAsync(MaterialGetFloatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.guid))
                throw new ArgumentException("guid is required");
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name (shader property name) is required");

            var assetPath = AssetDatabase.GUIDToAssetPath(request.guid);
            if (string.IsNullOrEmpty(assetPath))
                throw new CommandFailedException($"Asset not found for GUID: {request.guid}", new MaterialGetFloatResponse());

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
                throw new CommandFailedException($"Failed to load material at: {assetPath}", new MaterialGetFloatResponse());

            if (!material.HasProperty(request.name))
                throw new CommandFailedException($"Material does not have property: {request.name}", new MaterialGetFloatResponse());

            var value = material.GetFloat(request.name);

            return new ValueTask<MaterialGetFloatResponse>(new MaterialGetFloatResponse
            {
                guid = request.guid,
                name = request.name,
                value = value
            });
        }
    }

    [Serializable]
    public class MaterialGetFloatRequest
    {
        public string guid;
        public string name;
    }

    [Serializable]
    public class MaterialGetFloatResponse
    {
        public string guid;
        public string name;
        public float value;
    }
}
