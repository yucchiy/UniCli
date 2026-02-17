using System;
using System.Linq;
using System.Threading.Tasks;
using NugetForUnity;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers.NuGetForUnity
{
    public sealed class NuGetRestoreHandler : CommandHandler<Unit, NuGetRestoreResponse>
    {
        public override string CommandName => "NuGet.Restore";
        public override string Description => "Restore all NuGet packages from packages.config";

        protected override bool TryWriteFormatted(NuGetRestoreResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Restored {response.packageCount} package(s)"
                : "Failed to restore packages");
            return true;
        }

        protected override ValueTask<NuGetRestoreResponse> ExecuteAsync(Unit request)
        {
            PackageRestorer.Restore(false);

            var packageCount = InstalledPackagesManager.InstalledPackages.Count();

            return new ValueTask<NuGetRestoreResponse>(new NuGetRestoreResponse
            {
                packageCount = packageCount,
            });
        }
    }

    [Serializable]
    public class NuGetRestoreResponse
    {
        public int packageCount;
    }
}
