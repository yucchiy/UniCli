using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class RenameGameObjectHandler : CommandHandler<RenameGameObjectRequest, RenameGameObjectResponse>
    {
        public override string CommandName => CommandNames.GameObject.Rename;
        public override string Description => "Rename a GameObject";

        protected override bool TryWriteFormatted(RenameGameObjectResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Renamed \"{response.previousName}\" to \"{response.name}\"");
            else
                writer.WriteLine("Failed to rename GameObject");

            return true;
        }

        protected override ValueTask<RenameGameObjectResponse> ExecuteAsync(RenameGameObjectRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new RenameGameObjectResponse());
            }

            var previousName = go.name;
            Undo.RecordObject(go, $"Rename {previousName}");
            go.name = request.name;

            return new ValueTask<RenameGameObjectResponse>(new RenameGameObjectResponse
            {
                previousName = previousName,
                name = go.name,
                instanceId = go.GetInstanceID()
            });
        }
    }

    [Serializable]
    public class RenameGameObjectRequest
    {
        public int instanceId;
        public string path = "";
        public string name = "";
    }

    [Serializable]
    public class RenameGameObjectResponse
    {
        public string previousName;
        public string name;
        public int instanceId;
    }
}
