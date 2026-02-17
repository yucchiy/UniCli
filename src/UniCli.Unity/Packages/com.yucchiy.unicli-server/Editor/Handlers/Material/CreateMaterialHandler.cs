using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class CreateMaterialHandler : CommandHandler<CreateMaterialRequest, CreateMaterialResponse>
    {
        public override string CommandName => CommandNames.Material.Create;
        public override string Description => "Create a new material asset";

        protected override bool TryWriteFormatted(CreateMaterialResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Created material at {response.assetPath} (shader: {response.shaderName}, guid: {response.guid})");
            else
                writer.WriteLine("Failed to create material");

            return true;
        }

        protected override ValueTask<CreateMaterialResponse> ExecuteAsync(CreateMaterialRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.assetPath))
                throw new ArgumentException("assetPath is required");

            if (!request.assetPath.EndsWith(".mat"))
            {
                throw new CommandFailedException(
                    $"assetPath must end with .mat (got \"{request.assetPath}\")",
                    new CreateMaterialResponse());
            }

            var shader = ResolveShader(request.shader);
            if (shader == null)
            {
                throw new CommandFailedException(
                    string.IsNullOrEmpty(request.shader)
                        ? "No default shader found"
                        : $"Shader not found: \"{request.shader}\"",
                    new CreateMaterialResponse());
            }

            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, request.assetPath);
            AssetDatabase.SaveAssets();

            var guid = AssetDatabase.AssetPathToGUID(request.assetPath);

            return new ValueTask<CreateMaterialResponse>(new CreateMaterialResponse
            {
                assetPath = request.assetPath,
                guid = guid,
                shaderName = shader.name
            });
        }

        static Shader ResolveShader(string shaderName)
        {
            if (!string.IsNullOrEmpty(shaderName))
                return Shader.Find(shaderName);

            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null && pipeline.defaultShader != null)
                return pipeline.defaultShader;

            return Shader.Find("Standard");
        }
    }

    [Serializable]
    public class CreateMaterialRequest
    {
        public string assetPath;
        public string shader = "";
    }

    [Serializable]
    public class CreateMaterialResponse
    {
        public string assetPath;
        public string guid;
        public string shaderName;
    }
}
