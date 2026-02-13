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
            var propertySetNames = new HashSet<string>(
                properties.Where(p => p.HasSetter)
                    .Select(p => "Set" + p.PascalCaseName));

            var setMethods = CollectSetMethods(settingsType, commandPrefix, propertySetNames);
            var getMethods = CollectGetMethods(settingsType, commandPrefix);
            var nestedTypes = CollectNestedSettingsTypes(settingsType, commandPrefix);

            return new SettingsTypeInfo(
                settingsType,
                commandPrefix,
                properties,
                setMethods,
                getMethods,
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

        private static ImmutableArray<SettingsMethodInfo> CollectSetMethods(
            INamedTypeSymbol type, string commandPrefix, HashSet<string> propertySetNames)
        {
            var candidates = new List<SettingsMethodInfo>();

            foreach (var member in type.GetMembers())
            {
                if (member is not IMethodSymbol method)
                    continue;

                if (!method.Name.StartsWith("Set"))
                    continue;

                if (!IsValidSettingsMethod(method))
                    continue;

                // Skip all Obsolete methods
                if (HasObsolete(method))
                    continue;

                if (method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet)
                    continue;

                // Skip methods that duplicate a property setter
                if (propertySetNames.Contains(method.Name))
                    continue;

                if (!method.Parameters.All(p => IsSerializableParameter(p)))
                    continue;

                candidates.Add(new SettingsMethodInfo(method, commandPrefix));
            }

            // Deduplicate overloads: prefer NamedBuildTarget variant
            var grouped = candidates.GroupBy(m => m.Symbol.Name);
            var result = new List<SettingsMethodInfo>();

            foreach (var group in grouped)
            {
                var methods = group.ToList();
                if (methods.Count == 1)
                {
                    result.Add(methods[0]);
                    continue;
                }

                // Prefer NamedBuildTarget variant
                var preferred = methods
                    .Where(m => m.Symbol.Parameters.Any(p =>
                        TypeSerializabilityChecker.GetFullMetadataName(p.Type) ==
                        "UnityEditor.Build.NamedBuildTarget"))
                    .FirstOrDefault();

                result.Add(preferred ?? methods[0]);
            }

            return result.ToImmutableArray();
        }

        private static ImmutableArray<SettingsMethodInfo> CollectGetMethods(
            INamedTypeSymbol type, string commandPrefix)
        {
            var candidates = new List<SettingsMethodInfo>();

            foreach (var member in type.GetMembers())
            {
                if (member is not IMethodSymbol method)
                    continue;

                if (!method.Name.StartsWith("Get"))
                    continue;

                if (!IsValidSettingsMethod(method))
                    continue;

                // Skip all Obsolete methods
                if (HasObsolete(method))
                    continue;

                if (method.MethodKind == MethodKind.PropertyGet ||
                    method.MethodKind == MethodKind.PropertySet)
                    continue;

                // Skip parameterless Get methods (already covered by Inspect command)
                if (method.Parameters.Length == 0)
                    continue;

                // Return type must be serializable
                if (!TypeSerializabilityChecker.IsSerializableType(method.ReturnType))
                    continue;

                // All parameters must be serializable
                if (!method.Parameters.All(p => IsSerializableParameter(p)))
                    continue;

                candidates.Add(new SettingsMethodInfo(method, commandPrefix));
            }

            // Deduplicate overloads: prefer NamedBuildTarget variant
            var grouped = candidates.GroupBy(m => m.Symbol.Name);
            var result = new List<SettingsMethodInfo>();

            foreach (var group in grouped)
            {
                var methods = group.ToList();
                if (methods.Count == 1)
                {
                    result.Add(methods[0]);
                    continue;
                }

                var preferred = methods
                    .Where(m => m.Symbol.Parameters.Any(p =>
                        TypeSerializabilityChecker.GetFullMetadataName(p.Type) ==
                        "UnityEditor.Build.NamedBuildTarget"))
                    .FirstOrDefault();

                result.Add(preferred ?? methods[0]);
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
                var nestedPropertySetNames = new HashSet<string>(
                    properties.Where(p => p.HasSetter)
                        .Select(p => "Set" + p.PascalCaseName));
                var setMethods = CollectSetMethods(nestedType, nestedPrefix, nestedPropertySetNames);
                var getMethods = CollectGetMethods(nestedType, nestedPrefix);

                if (properties.Length == 0 && setMethods.Length == 0 && getMethods.Length == 0)
                    continue;

                result.Add(new NestedSettingsTypeInfo(
                    nestedType,
                    nestedPrefix,
                    parentPrefix,
                    properties,
                    setMethods,
                    getMethods));
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

        private static bool IsValidSettingsMethod(IMethodSymbol method)
        {
            if (!method.IsStatic)
                return false;

            if (method.DeclaredAccessibility != Accessibility.Public)
                return false;

            if (method.IsGenericMethod)
                return false;

            if (method.MethodKind != MethodKind.Ordinary)
                return false;

            return true;
        }

        private static bool IsSerializableParameter(IParameterSymbol parameter)
        {
            if (parameter.RefKind != RefKind.None)
                return false;

            var type = parameter.Type;

            if (TypeSerializabilityChecker.IsSerializableType(type))
                return true;

            // NamedBuildTarget is a special struct handled by helper
            if (TypeSerializabilityChecker.GetFullMetadataName(type) ==
                "UnityEditor.Build.NamedBuildTarget")
                return true;

            return false;
        }

        private static bool HasObsolete(ISymbol symbol)
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
        public ImmutableArray<SettingsMethodInfo> SetMethods { get; }
        public ImmutableArray<SettingsMethodInfo> GetMethods { get; }
        public ImmutableArray<NestedSettingsTypeInfo> NestedTypes { get; }

        public SettingsTypeInfo(
            INamedTypeSymbol type,
            string commandPrefix,
            ImmutableArray<SettingsPropertyInfo> properties,
            ImmutableArray<SettingsMethodInfo> setMethods,
            ImmutableArray<SettingsMethodInfo> getMethods,
            ImmutableArray<NestedSettingsTypeInfo> nestedTypes)
        {
            Type = type;
            CommandPrefix = commandPrefix;
            Properties = properties;
            SetMethods = setMethods;
            GetMethods = getMethods;
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

    internal sealed class SettingsMethodInfo
    {
        public IMethodSymbol Symbol { get; }
        public string CommandPrefix { get; }

        public SettingsMethodInfo(
            IMethodSymbol symbol,
            string commandPrefix)
        {
            Symbol = symbol;
            CommandPrefix = commandPrefix;
        }
    }

    internal sealed class NestedSettingsTypeInfo
    {
        public INamedTypeSymbol Type { get; }
        public string CommandPrefix { get; }
        public string ParentPrefix { get; }
        public ImmutableArray<SettingsPropertyInfo> Properties { get; }
        public ImmutableArray<SettingsMethodInfo> SetMethods { get; }
        public ImmutableArray<SettingsMethodInfo> GetMethods { get; }

        public NestedSettingsTypeInfo(
            INamedTypeSymbol type,
            string commandPrefix,
            string parentPrefix,
            ImmutableArray<SettingsPropertyInfo> properties,
            ImmutableArray<SettingsMethodInfo> setMethods,
            ImmutableArray<SettingsMethodInfo> getMethods)
        {
            Type = type;
            CommandPrefix = commandPrefix;
            ParentPrefix = parentPrefix;
            Properties = properties;
            SetMethods = setMethods;
            GetMethods = getMethods;
        }
    }
}
