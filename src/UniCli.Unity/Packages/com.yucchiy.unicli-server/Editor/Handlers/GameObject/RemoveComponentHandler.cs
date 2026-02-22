using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Scene")]
    public sealed class RemoveComponentHandler : CommandHandler<RemoveComponentRequest, RemoveComponentResponse>
    {
        public override string CommandName => "GameObject.RemoveComponent";
        public override string Description => "Remove a component from a GameObject by instance ID";

        protected override bool TryWriteFormatted(RemoveComponentResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Removed {response.typeName} from {response.gameObjectName}");
            else
                writer.WriteLine("Failed to remove component");

            return true;
        }

        protected override ValueTask<RemoveComponentResponse> ExecuteAsync(RemoveComponentRequest request, CancellationToken cancellationToken)
        {
            if (request.componentInstanceId == 0)
                throw new ArgumentException("componentInstanceId is required");

            var obj = EditorUtility.InstanceIDToObject(request.componentInstanceId);
            if (obj is not Component component)
            {
                throw new CommandFailedException(
                    $"Component not found for instanceId={request.componentInstanceId}",
                    new RemoveComponentResponse());
            }

            if (component is Transform)
            {
                throw new CommandFailedException(
                    "Cannot remove Transform component",
                    new RemoveComponentResponse());
            }

            var goName = component.gameObject.name;
            var typeName = component.GetType().FullName;
            var instanceId = component.GetInstanceID();

            Undo.DestroyObjectImmediate(component);

            return new ValueTask<RemoveComponentResponse>(new RemoveComponentResponse
            {
                gameObjectName = goName,
                typeName = typeName,
                componentInstanceId = instanceId
            });
        }
    }

    [Serializable]
    public class RemoveComponentRequest
    {
        public int componentInstanceId;
    }

    [Serializable]
    public class RemoveComponentResponse
    {
        public string gameObjectName;
        public string typeName;
        public int componentInstanceId;
    }
}
