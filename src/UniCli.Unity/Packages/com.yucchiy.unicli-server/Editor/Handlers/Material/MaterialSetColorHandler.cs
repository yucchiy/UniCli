using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Assets")]
    public sealed class MaterialSetColorHandler : CommandHandler<MaterialSetColorRequest, MaterialSetColorResponse>
    {
        public override string CommandName => "Material.SetColor";
        public override string Description => "Set a color property on a material (Material.SetColor)";

        protected override ValueTask<MaterialSetColorResponse> ExecuteAsync(MaterialSetColorRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.guid))
                throw new ArgumentException("guid is required");
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name (shader property name) is required");

            var assetPath = AssetDatabase.GUIDToAssetPath(request.guid);
            if (string.IsNullOrEmpty(assetPath))
                throw new CommandFailedException($"Asset not found for GUID: {request.guid}", new MaterialSetColorResponse());

            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
                throw new CommandFailedException($"Failed to load material at: {assetPath}", new MaterialSetColorResponse());

            if (!material.HasProperty(request.name))
                throw new CommandFailedException($"Material does not have property: {request.name}", new MaterialSetColorResponse());

            var color = new Color(request.value.r, request.value.g, request.value.b, request.value.a);
            material.SetColor(request.name, color);
            EditorUtility.SetDirty(material);

            return new ValueTask<MaterialSetColorResponse>(new MaterialSetColorResponse
            {
                guid = request.guid,
                name = request.name,
                value = request.value
            });
        }
    }

    [Serializable]
    public class MaterialSetColorRequest
    {
        public string guid;
        public string name;
        public ColorValue value;
    }

    [Serializable]
    public class MaterialSetColorResponse
    {
        public string guid;
        public string name;
        public ColorValue value;
    }

    [Serializable]
    public class ColorValue
    {
        public float r;
        public float g;
        public float b;
        public float a = 1f;
    }
}
