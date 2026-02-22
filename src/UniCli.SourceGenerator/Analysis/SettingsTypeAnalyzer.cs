using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UniCli.SourceGenerator.Analysis
{
    internal sealed class SettingsTypeAnalyzer
    {
        public static SettingsTypeInfo Analyze(INamedTypeSymbol settingsType, string commandPrefix)
        {
            var properties = CollectProperties(settingsType, commandPrefix);
            var nestedTypes = CollectNestedSettingsTypes(settingsType, commandPrefix);

            return new SettingsTypeInfo(
                settingsType,
                commandPrefix,
                properties,
                nestedTypes);
        }

        private static ImmutableArray<SettingsPropertyInfo> CollectProperties(
            INamedTypeSymbol type, string commandPrefix)
        {
            var result = new List<SettingsPropertyInfo>();

            foreach (var member in type.GetMembers())
            {
                if (member is not IPropertySymbol property)
                    continue;

                if (!IsValidProperty(property))
                    continue;

                // Skip all Obsolete properties
                if (HasObsolete(property))
                    continue;

                var propertyType = property.Type;

                if (!TypeSerializabilityChecker.IsSerializableType(propertyType))
                    continue;

                result.Add(new SettingsPropertyInfo(
                    property,
                    commandPrefix,
                    TypeSerializabilityChecker.IsEnumType(propertyType)));
            }

            return result.ToImmutableArray();
        }

        private static ImmutableArray<NestedSettingsTypeInfo> CollectNestedSettingsTypes(
            INamedTypeSymbol parentType, string parentPrefix)
        {
            var result = new List<NestedSettingsTypeInfo>();

            var allTypeMembers = parentType.GetTypeMembers();

            foreach (var nestedType in allTypeMembers)
            {
                // Accept public nested types (both static classes and regular classes with static members)
                if (nestedType.DeclaredAccessibility != Accessibility.Public &&
                    nestedType.DeclaredAccessibility != Accessibility.NotApplicable)
                    continue;

                // Skip enum types (they're not settings containers)
                if (nestedType.TypeKind == TypeKind.Enum)
                    continue;

                // Skip Obsolete nested types
                if (HasObsolete(nestedType))
                    continue;

                // Check if this nested type has any serializable properties
                var nestedPrefix = $"{parentPrefix}.{nestedType.Name}";
                var properties = CollectProperties(nestedType, nestedPrefix);

                if (properties.Length == 0)
                    continue;

                result.Add(new NestedSettingsTypeInfo(
                    nestedType,
                    nestedPrefix,
                    parentPrefix,
                    properties));
            }

            return result.ToImmutableArray();
        }

        private static bool IsValidProperty(IPropertySymbol property)
        {
            if (!property.IsStatic)
                return false;

            if (property.DeclaredAccessibility != Accessibility.Public)
                return false;

            if (property.IsIndexer)
                return false;

            if (property.GetMethod == null)
                return false;

            return true;
        }

        internal static bool HasObsolete(ISymbol symbol)
        {
            return symbol.GetAttributes().Any(a =>
                a.AttributeClass != null &&
                TypeSerializabilityChecker.GetFullMetadataName(a.AttributeClass) ==
                "System.ObsoleteAttribute");
        }

    }

    internal sealed class SettingsTypeInfo
    {
        public INamedTypeSymbol Type { get; }
        public string CommandPrefix { get; }
        public ImmutableArray<SettingsPropertyInfo> Properties { get; }
        public ImmutableArray<NestedSettingsTypeInfo> NestedTypes { get; }

        public SettingsTypeInfo(
            INamedTypeSymbol type,
            string commandPrefix,
            ImmutableArray<SettingsPropertyInfo> properties,
            ImmutableArray<NestedSettingsTypeInfo> nestedTypes)
        {
            Type = type;
            CommandPrefix = commandPrefix;
            Properties = properties;
            NestedTypes = nestedTypes;
        }
    }

    internal sealed class SettingsPropertyInfo
    {
        public IPropertySymbol Symbol { get; }
        public string CommandPrefix { get; }
        public bool IsEnum { get; }
        public bool HasSetter => Symbol.SetMethod != null;

        public string PascalCaseName
        {
            get
            {
                var name = Symbol.Name;
                if (name.Length == 0) return name;
                return char.ToUpperInvariant(name[0]) + name.Substring(1);
            }
        }

        public SettingsPropertyInfo(
            IPropertySymbol symbol,
            string commandPrefix,
            bool isEnum)
        {
            Symbol = symbol;
            CommandPrefix = commandPrefix;
            IsEnum = isEnum;
        }
    }

    internal sealed class NestedSettingsTypeInfo
    {
        public INamedTypeSymbol Type { get; }
        public string CommandPrefix { get; }
        public string ParentPrefix { get; }
        public ImmutableArray<SettingsPropertyInfo> Properties { get; }

        public NestedSettingsTypeInfo(
            INamedTypeSymbol type,
            string commandPrefix,
            string parentPrefix,
            ImmutableArray<SettingsPropertyInfo> properties)
        {
            Type = type;
            CommandPrefix = commandPrefix;
            ParentPrefix = parentPrefix;
            Properties = properties;
        }
    }
}
