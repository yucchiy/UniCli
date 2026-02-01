using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PackageManagerSearchHandler : CommandHandler<PackageManagerSearchRequest, PackageManagerSearchResponse>
    {
        public override string CommandName => CommandNames.PackageManager.Search;
        public override string Description => "Search for packages in the Unity registry";

        protected override bool TryFormat(PackageManagerSearchResponse response, bool success, out string formatted)
        {
            var sb = new StringBuilder();

            var nameWidth = "Name".Length;
            var versionWidth = "Version".Length;

            foreach (var pkg in response.packages)
            {
                nameWidth = Math.Max(nameWidth, pkg.name.Length);
                versionWidth = Math.Max(versionWidth, pkg.version.Length);
            }

            sb.AppendLine(
                $"{"Name".PadRight(nameWidth)}  {"Version".PadRight(versionWidth)}  Description");

            foreach (var pkg in response.packages)
            {
                sb.AppendLine(
                    $"{pkg.name.PadRight(nameWidth)}  {pkg.version.PadRight(versionWidth)}  {pkg.description}");
            }

            sb.Append($"{response.totalCount} result(s)");

            formatted = sb.ToString();
            return true;
        }

        protected override async ValueTask<PackageManagerSearchResponse> ExecuteAsync(PackageManagerSearchRequest request)
        {
            PackageSearchEntry[] entries;

            if (string.IsNullOrEmpty(request.query))
            {
                var searchRequest = Client.SearchAll();
                await PackageManagerRequestHelper.WaitForCompletion(searchRequest);

                if (searchRequest.Status == StatusCode.Failure)
                    throw new CommandFailedException(
                        $"Package search failed: {searchRequest.Error?.message ?? "Unknown error"}",
                        new PackageManagerSearchResponse { packages = Array.Empty<PackageSearchEntry>(), totalCount = 0 });

                entries = searchRequest.Result
                    .Select(ToEntry)
                    .OrderBy(e => e.name)
                    .ToArray();
            }
            else
            {
                var searchRequest = Client.Search(request.query);
                await PackageManagerRequestHelper.WaitForCompletion(searchRequest);

                if (searchRequest.Status == StatusCode.Failure)
                    throw new CommandFailedException(
                        $"Package search failed: {searchRequest.Error?.message ?? "Unknown error"}",
                        new PackageManagerSearchResponse { packages = Array.Empty<PackageSearchEntry>(), totalCount = 0 });

                entries = searchRequest.Result
                    .Select(ToEntry)
                    .OrderBy(e => e.name)
                    .ToArray();
            }

            return new PackageManagerSearchResponse
            {
                packages = entries,
                totalCount = entries.Length
            };
        }

        static PackageSearchEntry ToEntry(UnityEditor.PackageManager.PackageInfo p)
        {
            return new PackageSearchEntry
            {
                name = p.name,
                displayName = p.displayName ?? "",
                version = p.version,
                description = p.description ?? ""
            };
        }
    }

    [Serializable]
    public class PackageManagerSearchRequest
    {
        public string query;
    }

    [Serializable]
    public class PackageManagerSearchResponse
    {
        public PackageSearchEntry[] packages;
        public int totalCount;
    }

    [Serializable]
    public class PackageSearchEntry
    {
        public string name;
        public string displayName;
        public string version;
        public string description;
    }
}
