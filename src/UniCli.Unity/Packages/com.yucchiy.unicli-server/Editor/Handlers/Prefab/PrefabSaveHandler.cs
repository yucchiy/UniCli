using System;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PrefabSaveHandler : CommandHandler<PrefabSaveRequest, PrefabSaveResponse>
    {
        public override string CommandName => CommandNames.Prefab.Save;
        public override string Description => "Save a GameObject as a prefab asset";

        protected override bool TryWriteFormatted(PrefabSaveResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Saved {response.gameObjectName} as prefab at {response.assetPath}");
            else
                writer.WriteLine("Failed to save prefab");

            return true;
        }

        protected override ValueTask<PrefabSaveResponse> ExecuteAsync(PrefabSaveRequest request)
        {
            if (string.IsNullOrEmpty(request.assetPath))
                throw new ArgumentException("assetPath is required");

            if (!request.assetPath.EndsWith(".prefab"))
            {
                throw new CommandFailedException(
                    $"assetPath must end with .prefab (got \"{request.assetPath}\")",
                    new PrefabSaveResponse());
            }

            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new PrefabSaveResponse());
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, request.assetPath, out var success);
            if (!success || prefab == null)
            {
                throw new CommandFailedException(
                    $"Failed to save '{go.name}' as prefab at \"{request.assetPath}\"",
                    new PrefabSaveResponse());
            }

            return new ValueTask<PrefabSaveResponse>(new PrefabSaveResponse
            {
                gameObjectName = go.name,
                assetPath = request.assetPath
            });
        }
    }

    [Serializable]
    public class PrefabSaveRequest
    {
        public int instanceId;
        public string path = "";
        public string assetPath;
    }

    [Serializable]
    public class PrefabSaveResponse
    {
        public string gameObjectName;
        public string assetPath;
    }
}
