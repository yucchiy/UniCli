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
            if (type == typeof(Unit))
                return Array.Empty<CommandFieldInfo>();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var result = new List<CommandFieldInfo>(fields.Length);

            foreach (var field in fields)
            {
                result.Add(new CommandFieldInfo
                {
                    name = field.Name,
                    type = ToSimpleTypeName(field.FieldType),
                    defaultValue = GetDefaultValueString(field)
                });
            }

            return result.ToArray();
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
