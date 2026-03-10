using System;
using System.Collections.Generic;
using System.Reflection;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Internal
{
    internal readonly struct CommandFieldExtractionResult
    {
        public readonly CommandFieldInfo[] Fields;
        public readonly CommandTypeDetail[] TypeDetails;

        public CommandFieldExtractionResult(CommandFieldInfo[] fields, CommandTypeDetail[] typeDetails)
        {
            Fields = fields;
            TypeDetails = typeDetails;
        }
    }

    internal static class CommandFieldInfoExtractor
    {
        public static CommandFieldExtractionResult Extract(Type type)
        {
            var extraction = new ExtractionState();
            var fields = extraction.ExtractFields(type);
            return new CommandFieldExtractionResult(fields, extraction.GetTypeDetails());
        }

        private sealed class ExtractionState
        {
            private readonly List<CommandTypeDetail> _typeDetails = new();
            private readonly HashSet<string> _collectedTypeIds = new(StringComparer.Ordinal);
            private readonly HashSet<string> _visitingTypeIds = new(StringComparer.Ordinal);

            public CommandFieldInfo[] ExtractFields(Type type)
            {
                if (type == typeof(Unit))
                    return Array.Empty<CommandFieldInfo>();

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var result = new List<CommandFieldInfo>(fields.Length);

                foreach (var field in fields)
                {
                    var nestedType = ResolveNestedType(field.FieldType);
                    var nestedTypeId = nestedType == null ? "" : GetTypeId(nestedType);
                    result.Add(new CommandFieldInfo
                    {
                        name = field.Name,
                        type = ToSimpleTypeName(field.FieldType),
                        typeId = nestedTypeId,
                        defaultValue = GetDefaultValueString(field)
                    });

                    if (nestedType != null)
                        CollectTypeDetail(nestedType);
                }

                return result.ToArray();
            }

            public CommandTypeDetail[] GetTypeDetails()
            {
                return _typeDetails.ToArray();
            }

            private void CollectTypeDetail(Type type)
            {
                var typeId = GetTypeId(type);
                if (!_collectedTypeIds.Add(typeId))
                    return;

                if (!_visitingTypeIds.Add(typeId))
                    return;

                var detail = new CommandTypeDetail
                {
                    typeName = NormalizeTypeName(ToSimpleTypeName(type)),
                    typeId = typeId,
                    fields = Array.Empty<CommandFieldInfo>()
                };
                _typeDetails.Add(detail);

                try
                {
                    detail.fields = ExtractFields(type);
                }
                finally
                {
                    _visitingTypeIds.Remove(typeId);
                }
            }
        }

        private static Type ResolveNestedType(Type type)
        {
            var targetType = type.IsArray ? type.GetElementType() : type;

            if (targetType == null || !IsNestedFieldTarget(targetType))
                return null;

            return targetType;
        }

        private static bool IsNestedFieldTarget(Type type)
        {
            if (type == typeof(Unit))
                return false;

            if (type.IsPrimitive || type.IsEnum)
                return false;

            if (type == typeof(string) || type == typeof(decimal))
                return false;

            if (type.Namespace != null)
            {
                if (type.Namespace.StartsWith("System", StringComparison.Ordinal))
                    return false;

                if (type.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal)
                    || type.Namespace.StartsWith("UnityEditor", StringComparison.Ordinal))
                    return false;
            }

            return type.IsClass || type.IsValueType;
        }

        public static string ToSimpleTypeName(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "int";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(string[])) return "string[]";
            if (type == typeof(int[])) return "int[]";
            if (type.IsArray) return type.GetElementType()?.Name + "[]";
            return type.Name;
        }

        public static string NormalizeTypeName(string typeName)
        {
            var normalized = typeName;
            while (normalized.EndsWith("[]", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(0, normalized.Length - 2);
            }

            return normalized;
        }

        public static string GetTypeId(Type type)
        {
            var targetType = type.IsArray ? type.GetElementType() : type;
            if (targetType == null)
                return "";

            var assemblyName = targetType.Assembly.GetName().Name ?? "";
            var fullName = targetType.FullName ?? targetType.Name;
            return $"{assemblyName}:{fullName.Replace('+', '.')}";
        }

        private static string GetDefaultValueString(FieldInfo field)
        {
            try
            {
                var instance = Activator.CreateInstance(field.DeclaringType);
                var value = field.GetValue(instance);

                if (value == null) return "";

                if (field.FieldType == typeof(string))
                    return value is string s && s.Length > 0 ? s : "";

                if (field.FieldType == typeof(int))
                {
                    var intValue = (int)value;
                    return intValue != 0 ? intValue.ToString() : "";
                }

                if (field.FieldType == typeof(bool))
                {
                    var boolValue = (bool)value;
                    return boolValue ? "true" : "";
                }

                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}
