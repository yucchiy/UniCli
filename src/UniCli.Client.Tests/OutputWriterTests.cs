using System.Text;

namespace UniCli.Client.Tests;

public class OutputWriterTests
{
    // System.Text.Json's default encoder escapes these for HTML-embedding safety,
    // which renders Unity console logs unreadable (e.g. "it's obsolete").
    private const string Special = "it's <b>bold</b> & \"quoted\"";

    // `"` is still escaped as `\"` on output — that is JSON syntax, not the HTML encoder.
    private const string SpecialAsJsonContent = "it's <b>bold</b> & \\\"quoted\\\"";

    private static string Render(string json) =>
        Encoding.UTF8.GetString(OutputWriter.RenderPrettyJson(json));

    private static string RenderEnvelope(CliResult result) =>
        Encoding.UTF8.GetString(OutputWriter.RenderJson(result));

    [Fact]
    public void PrettyJsonDoesNotEscapeHtmlSensitiveCharacters()
    {
        var output = Render($$"""{"message":{{System.Text.Json.JsonSerializer.Serialize(Special)}}}""");

        Assert.Contains(SpecialAsJsonContent, output);
        Assert.DoesNotContain("\\u", output);
    }

    [Fact]
    public void PrettyJsonPreservesNonAsciiCharacters()
    {
        var output = Render("""{"message":"日本語"}""");

        Assert.Contains("日本語", output);
        Assert.DoesNotContain("\\u", output);
    }

    [Fact]
    public void JsonEnvelopeDoesNotEscapeHtmlSensitiveCharacters()
    {
        var result = CliResult.Ok("done", $$"""{"message":{{System.Text.Json.JsonSerializer.Serialize(Special)}}}""");

        var output = RenderEnvelope(result);

        Assert.Contains(SpecialAsJsonContent, output);
        Assert.DoesNotContain("\\u", output);
    }

    [Fact]
    public void JsonEnvelopeEscapesFormattedTextWithoutUnicodeEscapes()
    {
        var result = new CliResult { Success = true, Message = "done", FormattedText = Special };

        var output = RenderEnvelope(result);

        Assert.Contains(SpecialAsJsonContent, output);
        Assert.DoesNotContain("\\u", output);
    }
}
