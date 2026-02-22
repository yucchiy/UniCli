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

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10);
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);

                    EditorGUILayout.Space(2);
                    EditorGUILayout.HelpBox(
                        "Enable or disable command modules. Core commands (Compile, Eval, Console, PlayMode, Menu, etc.) are always available. User-defined modules are enabled by default.",
                        MessageType.Info);

                    EditorGUILayout.Space(8);

                    var changed = false;
                    foreach (var name in allNames)
                    {
                        var enabled = settings.IsModuleEnabled(name);
                        registeredDescriptions.TryGetValue(name, out var description);
                        var tooltip = string.IsNullOrEmpty(description) ? "" : description;

                        var newEnabled = EditorGUILayout.ToggleLeft(
                            new GUIContent(name, tooltip), enabled);

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
                        EditorGUILayout.Space(8);
                        EditorGUILayout.HelpBox(
                            "Module changes will take effect after the server reloads. Click the button below to apply now.",
                            MessageType.Warning);
                    }

                    EditorGUILayout.Space(8);

                    if (GUILayout.Button("Reload Server", GUILayout.MaxWidth(150)))
                    {
                        UniCliServerBootstrap.ReloadDispatcher();
                    }

                    EditorGUILayout.Space(4);
                }
                GUILayout.Space(4);
            }
        }
    }
}
