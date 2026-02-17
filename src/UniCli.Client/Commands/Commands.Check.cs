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
        string? serverVersion = null;
        if (installed)
        {
            (serverRunning, serverVersion) = await ProbeServerAsync(projectRoot);
        }

        var clientVersion = VersionInfo.Version;
        var versionMatch = serverVersion != null && serverVersion == clientVersion;

        var cliResult = BuildCheckResult(installed, source, serverRunning, clientVersion, serverVersion, versionMatch);
        return OutputWriter.Write(cliResult, json);
    }

    private static async Task<(bool running, string? serverVersion)> ProbeServerAsync(string projectRoot)
    {
        var pipeName = ProjectIdentifier.GetPipeName(projectRoot);

        using var client = new PipeClient(pipeName);
        var connectResult = await client.ConnectAsync(timeoutMs: 2000);
        if (connectResult.IsError)
            return (false, null);

        var request = new CommandRequest
        {
            command = "Project.Inspect",
            data = "",
            format = "json"
        };

        var sendResult = await client.SendCommandAsync(request, timeoutMs: 2000);
        return sendResult.Match(
            onSuccess: response =>
            {
                if (!response.success)
                    return (false, (string?)null);

                string? version = null;
                if (!string.IsNullOrEmpty(response.data))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(response.data);
                        if (doc.RootElement.TryGetProperty("serverVersion", out var versionEl))
                            version = versionEl.GetString();
                    }
                    catch
                    {
                        // ignore parse failures
                    }
                }

                return (true, version);
            },
            onError: _ => (false, null));
    }

    private static CliResult BuildCheckResult(
        bool installed, string? source, bool serverRunning,
        string clientVersion, string? serverVersion, bool versionMatch)
    {
        var jsonData = BuildCheckJson(installed, source, serverRunning, clientVersion, serverVersion, versionMatch);
        var formattedText = BuildCheckText(installed, source, serverRunning, clientVersion, serverVersion, versionMatch);
        var message = installed ? "Package is installed" : "Package is not installed";

        return CliResult.Ok(message, jsonData, formattedText);
    }

    private static string BuildCheckJson(
        bool installed, string? source, bool serverRunning,
        string clientVersion, string? serverVersion, bool versionMatch)
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
        writer.WriteString("clientVersion", clientVersion);

        if (serverVersion != null)
            writer.WriteString("serverVersion", serverVersion);
        else
            writer.WriteNull("serverVersion");

        writer.WriteBoolean("versionMatch", versionMatch);
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string BuildCheckText(
        bool installed, string? source, bool serverRunning,
        string clientVersion, string? serverVersion, bool versionMatch)
    {
        var sb = new System.Text.StringBuilder();

        if (installed)
            sb.AppendLine($"Package: installed ({source})");
        else
            sb.AppendLine("Package: not installed");

        sb.AppendLine($"Server:  {(serverRunning ? "running" : "not running")}");
        sb.Append($"Client Version: {clientVersion}");

        if (serverVersion != null)
        {
            sb.AppendLine();
            sb.Append($"Server Version: {serverVersion}");
            if (!versionMatch)
                sb.Append(" (mismatch! run 'unicli install --update')");
        }
        else if (serverRunning)
        {
            sb.AppendLine();
            sb.Append("Server Version: unknown");
        }

        return sb.ToString();
    }
}
