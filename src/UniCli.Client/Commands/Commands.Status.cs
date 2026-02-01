using System;
using System.Buffers;
using System.Text.Json;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Client;

public partial class Commands
{
    /// <summary>
    /// Show the connection status of the Unity Editor server
    /// </summary>
    public async Task<int> Status(bool json = false, bool noFocus = false)
    {
        var explicitPath = Environment.GetEnvironmentVariable("UNICLI_PROJECT");
        var projectRoot = explicitPath ?? ProjectIdentifier.FindUnityProjectRoot();

        if (projectRoot == null)
        {
            var result = BuildStatusResult(false, null, null, "Unity project not found", 0, 0);
            return OutputWriter.Write(result, json);
        }

        var pipeName = ProjectIdentifier.GetPipeName(projectRoot);

        await using var focus = UnityProcessActivator.ShouldFocus(noFocus)
            ? await FocusScope.ActivateAsync(pipeName)
            : FocusScope.Noop;

        int commandCount;
        {
            using var client = new PipeClient(pipeName);
            var connectResult = await client.ConnectAsync(timeoutMs: 2000);

            if (connectResult.IsError)
            {
                var result = BuildStatusResult(false, projectRoot, pipeName, "Server is not running", 0, 0);
                return OutputWriter.Write(result, json);
            }

            var listRequest = new CommandRequest
            {
                command = "Commands.List",
                data = ""
            };

            var sendResult = await client.SendCommandAsync(listRequest, timeoutMs: 2000);

            var listResult = sendResult.Match(
                onSuccess: response =>
                {
                    if (!response.success || string.IsNullOrEmpty(response.data))
                        return 0;

                    var listResponse = JsonSerializer.Deserialize(
                        response.data, ProtocolJsonContext.Default.CommandListResponse);
                    return listResponse?.commands?.Length ?? 0;
                },
                onError: _ => -1);

            if (listResult < 0)
            {
                var result = BuildStatusResult(false, projectRoot, pipeName, "Server is not running", 0, 0);
                return OutputWriter.Write(result, json);
            }

            commandCount = listResult;
        }

        var processId = await GetProcessIdAsync(pipeName);
        var cliResult = BuildStatusResult(true, projectRoot, pipeName, null, commandCount, processId);
        return OutputWriter.Write(cliResult, json);
    }

    private static async Task<int> GetProcessIdAsync(string pipeName)
    {
        try
        {
            using var client = new PipeClient(pipeName);
            var connectResult = await client.ConnectAsync(timeoutMs: 2000);
            if (connectResult.IsError)
                return 0;

            var request = new CommandRequest
            {
                command = "Project.Inspect",
                data = "",
                format = "json"
            };

            var result = await client.SendCommandAsync(request, timeoutMs: 2000);
            if (result.IsError)
                return 0;

            var response = result.Match(
                onSuccess: r => r,
                onError: _ => (CommandResponse?)null);

            if (response == null || !response.success || string.IsNullOrEmpty(response.data))
                return 0;

            using var doc = JsonDocument.Parse(response.data);
            if (!doc.RootElement.TryGetProperty("processId", out var pidElement))
                return 0;

            return pidElement.GetInt32();
        }
        catch
        {
            return 0;
        }
    }

    private static CliResult BuildStatusResult(
        bool running, string? project, string? pipe, string? error, int commandCount, int processId)
    {
        var jsonData = BuildStatusJson(running, project, pipe, error, commandCount, processId);
        var formattedText = BuildStatusText(running, project, pipe, error, commandCount, processId);

        return running
            ? CliResult.Ok("Server is running", jsonData, formattedText)
            : new CliResult
            {
                Success = false,
                Message = error ?? "Server is not running",
                JsonData = jsonData,
                FormattedText = formattedText
            };
    }

    private static string BuildStatusText(
        bool running, string? project, string? pipe, string? error, int commandCount, int processId)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Project: {project ?? "not found"}");

        if (pipe != null)
            sb.AppendLine($"Pipe:    {pipe}");

        if (running)
        {
            sb.AppendLine($"Server:  running ({commandCount} commands available)");
            if (processId > 0)
                sb.Append($"PID:     {processId}");
            else
                sb.Length -= Environment.NewLine.Length;
        }
        else
        {
            sb.Append($"Server:  not running");
        }

        return sb.ToString();
    }

    private static string BuildStatusJson(
        bool running, string? project, string? pipe, string? error, int commandCount, int processId)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();
        writer.WriteBoolean("running", running);

        if (project != null)
            writer.WriteString("project", project);
        else
            writer.WriteNull("project");

        if (pipe != null)
            writer.WriteString("pipe", pipe);
        else
            writer.WriteNull("pipe");

        if (running)
        {
            writer.WriteNumber("commandCount", commandCount);
            if (processId > 0)
                writer.WriteNumber("processId", processId);
        }

        if (error != null)
            writer.WriteString("error", error);

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
