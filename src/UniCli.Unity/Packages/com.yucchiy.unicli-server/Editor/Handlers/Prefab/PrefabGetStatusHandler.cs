using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PrefabGetStatusHandler : CommandHandler<PrefabGetStatusRequest, PrefabGetStatusResponse>
    {
        public override string CommandName => CommandNames.Prefab.GetStatus;
        public override string Description => "Get prefab instance status for a GameObject (PrefabUtility)";

        protected override bool TryWriteFormatted(PrefabGetStatusResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"{response.gameObjectName}: status={response.status}, asset={response.assetPath}");
                if (response.isPrefabInstance)
                    writer.WriteLine($"  hasOverrides={response.hasOverrides}");
            }
            else
            {
                writer.WriteLine("Failed to get prefab status");
            }

            return true;
        }

        protected override ValueTask<PrefabGetStatusResponse> ExecuteAsync(PrefabGetStatusRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new PrefabGetStatusResponse());
            }

            var status = PrefabUtility.GetPrefabInstanceStatus(go);
            var isPrefabInstance = status != PrefabInstanceStatus.NotAPrefab;

            var statusString = status switch
            {
                PrefabInstanceStatus.Connected => "Connected",
                PrefabInstanceStatus.Disconnected => "Disconnected",
                PrefabInstanceStatus.MissingAsset => "MissingAsset",
                _ => "NotAPrefab"
            };

            var assetPath = isPrefabInstance
                ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)
                : "";

            var hasOverrides = isPrefabInstance && PrefabUtility.HasPrefabInstanceAnyOverrides(go, false);

            return new ValueTask<PrefabGetStatusResponse>(new PrefabGetStatusResponse
            {
                gameObjectName = go.name,
                status = statusString,
                assetPath = assetPath,
                hasOverrides = hasOverrides,
                isPrefabInstance = isPrefabInstance
            });
        }
    }

    [Serializable]
    public class PrefabGetStatusRequest
    {
        public int instanceId;
        public string path = "";
    }

    [Serializable]
    public class PrefabGetStatusResponse
    {
        public string gameObjectName;
        public string status;
        public string assetPath;
        public bool hasOverrides;
        public bool isPrefabInstance;
    }
}
