using System;
using System.Buffers;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleAppFramework;
using UniCli.Protocol;

namespace UniCli.Client;

public partial class Commands
{
    /// <summary>
    /// Check if the UniCli server package is installed and the server is running
    /// </summary>
    [Command("check")]
    public async Task<int> Check(bool json = false)
    {
        var explicitPath = Environment.GetEnvironmentVariable("UNICLI_PROJECT");
        var projectRoot = explicitPath ?? ProjectIdentifier.FindUnityProjectRoot();

        if (projectRoot == null)
        {
            var result = CliResult.Error("Unity project not found.");
            return OutputWriter.Write(result, json);
        }

        var manifestPath = ManifestEditor.GetManifestPath(projectRoot);
        var source = ManifestEditor.FindPackageSource(manifestPath);
        var installed = source != null;

        var serverRunning = false;
        if (installed)
        {
            serverRunning = await ProbeServerAsync(projectRoot);
        }

        var cliResult = BuildCheckResult(installed, source, serverRunning);
        return OutputWriter.Write(cliResult, json);
    }

    private static async Task<bool> ProbeServerAsync(string projectRoot)
    {
        var pipeName = ProjectIdentifier.GetPipeName(projectRoot);

        using var client = new PipeClient(pipeName);
        var connectResult = await client.ConnectAsync(timeoutMs: 2000);
        if (connectResult.IsError)
            return false;

        var request = new CommandRequest
        {
            command = "Commands.List",
            data = ""
        };

        var sendResult = await client.SendCommandAsync(request, timeoutMs: 2000);
        return sendResult.Match(
            onSuccess: response => response.success,
            onError: _ => false);
    }

    private static CliResult BuildCheckResult(bool installed, string? source, bool serverRunning)
    {
        var jsonData = BuildCheckJson(installed, source, serverRunning);
        var formattedText = BuildCheckText(installed, source, serverRunning);
        var message = installed ? "Package is installed" : "Package is not installed";

        return CliResult.Ok(message, jsonData, formattedText);
    }

    private static string BuildCheckJson(bool installed, string? source, bool serverRunning)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();
        writer.WriteBoolean("installed", installed);

        if (source != null)
            writer.WriteString("source", source);
        else
            writer.WriteNull("source");

        writer.WriteBoolean("serverRunning", serverRunning);
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string BuildCheckText(bool installed, string? source, bool serverRunning)
    {
        var sb = new System.Text.StringBuilder();

        if (installed)
            sb.AppendLine($"Package: installed ({source})");
        else
            sb.AppendLine("Package: not installed");

        sb.Append($"Server:  {(serverRunning ? "running" : "not running")}");

        return sb.ToString();
    }
}
