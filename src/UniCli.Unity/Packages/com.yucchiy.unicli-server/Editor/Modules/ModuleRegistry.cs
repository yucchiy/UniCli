using System;
using System.Reflection;

namespace UniCli.Server.Editor
{
    public static class ModuleRegistry
    {
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
            new ModuleDefinition("Recorder", "Video recording operations (requires com.unity.recorder)"),
            new ModuleDefinition("NuGet", "NuGet package management (requires NuGetForUnity)"),
        };

        /// <summary>
        /// Resolve the module name for a handler type.
        /// Returns the [Module("X")] value if present, or null for core/user commands (always enabled).
        /// </summary>
        public static string ResolveModuleName(Type handlerType)
        {
            var attr = handlerType.GetCustomAttribute<ModuleAttribute>();
            return attr?.Name;
        }
    }
}
