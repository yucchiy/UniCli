using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Assets")]
    public sealed class PrefabInstantiateHandler : CommandHandler<PrefabInstantiateRequest, PrefabInstantiateResponse>
    {
        public override string CommandName => "Prefab.Instantiate";
        public override string Description => "Instantiate a prefab asset into the scene via PrefabUtility";

        protected override bool TryWriteFormatted(PrefabInstantiateResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Instantiated {response.name} (instanceId={response.instanceId}) from {response.assetPath}");
            else
                writer.WriteLine("Failed to instantiate prefab");

            return true;
        }

        protected override ValueTask<PrefabInstantiateResponse> ExecuteAsync(PrefabInstantiateRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.assetPath))
                throw new ArgumentException("assetPath is required");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(request.assetPath);
            if (prefab == null)
            {
                throw new CommandFailedException(
                    $"Prefab not found at \"{request.assetPath}\"",
                    new PrefabInstantiateResponse());
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new CommandFailedException(
                    $"Failed to instantiate prefab from \"{request.assetPath}\"",
                    new PrefabInstantiateResponse());
            }

            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");

            var parent = GameObjectResolver.Resolve(request.parentInstanceId, request.parentPath);
            if (parent != null)
                instance.transform.SetParent(parent.transform, true);

            return new ValueTask<PrefabInstantiateResponse>(new PrefabInstantiateResponse
            {
                instanceId = instance.GetInstanceID(),
                name = instance.name,
                assetPath = request.assetPath
            });
        }
    }

    [Serializable]
    public class PrefabInstantiateRequest
    {
        public string assetPath;
        public int parentInstanceId;
        public string parentPath = "";
    }

    [Serializable]
    public class PrefabInstantiateResponse
    {
        public int instanceId;
        public string name;
        public string assetPath;
    }
}
