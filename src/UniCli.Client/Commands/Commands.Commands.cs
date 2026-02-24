using ConsoleAppFramework;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Client;

public partial class Commands
{
    /// <summary>
    /// List all available commands from the Unity Editor server
    /// </summary>
    [Command("commands")]
    public async Task<int> ListCommands(bool json = false, bool noFocus = false)
    {
        var focus = UnityProcessActivator.ShouldFocus(noFocus);
        var sendResult = await CommandExecutor.SendAsync("Commands.List", "", focusEditor: focus);

        var cliResult = sendResult.Match(
            onSuccess: response =>
            {
                if (!response.success)
                    return CliResult.Error(response.message);

                if (string.IsNullOrEmpty(response.data))
                    return CliResult.Ok("No commands available.", "[]", "No commands available.");

                var listResponse = JsonSerializer.Deserialize(
                    response.data, ProtocolJsonContext.Default.CommandListResponse);
                if (listResponse?.commands == null || listResponse.commands.Length == 0)
                    return CliResult.Ok("No commands available.", "[]", "No commands available.");

                var projectRoot = Environment.GetEnvironmentVariable("UNICLI_PROJECT")
                    ?? ProjectIdentifier.FindUnityProjectRoot();
                if (projectRoot != null)
                    CompletionCache.Save(projectRoot, listResponse.commands);

                var commands = listResponse.commands
                    .OrderBy(c => c.name)
                    .ToArray();

                var jsonData = JsonSerializer.Serialize(
                    commands, ProtocolJsonContext.Default.CommandInfoArray);
                var formattedText = FormatCommandsTable(commands);

                return CliResult.Ok(
                    $"{commands.Length} commands available",
                    jsonData,
                    formattedText);
            },
            onError: CliResult.Error);

        return OutputWriter.Write(cliResult, json);
    }

    private static string FormatCommandsTable(CommandInfo[] commands)
    {
        var maxNameLen = commands.Max(c => c.name.Length);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Available commands ({commands.Length}):");
        sb.AppendLine();

        foreach (var cmd in commands)
        {
            var paddedName = cmd.name.PadRight(maxNameLen);
            var modulePart = $" [{(string.IsNullOrEmpty(cmd.module) ? "Core" : cmd.module)}]";
            sb.AppendLine($"  {paddedName}  {cmd.description}{modulePart}");

            if (cmd.requestFields.Length > 0)
            {
                foreach (var field in cmd.requestFields)
                {
                    var defaultPart = string.IsNullOrEmpty(field.defaultValue)
                        ? ""
                        : $" = {field.defaultValue}";
                    sb.AppendLine($"  {"".PadRight(maxNameLen)}    {field.name} ({field.type}{defaultPart})");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}
