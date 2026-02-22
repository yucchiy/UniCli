using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SetGameObjectSelectionHandler : CommandHandler<SetGameObjectSelectionRequest, SetGameObjectSelectionResponse>
    {
        public override string CommandName => "Selection.SetGameObject";
        public override string Description => "Select a GameObject by path";

        protected override bool TryWriteFormatted(SetGameObjectSelectionResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Selected: {response.path} (instanceId={response.instanceId})");
            return true;
        }

        protected override ValueTask<SetGameObjectSelectionResponse> ExecuteAsync(SetGameObjectSelectionRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.path))
            {
                throw new CommandFailedException(
                    "path is required",
                    new SetGameObjectSelectionResponse());
            }

            var go = GameObjectResolver.Resolve(0, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found: \"{request.path}\"",
                    new SetGameObjectSelectionResponse());
            }

            Selection.activeGameObject = go;

            return new ValueTask<SetGameObjectSelectionResponse>(new SetGameObjectSelectionResponse
            {
                path = GameObjectResolver.BuildPath(go.transform),
                instanceId = go.GetInstanceID()
            });
        }
    }

    [Serializable]
    public class SetGameObjectSelectionRequest
    {
        public string path;
    }

    [Serializable]
    public class SetGameObjectSelectionResponse
    {
        public string path;
        public int instanceId;
    }
}
