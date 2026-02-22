using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Assets")]
    public sealed class PrefabApplyHandler : CommandHandler<PrefabApplyRequest, PrefabApplyResponse>
    {
        public override string CommandName => "Prefab.Apply";
        public override string Description => "Apply overrides of a prefab instance to the source prefab asset via PrefabUtility";

        protected override bool TryWriteFormatted(PrefabApplyResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Applied overrides of {response.gameObjectName} to {response.assetPath}");
            else
                writer.WriteLine("Failed to apply prefab overrides");

            return true;
        }

        protected override ValueTask<PrefabApplyResponse> ExecuteAsync(PrefabApplyRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.ResolveByIdOrPath(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new PrefabApplyResponse());
            }

            var status = PrefabUtility.GetPrefabInstanceStatus(go);
            if (status == PrefabInstanceStatus.NotAPrefab)
            {
                throw new CommandFailedException(
                    $"'{go.name}' is not a prefab instance",
                    new PrefabApplyResponse());
            }

            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);

            PrefabUtility.ApplyPrefabInstance(go, InteractionMode.UserAction);

            return new ValueTask<PrefabApplyResponse>(new PrefabApplyResponse
            {
                gameObjectName = go.name,
                assetPath = assetPath
            });
        }
    }

    [Serializable]
    public class PrefabApplyRequest
    {
        public int instanceId;
        public string path = "";
    }

    [Serializable]
    public class PrefabApplyResponse
    {
        public string gameObjectName;
        public string assetPath;
    }
}
