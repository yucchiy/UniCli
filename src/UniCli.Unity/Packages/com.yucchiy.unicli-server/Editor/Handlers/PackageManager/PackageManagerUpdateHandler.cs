using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using UniCli.Server.Editor;
using UnityEditor.PackageManager;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PackageManagerUpdateHandler : CommandHandler<PackageManagerUpdateRequest, PackageManagerUpdateResponse>
    {
        private readonly EditorStateGuard _guard;

        public PackageManagerUpdateHandler(EditorStateGuard guard)
        {
            _guard = guard;
        }

        public override string CommandName => "PackageManager.Update";
        public override string Description => "Update a package to a specific version or the latest version";

        protected override bool TryWriteFormatted(PackageManagerUpdateResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Updated {response.name} {response.previousVersion} → {response.version}"
                : "Failed to update package");
            return true;
        }

        protected override async ValueTask<PackageManagerUpdateResponse> ExecuteAsync(PackageManagerUpdateRequest request, CancellationToken cancellationToken)
        {
            using var scope = _guard.BeginScope(CommandName, GuardCondition.NotPlayingOrCompiling);

            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var listRequest = Client.List(true);
            await PackageManagerRequestHelper.WaitForCompletion(listRequest, cancellationToken);

            if (listRequest.Status == StatusCode.Failure)
                throw new CommandFailedException(
                    $"Package list failed: {listRequest.Error?.message ?? "Unknown error"}",
                    new PackageManagerUpdateResponse());

            var currentPackage = listRequest.Result.FirstOrDefault(p => p.name == request.name);
            if (currentPackage == null)
                throw new CommandFailedException(
                    $"Package '{request.name}' is not installed",
                    new PackageManagerUpdateResponse());

            var previousVersion = currentPackage.version;
            var targetVersion = string.IsNullOrEmpty(request.version)
                ? currentPackage.versions.latest ?? currentPackage.version
                : request.version;

            var identifier = $"{request.name}@{targetVersion}";
            var addRequest = Client.Add(identifier);
            await PackageManagerRequestHelper.WaitForCompletion(addRequest, cancellationToken);

            if (addRequest.Status == StatusCode.Failure)
                throw new CommandFailedException(
                    $"Package update failed: {addRequest.Error?.message ?? "Unknown error"}",
                    new PackageManagerUpdateResponse
                    {
                        name = request.name,
                        previousVersion = previousVersion
                    });

            var info = addRequest.Result;
            return new PackageManagerUpdateResponse
            {
                name = info.name,
                displayName = info.displayName ?? "",
                previousVersion = previousVersion,
                version = info.version,
                source = info.source.ToString()
            };
        }
    }

    [Serializable]
    public class PackageManagerUpdateRequest
    {
        public string name;
        public string version;
    }

    [Serializable]
    public class PackageManagerUpdateResponse
    {
        public string name;
        public string displayName;
        public string previousVersion;
        public string version;
        public string source;
    }
}
