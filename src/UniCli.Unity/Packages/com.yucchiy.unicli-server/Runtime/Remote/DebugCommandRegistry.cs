using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    public sealed class DebugCommandRegistry
    {
        private readonly Dictionary<string, DebugCommand> _commands = new();
        private readonly Dictionary<string, DebugCommandAttribute> _attributes = new();

        public void DiscoverCommands()
        {
            _commands.Clear();
            _attributes.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface)
                        continue;

                    if (!typeof(DebugCommand).IsAssignableFrom(type))
                        continue;

                    var attr = type.GetCustomAttribute<DebugCommandAttribute>();
                    if (attr == null)
                        continue;

                    try
                    {
                        var instance = (DebugCommand)Activator.CreateInstance(type);
                        if (!_commands.TryAdd(attr.Name, instance))
                        {
                            UnityEngine.Debug.LogWarning($"[UniCli.Remote] Duplicate debug command '{attr.Name}', skipping {type.FullName}");
                            continue;
                        }

                        _attributes[attr.Name] = attr;
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[UniCli.Remote] Failed to create debug command '{attr.Name}' ({type.FullName}): {ex.Message}");
                    }
                }
            }

            UnityEngine.Debug.Log($"[UniCli.Remote] Discovered {_commands.Count} debug command(s)");
        }

        public bool TryGetCommand(string name, out DebugCommand command)
        {
            return _commands.TryGetValue(name, out command);
        }

        public RuntimeCommandInfo[] GetCommandInfos()
        {
            var infos = new List<RuntimeCommandInfo>(_commands.Count);
            foreach (var kvp in _attributes)
            {
                infos.Add(new RuntimeCommandInfo
                {
                    name = kvp.Value.Name,
                    description = kvp.Value.Description
                });
            }
            return infos.ToArray();
        }
    }
}
