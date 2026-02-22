using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Window")]
    public sealed class OpenWindowHandler : CommandHandler<OpenWindowRequest, OpenWindowResponse>
    {
        public override string CommandName => "Window.Open";
        public override string Description => "Open an EditorWindow by type name";

        protected override bool TryWriteFormatted(OpenWindowResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Opened: {response.typeName}");
            return true;
        }

        protected override ValueTask<OpenWindowResponse> ExecuteAsync(OpenWindowRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.typeName))
            {
                throw new CommandFailedException(
                    "typeName is required",
                    new OpenWindowResponse());
            }

            var type = WindowResolver.FindWindowType(request.typeName);
            if (type == null)
            {
                throw new CommandFailedException(
                    $"EditorWindow type '{request.typeName}' not found. Use Window.List to see available types.",
                    new OpenWindowResponse());
            }

            EditorWindow.GetWindow(type);

            return new ValueTask<OpenWindowResponse>(new OpenWindowResponse { typeName = type.FullName });
        }
    }

    [Serializable]
    public class OpenWindowRequest
    {
        public string typeName;
    }

    [Serializable]
    public class OpenWindowResponse
    {
        public string typeName;
    }
}
