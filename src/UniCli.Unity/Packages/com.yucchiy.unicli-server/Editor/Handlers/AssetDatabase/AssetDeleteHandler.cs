using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AssetDeleteHandler : CommandHandler<AssetDeleteRequest, AssetDeleteResponse>
    {
        public override string CommandName => CommandNames.AssetDatabase.Delete;
        public override string Description => "Delete an asset";

        protected override ValueTask<AssetDeleteResponse> ExecuteAsync(AssetDeleteRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.path))
                throw new ArgumentException("path must be specified");

            var assetType = AssetDatabase.GetMainAssetTypeAtPath(request.path);
            if (assetType == null)
            {
                throw new CommandFailedException(
                    $"Asset not found: {request.path}",
                    new AssetDeleteResponse { path = request.path, type = "" });
            }

            var typeName = assetType.Name;

            if (!AssetDatabase.DeleteAsset(request.path))
            {
                throw new CommandFailedException(
                    $"Failed to delete asset: {request.path}",
                    new AssetDeleteResponse { path = request.path, type = typeName });
            }

            return new ValueTask<AssetDeleteResponse>(new AssetDeleteResponse
            {
                path = request.path,
                type = typeName
            });
        }
    }

    [Serializable]
    public class AssetDeleteRequest
    {
        public string path = "";
    }

    [Serializable]
    public class AssetDeleteResponse
    {
        public string path;
        public string type;
    }
}
