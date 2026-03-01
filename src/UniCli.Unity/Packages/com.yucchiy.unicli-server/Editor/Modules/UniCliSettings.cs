using System;
using System.Collections.Generic;
using System.Linq;
using UniCli.Remote;
using UniCli.Server.Editor.Handlers;
using UnityEditor;

namespace UniCli.Server.Editor
{
    public class UniCliSettings
    {
        const string DisabledModulesConfigKey = "UniCli.disabledModules";
        const string EditorLoggingEnabledConfigKey = "UniCli.editor.logging.enabled";
        const string RemoteDiscoveryLogConfigKey = "UniCli.remote.logCommandDiscovery";

        HashSet<string> LoadDisabledModules()
        {
            var raw = EditorUserSettings.GetConfigValue(DisabledModulesConfigKey);
            return string.IsNullOrEmpty(raw)
                ? new HashSet<string>()
                : new HashSet<string>(raw.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        void SaveDisabledModules(HashSet<string> modules)
        {
            var value = modules.Count > 0 ? string.Join(",", modules) : "";
            EditorUserSettings.SetConfigValue(DisabledModulesConfigKey, value);
        }

        public bool IsEditorLoggingEnabled()
        {
            var raw = EditorUserSettings.GetConfigValue(EditorLoggingEnabledConfigKey);
            if (string.IsNullOrEmpty(raw))
            {
                // Backward compatibility with the previous remote-only setting key.
                raw = EditorUserSettings.GetConfigValue(RemoteDiscoveryLogConfigKey);
            }

            return ParseEnabledFlag(raw, defaultValue: true);
        }

        public void SetEditorLoggingEnabled(bool enabled)
        {
            var value = enabled ? "1" : "0";
            EditorUserSettings.SetConfigValue(EditorLoggingEnabledConfigKey, value);
            EditorUserSettings.SetConfigValue(RemoteDiscoveryLogConfigKey, value);

            UniCliEditorLog.EnableLogs = enabled;
            DebugCommandRegistry.EnableLogs = enabled;
        }

        private static bool ParseEnabledFlag(string raw, bool defaultValue)
        {
            if (string.IsNullOrEmpty(raw))
                return defaultValue;

            return raw switch
            {
                "1" => true,
                "0" => false,
                _ => bool.TryParse(raw, out var enabled) ? enabled : defaultValue
            };
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
