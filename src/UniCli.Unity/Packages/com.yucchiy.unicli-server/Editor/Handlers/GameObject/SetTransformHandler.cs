using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SetTransformHandler : CommandHandler<SetTransformRequest, SetTransformResponse>
    {
        public override string CommandName => CommandNames.GameObject.SetTransform;
        public override string Description => "Set the local transform (position, rotation, scale) of a GameObject";

        protected override bool TryWriteFormatted(SetTransformResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Updated transform of \"{response.name}\"");
            else
                writer.WriteLine("Failed to set transform");

            return true;
        }

        protected override ValueTask<SetTransformResponse> ExecuteAsync(SetTransformRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new SetTransformResponse());
            }

            var transform = go.transform;
            Undo.RecordObject(transform, $"Set Transform {go.name}");

            if (request.position != null && request.position.Length == 3)
                transform.localPosition = new Vector3(request.position[0], request.position[1], request.position[2]);

            if (request.rotation != null && request.rotation.Length == 3)
                transform.localEulerAngles = new Vector3(request.rotation[0], request.rotation[1], request.rotation[2]);

            if (request.localScale != null && request.localScale.Length == 3)
                transform.localScale = new Vector3(request.localScale[0], request.localScale[1], request.localScale[2]);

            return new ValueTask<SetTransformResponse>(new SetTransformResponse
            {
                instanceId = go.GetInstanceID(),
                name = go.name,
                position = new[] { transform.localPosition.x, transform.localPosition.y, transform.localPosition.z },
                rotation = new[] { transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z },
                localScale = new[] { transform.localScale.x, transform.localScale.y, transform.localScale.z }
            });
        }
    }

    [Serializable]
    public class SetTransformRequest
    {
        public int instanceId;
        public string path = "";
        public float[] position;
        public float[] rotation;
        public float[] localScale;
    }

    [Serializable]
    public class SetTransformResponse
    {
        public int instanceId;
        public string name;
        public float[] position;
        public float[] rotation;
        public float[] localScale;
    }
}
