using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    [Module("GameObject")]
    public sealed class DestroyGameObjectHandler : CommandHandler<DestroyGameObjectRequest, DestroyGameObjectResponse>
    {
        public override string CommandName => "GameObject.Destroy";
        public override string Description => "Destroy a GameObject from the scene";

        protected override bool TryWriteFormatted(DestroyGameObjectResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Destroyed GameObject \"{response.name}\" (instanceId={response.instanceId})");
            else
                writer.WriteLine("Failed to destroy GameObject");

            return true;
        }

        protected override ValueTask<DestroyGameObjectResponse> ExecuteAsync(DestroyGameObjectRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new DestroyGameObjectResponse());
            }

            var name = go.name;
            var instanceId = go.GetInstanceID();

            Undo.DestroyObjectImmediate(go);

            return new ValueTask<DestroyGameObjectResponse>(new DestroyGameObjectResponse
            {
                name = name,
                instanceId = instanceId
            });
        }
    }

    [Serializable]
    public class DestroyGameObjectRequest
    {
        public int instanceId;
        public string path = "";
    }

    [Serializable]
    public class DestroyGameObjectResponse
    {
        public string name;
        public int instanceId;
    }
}
