using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("GameObject")]
    public sealed class CreatePrimitiveHandler : CommandHandler<CreatePrimitiveRequest, CreateGameObjectResponse>
    {
        public override string CommandName => "GameObject.CreatePrimitive";
        public override string Description => "Create a primitive GameObject (Cube, Sphere, Capsule, Cylinder, Plane, Quad)";

        protected override bool TryWriteFormatted(CreateGameObjectResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Created primitive \"{response.name}\" (instanceId={response.instanceId})");
            else
                writer.WriteLine("Failed to create primitive");

            return true;
        }

        protected override ValueTask<CreateGameObjectResponse> ExecuteAsync(CreatePrimitiveRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.primitiveType))
                throw new ArgumentException("primitiveType is required (Cube, Sphere, Capsule, Cylinder, Plane, Quad)");

            if (!Enum.TryParse<PrimitiveType>(request.primitiveType, true, out var primitiveType))
            {
                throw new CommandFailedException(
                    $"Invalid primitiveType \"{request.primitiveType}\". Valid values: Cube, Sphere, Capsule, Cylinder, Plane, Quad",
                    new CreateGameObjectResponse());
            }

            var go = GameObject.CreatePrimitive(primitiveType);
            Undo.RegisterCreatedObjectUndo(go, $"Create {primitiveType}");

            if (!string.IsNullOrEmpty(request.name))
                go.name = request.name;

            if (!string.IsNullOrEmpty(request.parent))
            {
                var parentGo = GameObjectResolver.ResolveByPath(request.parent);
                if (parentGo == null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    throw new CommandFailedException(
                        $"Parent GameObject not found: \"{request.parent}\"",
                        new CreateGameObjectResponse());
                }

                go.transform.SetParent(parentGo.transform, false);
            }

            var components = go.GetComponents<Component>();
            var componentNames = new List<string>(components.Length);
            foreach (var component in components)
            {
                if (component != null)
                    componentNames.Add(component.GetType().FullName);
            }

            return new ValueTask<CreateGameObjectResponse>(new CreateGameObjectResponse
            {
                instanceId = go.GetInstanceID(),
                name = go.name,
                path = GameObjectResolver.BuildPath(go.transform),
                isActive = go.activeSelf,
                components = componentNames.ToArray()
            });
        }
    }

    [Serializable]
    public class CreatePrimitiveRequest
    {
        public string primitiveType = "";
        public string name = "";
        public string parent = "";
    }
}
