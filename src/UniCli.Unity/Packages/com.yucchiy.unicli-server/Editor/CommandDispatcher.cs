using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor
{
    public sealed class CommandDispatcher
    {
        private readonly Dictionary<string, ICommandHandler> _handlers =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly StringBuilder _formatBuffer = new();
        private readonly Lazy<CommandInfo[]> _commandInfoCache;

        public CommandDispatcher(ServiceRegistry services)
        {
            services.AddSingleton(this);
            RegisterClassHandlers(services);
            _commandInfoCache = new Lazy<CommandInfo[]>(
                () => _handlers.Values.Select(h => h.GetCommandInfo()).ToArray());
        }

        private void RegisterClassHandlers(ServiceRegistry services)
        {
            var handlerTypes = TypeCache.GetTypesDerivedFrom<ICommandHandler>();
            var settings = UniCliSettings.instance;

            foreach (var type in handlerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var moduleName = ModuleRegistry.ResolveModuleName(type);
                if (moduleName != null && !settings.IsModuleEnabled(moduleName))
                    continue;

                ICommandHandler handler;
                try
                {
                    var instance = services.CreateInstance(type);
                    if (instance == null)
                    {
                        Debug.LogWarning($"[UniCli] Failed to create handler instance: {type.FullName} (unresolvable constructor parameters)");
                        continue;
                    }
                    handler = (ICommandHandler)instance;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[UniCli] Failed to create handler instance: {type.FullName} ({ex.Message})");
                    continue;
                }

                if (!_handlers.TryAdd(handler.CommandName, handler))
                {
                    Debug.LogWarning($"[UniCli] Command '{handler.CommandName}' is already registered, skipping {type.FullName}");
                }
            }
        }

        public CommandInfo[] GetAllCommandInfo() => _commandInfoCache.Value;

        public async ValueTask<CommandResponse> DispatchAsync(CommandRequest request, CancellationToken cancellationToken)
        {
            if (!_handlers.TryGetValue(request.command, out var handler))
                return MakeResponse(false, $"Unknown command: {request.command}");

            var wantsText = request.format == "text";

            try
            {
                var result = await handler.ExecuteAsync(request, cancellationToken);
                return BuildResponse(true, $"Command '{request.command}' succeeded", result, handler, wantsText);
            }
            catch (CommandFailedException ex)
            {
                return BuildResponse(false, $"Command failed: {ex.Message}", ex.ResponseData, handler, wantsText);
            }
            catch (Exception ex)
            {
                return MakeResponse(false, $"Command failed: {ex.Message}");
            }
        }

        internal CommandResponse BuildResponse(bool success, string message, object data, ICommandHandler handler, bool wantsText)
        {
            if (data is Unit or null)
                return MakeResponse(success, message);

            if (wantsText && handler is IResponseFormatter formatter)
            {
                _formatBuffer.Clear();
                var writer = new StringFormatWriter(_formatBuffer);
                if (formatter.TryWriteFormatted(data, success, writer))
                    return MakeResponse(success, message, _formatBuffer.ToString(), "text");
            }

            var json = data is IRawJsonResponse rawJson
                ? rawJson.ToJson()
                : JsonUtility.ToJson(data);

            return MakeResponse(success, message, json);
        }

        internal static CommandResponse MakeResponse(bool success, string message, string data = "", string format = "json")
        {
            return new CommandResponse
            {
                success = success,
                message = message,
                data = data,
                format = format
            };
        }
    }
}
