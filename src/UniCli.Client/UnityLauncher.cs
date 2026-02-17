using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UniCli.Protocol;

namespace UniCli.Client;

internal static class UnityLauncher
{
    public static string? FindUnityEditorPath(string projectRoot)
    {
        var version = ReadEditorVersion(projectRoot);
        if (version == null)
            return null;

        string editorPath;
        if (OperatingSystem.IsMacOS())
        {
            editorPath = $"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity";
        }
        else if (OperatingSystem.IsWindows())
        {
            editorPath = $@"C:\Program Files\Unity\Hub\Editor\{version}\Editor\Unity.exe";
        }
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            editorPath = Path.Combine(home, "Unity", "Hub", "Editor", version, "Editor", "Unity");
        }
        else
        {
            return null;
        }

        return File.Exists(editorPath) ? editorPath : null;
    }

    public static Result<bool, string> Launch(string projectRoot)
    {
        var editorPath = FindUnityEditorPath(projectRoot);
        if (editorPath == null)
        {
            var version = ReadEditorVersion(projectRoot);
            var versionMsg = version != null
                ? $"Unity {version} is not installed at the expected Hub location."
                : "Could not read editor version from ProjectSettings/ProjectVersion.txt.";
            return Result<bool, string>.Error(versionMsg);
        }

        var normalizedRoot = NormalizeProjectRoot(projectRoot);

        Process.Start(new ProcessStartInfo
        {
            FileName = editorPath,
            Arguments = $"-projectPath \"{normalizedRoot}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });

        return Result<bool, string>.Success(true);
    }

    internal static string? ReadEditorVersion(string projectRoot)
    {
        var normalizedRoot = NormalizeProjectRoot(projectRoot);
        var versionFilePath = Path.Combine(normalizedRoot, "ProjectSettings", "ProjectVersion.txt");

        if (!File.Exists(versionFilePath))
            return null;

        try
        {
            foreach (var line in File.ReadLines(versionFilePath))
            {
                if (!line.StartsWith("m_EditorVersion:"))
                    continue;

                var colonIndex = line.IndexOf(':');
                if (colonIndex < 0)
                    continue;

                return line.Substring(colonIndex + 1).Trim();
            }
        }
        catch
        {
            // Best-effort: silently ignore read errors
        }

        return null;
    }

    private static string NormalizeProjectRoot(string projectRoot)
    {
        var trimmed = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (Path.GetFileName(trimmed) == "Assets")
            return Path.GetDirectoryName(trimmed) ?? trimmed;
        return trimmed;
    }
}
