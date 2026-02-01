using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class GetHierarchyHandler : CommandHandler<GetHierarchyRequest, GetHierarchyResponse>
    {
        public override string CommandName => CommandNames.GameObject.GetHierarchy;
        public override string Description => "Get the scene hierarchy of GameObjects";

        protected override bool TryFormat(GetHierarchyResponse response, bool success, out string formatted)
        {
            if (!success || response.scenes == null || response.scenes.Length == 0)
            {
                formatted = "No scenes found.";
                return true;
            }

            var sb = new StringBuilder();

            for (var s = 0; s < response.scenes.Length; s++)
            {
                var scene = response.scenes[s];
                if (s > 0) sb.AppendLine();
                sb.AppendLine($"[{scene.name}]");

                if (scene.nodes == null) continue;
                foreach (var node in scene.nodes)
                    FormatNode(node, 0, sb);
            }

            formatted = sb.ToString().TrimEnd();
            return true;
        }

        private static void FormatNode(HierarchyNode node, int indent, StringBuilder sb)
        {
            sb.Append(new string(' ', indent * 2));
            sb.Append(node.name);

            if (!node.isActive)
                sb.Append(" (inactive)");

            if (node.components != null && node.components.Length > 0)
            {
                sb.Append("  [");
                for (var i = 0; i < node.components.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    var fullName = node.components[i];
                    var dotIndex = fullName.LastIndexOf('.');
                    sb.Append(dotIndex >= 0 ? fullName.Substring(dotIndex + 1) : fullName);
                }
                sb.Append(']');
            }

            sb.AppendLine();

            if (node.children == null) return;
            foreach (var child in node.children)
                FormatNode(child, indent + 1, sb);
        }

        protected override ValueTask<GetHierarchyResponse> ExecuteAsync(GetHierarchyRequest request)
        {
            var scenes = new List<HierarchyScene>();

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                var nodes = new List<HierarchyNode>();
                var node = CollectNode(prefabStage.prefabContentsRoot, 0, request);
                if (node != null) nodes.Add(node);

                scenes.Add(new HierarchyScene
                {
                    name = prefabStage.scene.name,
                    nodes = nodes.ToArray()
                });
            }
            else
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    var nodes = new List<HierarchyNode>();
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var root in rootObjects)
                    {
                        var node = CollectNode(root, 0, request);
                        if (node != null) nodes.Add(node);
                    }

                    scenes.Add(new HierarchyScene
                    {
                        name = scene.name,
                        nodes = nodes.ToArray()
                    });
                }
            }

            return new ValueTask<GetHierarchyResponse>(new GetHierarchyResponse
            {
                scenes = scenes.ToArray()
            });
        }

        private static HierarchyNode CollectNode(
            GameObject go,
            int depth,
            GetHierarchyRequest request)
        {
            if (!request.includeInactive && !go.activeInHierarchy) return null;
            if (request.maxDepth >= 0 && depth > request.maxDepth) return null;

            var node = new HierarchyNode
            {
                instanceId = go.GetInstanceID(),
                name = go.name,
                depth = depth,
                isActive = go.activeSelf,
                tag = go.tag,
                layer = go.layer
            };

            if (request.includeComponents)
            {
                var components = go.GetComponents<Component>();
                var componentNames = new List<string>(components.Length);
                foreach (var component in components)
                {
                    if (component != null)
                        componentNames.Add(component.GetType().FullName);
                }
                node.components = componentNames.ToArray();
            }
            else
            {
                node.components = Array.Empty<string>();
            }

            var children = new List<HierarchyNode>();
            var transform = go.transform;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = CollectNode(transform.GetChild(i).gameObject, depth + 1, request);
                if (child != null) children.Add(child);
            }
            node.children = children.ToArray();

            return node;
        }
    }

    [Serializable]
    public class GetHierarchyRequest
    {
        public bool includeInactive;
        public int maxDepth = -1;
        public bool includeComponents = true;
    }

    [Serializable]
    public class GetHierarchyResponse
    {
        public HierarchyScene[] scenes;
    }

    [Serializable]
    public class HierarchyScene
    {
        public string name;
        public HierarchyNode[] nodes;
    }

    [Serializable]
    public class HierarchyNode
    {
        public int instanceId;
        public string name;
        public int depth;
        public bool isActive;
        public string[] components;
        public HierarchyNode[] children;
        public string tag;
        public int layer;
    }
}
