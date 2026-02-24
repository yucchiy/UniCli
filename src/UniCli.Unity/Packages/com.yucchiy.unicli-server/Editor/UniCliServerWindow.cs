using System;
using System.Collections.Generic;
using UniCli.Protocol;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor
{
    public sealed class UniCliServerWindow : EditorWindow
    {
        private static readonly Color SeparatorColor = new(0.5f, 0.5f, 0.5f, 0.3f);

        private readonly Dictionary<string, bool> _foldoutStates = new();
        private Vector2 _scrollPosition;
        private bool _needsSeparator;

        [MenuItem("Window/UniCli Server")]
        private static void Open()
        {
            GetWindow<UniCliServerWindow>("UniCli Server");
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawServerStatus();
            EditorGUILayout.Space(4);
            DrawCommands();

            EditorGUILayout.EndScrollView();
        }

        private void DrawServerStatus()
        {
            EditorGUILayout.LabelField("Server Status", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                var isRunning = UniCliServerBootstrap.IsRunning;
                var statusLabel = isRunning ? "\u25cf Running" : "\u25cf Stopped";
                var statusColor = isRunning ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);

                var style = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
                var prevColor = GUI.contentColor;
                GUI.contentColor = statusColor;
                EditorGUILayout.LabelField(statusLabel, style);
                GUI.contentColor = prevColor;

                EditorGUILayout.LabelField("Project", Application.dataPath);
                EditorGUILayout.LabelField("Pipe", ProjectIdentifier.GetPipeName());

                EditorGUILayout.Space(2);

                if (isRunning)
                {
                    if (GUILayout.Button("Stop Server", GUILayout.Height(24)))
                    {
                        UniCliServerBootstrap.StopServer();
                    }
                }
                else
                {
                    if (GUILayout.Button("Start Server", GUILayout.Height(24)))
                    {
                        UniCliServerBootstrap.StartServer();
                    }
                }
            }
        }

        private void DrawCommands()
        {
            EditorGUILayout.LabelField("Commands", EditorStyles.boldLabel);

            var dispatcher = UniCliServerBootstrap.Dispatcher;
            if (dispatcher == null)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Server is not running.");
                }
                return;
            }

            var commands = dispatcher.GetAllCommandInfo();
            Array.Sort(commands, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            var moduleGroups = new SortedDictionary<string, List<CommandInfo>>(StringComparer.Ordinal);
            foreach (var command in commands)
            {
                var moduleName = string.IsNullOrEmpty(command.module) ? "Core" : command.module;
                if (!moduleGroups.TryGetValue(moduleName, out var list))
                {
                    list = new List<CommandInfo>();
                    moduleGroups[moduleName] = list;
                }
                list.Add(command);
            }

            _needsSeparator = false;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // Draw "Core" first if it exists, then the rest alphabetically
                if (moduleGroups.TryGetValue("Core", out var coreCommands))
                {
                    DrawSeparatorIfNeeded();
                    DrawModuleGroup("Core", coreCommands);
                }

                foreach (var kvp in moduleGroups)
                {
                    if (kvp.Key == "Core")
                        continue;
                    DrawSeparatorIfNeeded();
                    DrawModuleGroup(kvp.Key, kvp.Value);
                }
            }
        }

        private void DrawModuleGroup(string moduleName, List<CommandInfo> commands)
        {
            var key = $"__module__:{moduleName}";
            _foldoutStates.TryGetValue(key, out var isExpanded);
            isExpanded = EditorGUILayout.Foldout(isExpanded, $"{moduleName} ({commands.Count} commands)", true);
            _foldoutStates[key] = isExpanded;

            if (!isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                var standaloneCommands = new List<CommandInfo>();
                var categorizedCommands = new SortedDictionary<string, List<CommandInfo>>(StringComparer.Ordinal);

                foreach (var command in commands)
                {
                    var dotIndex = command.name.IndexOf('.');
                    if (dotIndex < 0)
                    {
                        standaloneCommands.Add(command);
                    }
                    else
                    {
                        var category = command.name.Substring(0, dotIndex);
                        if (!categorizedCommands.TryGetValue(category, out var list))
                        {
                            list = new List<CommandInfo>();
                            categorizedCommands[category] = list;
                        }
                        list.Add(command);
                    }
                }

                foreach (var command in standaloneCommands)
                    DrawCommandFoldout(command);

                foreach (var kvp in categorizedCommands)
                    DrawCategoryFoldout(kvp.Key, kvp.Value);
            }
        }

        private void DrawSeparatorIfNeeded()
        {
            if (!_needsSeparator)
            {
                _needsSeparator = true;
                return;
            }

            var rect = EditorGUILayout.GetControlRect(false, 1);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, SeparatorColor);
            }
        }

        private void DrawCategoryFoldout(string category, List<CommandInfo> commands)
        {
            var key = $"__category__:{category}";
            _foldoutStates.TryGetValue(key, out var isExpanded);
            isExpanded = EditorGUILayout.Foldout(isExpanded, $"{category} ({commands.Count})", true);
            _foldoutStates[key] = isExpanded;

            if (!isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var command in commands)
                {
                    DrawCommandFoldout(command);
                }
            }
        }

        private void DrawCommandFoldout(CommandInfo command)
        {
            _foldoutStates.TryGetValue(command.name, out var isExpanded);
            isExpanded = EditorGUILayout.Foldout(isExpanded, command.name, true);
            _foldoutStates[command.name] = isExpanded;

            if (!isExpanded)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawCommandContent(command);
            }
        }

        private static void DrawCommandContent(CommandInfo command)
        {
            if (!string.IsNullOrEmpty(command.description))
            {
                EditorGUILayout.LabelField("Description", command.description);
            }

            DrawFieldTable("Request Fields", command.requestFields);
            DrawFieldTable("Response Fields", command.responseFields);
        }

        private static void DrawFieldTable(string label, CommandFieldInfo[] fields)
        {
            if (fields == null || fields.Length == 0)
            {
                EditorGUILayout.LabelField(label, "(none)");
                return;
            }

            EditorGUILayout.LabelField(label);

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var field in fields)
                {
                    var defaultPart = string.IsNullOrEmpty(field.defaultValue)
                        ? ""
                        : $" = {field.defaultValue}";
                    EditorGUILayout.LabelField(field.name, $"{field.type}{defaultPart}");
                }
            }
        }
    }
}
