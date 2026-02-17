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

    private static string GetVersionedGitUrl() =>
        $"{DefaultGitUrl}#v{VersionInfo.Version}";

    /// <summary>
    /// Install the UniCli server package into a Unity project
    /// </summary>
    [Command("install")]
    public Task<int> Install(string source = "", bool update = false, bool json = false)
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

        var effectiveSource = string.IsNullOrEmpty(source) ? GetVersionedGitUrl() : source;

        if (update)
        {
            return Task.FromResult(HandleUpdate(manifestPath, effectiveSource, json));
        }

        bool added;
        try
        {
            added = ManifestEditor.AddPackage(manifestPath, effectiveSource);
        }
        catch (Exception ex)
        {
            var result = CliResult.Error($"Failed to update manifest.json: {ex.Message}");
            return Task.FromResult(OutputWriter.Write(result, json));
        }

        var cliResult = BuildInstallResult(added, effectiveSource);
        return Task.FromResult(OutputWriter.Write(cliResult, json));
    }

    private static int HandleUpdate(string manifestPath, string newSource, bool json)
    {
        try
        {
            var updated = ManifestEditor.UpdatePackageSource(manifestPath, newSource);
            if (!updated)
            {
                var result = CliResult.Error(
                    $"{ManifestEditor.PackageName} is not installed. Run 'unicli install' first.");
                return OutputWriter.Write(result, json);
            }

            var jsonData = BuildInstallJson(true, newSource);
            var cliResult = CliResult.Ok(
                $"Updated {ManifestEditor.PackageName}",
                jsonData,
                $"Updated {ManifestEditor.PackageName} source to {newSource}");
            return OutputWriter.Write(cliResult, json);
        }
        catch (Exception ex)
        {
            var result = CliResult.Error($"Failed to update manifest.json: {ex.Message}");
            return OutputWriter.Write(result, json);
        }
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
