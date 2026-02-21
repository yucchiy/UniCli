using System.Threading;
using System;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SceneListHandler : CommandHandler<Unit, SceneListResponse>
    {
        public override string CommandName => CommandNames.Scene.List;
        public override string Description => "List all loaded scenes (SceneManager)";

        protected override bool TryWriteFormatted(SceneListResponse response, bool success, IFormatWriter writer)
        {
            if (!success || response.scenes == null || response.scenes.Length == 0)
            {
                writer.WriteLine("No scenes loaded.");
                return true;
            }

            writer.WriteLine($"Active scene: {response.activeSceneName}");
            writer.WriteLine($"Loaded scenes ({response.scenes.Length}):");

            foreach (var scene in response.scenes)
            {
                var dirty = scene.isDirty ? " [dirty]" : "";
                writer.WriteLine($"  {scene.name} ({scene.path}) buildIndex={scene.buildIndex}{dirty}");
            }

            return true;
        }

        protected override ValueTask<SceneListResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var count = SceneManager.sceneCount;
            var scenes = new SceneInfo[count];
            var activeScene = SceneManager.GetActiveScene();

            for (var i = 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes[i] = new SceneInfo
                {
                    name = scene.name,
                    path = scene.path,
                    buildIndex = scene.buildIndex,
                    isDirty = scene.isDirty,
                    isLoaded = scene.isLoaded,
                    rootCount = scene.rootCount
                };
            }

            return new ValueTask<SceneListResponse>(new SceneListResponse
            {
                scenes = scenes,
                activeSceneName = activeScene.name
            });
        }
    }

    [Serializable]
    public class SceneListResponse
    {
        public SceneInfo[] scenes;
        public string activeSceneName;
    }

    [Serializable]
    public class SceneInfo
    {
        public string name;
        public string path;
        public int buildIndex;
        public bool isDirty;
        public bool isLoaded;
        public int rootCount;
    }
}
