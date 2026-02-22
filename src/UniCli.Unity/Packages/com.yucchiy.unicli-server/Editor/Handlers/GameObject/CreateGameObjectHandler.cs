using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("GameObject")]
    public sealed class CreateGameObjectHandler : CommandHandler<CreateGameObjectRequest, CreateGameObjectResponse>
    {
        public override string CommandName => "GameObject.Create";
        public override string Description => "Create a new GameObject in the scene";

        protected override bool TryWriteFormatted(CreateGameObjectResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Created GameObject \"{response.name}\" (instanceId={response.instanceId})");
            else
                writer.WriteLine("Failed to create GameObject");

            return true;
        }

        protected override ValueTask<CreateGameObjectResponse> ExecuteAsync(CreateGameObjectRequest request, CancellationToken cancellationToken)
        {
            var name = string.IsNullOrEmpty(request.name) ? "GameObject" : request.name;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            if (!string.IsNullOrEmpty(request.parent))
            {
                var parentGo = GameObjectResolver.Resolve(0, request.parent);
                if (parentGo == null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    throw new CommandFailedException(
                        $"Parent GameObject not found: \"{request.parent}\"",
                        new CreateGameObjectResponse());
                }

                go.transform.SetParent(parentGo.transform, false);
            }

            if (request.components != null)
            {
                foreach (var typeName in request.components)
                {
                    if (string.IsNullOrEmpty(typeName)) continue;

                    var componentType = ResolveComponentType(typeName);
                    var component = go.AddComponent(componentType);
                    if (component == null)
                    {
                        throw new CommandFailedException(
                            $"Failed to add component {componentType.FullName} to {go.name}",
                            new CreateGameObjectResponse());
                    }
                }
            }

            var components = go.GetComponents<Component>();
            var componentNames = new List<string>(components.Length);
            foreach (var component in components)
            {
                if (component != null)
                    componentNames.Add(component.GetType().FullName);
            }

            var path = GameObjectResolver.BuildPath(go.transform);

            return new ValueTask<CreateGameObjectResponse>(new CreateGameObjectResponse
            {
                instanceId = go.GetInstanceID(),
                name = go.name,
                path = path,
                isActive = go.activeSelf,
                components = componentNames.ToArray()
            });
        }

        private static Type ResolveComponentType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null && typeof(Component).IsAssignableFrom(type))
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    return type;
            }

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
                        new CreateGameObjectResponse());
                }
            }

            throw new CommandFailedException(
                $"Component type \"{typeName}\" not found",
                new CreateGameObjectResponse());
        }
    }

    [Serializable]
    public class CreateGameObjectRequest
    {
        public string name = "";
        public string parent = "";
        public string[] components;
    }

    [Serializable]
    public class CreateGameObjectResponse
    {
        public int instanceId;
        public string name;
        public string path;
        public bool isActive;
        public string[] components;
    }
}
