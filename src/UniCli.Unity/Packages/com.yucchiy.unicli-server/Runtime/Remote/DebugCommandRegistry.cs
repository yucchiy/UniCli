using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    public sealed class DebugCommandRegistry
    {
        private readonly Dictionary<string, DebugCommand> _commands = new();

        public void DiscoverCommands()
        {
            _commands.Clear();

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

                    try
                    {
                        var instance = (DebugCommand)Activator.CreateInstance(type);
                        if (!_commands.TryAdd(instance.CommandName, instance))
                            continue;
                    }
                    catch
                    {
                        // Silently skip commands that fail to instantiate.
                    }
                }
            }
        }

        public bool TryGetCommand(string name, out DebugCommand command)
        {
            return _commands.TryGetValue(name, out command);
        }

        public RuntimeCommandInfo[] GetCommandInfos()
        {
            var infos = new List<RuntimeCommandInfo>(_commands.Count);
            foreach (var kvp in _commands)
            {
                infos.Add(new RuntimeCommandInfo
                {
                    name = kvp.Value.CommandName,
                    description = kvp.Value.Description
                });
            }
            return infos.ToArray();
        }
    }
}
