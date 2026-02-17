using ConsoleAppFramework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UniCli.Client;

internal sealed class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "exec")
        {
            var commandName = args[1];
            var remaining = args.Skip(2).ToArray();

            if (remaining.Contains("--help"))
            {
                Environment.ExitCode = await CommandExecutor.PrintCommandHelpAsync(commandName);
                return;
            }

            var timeoutMs = ExtractTimeout(ref remaining);
            var jsonFlag = ExtractFlag(ref remaining, "--json");
            var noFocusFlag = ExtractFlag(ref remaining, "--no-focus");
            var focusEditor = UnityProcessActivator.ShouldFocus(noFocusFlag);

            CliResult result;
            if (remaining.Any(a => a.StartsWith("--")))
            {
                result = await CommandExecutor.ExecuteWithKeyValueAsync(
                    commandName, remaining, timeoutMs, jsonFlag, focusEditor);
            }
            else
            {
                var data = remaining.Length > 0 ? remaining[0] : "";
                result = await CommandExecutor.ExecuteAsync(
                    commandName, data, timeoutMs, jsonFlag, focusEditor);
            }

            Environment.ExitCode = OutputWriter.Write(result, jsonFlag);
            return;
        }

        if (args.Length >= 1 && args[0] == "eval")
        {
            Environment.ExitCode = await RunEvalAsync(args.Skip(1).ToArray());
            return;
        }

        var app = ConsoleApp.Create();
        app.Add<Commands>();
        await app.RunAsync(args);
    }

    private static async Task<int> RunEvalAsync(string[] args)
    {
        var timeoutMs = ExtractTimeout(ref args);
        var jsonFlag = ExtractFlag(ref args, "--json");
        var noFocusFlag = ExtractFlag(ref args, "--no-focus");
        var declarations = ExtractValue(ref args, "--declarations");

        string code;
        if (args.Length > 0 && !args[0].StartsWith("--"))
        {
            code = args[0];
        }
        else
        {
            Console.Error.WriteLine("Error: code argument is required");
            Console.Error.WriteLine("Usage: unicli eval '<code>' [--json] [--declarations '<decl>']");
            return 1;
        }

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("code", code);
            if (declarations != null)
                writer.WriteString("declarations", declarations);
            writer.WriteEndObject();
        }

        var requestJson = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var focusEditor = UnityProcessActivator.ShouldFocus(noFocusFlag);
        var result = await CommandExecutor.ExecuteAsync("Eval", requestJson, timeoutMs, jsonFlag, focusEditor);
        return OutputWriter.Write(result, jsonFlag);
    }

    private static int ExtractTimeout(ref string[] args)
    {
        var timeoutMs = 0;
        var remaining = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--timeout" && i + 1 < args.Length && int.TryParse(args[i + 1], out var value))
            {
                timeoutMs = value;
                i++;
            }
            else
            {
                remaining.Add(args[i]);
            }
        }

        args = remaining.ToArray();
        return timeoutMs;
    }

    private static bool ExtractFlag(ref string[] args, string flag)
    {
        var found = args.Contains(flag);
        if (found)
            args = args.Where(a => a != flag).ToArray();
        return found;
    }

    private static string ExtractValue(ref string[] args, string flag)
    {
        string value = null;
        var remaining = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == flag && i + 1 < args.Length)
            {
                value = args[i + 1];
                i++;
            }
            else
            {
                remaining.Add(args[i]);
            }
        }

        args = remaining.ToArray();
        return value;
    }
}
