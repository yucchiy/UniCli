using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace UniCli.SourceGenerator.Analysis
{
    internal sealed class InstanceTypeInfo
    {
        public INamedTypeSymbol Type { get; }
        public string CommandPrefix { get; }
        public SettingsCommandGenerator.ResolveMode ResolveMode { get; }
        public ImmutableArray<InstancePropertyInfo> Properties { get; }
        public ImmutableArray<InstanceMethodInfo> SetMethods { get; }
        public ImmutableArray<InstanceMethodInfo> GetMethods { get; }

        public InstanceTypeInfo(
            INamedTypeSymbol type,
            string commandPrefix,
            SettingsCommandGenerator.ResolveMode resolveMode,
            ImmutableArray<InstancePropertyInfo> properties,
            ImmutableArray<InstanceMethodInfo> setMethods,
            ImmutableArray<InstanceMethodInfo> getMethods)
        {
            Type = type;
            CommandPrefix = commandPrefix;
            ResolveMode = resolveMode;
            Properties = properties;
            SetMethods = setMethods;
            GetMethods = getMethods;
        }
    }

    internal sealed class InstancePropertyInfo
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

        public InstancePropertyInfo(
            IPropertySymbol symbol,
            string commandPrefix,
            bool isEnum)
        {
            Symbol = symbol;
            CommandPrefix = commandPrefix;
            IsEnum = isEnum;
        }
    }

    internal sealed class InstanceMethodInfo
    {
        public IMethodSymbol Symbol { get; }
        public string CommandPrefix { get; }

        public InstanceMethodInfo(
            IMethodSymbol symbol,
            string commandPrefix)
        {
            Symbol = symbol;
            CommandPrefix = commandPrefix;
        }
    }
}
