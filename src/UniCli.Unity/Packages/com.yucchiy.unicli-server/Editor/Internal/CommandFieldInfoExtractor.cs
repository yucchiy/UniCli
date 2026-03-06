using System;
using System.Collections.Generic;
using System.Reflection;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Internal
{
    internal static class CommandFieldInfoExtractor
    {
        public static CommandFieldInfo[] ExtractFieldInfos(Type type)
        {
            return ExtractFieldInfos(type, new HashSet<Type>());
        }

        private static CommandFieldInfo[] ExtractFieldInfos(Type type, HashSet<Type> visitingTypes)
        {
            if (type == typeof(Unit))
                return Array.Empty<CommandFieldInfo>();

            if (!visitingTypes.Add(type))
                return Array.Empty<CommandFieldInfo>();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var result = new List<CommandFieldInfo>(fields.Length);

            try
            {
                foreach (var field in fields)
                {
                    var nestedType = ResolveNestedType(field.FieldType);
                    result.Add(new CommandFieldInfo
                    {
                        name = field.Name,
                        type = ToSimpleTypeName(field.FieldType),
                        defaultValue = GetDefaultValueString(field),
                        children = nestedType == null
                            ? Array.Empty<CommandFieldInfo>()
                            : ExtractFieldInfos(nestedType, visitingTypes)
                    });
                }
            }
            finally
            {
                visitingTypes.Remove(type);
            }

            return result.ToArray();
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
