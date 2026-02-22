using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using UniCli.SourceGenerator.Analysis;
using UniCli.SourceGenerator.Emitters;

namespace UniCli.SourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class SettingsCommandGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilationProvider = context.CompilationProvider;

            context.RegisterSourceOutput(compilationProvider, (spc, compilation) =>
            {
                Execute(spc, compilation);
            });
        }

        internal enum ResolveMode
        {
            Static = 0,
            Guid = 1,
            InstanceId = 2,
        }

        private static List<(string TypeName, string CommandPrefix, ResolveMode Mode, string Module)> CollectTargetTypes(
            Compilation compilation)
        {
            var targets = new List<(string TypeName, string CommandPrefix, ResolveMode Mode, string Module)>();

            foreach (var attr in compilation.Assembly.GetAttributes())
            {
                if (attr.AttributeClass == null ||
                    attr.AttributeClass.Name != "GenerateCommandsAttribute")
                    continue;

                if (attr.ConstructorArguments.Length < 2)
                    continue;

                var typeName = attr.ConstructorArguments[0].Value as string;
                var commandPrefix = attr.ConstructorArguments[1].Value as string;

                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(commandPrefix))
                    continue;

                var mode = ResolveMode.Static;
                var module = "";
                foreach (var named in attr.NamedArguments)
                {
                    if (named.Key == "ResolveMode" && named.Value.Value is int modeValue)
                    {
                        mode = (ResolveMode)modeValue;
                    }
                    else if (named.Key == "Module" && named.Value.Value is string moduleValue)
                    {
                        module = moduleValue;
                    }
                }

                targets.Add((typeName, commandPrefix, mode, module));
            }

            return targets;
        }

        private static void Execute(SourceProductionContext context, Compilation compilation)
        {
            // Only generate for the main server assembly to avoid duplicates
            // when the Source Generator is also applied to referencing assemblies (e.g. Tests)
            if (compilation.AssemblyName != "UniCli.Server.Editor")
                return;

            var targetTypes = CollectTargetTypes(compilation);
            if (targetTypes.Count == 0)
                return;

            var generatedFileNames = new HashSet<string>();
            var generatedCommandNames = new HashSet<string>();

            foreach (var (typeName, commandPrefix, mode, module) in targetTypes)
            {
                try
                {
                    var typeSymbol = compilation.GetTypeByMetadataName(typeName);
                    if (typeSymbol == null)
                        continue;

                    if (mode != ResolveMode.Static)
                    {
                        EmitInstanceType(context, typeSymbol, commandPrefix, mode, module,
                            generatedFileNames, generatedCommandNames);
                        continue;
                    }

                    var info = SettingsTypeAnalyzer.Analyze(typeSymbol, commandPrefix);

                    // Emit Inspect handler
                    var inspectCommandName = $"{info.CommandPrefix}.Inspect";
                    if (generatedCommandNames.Add(inspectCommandName))
                    {
                        var inspectSource = GetCommandEmitter.Emit(info, module);
                        AddSourceSafe(context, generatedFileNames,
                            $"{commandPrefix}InspectHandler.g.cs", inspectSource);
                    }
                }
                catch (System.Exception)
                {
                    // Skip this settings type entirely on error
                }
            }
        }

        private static void EmitInstanceType(
            SourceProductionContext context,
            INamedTypeSymbol typeSymbol,
            string commandPrefix,
            ResolveMode mode,
            string module,
            HashSet<string> generatedFileNames,
            HashSet<string> generatedCommandNames)
        {
            var info = InstanceTypeAnalyzer.Analyze(typeSymbol, commandPrefix, mode);

            // Emit Inspect handler
            var inspectCommandName = $"{info.CommandPrefix}.Inspect";
            if (generatedCommandNames.Add(inspectCommandName))
            {
                var inspectSource = InstanceInspectCommandEmitter.Emit(info, module);
                AddSourceSafe(context, generatedFileNames,
                    $"{commandPrefix}InspectHandler.g.cs", inspectSource);
            }
        }

        private static void AddSourceSafe(
            SourceProductionContext context,
            HashSet<string> generatedFileNames,
            string hintName,
            string source)
        {
            // Avoid duplicate file names
            if (!generatedFileNames.Add(hintName))
            {
                var counter = 2;
                var baseName = hintName.Replace(".g.cs", "");
                while (!generatedFileNames.Add($"{baseName}_{counter}.g.cs"))
                    counter++;
                hintName = $"{baseName}_{counter}.g.cs";
            }

            context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
        }
    }
}
