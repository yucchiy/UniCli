using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class SetAssetSelectionHandler : CommandHandler<SetAssetSelectionRequest, SetAssetSelectionResponse>
    {
        public override string CommandName => CommandNames.Selection.SetAsset;
        public override string Description => "Select an asset by path";

        protected override bool TryWriteFormatted(SetAssetSelectionResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Selected: {response.assetPath} ({response.typeName})");
            return true;
        }

        protected override ValueTask<SetAssetSelectionResponse> ExecuteAsync(SetAssetSelectionRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.assetPath))
            {
                throw new CommandFailedException(
                    "assetPath is required",
                    new SetAssetSelectionResponse());
            }

            var asset = AssetDatabase.LoadMainAssetAtPath(request.assetPath);
            if (asset == null)
            {
                throw new CommandFailedException(
                    $"Asset not found: \"{request.assetPath}\"",
                    new SetAssetSelectionResponse());
            }

            Selection.activeObject = asset;

            return new ValueTask<SetAssetSelectionResponse>(new SetAssetSelectionResponse
            {
                assetPath = request.assetPath,
                typeName = asset.GetType().FullName,
                name = asset.name
            });
        }
    }

    [Serializable]
    public class SetAssetSelectionRequest
    {
        public string assetPath;
    }

    [Serializable]
    public class SetAssetSelectionResponse
    {
        public string assetPath;
        public string typeName;
        public string name;
    }
}
