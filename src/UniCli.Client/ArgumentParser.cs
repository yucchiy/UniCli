using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using UniCli.Protocol;

namespace UniCli.Client;

internal static class ArgumentParser
{
    public static Dictionary<string, List<string?>> ParseKeyValueArgs(string[] args)
    {
        var result = new Dictionary<string, List<string?>>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--"))
                continue;

            var key = args[i].Substring(2);

            if (!result.TryGetValue(key, out var list))
            {
                list = new List<string?>();
                result[key] = list;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                list.Add(args[i + 1]);
                i++;
            }
            else
            {
                list.Add(null);
            }
        }

        return result;
    }

    public static bool IsArrayType(string fieldType) => fieldType.EndsWith("[]");

    public static string GetArrayElementType(string fieldType) => fieldType.Substring(0, fieldType.Length - 2);

    public static string BuildJsonFromKeyValues(Dictionary<string, List<string?>> pairs, CommandFieldInfo[] fields)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();

        foreach (var (key, values) in pairs)
        {
            var field = fields.FirstOrDefault(
                f => f.name.Equals(key, StringComparison.OrdinalIgnoreCase));
            var fieldName = field?.name ?? key;
            var fieldType = field?.type ?? "string";

            writer.WritePropertyName(fieldName);

            if (IsArrayType(fieldType))
            {
                var elementType = GetArrayElementType(fieldType);
                writer.WriteStartArray();
                foreach (var value in values)
                {
                    if (value != null)
                        WriteScalarValue(writer, value, elementType);
                }
                writer.WriteEndArray();
            }
            else
            {
                var value = values[values.Count - 1];
                if (value == null)
                    writer.WriteBooleanValue(true);
                else
                    WriteScalarValue(writer, value, fieldType);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static void WriteScalarValue(Utf8JsonWriter writer, string value, string fieldType)
    {
        switch (fieldType)
        {
            case "int" when int.TryParse(value, out var intVal):
                writer.WriteNumberValue(intVal);
                break;
            case "float" when float.TryParse(value, out var floatVal):
                writer.WriteNumberValue(floatVal);
                break;
            case "double" when double.TryParse(value, out var doubleVal):
                writer.WriteNumberValue(doubleVal);
                break;
            case "bool" when bool.TryParse(value, out var boolVal):
                writer.WriteBooleanValue(boolVal);
                break;
            default:
                writer.WriteStringValue(value);
                break;
        }
    }

    public static bool IsRetryableError(string error)
    {
        return error.StartsWith("Server closed connection", StringComparison.Ordinal)
            || error.StartsWith("Communication error", StringComparison.Ordinal);
    }
}
