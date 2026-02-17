using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Client;

internal static class CommandExecutor
{
    private const int DefaultMaxRetries = 5;
    private const int LaunchMaxRetries = 30;
    private const int RetryDelayMs = 1000;

    public static async Task<Result<CommandResponse, string>> SendAsync(
        string command, string data, int timeoutMs = 0, string format = "",
        int maxRetries = DefaultMaxRetries, bool focusEditor = false)
    {
        var explicitPath = Environment.GetEnvironmentVariable("UNICLI_PROJECT");
        var unityProjectRoot = explicitPath ?? ProjectIdentifier.FindUnityProjectRoot();

        if (unityProjectRoot == null)
            return Result<CommandResponse, string>.Error(
                "Unity project not found.\n  Run this command from within a Unity project directory,\n  or set UNICLI_PROJECT environment variable to specify the project path.");

        var pipeName = ProjectIdentifier.GetPipeName(unityProjectRoot);

        var request = new CommandRequest
        {
            command = command,
            data = data,
            format = format
        };

        string lastError = "";
        long focusSavedState = 0;
        var launchAttempted = false;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var client = new PipeClient(pipeName);

            var connectResult = await client.ConnectAsync();
            if (connectResult.IsError)
            {
                lastError = connectResult.ErrorValue;

                if (!launchAttempted && !UnityProcessActivator.IsUnityRunning(unityProjectRoot))
                {
                    launchAttempted = true;
                    Console.Error.WriteLine("Unity is not running, launching...");
                    var launchResult = UnityLauncher.Launch(unityProjectRoot);
                    if (launchResult.IsError)
                    {
                        await RestoreFocusAsync(focusSavedState);
                        return Result<CommandResponse, string>.Error(
                            $"Failed to launch Unity Editor: {launchResult.ErrorValue}");
                    }

                    maxRetries = LaunchMaxRetries;
                }

                focusSavedState = await TryFocusOnceAsync(
                    focusEditor, focusSavedState, unityProjectRoot);

                if (attempt < maxRetries)
                {
                    Console.Error.WriteLine(
                        $"Waiting for server... (attempt {attempt}/{maxRetries})");
                    await Task.Delay(RetryDelayMs);
                }
                continue;
            }

            var result = await client.SendCommandAsync(request, timeoutMs);
            if (result.IsSuccess)
            {
                await RestoreFocusAsync(focusSavedState);
                return result;
            }

            if (!IsRetryableError(result.ErrorValue))
            {
                await RestoreFocusAsync(focusSavedState);
                return result;
            }

            lastError = result.ErrorValue;
            focusSavedState = await TryFocusOnceAsync(
                focusEditor, focusSavedState, unityProjectRoot);

            if (attempt < maxRetries)
            {
                Console.Error.WriteLine(
                    $"Server disconnected, retrying... (attempt {attempt}/{maxRetries})");
                await Task.Delay(RetryDelayMs);
            }
        }

