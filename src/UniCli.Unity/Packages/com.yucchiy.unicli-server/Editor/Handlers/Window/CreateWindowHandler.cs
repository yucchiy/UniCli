using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class CreateWindowHandler : CommandHandler<CreateWindowRequest, CreateWindowResponse>
    {
        public override string CommandName => "Window.Create";
        public override string Description => "Create a new EditorWindow instance by type name";

        protected override bool TryWriteFormatted(CreateWindowResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Created: {response.typeName} (instanceId={response.instanceId})");
            return true;
        }

        protected override ValueTask<CreateWindowResponse> ExecuteAsync(CreateWindowRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.typeName))
            {
                throw new CommandFailedException(
                    "typeName is required",
                    new CreateWindowResponse());
            }

            var type = WindowResolver.FindWindowType(request.typeName);
            if (type == null)
            {
                throw new CommandFailedException(
                    $"EditorWindow type '{request.typeName}' not found. Use Window.List to see available types.",
                    new CreateWindowResponse());
            }

            var window = ScriptableObject.CreateInstance(type) as EditorWindow;
            if (window == null)
            {
                throw new CommandFailedException(
                    $"Failed to create instance of '{type.FullName}'",
                    new CreateWindowResponse { typeName = type.FullName });
            }

            window.Show();

            return new ValueTask<CreateWindowResponse>(new CreateWindowResponse
            {
                typeName = type.FullName,
                instanceId = window.GetInstanceID()
            });
        }
    }

    [Serializable]
    public class CreateWindowRequest
    {
        public string typeName;
    }

    [Serializable]
    public class CreateWindowResponse
    {
        public string typeName;
        public int instanceId;
    }
}
