using System;
using System.IO;
using System.Text.Json;
using UniCli.Protocol;

namespace UniCli.Client;

internal static class CompletionCache
{
    public static string GetCacheDir(string projectRoot)
    {
        var hash = ProjectIdentifier.GetProjectHash(projectRoot);
        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "unicli", hash);
        return cacheDir;
    }

    public static void Save(string projectRoot, CommandInfo[] commands)
    {
        var cacheDir = GetCacheDir(projectRoot);
        Directory.CreateDirectory(cacheDir);

        var cachePath = Path.Combine(cacheDir, "commands.json");
        var json = JsonSerializer.Serialize(commands, ProtocolJsonContext.Default.CommandInfoArray);
        File.WriteAllText(cachePath, json);
    }

    public static CommandInfo[]? Load(string projectRoot)
    {
        var cacheDir = GetCacheDir(projectRoot);
        var cachePath = Path.Combine(cacheDir, "commands.json");

        if (!File.Exists(cachePath))
            return null;

        var json = File.ReadAllText(cachePath);
        return JsonSerializer.Deserialize(json, ProtocolJsonContext.Default.CommandInfoArray);
    }
}
