using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class GetSelectionHandler : CommandHandler<Unit, GetSelectionResponse>
    {
        public override string CommandName => CommandNames.Selection.Get;
        public override string Description => "Get the current selection in the editor";

        protected override bool TryWriteFormatted(GetSelectionResponse response, bool success, IFormatWriter writer)
        {
            if (!success) return true;

            if (response.gameObjects != null && response.gameObjects.Length > 0)
            {
                writer.WriteLine($"Selected GameObjects ({response.gameObjects.Length}):");
                foreach (var go in response.gameObjects)
                    writer.WriteLine($"  {go.path} (instanceId={go.instanceId})");
            }

            if (response.assets != null && response.assets.Length > 0)
            {
                writer.WriteLine($"Selected Assets ({response.assets.Length}):");
                foreach (var asset in response.assets)
                    writer.WriteLine($"  {asset.assetPath} ({asset.typeName})");
            }

            if ((response.gameObjects == null || response.gameObjects.Length == 0) &&
                (response.assets == null || response.assets.Length == 0))
            {
                writer.WriteLine("Nothing selected.");
            }

            return true;
        }

        protected override ValueTask<GetSelectionResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var gameObjects = new List<SelectedGameObjectInfo>();
            var assets = new List<SelectedAssetInfo>();

            foreach (var obj in UnityEditor.Selection.objects)
            {
                if (obj is GameObject go)
                {
                    gameObjects.Add(new SelectedGameObjectInfo
                    {
                        instanceId = go.GetInstanceID(),
                        name = go.name,
                        path = GameObjectResolver.BuildPath(go.transform)
                    });
                }
                else
                {
                    var assetPath = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        assets.Add(new SelectedAssetInfo
                        {
                            assetPath = assetPath,
                            typeName = obj.GetType().FullName,
                            name = obj.name
                        });
                    }
                }
            }

            return new ValueTask<GetSelectionResponse>(new GetSelectionResponse
            {
                gameObjects = gameObjects.ToArray(),
                assets = assets.ToArray()
            });
        }
    }

    [Serializable]
    public class GetSelectionResponse
    {
        public SelectedGameObjectInfo[] gameObjects;
        public SelectedAssetInfo[] assets;
    }

    [Serializable]
    public class SelectedGameObjectInfo
    {
        public int instanceId;
        public string name;
        public string path;
    }

    [Serializable]
    public class SelectedAssetInfo
    {
        public string assetPath;
        public string typeName;
        public string name;
    }
}
