using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SceneSetActiveHandler : CommandHandler<SceneSetActiveRequest, SceneInfoResponse>
    {
        public override string CommandName => CommandNames.Scene.SetActive;
        public override string Description => "Set the active scene via SceneManager";

        protected override bool TryWriteFormatted(SceneInfoResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Active scene set to: {response.name} ({response.path})");
            else
                writer.WriteLine("Failed to set active scene");

            return true;
        }

        protected override ValueTask<SceneInfoResponse> ExecuteAsync(SceneSetActiveRequest request, CancellationToken cancellationToken)
        {
            var scene = SceneResolver.Resolve(request.name, request.path, request.sceneIndex);
            if (!scene.IsValid())
            {
                throw new CommandFailedException(
                    $"Scene not found (name=\"{request.name}\", path=\"{request.path}\")",
                    new SceneInfoResponse());
            }

            if (!scene.isLoaded)
            {
                throw new CommandFailedException(
                    $"Scene '{scene.name}' is not loaded",
                    new SceneInfoResponse());
            }

            if (!SceneManager.SetActiveScene(scene))
            {
                throw new CommandFailedException(
                    $"Failed to set '{scene.name}' as active scene",
                    new SceneInfoResponse());
            }

            return new ValueTask<SceneInfoResponse>(SceneInfoResponse.From(scene));
        }
    }

    [Serializable]
    public class SceneSetActiveRequest
    {
        public string name = "";
        public string path = "";
        public int sceneIndex = -1;
    }
}
