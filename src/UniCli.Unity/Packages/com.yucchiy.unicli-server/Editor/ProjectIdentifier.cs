using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace UniCli.Server.Editor
{
    public static class ProjectIdentifier
    {
        private static string GetProjectHash()
        {
            var projectPath = NormalizePathForHash(Application.dataPath);

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(projectPath));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            return hash.Substring(0, 8);
        }

        internal static string NormalizePathForHash(string path)
        {
            path = path.Replace('\\', '/');

            if (Application.platform == RuntimePlatform.WindowsEditor)
                path = path.ToLowerInvariant();

            return path;
        }

        public static string GetPipeName()
        {
            return $"unicli-{GetProjectHash()}";
        }
    }
}
