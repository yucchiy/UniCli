using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SetParentHandler : CommandHandler<SetParentRequest, SetParentResponse>
    {
        public override string CommandName => CommandNames.GameObject.SetParent;
        public override string Description => "Change the parent of a GameObject (or move to root)";

        protected override bool TryWriteFormatted(SetParentResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Moved \"{response.name}\" to \"{response.path}\"");
            else
                writer.WriteLine("Failed to set parent");

            return true;
        }

        protected override ValueTask<SetParentResponse> ExecuteAsync(SetParentRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new SetParentResponse());
            }

            Transform newParent = null;
            if (request.parentInstanceId != 0 || !string.IsNullOrEmpty(request.parentPath))
            {
                var parentGo = GameObjectResolver.Resolve(request.parentInstanceId, request.parentPath);
                if (parentGo == null)
                {
                    throw new CommandFailedException(
                        $"Parent GameObject not found (instanceId={request.parentInstanceId}, path=\"{request.parentPath}\")",
                        new SetParentResponse());
                }

                newParent = parentGo.transform;
            }

            Undo.SetTransformParent(go.transform, newParent, $"Set Parent {go.name}");

            if (!request.worldPositionStays)
                go.transform.SetParent(newParent, false);

            return new ValueTask<SetParentResponse>(new SetParentResponse
            {
                instanceId = go.GetInstanceID(),
                name = go.name,
                path = GameObjectResolver.BuildPath(go.transform)
            });
        }
    }

    [Serializable]
    public class SetParentRequest
    {
        public int instanceId;
        public string path = "";
        public int parentInstanceId;
        public string parentPath = "";
        public bool worldPositionStays = true;
    }

    [Serializable]
    public class SetParentResponse
    {
        public int instanceId;
        public string name;
        public string path;
    }
}
