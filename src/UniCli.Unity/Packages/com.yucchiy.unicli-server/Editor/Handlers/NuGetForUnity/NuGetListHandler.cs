using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using NugetForUnity;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers.NuGetForUnity
{
    public sealed class NuGetListHandler : CommandHandler<Unit, NuGetListResponse>
    {
        public override string CommandName => "NuGet.List";
        public override string Description => "List all installed NuGet packages";

        protected override bool TryWriteFormatted(NuGetListResponse response, bool success, IFormatWriter writer)
        {
            var idWidth = "Id".Length;
            var versionWidth = "Version".Length;

            foreach (var pkg in response.packages)
            {
                idWidth = Math.Max(idWidth, pkg.id.Length);
                versionWidth = Math.Max(versionWidth, pkg.version.Length);
            }

            writer.WriteLine($"{"Id".PadRight(idWidth)}  {"Version".PadRight(versionWidth)}");

            foreach (var pkg in response.packages)
            {
                writer.WriteLine($"{pkg.id.PadRight(idWidth)}  {pkg.version.PadRight(versionWidth)}");
            }

            writer.WriteLine($"{response.totalCount} package(s)");

            return true;
        }

        protected override ValueTask<NuGetListResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var packages = InstalledPackagesManager.InstalledPackages
                .Select(p => new NuGetPackageEntry
                {
                    id = p.Id,
                    version = p.Version,
                })
                .OrderBy(p => p.id)
                .ToArray();

            return new ValueTask<NuGetListResponse>(new NuGetListResponse
            {
                packages = packages,
                totalCount = packages.Length,
            });
        }
    }

    [Serializable]
    public class NuGetListResponse
    {
        public NuGetPackageEntry[] packages;
        public int totalCount;
    }

    [Serializable]
    public class NuGetPackageEntry
    {
        public string id;
        public string version;
    }
}
