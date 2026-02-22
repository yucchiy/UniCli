using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PackageManagerAddHandler : CommandHandler<PackageManagerAddRequest, PackageManagerAddResponse>
    {
        public override string CommandName => "PackageManager.Add";
        public override string Description => "Add a package by identifier (e.g., com.unity.foo@1.2.3 or git URL)";

        protected override bool TryWriteFormatted(PackageManagerAddResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Added {response.name}@{response.version} ({response.source})"
                : "Failed to add package");
            return true;
        }

        protected override async ValueTask<PackageManagerAddResponse> ExecuteAsync(PackageManagerAddRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.identifier))
                throw new ArgumentException("identifier is required");

            var addRequest = Client.Add(request.identifier);
            await PackageManagerRequestHelper.WaitForCompletion(addRequest, cancellationToken);

            if (addRequest.Status == StatusCode.Failure)
                throw new CommandFailedException(
                    $"Package add failed: {addRequest.Error?.message ?? "Unknown error"}",
                    new PackageManagerAddResponse());

            var info = addRequest.Result;
            return new PackageManagerAddResponse
            {
                name = info.name,
                displayName = info.displayName ?? "",
                version = info.version,
                source = info.source.ToString()
            };
        }
    }

    [Serializable]
    public class PackageManagerAddRequest
    {
        public string identifier;
    }

    [Serializable]
    public class PackageManagerAddResponse
    {
        public string name;
        public string displayName;
        public string version;
        public string source;
    }
}
