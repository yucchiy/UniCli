using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniCli.Server.Editor.Handlers.NuGetForUnity
{
    [Module("NuGet")]
    public sealed class NuGetAddSourceHandler : CommandHandler<NuGetAddSourceRequest, NuGetAddSourceResponse>
    {
        public override string CommandName => "NuGet.AddSource";
        public override string Description => "Add a NuGet package source";

        protected override bool TryWriteFormatted(NuGetAddSourceResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Added source '{response.name}' ({response.path})"
                : "Failed to add source");
            return true;
        }

        protected override ValueTask<NuGetAddSourceResponse> ExecuteAsync(NuGetAddSourceRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");
            if (string.IsNullOrEmpty(request.path))
                throw new ArgumentException("path is required");

            NuGetConfigHelper.AddSource(request.name, request.path);

            return new ValueTask<NuGetAddSourceResponse>(new NuGetAddSourceResponse
            {
                name = request.name,
                path = request.path,
            });
        }
    }

    [Serializable]
    public class NuGetAddSourceRequest
    {
        public string name;
        public string path;
    }

    [Serializable]
    public class NuGetAddSourceResponse
    {
        public string name;
        public string path;
    }
}
