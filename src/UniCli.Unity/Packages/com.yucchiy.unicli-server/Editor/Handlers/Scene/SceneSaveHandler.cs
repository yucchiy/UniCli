using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SceneSaveHandler : CommandHandler<SceneSaveRequest, SceneSaveResponse>
    {
        public override string CommandName => CommandNames.Scene.Save;
        public override string Description => "Save a scene or all open scenes via EditorSceneManager";

        protected override bool TryWriteFormatted(SceneSaveResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"Saved {response.savedCount} scene(s):");
                if (response.savedScenePaths != null)
                {
                    foreach (var p in response.savedScenePaths)
                        writer.WriteLine($"  {p}");
                }
            }
            else
            {
                writer.WriteLine("Failed to save scene(s)");
            }

            return true;
        }

        protected override ValueTask<SceneSaveResponse> ExecuteAsync(SceneSaveRequest request, CancellationToken cancellationToken)
        {
            if (request.all)
                return SaveAllScenes();

            return SaveSingleScene(request);
        }

        private static ValueTask<SceneSaveResponse> SaveAllScenes()
        {
            var saved = EditorSceneManager.SaveOpenScenes();
            if (!saved)
            {
                throw new CommandFailedException(
                    "Failed to save open scenes",
                    new SceneSaveResponse { savedScenePaths = Array.Empty<string>(), savedCount = 0 });
            }

            var paths = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
                    paths.Add(scene.path);
            }

            return new ValueTask<SceneSaveResponse>(new SceneSaveResponse
            {
                savedScenePaths = paths.ToArray(),
                savedCount = paths.Count
            });
        }

        private static ValueTask<SceneSaveResponse> SaveSingleScene(SceneSaveRequest request)
        {
            Scene scene;

            if (!string.IsNullOrEmpty(request.name) || !string.IsNullOrEmpty(request.path) || request.sceneIndex >= 0)
            {
                scene = SceneResolver.Resolve(request.name, request.path, request.sceneIndex);
                if (!scene.IsValid())
                {
                    throw new CommandFailedException(
                        $"Scene not found (name=\"{request.name}\", path=\"{request.path}\")",
                        new SceneSaveResponse { savedScenePaths = Array.Empty<string>(), savedCount = 0 });
                }
            }
            else
            {
                scene = SceneManager.GetActiveScene();
            }

            var savePath = !string.IsNullOrEmpty(request.saveAsPath)
                ? request.saveAsPath
                : scene.path;

            var saved = EditorSceneManager.SaveScene(scene, savePath);
            if (!saved)
            {
                throw new CommandFailedException(
                    $"Failed to save scene '{scene.name}' to \"{savePath}\"",
                    new SceneSaveResponse { savedScenePaths = Array.Empty<string>(), savedCount = 0 });
            }

            return new ValueTask<SceneSaveResponse>(new SceneSaveResponse
            {
                savedScenePaths = new[] { savePath },
                savedCount = 1
            });
        }
    }

    [Serializable]
    public class SceneSaveRequest
    {
        public string name = "";
        public string path = "";
        public int sceneIndex = -1;
        public string saveAsPath = "";
        public bool all;
    }

    [Serializable]
    public class SceneSaveResponse
    {
        public string[] savedScenePaths;
        public int savedCount;
    }
}
