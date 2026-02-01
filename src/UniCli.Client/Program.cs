using ConsoleAppFramework;
using System;
using System.Collections.Generic;
using System.Linq;
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

        var app = ConsoleApp.Create();
        app.Add<Commands>();
        await app.RunAsync(args);
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
}
