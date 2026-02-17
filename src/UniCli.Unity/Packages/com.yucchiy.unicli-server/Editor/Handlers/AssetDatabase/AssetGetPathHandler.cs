using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AssetGetPathHandler : CommandHandler<AssetGetPathRequest, AssetGetPathResponse>
    {
        public override string CommandName => CommandNames.AssetDatabase.GetPath;
        public override string Description => "Convert between asset GUID and path";

        protected override ValueTask<AssetGetPathResponse> ExecuteAsync(AssetGetPathRequest request, CancellationToken cancellationToken)
        {
            string guid;
            string path;

            if (!string.IsNullOrEmpty(request.guid))
            {
                guid = request.guid;
                path = AssetDatabase.GUIDToAssetPath(guid);
            }
            else if (!string.IsNullOrEmpty(request.path))
            {
                path = request.path;
                guid = AssetDatabase.AssetPathToGUID(path);
            }
            else
            {
                throw new CommandFailedException(
                    "Either guid or path must be specified",
                    new AssetGetPathResponse { guid = "", path = "", type = "", exists = false });
            }

            var exists = !string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(guid);
            var assetType = exists ? AssetDatabase.GetMainAssetTypeAtPath(path) : null;

            return new ValueTask<AssetGetPathResponse>(new AssetGetPathResponse
            {
                guid = guid ?? "",
                path = path ?? "",
                type = assetType?.Name ?? "",
                exists = exists
            });
        }
    }

    [Serializable]
    public class AssetGetPathRequest
    {
        public string guid = "";
        public string path = "";
    }

    [Serializable]
    public class AssetGetPathResponse
    {
        public string guid;
        public string path;
        public string type;
        public bool exists;
    }
}
