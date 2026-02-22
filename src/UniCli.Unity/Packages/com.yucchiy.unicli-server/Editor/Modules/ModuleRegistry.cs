using System;
using System.Reflection;

namespace UniCli.Server.Editor
{
    public static class ModuleRegistry
    {
        public const string UserCommandsModule = "UserCommands";

        private static readonly Assembly ServerAssembly = typeof(ModuleRegistry).Assembly;

        public static readonly ModuleDefinition[] All = new[]
        {
            new ModuleDefinition("Scene", "Scene and GameObject operations"),
            new ModuleDefinition("Assets", "AssetDatabase, Prefab, Component, Selection, Material operations"),
            new ModuleDefinition("Build", "BuildPlayer, BuildProfile, TestRunner operations"),
            new ModuleDefinition("Profiler", "Profiler operations"),
            new ModuleDefinition("Animation", "Animator and AnimatorController operations"),
            new ModuleDefinition("Packages", "PackageManager and AssemblyDefinition operations"),
            new ModuleDefinition("Settings", "PlayerSettings, EditorSettings, EditorUserBuildSettings operations"),
            new ModuleDefinition("Remote", "Remote debug and Connection operations"),
            new ModuleDefinition("Window", "Window and Screenshot operations"),
        };

        public static readonly string[] DefaultModules = { "Scene", "Assets", "Build" };

        /// <summary>
        /// Resolve the module name for a handler type.
        /// - [Module("X")] → "X"
        /// - No attribute + server assembly → null (Core, always enabled)
        /// - No attribute + external assembly → UserCommandsModule
        /// </summary>
        public static string ResolveModuleName(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<ModuleAttribute>();
            if (attr != null)
                return attr.Name;

            if (handlerType.Assembly == ServerAssembly)
                return null;

            return UserCommandsModule;
        }
    }
}
