using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor
{
    internal static class GameObjectResolver
    {
        public static string BuildPath(Transform transform)
        {
            var parts = new List<string>();
            var current = transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        public static GameObject Resolve(int instanceId, string path)
        {
            if (instanceId != 0)
                return EditorUtility.InstanceIDToObject(instanceId) as GameObject;

            if (!string.IsNullOrEmpty(path))
                return FindByPath(path);

            return null;
        }

        private static GameObject FindByPath(string path)
        {
            var parts = path.Split('/');
            if (parts.Length == 0) return null;

            var rootName = parts[0];
            GameObject root = null;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var go in scene.GetRootGameObjects())
                {
                    if (go.name == rootName)
                    {
                        root = go;
                        break;
                    }
                }

                if (root != null) break;
            }

            if (root == null) return null;

            var current = root.transform;
            for (var i = 1; i < parts.Length; i++)
            {
                var child = current.Find(parts[i]);
                if (child == null) return null;
                current = child;
            }

            return current.gameObject;
        }
    }
}
