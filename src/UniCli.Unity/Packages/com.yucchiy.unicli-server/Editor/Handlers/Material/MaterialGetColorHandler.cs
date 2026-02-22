using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Assets")]
    public sealed class MaterialGetColorHandler : CommandHandler<MaterialGetColorRequest, MaterialGetColorResponse>
    {
        public override string CommandName => "Material.GetColor";
        public override string Description => "Get a color property from a material (Material.GetColor)";

        protected override ValueTask<MaterialGetColorResponse> ExecuteAsync(MaterialGetColorRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.guid))
                throw new ArgumentException("guid is required");
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name (shader property name) is required");

            var assetPath = AssetDatabase.GUIDToAssetPath(request.guid);
            if (string.IsNullOrEmpty(assetPath))
                throw new CommandFailedException($"Asset not found for GUID: {request.guid}", new MaterialGetColorResponse());

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
                throw new CommandFailedException($"Failed to load material at: {assetPath}", new MaterialGetColorResponse());

            if (!material.HasProperty(request.name))
                throw new CommandFailedException($"Material does not have property: {request.name}", new MaterialGetColorResponse());

            var color = material.GetColor(request.name);

            return new ValueTask<MaterialGetColorResponse>(new MaterialGetColorResponse
            {
                guid = request.guid,
                name = request.name,
                value = new ColorValue { r = color.r, g = color.g, b = color.b, a = color.a }
            });
        }
    }

    [Serializable]
    public class MaterialGetColorRequest
    {
        public string guid;
        public string name;
    }

    [Serializable]
    public class MaterialGetColorResponse
    {
        public string guid;
        public string name;
        public ColorValue value;
    }
}
