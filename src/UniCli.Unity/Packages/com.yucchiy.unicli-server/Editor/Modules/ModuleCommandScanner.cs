using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UniCli.Server.Editor.Handlers;
using UnityEditor;

namespace UniCli.Server.Editor
{
    internal static class ModuleCommandScanner
    {
        private static Dictionary<string, List<string>> _cache;

        /// <summary>
        /// Returns a mapping of module name to sorted command names.
        /// Core commands (no module) are mapped to empty string key.
        /// Results are cached per domain reload.
        /// </summary>
        public static Dictionary<string, List<string>> GetCommandsByModule()
        {
            if (_cache != null)
                return _cache;

            var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var handlerTypes = TypeCache.GetTypesDerivedFrom<ICommandHandler>();

            foreach (var type in handlerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (type.Assembly.GetName().Name.Contains(".Tests"))
                    continue;

                var moduleName = ModuleRegistry.ResolveModuleName(type) ?? "";

                string commandName;
                try
                {
                    var instance = (ICommandHandler)FormatterServices.GetUninitializedObject(type);
                    commandName = instance.CommandName;
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrEmpty(commandName))
                    continue;

                if (!result.TryGetValue(moduleName, out var list))
                {
                    list = new List<string>();
                    result[moduleName] = list;
                }

                list.Add(commandName);
            }

            foreach (var list in result.Values)
                list.Sort(StringComparer.Ordinal);

            _cache = result;
            return _cache;
        }
    }
}
