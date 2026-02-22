using System.Collections.Generic;
using System.Collections.Immutable;
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

            return new InstanceTypeInfo(
                type,
                commandPrefix,
                resolveMode,
                properties);
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
    }
}
