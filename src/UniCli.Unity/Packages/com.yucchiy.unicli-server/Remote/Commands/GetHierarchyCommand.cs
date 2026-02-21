using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniCli.Remote.Commands
{
    [DebugCommand("Debug.GetHierarchy", "Get scene hierarchy including inactive objects")]
    public sealed class GetHierarchyCommand : DebugCommand<Unit, GetHierarchyCommand.Response>
    {
        protected override Response ExecuteCommand(Unit request)
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            var nodes = new List<HierarchyNode>();

            foreach (var root in rootObjects)
                CollectNodes(root.transform, 0, nodes);

            return new Response
            {
                sceneName = scene.name,
                nodes = nodes.ToArray()
            };
        }

        private static void CollectNodes(Transform transform, int depth, List<HierarchyNode> nodes)
        {
            var go = transform.gameObject;
            var components = go.GetComponents<Component>();
            var componentNames = new List<string>(components.Length);
            foreach (var component in components)
            {
                if (component != null)
                    componentNames.Add(component.GetType().FullName);
            }

            nodes.Add(new HierarchyNode
            {
                name = go.name,
                depth = depth,
                isActive = go.activeSelf,
                componentCount = components.Length,
                components = componentNames.ToArray()
            });

            for (var i = 0; i < transform.childCount; i++)
                CollectNodes(transform.GetChild(i), depth + 1, nodes);
        }

        [Serializable]
        public class Response
        {
            public string sceneName;
            public HierarchyNode[] nodes;
        }

        [Serializable]
        public class HierarchyNode
        {
            public string name;
            public int depth;
            public bool isActive;
            public int componentCount;
            public string[] components;
        }
    }
}