        await RestoreFocusAsync(focusSavedState);
        return Result<CommandResponse, string>.Error(
            $"Failed to communicate with Unity Editor server after {maxRetries} attempts.\n"
            + $"  Project: {unityProjectRoot}\n"
            + $"  Pipe: {pipeName}\n"
            + $"  Last error: {lastError}\n\n"
            + $"  Make sure:\n"
            + $"  - Unity Editor is running with the project open\n"
            + $"  - UniCli server package is installed and enabled");
    }

    private static bool IsRetryableError(string error)
    {
        return error.StartsWith("Server closed connection", StringComparison.Ordinal)
            || error.StartsWith("Communication error", StringComparison.Ordinal);
    }

    private static async Task<long> TryFocusOnceAsync(
        bool focusEditor, long savedState, string projectRoot)
    {
        if (!focusEditor || savedState != 0)
            return savedState;

        var pid = UnityProcessActivator.ReadPidFile(projectRoot);
        if (pid <= 0)
            return 0;

        Console.Error.WriteLine("Server not responding, focusing Unity Editor...");
        return await UnityProcessActivator.TryActivateAsync(projectRoot);
    }

    private static async Task RestoreFocusAsync(long savedState)
    {
        if (savedState != 0)
            await UnityProcessActivator.TryRestoreFocusAsync(savedState);
    }

    public static async Task<int> PrintCommandHelpAsync(string commandName)
    {
        var result = await SendAsync("Commands.List", "");

        return result.Match(
            onSuccess: response =>
            {
                if (!response.success || string.IsNullOrEmpty(response.data))
                {
                    Console.Error.WriteLine($"Failed to fetch command list: {response.message}");
                    return 1;
                }

                var listResponse = JsonSerializer.Deserialize(response.data, ProtocolJsonContext.Default.CommandListResponse);
                var cmd = listResponse?.commands?.FirstOrDefault(
                    c => c.name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

                if (cmd == null)
                {
                    Console.Error.WriteLine($"Unknown command: {commandName}");
                    return 1;
                }

                WriteCommandHelp(cmd);
                return 0;
            },
            onError: error =>
            {
                Console.Error.WriteLine(error);
                return 1;
            });
    }

    private static void WriteCommandHelp(CommandInfo cmd)
    {
        Console.WriteLine($"{cmd.name} - {cmd.description}");

        if (cmd.requestFields != null && cmd.requestFields.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Request parameters:");
            var maxNameLen = cmd.requestFields.Max(f => f.name.Length);
            var maxTypeLen = cmd.requestFields.Max(f => f.type.Length);

            foreach (var field in cmd.requestFields)
            {
                var defaultPart = string.IsNullOrEmpty(field.defaultValue)
                    ? ""
                    : $"  (default: {field.defaultValue})";
                Console.WriteLine($"  {field.name.PadRight(maxNameLen)}  {field.type.PadRight(maxTypeLen)}{defaultPart}");
            }
        }

        if (cmd.responseFields != null && cmd.responseFields.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Response fields:");
            var maxNameLen = cmd.responseFields.Max(f => f.name.Length);
            var maxTypeLen = cmd.responseFields.Max(f => f.type.Length);

            foreach (var field in cmd.responseFields)
            {
                Console.WriteLine($"  {field.name.PadRight(maxNameLen)}  {field.type.PadRight(maxTypeLen)}");
            }
        }
    }

    public static async Task<CliResult> ExecuteWithKeyValueAsync(
        string commandName, string[] args, int timeoutMs = 0, bool json = false,
        bool focusEditor = true)
    {
        var listResult = await SendAsync("Commands.List", "");
        if (listResult.IsError)
            return CliResult.Error(listResult.ErrorValue);

        var listResponse = listResult.Match(
            onSuccess: r =>
            {
                if (!r.success || string.IsNullOrEmpty(r.data)) return null;
                return JsonSerializer.Deserialize(r.data, ProtocolJsonContext.Default.CommandListResponse);
            },
            onError: _ => null);

        var cmd = listResponse?.commands?.FirstOrDefault(
            c => c.name.Equals(commandName, StringComparison.OrdinalIgnoreCase));

        var fields = cmd?.requestFields ?? Array.Empty<CommandFieldInfo>();
        var pairs = ParseKeyValueArgs(args);

        foreach (var key in pairs.Keys)
        {
            if (fields.All(f => !f.name.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                Console.Error.WriteLine($"Warning: unknown parameter '{key}' for command '{commandName}'");
            }
        }

        var jsonData = BuildJsonFromKeyValues(pairs, fields);
        return await ExecuteAsync(commandName, jsonData, timeoutMs, json, focusEditor);
    }

    private static Dictionary<string, List<string?>> ParseKeyValueArgs(string[] args)
    {
        var result = new Dictionary<string, List<string?>>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--"))
                continue;

            var key = args[i].Substring(2);

            if (!result.TryGetValue(key, out var list))
            {
                list = new List<string?>();
                result[key] = list;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                list.Add(args[i + 1]);
                i++;
            }
            else
            {
                list.Add(null);
            }
        }

        return result;
    }

    private static bool IsArrayType(string fieldType) => fieldType.EndsWith("[]");

    private static string GetArrayElementType(string fieldType) => fieldType.Substring(0, fieldType.Length - 2);

    private static string BuildJsonFromKeyValues(Dictionary<string, List<string?>> pairs, CommandFieldInfo[] fields)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();

        foreach (var (key, values) in pairs)
        {
            var field = fields.FirstOrDefault(
                f => f.name.Equals(key, StringComparison.OrdinalIgnoreCase));
            var fieldName = field?.name ?? key;
            var fieldType = field?.type ?? "string";

            writer.WritePropertyName(fieldName);

            if (IsArrayType(fieldType))
            {
                var elementType = GetArrayElementType(fieldType);
                writer.WriteStartArray();
                foreach (var value in values)
                {
                    if (value != null)
                        WriteScalarValue(writer, value, elementType);
                }
                writer.WriteEndArray();
            }
            else
            {
                var value = values[values.Count - 1];
                if (value == null)
                    writer.WriteBooleanValue(true);
                else
                    WriteScalarValue(writer, value, fieldType);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteScalarValue(Utf8JsonWriter writer, string value, string fieldType)
    {
        switch (fieldType)
        {
            case "int" when int.TryParse(value, out var intVal):
                writer.WriteNumberValue(intVal);
                break;
            case "float" when float.TryParse(value, out var floatVal):
                writer.WriteNumberValue(floatVal);
                break;
            case "double" when double.TryParse(value, out var doubleVal):
                writer.WriteNumberValue(doubleVal);
                break;
            case "bool" when bool.TryParse(value, out var boolVal):
                writer.WriteBooleanValue(boolVal);
                break;
            default:
                writer.WriteStringValue(value);
                break;
        }
    }

    public static async Task<CliResult> ExecuteAsync(
        string command, string data, int timeoutMs = 0, bool json = false,
        bool focusEditor = true)
    {
        var format = json ? "json" : "text";
        var result = await SendAsync(command, data, timeoutMs, format, focusEditor: focusEditor);

        return result.Match(
            onSuccess: CliResult.FromResponse,
            onError: CliResult.Error);
    }
}
