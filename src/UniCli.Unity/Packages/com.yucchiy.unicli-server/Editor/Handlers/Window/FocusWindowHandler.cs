using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class FocusWindowHandler : CommandHandler<FocusWindowRequest, FocusWindowResponse>
    {
        public override string CommandName => "Window.Focus";
        public override string Description => "Focus an already-open EditorWindow by type name";

        protected override bool TryWriteFormatted(FocusWindowResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Focused: {response.typeName}");
            return true;
        }

        protected override ValueTask<FocusWindowResponse> ExecuteAsync(FocusWindowRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.typeName))
            {
                throw new CommandFailedException(
                    "typeName is required",
                    new FocusWindowResponse());
            }

            var type = WindowResolver.FindWindowType(request.typeName);
            if (type == null)
            {
                throw new CommandFailedException(
                    $"EditorWindow type '{request.typeName}' not found. Use Window.List to see available types.",
                    new FocusWindowResponse());
            }

            var windows = Resources.FindObjectsOfTypeAll(type);
            if (windows == null || windows.Length == 0)
            {
                throw new CommandFailedException(
                    $"No open window of type '{type.FullName}'. Use Window.Open to open it first.",
                    new FocusWindowResponse { typeName = type.FullName });
            }

            var window = (EditorWindow)windows[0];
            window.Focus();

            return new ValueTask<FocusWindowResponse>(new FocusWindowResponse { typeName = type.FullName });
        }
    }

    [Serializable]
    public class FocusWindowRequest
    {
        public string typeName;
    }

    [Serializable]
    public class FocusWindowResponse
    {
        public string typeName;
    }
}
