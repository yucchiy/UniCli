using UniCli.Protocol;

namespace UniCli.Client;

internal sealed class CliResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string? JsonData { get; init; }
    public string? FormattedText { get; init; }

    public static CliResult FromResponse(CommandResponse response)
    {
        var hasData = !string.IsNullOrEmpty(response.data);
        var isText = response.format == "text";

        return new CliResult
        {
            Success = response.success,
            Message = response.message,
            JsonData = hasData && !isText ? response.data : null,
            FormattedText = hasData && isText ? response.data : null
        };
    }

    public static CliResult Error(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static CliResult Ok(string message, string? jsonData = null, string? formattedText = null) => new()
    {
        Success = true,
        Message = message,
        JsonData = jsonData,
        FormattedText = formattedText
    };
}
