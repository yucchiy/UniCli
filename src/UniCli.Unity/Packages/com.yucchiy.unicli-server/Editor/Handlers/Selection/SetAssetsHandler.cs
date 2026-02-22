using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SetAssetsSelectionHandler : CommandHandler<SetAssetsSelectionRequest, SetAssetsSelectionResponse>
    {
        public override string CommandName => "Selection.SetAssets";
        public override string Description => "Select multiple assets by paths";

        protected override bool TryWriteFormatted(SetAssetsSelectionResponse response, bool success, IFormatWriter writer)
        {
            if (!success) return true;

            writer.WriteLine($"Selected {response.selected.Length} assets:");
            foreach (var item in response.selected)
                writer.WriteLine($"  {item.assetPath} ({item.typeName})");

            if (response.notFound != null && response.notFound.Length > 0)
            {
                writer.WriteLine($"Not found ({response.notFound.Length}):");
                foreach (var path in response.notFound)
                    writer.WriteLine($"  {path}");
            }

            return true;
        }

        protected override ValueTask<SetAssetsSelectionResponse> ExecuteAsync(SetAssetsSelectionRequest request, CancellationToken cancellationToken)
        {
            if (request.assetPaths == null || request.assetPaths.Length == 0)
            {
                throw new CommandFailedException(
                    "assetPaths is required",
                    new SetAssetsSelectionResponse
                    {
                        selected = Array.Empty<SelectedAssetInfo>(),
                        notFound = Array.Empty<string>()
                    });
            }

            var objects = new List<UnityEngine.Object>();
            var selected = new List<SelectedAssetInfo>();
            var notFound = new List<string>();

            foreach (var assetPath in request.assetPaths)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset != null)
                {
                    objects.Add(asset);
                    selected.Add(new SelectedAssetInfo
                    {
                        assetPath = assetPath,
                        typeName = asset.GetType().FullName,
                        name = asset.name
                    });
                }
                else
                {
                    notFound.Add(assetPath);
                }
            }

            Selection.objects = objects.ToArray();

            return new ValueTask<SetAssetsSelectionResponse>(new SetAssetsSelectionResponse
            {
                selected = selected.ToArray(),
                notFound = notFound.ToArray()
            });
        }
    }

    [Serializable]
    public class SetAssetsSelectionRequest
    {
        public string[] assetPaths;
    }

    [Serializable]
    public class SetAssetsSelectionResponse
    {
        public SelectedAssetInfo[] selected;
        public string[] notFound;
    }
}
