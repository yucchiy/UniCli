using System.Collections.Generic;
using System.Linq;
using UniCli.Server.Editor.Handlers;
using UnityEditor;

namespace UniCli.Server.Editor
{
    [FilePath("ProjectSettings/UniCliSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UniCliSettings : ScriptableSingleton<UniCliSettings>
    {
        public List<string> enabledModules = new(ModuleRegistry.DefaultModules);
        public List<string> disabledModules = new();

        public bool IsModuleEnabled(string name)
        {
            if (disabledModules.Contains(name))
                return false;
            if (enabledModules.Contains(name))
                return true;

            // Unknown module (user-defined) — enabled by default
            return !IsRegisteredModule(name);
        }

        public void EnableModule(string name)
        {
            disabledModules.Remove(name);
            if (!enabledModules.Contains(name))
            {
                enabledModules.Add(name);
                Save(true);
            }
            else
            {
                Save(true);
            }
        }

        public void DisableModule(string name)
        {
            enabledModules.Remove(name);
            if (!disabledModules.Contains(name))
            {
                disabledModules.Add(name);
                Save(true);
            }
            else
            {
                Save(true);
            }
        }

        private static bool IsRegisteredModule(string name)
        {
            foreach (var m in ModuleRegistry.All)
            {
                if (m.Name == name)
                    return true;
            }
            return false;
        }

        public static string[] DiscoverAllModuleNames()
        {
            var names = new HashSet<string>();
            foreach (var m in ModuleRegistry.All)
                names.Add(m.Name);

            var handlerTypes = TypeCache.GetTypesDerivedFrom<ICommandHandler>();
            foreach (var type in handlerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var moduleName = ModuleRegistry.ResolveModuleName(type);
                if (moduleName != null)
                    names.Add(moduleName);
            }

            var sorted = names.ToList();
            sorted.Sort();
            return sorted.ToArray();
        }
    }
}
