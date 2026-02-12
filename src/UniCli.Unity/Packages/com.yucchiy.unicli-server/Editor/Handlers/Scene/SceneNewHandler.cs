using System;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SceneNewHandler : CommandHandler<SceneNewRequest, SceneInfoResponse>
    {
        public override string CommandName => CommandNames.Scene.New;
        public override string Description => "Create a new scene";

        protected override bool TryWriteFormatted(SceneInfoResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Created new scene: {response.name}");
            else
                writer.WriteLine("Failed to create new scene");

            return true;
        }

        protected override ValueTask<SceneInfoResponse> ExecuteAsync(SceneNewRequest request)
        {
            var setup = request.empty
                ? NewSceneSetup.EmptyScene
                : NewSceneSetup.DefaultGameObjects;

            var mode = request.additive
                ? NewSceneMode.Additive
                : NewSceneMode.Single;

            var scene = EditorSceneManager.NewScene(setup, mode);
            if (!scene.IsValid())
            {
                throw new CommandFailedException(
                    "Failed to create new scene",
                    new SceneInfoResponse());
            }

            return new ValueTask<SceneInfoResponse>(SceneInfoResponse.From(scene));
        }
    }

    [Serializable]
    public class SceneNewRequest
    {
        public bool empty;
        public bool additive;
    }
}
