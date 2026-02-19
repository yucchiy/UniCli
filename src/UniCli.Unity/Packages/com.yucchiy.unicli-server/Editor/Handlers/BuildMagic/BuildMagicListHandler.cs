using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildMagicEditor;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers.BuildMagic
{
    public sealed class BuildMagicListHandler : CommandHandler<Unit, BuildMagicListResponse>
    {
        public override string CommandName => "BuildMagic.List";
        public override string Description => "List all BuildMagic build schemes";

        protected override bool TryWriteFormatted(BuildMagicListResponse response, bool success, IFormatWriter writer)
        {
            var nameWidth = "Name".Length;
            var baseWidth = "Base".Length;

            foreach (var scheme in response.schemes)
            {
                nameWidth = Math.Max(nameWidth, scheme.name.Length);
                baseWidth = Math.Max(baseWidth, (scheme.baseSchemeName ?? "").Length);
            }

            writer.WriteLine($"{"Name".PadRight(nameWidth)}  {"Base".PadRight(baseWidth)}  {"PreBuild".PadRight(10)}  {"PostBuild".PadRight(10)}");

            foreach (var scheme in response.schemes)
            {
                writer.WriteLine(
                    $"{scheme.name.PadRight(nameWidth)}  " +
                    $"{(scheme.baseSchemeName ?? "").PadRight(baseWidth)}  " +
                    $"{scheme.preBuildConfigCount.ToString().PadRight(10)}  " +
                    $"{scheme.postBuildConfigCount.ToString().PadRight(10)}");
            }

            writer.WriteLine($"{response.totalCount} scheme(s)");

            return true;
        }

        protected override ValueTask<BuildMagicListResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var schemes = BuildSchemeLoader.LoadAll<BuildScheme>()
                .Select(s => new BuildMagicSchemeEntry
                {
                    name = s.Name,
                    baseSchemeName = s.BaseSchemeName ?? "",
                    preBuildConfigCount = s.PreBuildConfigurations.Count,
                    postBuildConfigCount = s.PostBuildConfigurations.Count,
                })
                .OrderBy(s => s.name)
                .ToArray();

            return new ValueTask<BuildMagicListResponse>(new BuildMagicListResponse
            {
                schemes = schemes,
                totalCount = schemes.Length,
            });
        }
    }

    [Serializable]
    public class BuildMagicListResponse
    {
        public BuildMagicSchemeEntry[] schemes;
        public int totalCount;
    }

    [Serializable]
    public class BuildMagicSchemeEntry
    {
        public string name;
        public string baseSchemeName;
        public int preBuildConfigCount;
        public int postBuildConfigCount;
    }
}
