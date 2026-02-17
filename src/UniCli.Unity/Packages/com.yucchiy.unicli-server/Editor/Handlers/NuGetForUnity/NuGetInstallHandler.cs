using System;
using System.Linq;
using System.Threading.Tasks;
using NugetForUnity;
using NugetForUnity.Models;

namespace UniCli.Server.Editor.Handlers.NuGetForUnity
{
    public sealed class NuGetInstallHandler : CommandHandler<NuGetInstallRequest, NuGetInstallResponse>
    {
        public override string CommandName => "NuGet.Install";
        public override string Description => "Install a NuGet package by id and optional version";

        protected override bool TryWriteFormatted(NuGetInstallResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Installed {response.id}@{response.version}"
                : "Failed to install package");
            return true;
        }

        protected override ValueTask<NuGetInstallResponse> ExecuteAsync(NuGetInstallRequest request)
        {
            if (string.IsNullOrEmpty(request.id))
                throw new ArgumentException("id is required");

            var existing = InstalledPackagesManager.InstalledPackages
                .FirstOrDefault(p => string.Equals(p.Id, request.id, StringComparison.OrdinalIgnoreCase));

            if (existing != null && string.IsNullOrEmpty(request.version))
                throw new CommandFailedException(
                    $"Package {request.id}@{existing.Version} is already installed",
                    new NuGetInstallResponse { id = existing.Id, version = existing.Version });

            var identifier = string.IsNullOrEmpty(request.version)
                ? new NugetPackageIdentifier(request.id, string.Empty)
                : new NugetPackageIdentifier(request.id, request.version);

            var success = NugetPackageInstaller.InstallIdentifier(identifier);

            if (!success)
                throw new CommandFailedException(
                    $"Failed to install package {request.id}",
                    new NuGetInstallResponse { id = request.id, version = request.version ?? "" });

            var installed = InstalledPackagesManager.InstalledPackages
                .FirstOrDefault(p => string.Equals(p.Id, request.id, StringComparison.OrdinalIgnoreCase));

            return new ValueTask<NuGetInstallResponse>(new NuGetInstallResponse
            {
                id = installed?.Id ?? request.id,
                version = installed?.Version ?? request.version ?? "",
            });
        }
    }

    [Serializable]
    public class NuGetInstallRequest
    {
        public string id;
        public string version;
    }

    [Serializable]
    public class NuGetInstallResponse
    {
        public string id;
        public string version;
    }
}
