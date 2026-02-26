using System.Threading;
using System;
using System.Threading.Tasks;
using UniCli.Server.Editor;
using UnityEditor.PackageManager;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PackageManagerRemoveHandler : CommandHandler<PackageManagerRemoveRequest, PackageManagerRemoveResponse>
    {
        private readonly EditorStateGuard _guard;

        public PackageManagerRemoveHandler(EditorStateGuard guard)
        {
            _guard = guard;
        }

        public override string CommandName => "PackageManager.Remove";
        public override string Description => "Remove a package by name (e.g., com.unity.cinemachine)";

        protected override bool TryWriteFormatted(PackageManagerRemoveResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Removed {response.name}"
                : "Failed to remove package");
            return true;
        }

        protected override async ValueTask<PackageManagerRemoveResponse> ExecuteAsync(PackageManagerRemoveRequest request, CancellationToken cancellationToken)
        {
            using var scope = _guard.BeginScope(CommandName, GuardCondition.NotPlayingOrCompiling);

            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var removeRequest = Client.Remove(request.name);
            await PackageManagerRequestHelper.WaitForCompletion(removeRequest, cancellationToken);

            if (removeRequest.Status == StatusCode.Failure)
                throw new CommandFailedException(
                    $"Package remove failed: {removeRequest.Error?.message ?? "Unknown error"}",
                    new PackageManagerRemoveResponse());

            return new PackageManagerRemoveResponse
            {
                name = request.name
            };
        }
    }

    [Serializable]
    public class PackageManagerRemoveRequest
    {
        public string name;
    }

    [Serializable]
    public class PackageManagerRemoveResponse
    {
        public string name;
    }
}
