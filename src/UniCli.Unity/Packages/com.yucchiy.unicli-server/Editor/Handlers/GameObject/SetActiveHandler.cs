using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    [Module("GameObject")]
    public sealed class SetActiveHandler : CommandHandler<SetActiveRequest, SetActiveResponse>
    {
        public override string CommandName => "GameObject.SetActive";
        public override string Description => "Set active state of a GameObject";

        protected override ValueTask<SetActiveResponse> ExecuteAsync(SetActiveRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new SetActiveResponse { name = "", previousState = false, currentState = false });
            }

            var previousState = go.activeSelf;

            Undo.RecordObject(go, $"SetActive {go.name}");
            go.SetActive(request.active);

            return new ValueTask<SetActiveResponse>(new SetActiveResponse
            {
                name = go.name,
                previousState = previousState,
                currentState = go.activeSelf
            });
        }
    }

    [Serializable]
    public class SetActiveRequest
    {
        public int instanceId;
        public string path = "";
        public bool active;
    }

    [Serializable]
    public class SetActiveResponse
    {
        public string name;
        public bool previousState;
        public bool currentState;
    }
}
