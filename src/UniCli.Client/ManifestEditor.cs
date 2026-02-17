using System;
using System.IO;
using System.Text.Json.Nodes;

namespace UniCli.Client;

internal static class ManifestEditor
{
    public const string PackageName = "com.yucchiy.unicli-server";

    public static string GetManifestPath(string projectRoot)
    {
        var normalized = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (Path.GetFileName(normalized) == "Assets")
            normalized = Path.GetDirectoryName(normalized)!;

        return Path.Combine(normalized, "Packages", "manifest.json");
    }

    /// <summary>
    /// Returns the source string for the package from manifest.json, or null if not installed.
    /// </summary>
    public static string? FindPackageSource(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            return null;

        var text = File.ReadAllText(manifestPath);
        var root = JsonNode.Parse(text);
        var deps = root?["dependencies"];
        var value = deps?[PackageName];

        return value?.GetValue<string>();
    }

    /// <summary>
    /// Adds the package to manifest.json dependencies.
    /// Returns true if added, false if already present.
    /// </summary>
    public static bool AddPackage(string manifestPath, string source)
    {
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"manifest.json not found: {manifestPath}");

        var existing = FindPackageSource(manifestPath);
        if (existing != null)
            return false;

        var text = File.ReadAllText(manifestPath);
        var insertIndex = FindDependenciesInsertPosition(text);
        if (insertIndex < 0)
            throw new InvalidOperationException("Could not find \"dependencies\" block in manifest.json");

        var indent = DetectEntryIndent(text, insertIndex);
        var newEntry = $",{Environment.NewLine}{indent}\"{PackageName}\": \"{source}\"";

        var result = text.Insert(insertIndex, newEntry);
        File.WriteAllText(manifestPath, result);
        return true;
    }

    /// <summary>
    /// Updates the source URL for the package in manifest.json.
    /// Returns true if updated, false if the package is not installed.
    /// </summary>
    public static bool UpdatePackageSource(string manifestPath, string newSource)
    {
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"manifest.json not found: {manifestPath}");

        var oldSource = FindPackageSource(manifestPath);
        if (oldSource == null)
            return false;

        var text = File.ReadAllText(manifestPath);
        var oldEntry = $"\"{PackageName}\": \"{oldSource}\"";
        var newEntry = $"\"{PackageName}\": \"{newSource}\"";

        var index = text.IndexOf(oldEntry, StringComparison.Ordinal);
        if (index < 0)
            return false;

        var result = text.Remove(index, oldEntry.Length).Insert(index, newEntry);
        File.WriteAllText(manifestPath, result);
        return true;
    }

    /// <summary>
    /// Finds the position after the last entry value in the dependencies block
    /// (right after the closing quote of the last value, before any trailing whitespace/closing brace).
    /// </summary>
    private static int FindDependenciesInsertPosition(string text)
    {
        var depsKey = "\"dependencies\"";
        var depsStart = text.IndexOf(depsKey, StringComparison.Ordinal);
        if (depsStart < 0)
            return -1;

        var braceOpen = text.IndexOf('{', depsStart + depsKey.Length);
        if (braceOpen < 0)
            return -1;

        // Find the matching closing brace for the dependencies block
        var depth = 1;
        var braceClose = -1;
        for (var i = braceOpen + 1; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    braceClose = i;
                    break;
                }
            }
        }

        if (braceClose < 0)
            return -1;

        // Find the last quote before the closing brace (end of the last entry value)
        var lastQuote = text.LastIndexOf('"', braceClose - 1, braceClose - braceOpen);
        if (lastQuote < 0)
            return -1;

        return lastQuote + 1;
    }

    /// <summary>
    /// Detects the indentation of entries inside the dependencies block
    /// by looking at the line preceding the insert position.
    /// </summary>
    private static string DetectEntryIndent(string text, int positionInBlock)
    {
        // Walk back from the position to find the start of the current line
        var lineStart = positionInBlock;
        while (lineStart > 0 && text[lineStart - 1] != '\n')
            lineStart--;

        // Extract leading whitespace
        var indent = "";
        for (var i = lineStart; i < text.Length && (text[i] == ' ' || text[i] == '\t'); i++)
            indent += text[i];

        return indent.Length > 0 ? indent : "    ";
    }
}
