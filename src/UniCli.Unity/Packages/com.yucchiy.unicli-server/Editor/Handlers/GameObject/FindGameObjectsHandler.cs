using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class FindGameObjectsHandler : CommandHandler<FindGameObjectsRequest, FindGameObjectsResponse>
    {
        public override string CommandName => CommandNames.GameObject.Find;
        public override string Description => "Find GameObjects by name, tag, layer, or component";

        protected override ValueTask<FindGameObjectsResponse> ExecuteAsync(FindGameObjectsRequest request)
        {
            var results = new List<GameObjectResult>();
            var totalFound = 0;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                SearchGameObject(prefabStage.prefabContentsRoot, prefabStage.scene.name, "", request, results, ref totalFound);
            }
            else
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var root in rootObjects)
                    {
                        SearchGameObject(root, scene.name, "", request, results, ref totalFound);
                    }
                }
            }

            return new ValueTask<FindGameObjectsResponse>(new FindGameObjectsResponse
            {
                results = results.ToArray(),
                totalFound = totalFound
            });
        }

        private static void SearchGameObject(
            GameObject go,
            string sceneName,
            string parentPath,
            FindGameObjectsRequest request,
            List<GameObjectResult> results,
            ref int totalFound)
        {
            if (!request.includeInactive && !go.activeInHierarchy) return;

            var path = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";

            if (MatchesFilter(go, request))
            {
                totalFound++;

                if (results.Count < request.maxResults)
                {
                    var components = go.GetComponents<Component>();
                    var componentNames = new List<string>(components.Length);
                    foreach (var component in components)
                    {
                        if (component != null)
                        {
                            componentNames.Add(component.GetType().FullName);
                        }
                    }

                    results.Add(new GameObjectResult
                    {
                        instanceId = go.GetInstanceID(),
                        name = go.name,
                        path = path,
                        isActive = go.activeSelf,
                        components = componentNames.ToArray(),
                        sceneName = sceneName,
                        tag = go.tag,
                        layer = go.layer
                    });
                }
            }

            var transform = go.transform;
            for (var i = 0; i < transform.childCount; i++)
            {
                SearchGameObject(transform.GetChild(i).gameObject, sceneName, path, request, results, ref totalFound);
            }
        }

        private static bool MatchesFilter(GameObject go, FindGameObjectsRequest request)
        {
            if (!string.IsNullOrEmpty(request.namePattern))
            {
                if (!go.name.Contains(request.namePattern, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(request.tag))
            {
                if (!go.CompareTag(request.tag))
                {
                    return false;
                }
            }

            if (request.layer >= 0)
            {
                if (go.layer != request.layer)
                {
                    return false;
                }
            }

            if (request.requiredComponents != null && request.requiredComponents.Length > 0)
            {
                foreach (var componentName in request.requiredComponents)
                {
                    if (!HasComponent(go, componentName))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool HasComponent(GameObject go, string typeName)
        {
            var components = go.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;

                var type = component.GetType();
                if (type.FullName == typeName || type.Name == typeName)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [Serializable]
    public class FindGameObjectsRequest
    {
        public string namePattern;
        public string tag;
        public int layer = -1;
        public string[] requiredComponents;
        public bool includeInactive;
        public int maxResults = 100;
    }

    [Serializable]
    public class FindGameObjectsResponse
    {
        public GameObjectResult[] results;
        public int totalFound;
    }

    [Serializable]
    public class GameObjectResult
    {
        public int instanceId;
        public string name;
        public string path;
        public bool isActive;
        public string[] components;
        public string sceneName;
        public string tag;
        public int layer;
    }
}
