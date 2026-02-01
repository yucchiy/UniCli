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
    /// Output completion candidates for shell completion
    /// </summary>
    public async Task<int> Complete([Argument] string line = "")
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Strip program name if present (COMP_LINE / $words includes it)
        if (parts.Length > 0 && !IsKnownSubcommand(parts[0]) && !IsSubcommandPrefix(parts[0]))
            parts = parts.Skip(1).ToArray();

        // "" or partial subcommand → subcommand candidates
        if (parts.Length == 0 || (parts.Length == 1 && !line.EndsWith(' ')))
        {
            var prefix = parts.Length > 0 ? parts[0] : "";
            foreach (var sub in SubcommandNames)
            {
                if (sub.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine(sub);
            }
            return 0;
        }

        var subcommand = parts[0];

        if (subcommand == "exec")
        {
            var commands = await GetCommandsAsync();
            if (commands == null) return 0;

            // "exec " → command name candidates
            if (parts.Length == 1 || (parts.Length == 2 && !line.EndsWith(' ')))
            {
                var prefix = parts.Length >= 2 ? parts[1] : "";
                foreach (var cmd in commands)
                {
                    if (cmd.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        Console.WriteLine(cmd.name);
                }
                return 0;
            }

            // "exec SomeCommand --" → parameter candidates
            if (parts.Length >= 2)
            {
                var cmdName = parts[1];
                var cmd = commands.FirstOrDefault(
                    c => c.name.Equals(cmdName, StringComparison.OrdinalIgnoreCase));

                if (cmd?.requestFields != null)
                {
                    var lastPart = parts[^1];
                    var isTypingParam = lastPart.StartsWith("--") && !line.EndsWith(' ');
                    var prefix = isTypingParam ? lastPart.Substring(2) : "";

                    foreach (var field in cmd.requestFields)
                    {
                        if (field.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            Console.WriteLine($"--{field.name}");
                    }
                }
                return 0;
            }
        }

        // "completions " → shell candidates
        if (subcommand == "completions")
        {
            var prefix = parts.Length >= 2 ? parts[1] : "";
            foreach (var shell in new[] { "bash", "zsh", "fish" })
            {
                if (shell.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine(shell);
            }
            return 0;
        }

        return 0;
    }

    private static readonly string[] SubcommandNames = ["exec", "commands", "status", "check", "install", "complete", "completions"];

    private static bool IsKnownSubcommand(string word)
    {
        return SubcommandNames.Any(s => s.Equals(word, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSubcommandPrefix(string word)
    {
        return SubcommandNames.Any(s => s.StartsWith(word, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<CommandInfo[]?> GetCommandsAsync()
    {
        var projectRoot = Environment.GetEnvironmentVariable("UNICLI_PROJECT")
            ?? ProjectIdentifier.FindUnityProjectRoot();

        if (projectRoot == null)
            return null;

        var result = await CommandExecutor.SendAsync(
            "Commands.List", "", timeoutMs: 500, maxRetries: 1);
        var serverCommands = result.Match(
            onSuccess: response =>
            {
                if (!response.success || string.IsNullOrEmpty(response.data)) return null;
                var listResponse = JsonSerializer.Deserialize(
                    response.data, ProtocolJsonContext.Default.CommandListResponse);
                return listResponse?.commands;
            },
            onError: _ => (CommandInfo[]?)null);

        if (serverCommands != null)
        {
            CompletionCache.Save(projectRoot, serverCommands);
            return serverCommands;
        }

        return CompletionCache.Load(projectRoot);
    }
}
