using System;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SceneGetActiveHandler : CommandHandler<Unit, SceneInfoResponse>
    {
        public override string CommandName => CommandNames.Scene.GetActive;
        public override string Description => "Get the active scene";

        protected override bool TryWriteFormatted(SceneInfoResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"{response.name} ({response.path}) buildIndex={response.buildIndex} dirty={response.isDirty}");
            else
                writer.WriteLine("Failed to get active scene");

            return true;
        }

        protected override ValueTask<SceneInfoResponse> ExecuteAsync(Unit request)
        {
            var scene = SceneManager.GetActiveScene();

            return new ValueTask<SceneInfoResponse>(SceneInfoResponse.From(scene));
        }
    }

    [Serializable]
    public class SceneInfoResponse
    {
        public string name;
        public string path;
        public int buildIndex;
        public bool isDirty;
        public bool isLoaded;
        public int rootCount;

        public static SceneInfoResponse From(Scene scene)
        {
            return new SceneInfoResponse
            {
                name = scene.name,
                path = scene.path,
                buildIndex = scene.buildIndex,
                isDirty = scene.isDirty,
                isLoaded = scene.isLoaded,
                rootCount = scene.rootCount
            };
        }
    }
}
