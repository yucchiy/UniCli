using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    internal static class SceneResolver
    {
        public static Scene Resolve(string name, string path, int sceneIndex = -1)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid())
                    return scene;
            }

            if (!string.IsNullOrEmpty(name))
            {
                var scene = SceneManager.GetSceneByName(name);
                if (scene.IsValid())
                    return scene;
            }

            if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCount)
                return SceneManager.GetSceneAt(sceneIndex);

            return default;
        }
    }
}
