using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NugetForUnity.Configuration;
using NugetForUnity.PackageSource;

namespace UniCli.Server.Editor.Handlers.NuGetForUnity
{
    internal static class NuGetConfigHelper
    {
        private static readonly MethodInfo CreatePackageSourceMethod;

        static NuGetConfigHelper()
        {
            var creatorType = typeof(INugetPackageSource).Assembly
                .GetType("NugetForUnity.PackageSource.NugetPackageSourceCreator");

            CreatePackageSourceMethod = creatorType?.GetMethod(
                "CreatePackageSource",
                BindingFlags.Public | BindingFlags.Static);

            if (CreatePackageSourceMethod == null)
                throw new InvalidOperationException(
                    "NugetPackageSourceCreator.CreatePackageSource not found. NuGetForUnity version may be incompatible.");
        }

        public static void AddSource(string name, string path)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name is required");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path is required");

            var config = ConfigurationManager.NugetConfigFile;

            var existing = config.PackageSources
                .FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
                throw new InvalidOperationException($"Package source '{name}' already exists");

            var source = (INugetPackageSource)CreatePackageSourceMethod.Invoke(
                null,
                new object[] { name, path, null, config.PackageSources });

            config.PackageSources.Add(source);
            config.Save(ConfigurationManager.NugetConfigFilePath);
            ConfigurationManager.LoadNugetConfigFile();
        }

        public static void RemoveSource(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("name is required");

            var config = ConfigurationManager.NugetConfigFile;

            var source = config.PackageSources
                .FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

            if (source == null)
                throw new InvalidOperationException($"Package source '{name}' not found");

            config.PackageSources.Remove(source);
            config.Save(ConfigurationManager.NugetConfigFilePath);
            ConfigurationManager.LoadNugetConfigFile();
        }
    }
}
