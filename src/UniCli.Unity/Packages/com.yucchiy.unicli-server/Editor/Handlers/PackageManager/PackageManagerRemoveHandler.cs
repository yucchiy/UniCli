using System;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PackageManagerRemoveHandler : CommandHandler<PackageManagerRemoveRequest, PackageManagerRemoveResponse>
    {
        public override string CommandName => CommandNames.PackageManager.Remove;
        public override string Description => "Remove a package by name (e.g., com.unity.cinemachine)";

        protected override bool TryFormat(PackageManagerRemoveResponse response, bool success, out string formatted)
        {
            formatted = success
                ? $"Removed {response.name}"
                : $"Failed to remove package";
            return true;
        }

        protected override async ValueTask<PackageManagerRemoveResponse> ExecuteAsync(PackageManagerRemoveRequest request)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var removeRequest = Client.Remove(request.name);
            await PackageManagerRequestHelper.WaitForCompletion(removeRequest);

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
