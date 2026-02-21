using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniCli.Remote.Commands
{
    [DebugCommand("Debug.FindGameObjects", "Find GameObjects by name (substring match)")]
    public sealed class FindGameObjectsCommand : DebugCommand<FindGameObjectsCommand.Request, FindGameObjectsCommand.Response>
    {
        protected override Response ExecuteCommand(Request request)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("'name' is required");

            var results = new List<GameObjectInfo>();
            var allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var go in allObjects)
            {
                if (go.name.IndexOf(request.name, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                results.Add(new GameObjectInfo
                {
                    name = go.name,
                    path = GetPath(go.transform),
                    isActive = go.activeInHierarchy,
                    scene = go.scene.name,
                    componentCount = go.GetComponents<Component>().Length
                });
            }

            return new Response
            {
                query = request.name,
                count = results.Count,
                results = results.ToArray()
            };
        }

        private static string GetPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        [Serializable]
        public class Request
        {
            public string name;
        }

        [Serializable]
        public class Response
        {
            public string query;
            public int count;
            public GameObjectInfo[] results;
        }

        [Serializable]
        public class GameObjectInfo
        {
            public string name;
            public string path;
            public bool isActive;
            public string scene;
            public int componentCount;
        }
    }
}
