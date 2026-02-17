using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor.PackageManager;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PackageManagerListHandler : CommandHandler<Unit, PackageManagerListResponse>
    {
        public override string CommandName => CommandNames.PackageManager.List;
        public override string Description => "List all installed packages in the project";

        protected override bool TryWriteFormatted(PackageManagerListResponse response, bool success, IFormatWriter writer)
        {
            var nameWidth = "Name".Length;
            var versionWidth = "Version".Length;
            var sourceWidth = "Source".Length;

            foreach (var pkg in response.packages)
            {
                nameWidth = Math.Max(nameWidth, pkg.name.Length);
                versionWidth = Math.Max(versionWidth, pkg.version.Length);
                sourceWidth = Math.Max(sourceWidth, pkg.source.Length);
            }

            writer.WriteLine(
                $"{"Name".PadRight(nameWidth)}  {"Version".PadRight(versionWidth)}  {"Source".PadRight(sourceWidth)}  Direct");

            foreach (var pkg in response.packages)
            {
                var direct = pkg.isDirectDependency ? "yes" : "";
                writer.WriteLine(
                    $"{pkg.name.PadRight(nameWidth)}  {pkg.version.PadRight(versionWidth)}  {pkg.source.PadRight(sourceWidth)}  {direct}");
            }

            writer.WriteLine($"{response.totalCount} package(s)");

            return true;
        }

        protected override async ValueTask<PackageManagerListResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var listRequest = Client.List(true);
            await PackageManagerRequestHelper.WaitForCompletion(listRequest, cancellationToken);

            if (listRequest.Status == StatusCode.Failure)
                throw new CommandFailedException(
                    $"Package list failed: {listRequest.Error?.message ?? "Unknown error"}",
                    new PackageManagerListResponse { packages = Array.Empty<PackageEntry>(), totalCount = 0 });

            var packages = listRequest.Result
                .Select(p => new PackageEntry
                {
                    name = p.name,
                    displayName = p.displayName ?? "",
                    version = p.version,
                    source = p.source.ToString(),
                    isDirectDependency = p.isDirectDependency
                })
                .OrderBy(p => p.name)
                .ToArray();

            return new PackageManagerListResponse
            {
                packages = packages,
                totalCount = packages.Length
            };
        }
    }

    [Serializable]
    public class PackageManagerListResponse
    {
        public PackageEntry[] packages;
        public int totalCount;
    }

    [Serializable]
    public class PackageEntry
    {
        public string name;
        public string displayName;
        public string version;
        public string source;
        public bool isDirectDependency;
    }
}
