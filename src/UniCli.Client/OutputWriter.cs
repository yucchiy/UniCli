using System;
using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace UniCli.Client;

internal static class OutputWriter
{
    // The default JavaScriptEncoder escapes ' < > & " as \uXXXX for HTML-embedding
    // safety, which makes console logs and other Unity strings unreadable on stdout.
    // CLI output is never embedded in HTML, so use the relaxed encoder.
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

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
        WriteToStdout(RenderJson(result));
    }

    internal static byte[] RenderJson(CliResult result)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);

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

        return buffer.WrittenSpan.ToArray();
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
        WriteToStdout(RenderPrettyJson(json));
    }

    internal static byte[] RenderPrettyJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, WriterOptions);
        doc.RootElement.WriteTo(writer);
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteToStdout(byte[] bytes)
    {
        using var stdout = Console.OpenStandardOutput();
        stdout.Write(bytes);
        stdout.Flush();
        Console.WriteLine();
    }
}
