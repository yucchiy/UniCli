using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Assets")]
    public sealed class PrefabUnpackHandler : CommandHandler<PrefabUnpackRequest, PrefabUnpackResponse>
    {
        public override string CommandName => "Prefab.Unpack";
        public override string Description => "Unpack a prefab instance via PrefabUtility, disconnecting it from the source prefab";

        protected override bool TryWriteFormatted(PrefabUnpackResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Unpacked {response.gameObjectName} (mode={response.unpackMode})");
            else
                writer.WriteLine("Failed to unpack prefab");

            return true;
        }

        protected override ValueTask<PrefabUnpackResponse> ExecuteAsync(PrefabUnpackRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new PrefabUnpackResponse());
            }

            var status = PrefabUtility.GetPrefabInstanceStatus(go);
            if (status == PrefabInstanceStatus.NotAPrefab)
            {
                throw new CommandFailedException(
                    $"'{go.name}' is not a prefab instance",
                    new PrefabUnpackResponse());
            }

            var mode = request.completely
                ? PrefabUnpackMode.Completely
                : PrefabUnpackMode.OutermostRoot;

            PrefabUtility.UnpackPrefabInstance(go, mode, InteractionMode.UserAction);

            return new ValueTask<PrefabUnpackResponse>(new PrefabUnpackResponse
            {
                gameObjectName = go.name,
                unpackMode = request.completely ? "Completely" : "OutermostRoot"
            });
        }
    }

    [Serializable]
    public class PrefabUnpackRequest
    {
        public int instanceId;
        public string path = "";
        public bool completely;
    }

    [Serializable]
    public class PrefabUnpackResponse
    {
        public string gameObjectName;
        public string unpackMode;
    }
}
