using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace UniCli.Client
{
    public static class ProjectIdentifier
    {
        public static string GetProjectHash(string projectPath)
        {
            using var sha256 = SHA256.Create();

            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(projectPath));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return hash.Substring(0, 8);
        }

        /// <summary>
        /// Generates a project-specific pipe name.
        /// Normalizes the path to match Unity's Application.dataPath format
        /// (absolute path to the Assets directory).
        /// </summary>
        public static string GetPipeName(string projectPath)
        {
            var normalized = NormalizeToDataPath(projectPath);
            return $"unicli-{GetProjectHash(normalized)}";
        }

        /// <summary>
        /// Normalizes a project path to match Unity's Application.dataPath
        /// (absolute path ending with /Assets).
        /// </summary>
        internal static string NormalizeToDataPath(string projectPath)
        {
            var fullPath = Path.GetFullPath(projectPath);
            var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (Path.GetFileName(trimmed) != "Assets")
            {
                fullPath = Path.Combine(trimmed, "Assets");
            }

            return NormalizePathForHash(fullPath);
        }

        internal static string NormalizePathForHash(string path)
        {
            path = path.Replace('\\', '/');

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                path = path.ToLowerInvariant();

            return path;
        }

        /// <summary>
        /// Detects the Unity project root from the current directory
        /// </summary>
        public static string? FindUnityProjectRoot(string? startPath = null)
        {
            var currentDir = startPath ?? Directory.GetCurrentDirectory();

            // Traverse up to 10 parent directories
            for (int i = 0; i < 10; i++)
            {
                var assetsDir = Path.Combine(currentDir, "Assets");
                if (Directory.Exists(assetsDir))
                {
                    // Unity's dataPath points to the Assets directory
                    return assetsDir;
                }

                var parentDir = Directory.GetParent(currentDir);
                if (parentDir == null)
                {
                    break;
                }

                currentDir = parentDir.FullName;
            }

            return null;
        }
    }
}
