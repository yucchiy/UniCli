using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor
{
    public class UniCliSettingsProvider : SettingsProvider
    {
        public UniCliSettingsProvider()
            : base("Project/UniCli", SettingsScope.Project)
        {
        }

        [SettingsProvider]
        public static SettingsProvider Create() => new UniCliSettingsProvider();

        public override void OnGUI(string searchContext)
        {
            var settings = UniCliSettings.instance;
            var allNames = UniCliSettings.DiscoverAllModuleNames();

            var registeredDescriptions = new Dictionary<string, string>();
            foreach (var m in ModuleRegistry.All)
                registeredDescriptions[m.Name] = m.Description;

            EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enable or disable command modules. Core commands (Compile, Eval, Console, PlayMode, Menu, etc.) are always available. User-defined modules are enabled by default.",
                MessageType.Info);

            EditorGUILayout.Space();

            var changed = false;
            foreach (var name in allNames)
            {
                var enabled = settings.IsModuleEnabled(name);
                registeredDescriptions.TryGetValue(name, out var description);
                var label = string.IsNullOrEmpty(description)
                    ? name
                    : name;
                var tooltip = string.IsNullOrEmpty(description) ? "" : description;

                var newEnabled = EditorGUILayout.ToggleLeft(
                    new GUIContent(label, tooltip), enabled);

                if (newEnabled != enabled)
                {
                    if (newEnabled)
                        settings.EnableModule(name);
                    else
                        settings.DisableModule(name);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Module changes will take effect after the server reloads. Click the button below to apply now.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Reload Server"))
            {
                UniCliServerBootstrap.ReloadDispatcher();
            }
        }
    }
}
