using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("GameObject")]
    public sealed class DuplicateGameObjectHandler : CommandHandler<DuplicateGameObjectRequest, CreateGameObjectResponse>
    {
        public override string CommandName => "GameObject.Duplicate";
        public override string Description => "Duplicate an existing GameObject in the scene";

        protected override bool TryWriteFormatted(CreateGameObjectResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Duplicated GameObject \"{response.name}\" (instanceId={response.instanceId})");
            else
                writer.WriteLine("Failed to duplicate GameObject");

            return true;
        }

        protected override ValueTask<CreateGameObjectResponse> ExecuteAsync(DuplicateGameObjectRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new CreateGameObjectResponse());
            }

            var duplicate = UnityEngine.Object.Instantiate(go, go.transform.parent);
            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate {go.name}");

            if (!string.IsNullOrEmpty(request.name))
                duplicate.name = request.name;

            var components = duplicate.GetComponents<Component>();
            var componentNames = new List<string>(components.Length);
            foreach (var component in components)
            {
                if (component != null)
                    componentNames.Add(component.GetType().FullName);
            }

            return new ValueTask<CreateGameObjectResponse>(new CreateGameObjectResponse
            {
                instanceId = duplicate.GetInstanceID(),
                name = duplicate.name,
                path = GameObjectResolver.BuildPath(duplicate.transform),
                isActive = duplicate.activeSelf,
                components = componentNames.ToArray()
            });
        }
    }

    [Serializable]
    public class DuplicateGameObjectRequest
    {
        public int instanceId;
        public string path = "";
        public string name = "";
    }
}
