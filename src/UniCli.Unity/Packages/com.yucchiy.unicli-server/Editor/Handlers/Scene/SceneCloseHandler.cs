using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SceneCloseHandler : CommandHandler<SceneCloseRequest, SceneCloseResponse>
    {
        public override string CommandName => CommandNames.Scene.Close;
        public override string Description => "Close a loaded scene via EditorSceneManager";

        protected override bool TryWriteFormatted(SceneCloseResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Closed scene: {response.name} ({response.path}) removed={response.removed}");
            else
                writer.WriteLine("Failed to close scene");

            return true;
        }

        protected override ValueTask<SceneCloseResponse> ExecuteAsync(SceneCloseRequest request, CancellationToken cancellationToken)
        {
            var scene = SceneResolver.Resolve(request.name, request.path, request.sceneIndex);
            if (!scene.IsValid())
            {
                throw new CommandFailedException(
                    $"Scene not found (name=\"{request.name}\", path=\"{request.path}\", sceneIndex={request.sceneIndex})",
                    new SceneCloseResponse());
            }

            var sceneName = scene.name;
            var scenePath = scene.path;

            var closed = EditorSceneManager.CloseScene(scene, request.removeScene);
            if (!closed)
            {
                throw new CommandFailedException(
                    $"Failed to close scene '{sceneName}' (it may be the only loaded scene)",
                    new SceneCloseResponse { name = sceneName, path = scenePath });
            }

            return new ValueTask<SceneCloseResponse>(new SceneCloseResponse
            {
                name = sceneName,
                path = scenePath,
                removed = request.removeScene
            });
        }
    }

    [Serializable]
    public class SceneCloseRequest
    {
        public string name = "";
        public string path = "";
        public int sceneIndex = -1;
        public bool removeScene;
    }

    [Serializable]
    public class SceneCloseResponse
    {
        public string name;
        public string path;
        public bool removed;
    }
}
