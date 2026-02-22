using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ModuleListHandler : CommandHandler<Unit, ModuleListResponse>, IResponseFormatter
    {
        public override string CommandName => "Module.List";
        public override string Description => "List all available modules and their enabled status";

        protected override ValueTask<ModuleListResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var settings = UniCliSettings.instance;
            var allNames = UniCliSettings.DiscoverAllModuleNames();

            var registeredDescriptions = new Dictionary<string, string>();
            foreach (var m in ModuleRegistry.All)
                registeredDescriptions[m.Name] = m.Description;

            var items = new ModuleInfo[allNames.Length];
            for (var i = 0; i < allNames.Length; i++)
            {
                var name = allNames[i];
                registeredDescriptions.TryGetValue(name, out var description);

                items[i] = new ModuleInfo
                {
                    name = name,
                    description = description ?? "",
                    enabled = settings.IsModuleEnabled(name)
                };
            }

            return new ValueTask<ModuleListResponse>(new ModuleListResponse { modules = items });
        }

        protected override bool TryWriteFormatted(ModuleListResponse response, bool success, IFormatWriter writer)
        {
            if (!success || response.modules == null)
                return false;

            writer.WriteLine("Modules:");
            foreach (var m in response.modules)
            {
                var status = m.enabled ? "enabled" : "disabled";
                var desc = string.IsNullOrEmpty(m.description) ? "" : $" - {m.description}";
                writer.WriteLine($"  [{status}] {m.name}{desc}");
            }

            return true;
        }
    }

    [Serializable]
    public class ModuleListResponse
    {
        public ModuleInfo[] modules;
    }

    [Serializable]
    public class ModuleInfo
    {
        public string name;
        public string description;
        public bool enabled;
    }
}
