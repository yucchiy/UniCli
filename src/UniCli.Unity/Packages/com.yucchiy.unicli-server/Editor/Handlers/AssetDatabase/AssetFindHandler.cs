using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Assets")]
    public sealed class AssetFindHandler : CommandHandler<AssetFindRequest, AssetFindResponse>
    {
        public override string CommandName => "AssetDatabase.Find";
        public override string Description => "Find assets by filter (e.g. t:Texture, l:MyLabel)";

        protected override ValueTask<AssetFindResponse> ExecuteAsync(AssetFindRequest request, CancellationToken cancellationToken)
        {
            var guids = request.searchInFolders != null && request.searchInFolders.Length > 0
                ? AssetDatabase.FindAssets(request.filter, request.searchInFolders)
                : AssetDatabase.FindAssets(request.filter);

            var totalFound = guids.Length;
            var count = Math.Min(guids.Length, request.maxResults);
            var assets = new List<AssetInfo>(count);

            for (var i = 0; i < count; i++)
            {
                var guid = guids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

                assets.Add(new AssetInfo
                {
                    guid = guid,
                    path = path,
                    type = assetType?.Name ?? "Unknown"
                });
            }

            return new ValueTask<AssetFindResponse>(new AssetFindResponse
            {
                assets = assets.ToArray(),
                totalFound = totalFound
            });
        }
    }

    [Serializable]
    public class AssetFindRequest
    {
        public string filter = "";
        public string[] searchInFolders;
        public int maxResults = 100;
    }

    [Serializable]
    public class AssetFindResponse
    {
        public AssetInfo[] assets;
        public int totalFound;
    }

    [Serializable]
    public class AssetInfo
    {
        public string guid;
        public string path;
        public string type;
    }
}
