using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NugetForUnity.Configuration;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers.NuGetForUnity
{
    [Module("NuGet")]
    public sealed class NuGetListSourcesHandler : CommandHandler<Unit, NuGetListSourcesResponse>
    {
        public override string CommandName => "NuGet.ListSources";
        public override string Description => "List all configured NuGet package sources";

        protected override bool TryWriteFormatted(NuGetListSourcesResponse response, bool success, IFormatWriter writer)
        {
            var nameWidth = "Name".Length;
            var pathWidth = "Path".Length;

            foreach (var src in response.sources)
            {
                nameWidth = Math.Max(nameWidth, src.name.Length);
                pathWidth = Math.Max(pathWidth, src.path.Length);
            }

            writer.WriteLine($"{"Name".PadRight(nameWidth)}  {"Path".PadRight(pathWidth)}  Enabled");

            foreach (var src in response.sources)
            {
                writer.WriteLine($"{src.name.PadRight(nameWidth)}  {src.path.PadRight(pathWidth)}  {src.isEnabled}");
            }

            writer.WriteLine($"{response.totalCount} source(s)");

            return true;
        }

        protected override ValueTask<NuGetListSourcesResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var sources = ConfigurationManager.NugetConfigFile.PackageSources
                .Select(s => new NuGetSourceEntry
                {
                    name = s.Name,
                    path = s.SavedPath,
                    isEnabled = s.IsEnabled,
                })
                .ToArray();

            return new ValueTask<NuGetListSourcesResponse>(new NuGetListSourcesResponse
            {
                sources = sources,
                totalCount = sources.Length,
            });
        }
    }

    [Serializable]
    public class NuGetListSourcesResponse
    {
        public NuGetSourceEntry[] sources;
        public int totalCount;
    }

    [Serializable]
    public class NuGetSourceEntry
    {
        public string name;
        public string path;
        public bool isEnabled;
    }
}
