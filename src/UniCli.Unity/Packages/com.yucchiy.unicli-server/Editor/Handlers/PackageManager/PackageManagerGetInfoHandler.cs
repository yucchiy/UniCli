using System;
using System.Linq;
using System.Threading.Tasks;
using UniCli.Server.Editor;
using UnityEditor.PackageManager;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PackageManagerGetInfoHandler : CommandHandler<PackageManagerGetInfoRequest, PackageManagerGetInfoResponse>
    {
        public override string CommandName => CommandNames.PackageManager.GetInfo;
        public override string Description => "Get detailed information about a specific installed package";

        protected override bool TryWriteFormatted(PackageManagerGetInfoResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
            {
                writer.WriteLine("Failed to get package info");
                return true;
            }

            writer.WriteLine($"Name:              {response.name}");
            writer.WriteLine($"Display Name:      {response.displayName}");
            writer.WriteLine($"Version:           {response.version}");
            writer.WriteLine($"Latest Version:    {response.latestVersion}");
            writer.WriteLine($"Source:            {response.source}");
            writer.WriteLine($"Direct Dependency: {(response.isDirectDependency ? "yes" : "no")}");
            writer.WriteLine($"Description:       {response.description}");

            if (response.dependencies != null && response.dependencies.Length > 0)
            {
                writer.WriteLine("Dependencies:");
                foreach (var dep in response.dependencies)
                    writer.WriteLine($"  - {dep}");
            }

            return true;
        }

        protected override async ValueTask<PackageManagerGetInfoResponse> ExecuteAsync(PackageManagerGetInfoRequest request)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var listRequest = Client.List(true);
            await PackageManagerRequestHelper.WaitForCompletion(listRequest);

            if (listRequest.Status == StatusCode.Failure)
                throw new CommandFailedException(
                    $"Package list failed: {listRequest.Error?.message ?? "Unknown error"}",
                    new PackageManagerGetInfoResponse());

            var packageInfo = listRequest.Result.FirstOrDefault(p => p.name == request.name);
            if (packageInfo == null)
                throw new CommandFailedException(
                    $"Package '{request.name}' is not installed",
                    new PackageManagerGetInfoResponse());

            var dependencies = packageInfo.dependencies != null
                ? packageInfo.dependencies.Select(d => $"{d.name}@{d.version}").ToArray()
                : Array.Empty<string>();

            return new PackageManagerGetInfoResponse
            {
                name = packageInfo.name,
                displayName = packageInfo.displayName ?? "",
                version = packageInfo.version,
                source = packageInfo.source.ToString(),
                description = packageInfo.description ?? "",
                isDirectDependency = packageInfo.isDirectDependency,
                latestVersion = packageInfo.versions.latest ?? packageInfo.version,
                dependencies = dependencies
            };
        }
    }

    [Serializable]
    public class PackageManagerGetInfoRequest
    {
        public string name;
    }

    [Serializable]
    public class PackageManagerGetInfoResponse
    {
        public string name;
        public string displayName;
        public string version;
        public string source;
        public string description;
        public bool isDirectDependency;
        public string latestVersion;
        public string[] dependencies;
    }
}
