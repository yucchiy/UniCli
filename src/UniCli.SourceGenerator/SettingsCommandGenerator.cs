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
        private static readonly (string TypeName, string CommandPrefix)[] TargetTypes =
        {
            ("UnityEditor.PlayerSettings", "PlayerSettings"),
            ("UnityEditor.EditorSettings", "EditorSettings"),
            ("UnityEditor.EditorUserBuildSettings", "EditorUserBuildSettings"),
        };

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilationProvider = context.CompilationProvider;

            context.RegisterSourceOutput(compilationProvider, (spc, compilation) =>
            {
                Execute(spc, compilation);
            });
        }

        private static void Execute(SourceProductionContext context, Compilation compilation)
        {
            // Only generate for the main server assembly to avoid duplicates
            // when the Source Generator is also applied to referencing assemblies (e.g. Tests)
            if (compilation.AssemblyName != "UniCli.Server.Editor")
                return;

            var needsNamedBuildTargetHelper = false;
            var generatedFileNames = new System.Collections.Generic.HashSet<string>();
            var generatedCommandNames = new System.Collections.Generic.HashSet<string>();

            foreach (var (typeName, commandPrefix) in TargetTypes)
            {
                try
                {
                    var typeSymbol = compilation.GetTypeByMetadataName(typeName);
                    if (typeSymbol == null)
                        continue;

                    var info = SettingsTypeAnalyzer.Analyze(typeSymbol, commandPrefix);

                    // Emit bulk Inspect handler
                    var inspectCommandName = $"{info.CommandPrefix}.Inspect";
                    if (generatedCommandNames.Add(inspectCommandName))
                    {
                        var inspectSource = GetCommandEmitter.Emit(info);
                        AddSourceSafe(context, generatedFileNames,
                            $"{commandPrefix}InspectHandler.g.cs", inspectSource);
                    }

                    // Emit Set handlers for flat properties with setters
                    foreach (var prop in info.Properties)
                    {
                        if (!prop.HasSetter) continue;

                        var cmdName = $"{prop.CommandPrefix}.Set{prop.PascalCaseName}";
                        if (!generatedCommandNames.Add(cmdName)) continue;

                        try
                        {
                            var setSource = SetPropertyCommandEmitter.Emit(
                                prop,
                                typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                            AddSourceSafe(context, generatedFileNames,
                                $"{commandPrefix}Set{prop.PascalCaseName}Handler.g.cs", setSource);
                        }
                        catch (System.Exception)
                        {
                            // Skip this property on error
                        }
                    }

                    // Emit handlers for nested types
                    foreach (var nested in info.NestedTypes)
                    {
                        var nestedFullName = nested.Type.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat);

                        foreach (var prop in nested.Properties)
                        {
                            if (!prop.HasSetter) continue;

                            var cmdName = $"{prop.CommandPrefix}.Set{prop.PascalCaseName}";
                            if (!generatedCommandNames.Add(cmdName)) continue;

                            try
                            {
                                var setSource = SetPropertyCommandEmitter.Emit(prop, nestedFullName);
                                AddSourceSafe(context, generatedFileNames,
                                    $"{nested.CommandPrefix.Replace(".", "")}Set{prop.PascalCaseName}Handler.g.cs",
                                    setSource);
                            }
                            catch (System.Exception)
                            {
                            }
                        }

                        foreach (var method in nested.SetMethods)
                        {
                            var cmdName = $"{method.CommandPrefix}.{method.Symbol.Name}";
                            if (!generatedCommandNames.Add(cmdName)) continue;

                            try
                            {
                                var methodSource = MethodCommandEmitter.EmitSetMethod(method, nestedFullName);
                                AddSourceSafe(context, generatedFileNames,
                                    $"{nested.CommandPrefix.Replace(".", "")}{method.Symbol.Name}Handler.g.cs",
                                    methodSource);

                                if (HasNamedBuildTargetParam(method))
                                    needsNamedBuildTargetHelper = true;
                            }
                            catch (System.Exception)
                            {
                            }
                        }

                        foreach (var method in nested.GetMethods)
                        {
                            var cmdName = $"{method.CommandPrefix}.{method.Symbol.Name}";
                            if (!generatedCommandNames.Add(cmdName)) continue;

                            try
                            {
                                var methodSource = MethodCommandEmitter.EmitGetMethod(method, nestedFullName);
                                AddSourceSafe(context, generatedFileNames,
                                    $"{nested.CommandPrefix.Replace(".", "")}{method.Symbol.Name}Handler.g.cs",
                                    methodSource);

                                if (HasNamedBuildTargetParam(method))
                                    needsNamedBuildTargetHelper = true;
                            }
                            catch (System.Exception)
                            {
                            }
                        }
                    }

                    // Emit Set method handlers
                    foreach (var method in info.SetMethods)
                    {
                        var cmdName = $"{method.CommandPrefix}.{method.Symbol.Name}";
                        if (!generatedCommandNames.Add(cmdName)) continue;

                        try
                        {
                            var methodSource = MethodCommandEmitter.EmitSetMethod(
                                method,
                                typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                            AddSourceSafe(context, generatedFileNames,
                                $"{commandPrefix}{method.Symbol.Name}Handler.g.cs", methodSource);

                            if (HasNamedBuildTargetParam(method))
                                needsNamedBuildTargetHelper = true;
                        }
                        catch (System.Exception)
                        {
                        }
                    }

                    // Emit Get method handlers
                    foreach (var method in info.GetMethods)
                    {
                        var cmdName = $"{method.CommandPrefix}.{method.Symbol.Name}";
                        if (!generatedCommandNames.Add(cmdName)) continue;

                        try
                        {
                            var methodSource = MethodCommandEmitter.EmitGetMethod(
                                method,
                                typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                            AddSourceSafe(context, generatedFileNames,
                                $"{commandPrefix}{method.Symbol.Name}Handler.g.cs", methodSource);

                            if (HasNamedBuildTargetParam(method))
                                needsNamedBuildTargetHelper = true;
                        }
                        catch (System.Exception)
                        {
                        }
                    }
                }
                catch (System.Exception)
                {
                    // Skip this settings type entirely on error
                }
            }

            // Emit NamedBuildTargetHelper if needed
            if (needsNamedBuildTargetHelper)
            {
                try
                {
                    var namedBuildTargetType = compilation.GetTypeByMetadataName(
                        "UnityEditor.Build.NamedBuildTarget");
                    if (namedBuildTargetType != null)
                    {
                        var helperSource = NamedBuildTargetHelperEmitter.Emit(namedBuildTargetType);
                        AddSourceSafe(context, generatedFileNames,
                            "NamedBuildTargetHelper.g.cs", helperSource);
                    }
                }
                catch (System.Exception)
                {
                }
            }
        }

        private static void AddSourceSafe(
            SourceProductionContext context,
            System.Collections.Generic.HashSet<string> generatedFileNames,
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

        private static bool HasNamedBuildTargetParam(SettingsMethodInfo method)
        {
            foreach (var param in method.Symbol.Parameters)
            {
                if (TypeSerializabilityChecker.GetFullMetadataName(param.Type) ==
                    "UnityEditor.Build.NamedBuildTarget")
                    return true;
            }
            return false;
        }
    }
}
