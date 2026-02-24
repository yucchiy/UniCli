using System.Collections.Generic;
using System.Linq;
using UniCli.Server.Editor.Handlers;
using UnityEditor;

namespace UniCli.Server.Editor
{
    [FilePath("ProjectSettings/UniCliSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UniCliSettings : ScriptableSingleton<UniCliSettings>
    {
        public List<string> disabledModules = new();

        public bool IsModuleEnabled(string name)
        {
            if (disabledModules.Contains(name))
                return false;

            // All modules are enabled by default (both registered and user-defined)
            return true;
        }

        public void EnableModule(string name)
        {
            if (disabledModules.Remove(name))
                Save(true);
        }

        public void DisableModule(string name)
        {
            if (!disabledModules.Contains(name))
            {
                disabledModules.Add(name);
                Save(true);
            }
        }

        public string[] DiscoverAllModuleNames()
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
