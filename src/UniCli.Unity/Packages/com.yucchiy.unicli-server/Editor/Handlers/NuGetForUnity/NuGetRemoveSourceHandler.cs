using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniCli.Server.Editor.Handlers.NuGetForUnity
{
    [Module("NuGet")]
    public sealed class NuGetRemoveSourceHandler : CommandHandler<NuGetRemoveSourceRequest, NuGetRemoveSourceResponse>
    {
        public override string CommandName => "NuGet.RemoveSource";
        public override string Description => "Remove a NuGet package source";

        protected override bool TryWriteFormatted(NuGetRemoveSourceResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Removed source '{response.name}'"
                : "Failed to remove source");
            return true;
        }

        protected override ValueTask<NuGetRemoveSourceResponse> ExecuteAsync(NuGetRemoveSourceRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            NuGetConfigHelper.RemoveSource(request.name);

            return new ValueTask<NuGetRemoveSourceResponse>(new NuGetRemoveSourceResponse
            {
                name = request.name,
            });
        }
    }

    [Serializable]
    public class NuGetRemoveSourceRequest
    {
        public string name;
    }

    [Serializable]
    public class NuGetRemoveSourceResponse
    {
        public string name;
    }
}
