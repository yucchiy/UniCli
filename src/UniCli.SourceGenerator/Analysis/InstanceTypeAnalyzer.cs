using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UniCli.SourceGenerator.Analysis
{
    internal static class InstanceTypeAnalyzer
    {
        public static InstanceTypeInfo Analyze(
            INamedTypeSymbol type,
            string commandPrefix,
            SettingsCommandGenerator.ResolveMode resolveMode)
        {
            var properties = CollectProperties(type, commandPrefix);
            var setMethods = CollectSetMethods(type, commandPrefix);
            var getMethods = CollectGetMethods(type, commandPrefix);

            return new InstanceTypeInfo(
                type,
                commandPrefix,
                resolveMode,
                properties,
                setMethods,
                getMethods);
        }

        private static ImmutableArray<InstancePropertyInfo> CollectProperties(
            INamedTypeSymbol type, string commandPrefix)
        {
            var result = new List<InstancePropertyInfo>();

            foreach (var member in type.GetMembers())
            {
                if (member is not IPropertySymbol property)
                    continue;

                if (!IsValidInstanceProperty(property))
                    continue;

                if (SettingsTypeAnalyzer.HasObsolete(property))
                    continue;

                if (!TypeSerializabilityChecker.IsSerializableType(property.Type))
                    continue;

                result.Add(new InstancePropertyInfo(
                    property,
                    commandPrefix,
                    TypeSerializabilityChecker.IsEnumType(property.Type)));
            }

            return result.ToImmutableArray();
        }

        private static ImmutableArray<InstanceMethodInfo> CollectSetMethods(
            INamedTypeSymbol type,
            string commandPrefix)
        {
            var candidates = new List<InstanceMethodInfo>();

            foreach (var member in type.GetMembers())
            {
                if (member is not IMethodSymbol method)
                    continue;

                if (!method.Name.StartsWith("Set"))
                    continue;

                if (!IsValidInstanceMethod(method))
                    continue;

                if (SettingsTypeAnalyzer.HasObsolete(method))
                    continue;

                if (method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet)
                    continue;

                if (!method.Parameters.All(p => SettingsTypeAnalyzer.IsSerializableParameter(p)))
                    continue;

                candidates.Add(new InstanceMethodInfo(method, commandPrefix));
            }

            // Deduplicate overloads: prefer variant with more parameters (more specific)
            var grouped = candidates.GroupBy(m => m.Symbol.Name);
            var result = new List<InstanceMethodInfo>();

            foreach (var group in grouped)
            {
                var methods = group.ToList();
                if (methods.Count == 1)
                {
                    result.Add(methods[0]);
                    continue;
                }

                var preferred = methods
                    .OrderByDescending(m => m.Symbol.Parameters.Length)
                    .First();

                result.Add(preferred);
            }

            return result.ToImmutableArray();
        }

        private static ImmutableArray<InstanceMethodInfo> CollectGetMethods(
            INamedTypeSymbol type, string commandPrefix)
        {
            var candidates = new List<InstanceMethodInfo>();

            foreach (var member in type.GetMembers())
            {
                if (member is not IMethodSymbol method)
                    continue;

                if (!method.Name.StartsWith("Get"))
                    continue;

                if (!IsValidInstanceMethod(method))
                    continue;

                if (SettingsTypeAnalyzer.HasObsolete(method))
                    continue;

                if (method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet)
                    continue;

                // Skip parameterless Get methods (covered by Inspect)
                if (method.Parameters.Length == 0)
                    continue;

                if (!TypeSerializabilityChecker.IsSerializableType(method.ReturnType))
                    continue;

                if (!method.Parameters.All(p => SettingsTypeAnalyzer.IsSerializableParameter(p)))
                    continue;

                candidates.Add(new InstanceMethodInfo(method, commandPrefix));
            }

            // Deduplicate overloads: prefer variant with more parameters
            var grouped = candidates.GroupBy(m => m.Symbol.Name);
            var result = new List<InstanceMethodInfo>();

            foreach (var group in grouped)
            {
                var methods = group.ToList();
                if (methods.Count == 1)
                {
                    result.Add(methods[0]);
                    continue;
                }

                var preferred = methods
                    .OrderByDescending(m => m.Symbol.Parameters.Length)
                    .First();

                result.Add(preferred);
            }

            return result.ToImmutableArray();
        }

        private static bool IsValidInstanceProperty(IPropertySymbol property)
        {
            if (property.IsStatic)
                return false;

            if (property.DeclaredAccessibility != Accessibility.Public)
                return false;

            if (property.IsIndexer)
                return false;

            if (property.GetMethod == null)
                return false;

            return true;
        }

        private static bool IsValidInstanceMethod(IMethodSymbol method)
        {
            if (method.IsStatic)
                return false;

            if (method.DeclaredAccessibility != Accessibility.Public)
                return false;

            if (method.IsGenericMethod)
                return false;

            if (method.MethodKind != MethodKind.Ordinary)
                return false;

            return true;
        }
    }
}
