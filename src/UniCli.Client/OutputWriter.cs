using System;
using System.Buffers;
using System.Text.Json;

namespace UniCli.Client;

internal static class OutputWriter
{
    public static int Write(CliResult result, bool json)
    {
        if (json)
            WriteJson(result);
        else
            WriteHuman(result);

        return result.Success ? 0 : 1;
    }

    private static void WriteJson(CliResult result)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteBoolean("success", result.Success);
        writer.WriteString("message", result.Message);

        var data = result.JsonData ?? result.FormattedText;
        if (data == null)
        {
            writer.WriteNull("data");
        }
        else if (result.JsonData != null)
        {
            writer.WritePropertyName("data");
            try
            {
                using var doc = JsonDocument.Parse(result.JsonData);
                doc.RootElement.WriteTo(writer);
            }
            catch
            {
                writer.WriteStringValue(result.JsonData);
            }
        }
        else
        {
            writer.WriteString("data", result.FormattedText);
        }

        writer.WriteEndObject();
        writer.Flush();

        using var stdout = Console.OpenStandardOutput();
        stdout.Write(buffer.WrittenSpan);
        stdout.Flush();
        Console.WriteLine();
    }

    private static void WriteHuman(CliResult result)
    {
        if (result.FormattedText != null)
        {
            if (result.Success)
                Console.WriteLine(result.FormattedText);
            else
                Console.Error.WriteLine(result.FormattedText);
            return;
        }

        if (!result.Success)
            Console.Error.WriteLine(result.Message);

        if (result.JsonData != null)
            WritePrettyJson(result.JsonData);
        else if (result.Success)
            Console.WriteLine(result.Message);
    }

    private static void WritePrettyJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true });
        doc.RootElement.WriteTo(writer);
        writer.Flush();

        using var stdout = Console.OpenStandardOutput();
        stdout.Write(buffer.WrittenSpan);
        stdout.Flush();
        Console.WriteLine();
    }
}
