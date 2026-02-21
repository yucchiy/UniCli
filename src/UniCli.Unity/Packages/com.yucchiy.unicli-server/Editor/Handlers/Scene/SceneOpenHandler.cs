using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SceneOpenHandler : CommandHandler<SceneOpenRequest, SceneInfoResponse>
    {
        public override string CommandName => CommandNames.Scene.Open;
        public override string Description => "Open a scene by asset path via EditorSceneManager";

        protected override bool TryWriteFormatted(SceneInfoResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Opened scene: {response.name} ({response.path})");
            else
                writer.WriteLine("Failed to open scene");

            return true;
        }

        protected override ValueTask<SceneInfoResponse> ExecuteAsync(SceneOpenRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.path))
                throw new ArgumentException("path is required");

            var mode = request.additive
                ? OpenSceneMode.Additive
                : OpenSceneMode.Single;

            var scene = EditorSceneManager.OpenScene(request.path, mode);
            if (!scene.IsValid())
            {
                throw new CommandFailedException(
                    $"Failed to open scene at \"{request.path}\"",
                    new SceneInfoResponse());
            }

            return new ValueTask<SceneInfoResponse>(SceneInfoResponse.From(scene));
        }
    }

    [Serializable]
    public class SceneOpenRequest
    {
        public string path;
        public bool additive;
    }
}
