using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleAppFramework;

namespace UniCli.Client;

public partial class Commands
{
    private const string DefaultGitUrl =
        "https://github.com/yucchiy/UniCli.git?path=src/UniCli.Unity/Packages/com.yucchiy.unicli-server";

    /// <summary>
    /// Install the UniCli server package into a Unity project
    /// </summary>
    [Command("install")]
    public Task<int> Install(string source = DefaultGitUrl, bool json = false)
    {
        var explicitPath = Environment.GetEnvironmentVariable("UNICLI_PROJECT");
        var projectRoot = explicitPath ?? ProjectIdentifier.FindUnityProjectRoot();

        if (projectRoot == null)
        {
            var result = CliResult.Error("Unity project not found.");
            return Task.FromResult(OutputWriter.Write(result, json));
        }

        var manifestPath = ManifestEditor.GetManifestPath(projectRoot);

        if (!File.Exists(manifestPath))
        {
            var result = CliResult.Error($"manifest.json not found: {manifestPath}");
            return Task.FromResult(OutputWriter.Write(result, json));
        }

        bool added;
        try
        {
            added = ManifestEditor.AddPackage(manifestPath, source);
        }
        catch (Exception ex)
        {
            var result = CliResult.Error($"Failed to update manifest.json: {ex.Message}");
            return Task.FromResult(OutputWriter.Write(result, json));
        }

        var cliResult = BuildInstallResult(added, source);
        return Task.FromResult(OutputWriter.Write(cliResult, json));
    }

    private static CliResult BuildInstallResult(bool added, string source)
    {
        var jsonData = BuildInstallJson(added, source);
        var message = added
            ? $"Installed {ManifestEditor.PackageName}"
            : $"{ManifestEditor.PackageName} is already installed";
        var formattedText = added
            ? $"Installed {ManifestEditor.PackageName} from {source}"
            : $"{ManifestEditor.PackageName} is already installed";

        return CliResult.Ok(message, jsonData, formattedText);
    }

    private static string BuildInstallJson(bool added, string source)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();
        writer.WriteBoolean("added", added);
        writer.WriteString("package", ManifestEditor.PackageName);
        writer.WriteString("source", source);
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
