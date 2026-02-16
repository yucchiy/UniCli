using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace UniCli.Client;

internal static class UnityProcessActivator
{
    private static readonly IProcessActivator Activator = CreateActivator();

    private static IProcessActivator CreateActivator()
    {
        if (OperatingSystem.IsMacOS()) return new MacOSProcessActivator();
        if (OperatingSystem.IsWindows()) return new WindowsProcessActivator();
        return new NullProcessActivator();
    }

    public static bool ShouldFocus(bool noFocusFlag)
    {
        if (noFocusFlag)
            return false;

        var env = Environment.GetEnvironmentVariable("UNICLI_FOCUS");
        if (env != null)
            return env is "1" or "true";

        return true;
    }

    public static int ReadPidFile(string projectPath)
    {
        try
        {
            var root = StripAssetsSuffix(projectPath);
            var pidFilePath = Path.Combine(root, "Library", "UniCli", "server.pid");
            if (!File.Exists(pidFilePath))
                return 0;

            var content = File.ReadAllText(pidFilePath).Trim();
            return int.TryParse(content, out var pid) ? pid : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string StripAssetsSuffix(string path)
    {
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (Path.GetFileName(trimmed) == "Assets")
            return Path.GetDirectoryName(trimmed) ?? trimmed;
        return path;
    }

    public static bool IsUnityRunning(string projectRoot)
    {
        var pid = ReadPidFile(projectRoot);
        if (pid <= 0)
            return false;

        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static async Task<long> TryActivateAsync(string projectRoot)
    {
        try
        {
            var unityPid = ReadPidFile(projectRoot);
            if (unityPid <= 0)
                return 0;

            return await Activator.ActivateProcessAsync(unityPid);
        }
        catch
        {
            return 0;
        }
    }

    public static async Task TryRestoreFocusAsync(long savedState)
    {
        if (savedState == 0)
            return;

        try
        {
            await Activator.RestoreFocusAsync(savedState);
        }
        catch
        {
            // Best-effort: silently ignore all errors
        }
    }
}
