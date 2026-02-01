using System;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AssetImportHandler : CommandHandler<AssetImportRequest, AssetImportResponse>
    {
        public override string CommandName => CommandNames.AssetDatabase.Import;
        public override string Description => "Reimport an asset or refresh the AssetDatabase";

        protected override ValueTask<AssetImportResponse> ExecuteAsync(AssetImportRequest request)
        {
            if (string.IsNullOrEmpty(request.path))
            {
                AssetDatabase.Refresh();
                return new ValueTask<AssetImportResponse>(new AssetImportResponse
                {
                    path = "",
                    refreshed = true
                });
            }

            var options = request.forceUpdate
                ? ImportAssetOptions.ForceUpdate
                : ImportAssetOptions.Default;

            AssetDatabase.ImportAsset(request.path, options);

            return new ValueTask<AssetImportResponse>(new AssetImportResponse
            {
                path = request.path,
                refreshed = true
            });
        }
    }

    [Serializable]
    public class AssetImportRequest
    {
        public string path = "";
        public bool forceUpdate;
    }

    [Serializable]
    public class AssetImportResponse
    {
        public string path;
        public bool refreshed;
    }
}
