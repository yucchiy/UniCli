using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BuildMagicEditor;

namespace UniCli.Server.Editor.Handlers.BuildMagic
{
    public sealed class BuildMagicApplyHandler : CommandHandler<BuildMagicApplyRequest, BuildMagicApplyResponse>
    {
        public override string CommandName => "BuildMagic.Apply";
        public override string Description => "Apply a BuildMagic build scheme (run PreBuild tasks to configure project settings)";

        protected override bool TryWriteFormatted(BuildMagicApplyResponse response, bool success, IFormatWriter writer)
        {
            writer.WriteLine(success
                ? $"Applied scheme '{response.name}' ({response.appliedTaskCount} task(s))"
                : $"Failed to apply scheme '{response.name}'");
            return true;
        }

        protected override ValueTask<BuildMagicApplyResponse> ExecuteAsync(BuildMagicApplyRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var allSchemes = BuildSchemeLoader.LoadAll<BuildScheme>().ToList();
            var scheme = allSchemes.FirstOrDefault(s => s.Name == request.name);

            if (scheme == null)
                throw new CommandFailedException(
                    $"Build scheme '{request.name}' not found",
                    new BuildMagicApplyResponse { name = request.name, appliedTaskCount = 0 });

            var preBuildTasks = ResolvePreBuildTasks(scheme, allSchemes);
            BuildMagicEditor.BuildPipeline.PreBuild(preBuildTasks);

            return new ValueTask<BuildMagicApplyResponse>(new BuildMagicApplyResponse
            {
                name = scheme.Name,
                appliedTaskCount = preBuildTasks.Count,
            });
        }

        private static IReadOnlyList<IBuildTask<IPreBuildContext>> ResolvePreBuildTasks(
            BuildScheme scheme,
            List<BuildScheme> allSchemes)
        {
            var buildMagicEditorAssembly = typeof(BuildSchemeLoader).Assembly;

            var schemeUtilityType = buildMagicEditorAssembly.GetType("BuildMagicEditor.BuildSchemeUtility");
            if (schemeUtilityType == null)
                throw new InvalidOperationException("BuildSchemeUtility type not found");

            // BuildSchemeUtility.EnumerateComposedConfigurations<IPreBuildContext>(scheme, allSchemes)
            var enumerateConfigsMethod = schemeUtilityType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "EnumerateComposedConfigurations" &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 2);
            if (enumerateConfigsMethod == null)
                throw new InvalidOperationException("EnumerateComposedConfigurations method not found");

            var genericConfigsMethod = enumerateConfigsMethod.MakeGenericMethod(typeof(IPreBuildContext));
            var configurations = genericConfigsMethod.Invoke(null, new object[] { scheme, allSchemes });

            // BuildTaskBuilderUtility.CreateBuildTasks<IPreBuildContext>(configurations)
            var taskBuilderType = buildMagicEditorAssembly.GetType("BuildMagicEditor.BuildTaskBuilderUtility");
            if (taskBuilderType == null)
                throw new InvalidOperationException("BuildTaskBuilderUtility type not found");

            var createTasksMethod = taskBuilderType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "CreateBuildTasks" &&
                    m.IsGenericMethod);
            if (createTasksMethod == null)
                throw new InvalidOperationException("CreateBuildTasks method not found");

            var genericCreateMethod = createTasksMethod.MakeGenericMethod(typeof(IPreBuildContext));
            var tasks = genericCreateMethod.Invoke(null, new object[] { configurations });

            return (IReadOnlyList<IBuildTask<IPreBuildContext>>)tasks;
        }
    }

    [Serializable]
    public class BuildMagicApplyRequest
    {
        public string name;
    }

    [Serializable]
    public class BuildMagicApplyResponse
    {
        public string name;
        public int appliedTaskCount;
    }
}
