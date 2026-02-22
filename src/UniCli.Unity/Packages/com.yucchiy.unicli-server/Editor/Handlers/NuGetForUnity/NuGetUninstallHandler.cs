using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using NugetForUnity;
using NugetForUnity.Models;
using NugetForUnity.PluginAPI;

namespace UniCli.Server.Editor.Handlers.NuGetForUnity
{
    [Module("NuGet")]
    public sealed class NuGetUninstallHandler : CommandHandler<NuGetUninstallRequest, NuGetUninstallResponse>
    {
        public override string CommandName => "NuGet.Uninstall";
        public override string Description => "Uninstall a NuGet package by id";

        protected override bool TryWriteFormatted(NuGetUninstallResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Uninstalled {response.id}"
                : "Failed to uninstall package");
            return true;
        }

        protected override ValueTask<NuGetUninstallResponse> ExecuteAsync(NuGetUninstallRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.id))
                throw new ArgumentException("id is required");

            var installed = InstalledPackagesManager.InstalledPackages
                .FirstOrDefault(p => string.Equals(p.Id, request.id, StringComparison.OrdinalIgnoreCase));

            if (installed == null)
                throw new CommandFailedException(
                    $"Package {request.id} is not installed",
                    new NuGetUninstallResponse { id = request.id });

            NugetPackageUninstaller.Uninstall(
                new NugetPackageIdentifier(installed.Id, installed.Version),
                PackageUninstallReason.IndividualUninstall);

            return new ValueTask<NuGetUninstallResponse>(new NuGetUninstallResponse
            {
                id = installed.Id,
            });
        }
    }

    [Serializable]
    public class NuGetUninstallRequest
    {
        public string id;
    }

    [Serializable]
    public class NuGetUninstallResponse
    {
        public string id;
    }
}
