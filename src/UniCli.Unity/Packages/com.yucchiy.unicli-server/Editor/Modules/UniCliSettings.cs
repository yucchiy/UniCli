using System;
using System.Collections.Generic;
using System.Linq;
using UniCli.Server.Editor.Handlers;
using UnityEditor;

namespace UniCli.Server.Editor
{
    public class UniCliSettings
    {
        const string ConfigKey = "UniCli.disabledModules";

        HashSet<string> LoadDisabledModules()
        {
            var raw = EditorUserSettings.GetConfigValue(ConfigKey);
            return string.IsNullOrEmpty(raw)
                ? new HashSet<string>()
                : new HashSet<string>(raw.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        void SaveDisabledModules(HashSet<string> modules)
        {
            var value = modules.Count > 0 ? string.Join(",", modules) : "";
            EditorUserSettings.SetConfigValue(ConfigKey, value);
        }

        public bool IsModuleEnabled(string name)
        {
            return !LoadDisabledModules().Contains(name);
        }

        public void EnableModule(string name)
        {
            var modules = LoadDisabledModules();
            if (modules.Remove(name))
                SaveDisabledModules(modules);
        }

        public void DisableModule(string name)
        {
            var modules = LoadDisabledModules();
            if (modules.Add(name))
                SaveDisabledModules(modules);
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
