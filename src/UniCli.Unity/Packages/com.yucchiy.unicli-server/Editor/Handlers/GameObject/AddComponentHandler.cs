using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("GameObject")]
    public sealed class AddComponentHandler : CommandHandler<AddComponentRequest, AddComponentResponse>
    {
        public override string CommandName => "GameObject.AddComponent";
        public override string Description => "Add a component to a GameObject by type name";

        protected override bool TryWriteFormatted(AddComponentResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Added {response.typeName} to {response.gameObjectName}");
            else
                writer.WriteLine("Failed to add component");

            return true;
        }

        protected override ValueTask<AddComponentResponse> ExecuteAsync(AddComponentRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.typeName))
                throw new ArgumentException("typeName is required");

            var componentType = ResolveComponentType(request.typeName);

            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new AddComponentResponse());
            }

            var component = Undo.AddComponent(go, componentType);
            if (component == null)
            {
                throw new CommandFailedException(
                    $"Failed to add component {componentType.FullName} to {go.name}",
                    new AddComponentResponse());
            }

            return new ValueTask<AddComponentResponse>(new AddComponentResponse
            {
                gameObjectName = go.name,
                typeName = componentType.FullName,
                instanceId = component.GetInstanceID(),
                enabled = component is not Behaviour behaviour || behaviour.enabled
            });
        }

        private static Type ResolveComponentType(string typeName)
        {
            // 1. Try Type.GetType for assembly-qualified names
            var type = Type.GetType(typeName);
            if (type != null && typeof(Component).IsAssignableFrom(type))
                return type;

            // 2. Search all assemblies by full name
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    return type;
            }

            // 3. If no dot in name, search by short name across all assemblies
            if (!typeName.Contains("."))
            {
                var candidates = new List<Type>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (System.Reflection.ReflectionTypeLoadException e)
                    {
                        types = e.Types;
                    }

                    foreach (var t in types)
                    {
                        if (t != null && t.Name == typeName && typeof(Component).IsAssignableFrom(t))
                            candidates.Add(t);
                    }
                }

                if (candidates.Count == 1)
                    return candidates[0];

                if (candidates.Count > 1)
                {
                    throw new CommandFailedException(
                        $"Ambiguous type name \"{typeName}\". Candidates: {candidates[0].FullName}, {candidates[1].FullName}",
                        new AddComponentResponse());
                }
            }

            throw new CommandFailedException(
                $"Component type \"{typeName}\" not found",
                new AddComponentResponse());
        }
    }

    [Serializable]
    public class AddComponentRequest
    {
        public int instanceId;
        public string path = "";
        public string typeName;
    }

    [Serializable]
    public class AddComponentResponse
    {
        public string gameObjectName;
        public string typeName;
        public int instanceId;
        public bool enabled;
    }
}
