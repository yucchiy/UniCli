using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildMagicEditor;

namespace UniCli.Server.Editor.Handlers.BuildMagic
{
    public sealed class BuildMagicInspectHandler : CommandHandler<BuildMagicInspectRequest, BuildMagicInspectResponse>
    {
        public override string CommandName => "BuildMagic.Inspect";
        public override string Description => "Inspect a BuildMagic build scheme's configurations";

        protected override bool TryWriteFormatted(BuildMagicInspectResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine($"Scheme: {response.name}");
            if (!string.IsNullOrEmpty(response.baseSchemeName))
                writer.WriteLine($"Base: {response.baseSchemeName}");

            if (response.preBuildConfigurations.Length > 0)
            {
                writer.WriteLine("");
                writer.WriteLine("PreBuild Configurations:");
                foreach (var config in response.preBuildConfigurations)
                {
                    writer.WriteLine($"  - {config.taskType}: {config.propertyName}");
                }
            }

            if (response.postBuildConfigurations.Length > 0)
            {
                writer.WriteLine("");
                writer.WriteLine("PostBuild Configurations:");
                foreach (var config in response.postBuildConfigurations)
                {
                    writer.WriteLine($"  - {config.taskType}: {config.propertyName}");
                }
            }

            return true;
        }

        protected override ValueTask<BuildMagicInspectResponse> ExecuteAsync(BuildMagicInspectRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var allSchemes = BuildSchemeLoader.LoadAll<BuildScheme>().ToList();
            var scheme = allSchemes.FirstOrDefault(s => s.Name == request.name);

            if (scheme == null)
                throw new CommandFailedException(
                    $"Build scheme '{request.name}' not found",
                    new BuildMagicInspectResponse
                    {
                        name = request.name,
                        baseSchemeName = "",
                        preBuildConfigurations = Array.Empty<BuildMagicConfigurationEntry>(),
                        postBuildConfigurations = Array.Empty<BuildMagicConfigurationEntry>(),
                    });

            var preBuildConfigs = scheme.PreBuildConfigurations
                .Select(ExtractConfigInfo)
                .ToArray();

            var postBuildConfigs = scheme.PostBuildConfigurations
                .Select(ExtractConfigInfo)
                .ToArray();

            return new ValueTask<BuildMagicInspectResponse>(new BuildMagicInspectResponse
            {
                name = scheme.Name,
                baseSchemeName = scheme.BaseSchemeName ?? "",
                preBuildConfigurations = preBuildConfigs,
                postBuildConfigurations = postBuildConfigs,
            });
        }

        private static BuildMagicConfigurationEntry ExtractConfigInfo(IBuildConfiguration config)
        {
            return new BuildMagicConfigurationEntry
            {
                taskType = config.TaskType?.Name ?? "",
                propertyName = config.PropertyName ?? "",
            };
        }
    }

    [Serializable]
    public class BuildMagicInspectRequest
    {
        public string name;
    }

    [Serializable]
    public class BuildMagicInspectResponse
    {
        public string name;
        public string baseSchemeName;
        public BuildMagicConfigurationEntry[] preBuildConfigurations;
        public BuildMagicConfigurationEntry[] postBuildConfigurations;
    }

    [Serializable]
    public class BuildMagicConfigurationEntry
    {
        public string taskType;
        public string propertyName;
    }
}
