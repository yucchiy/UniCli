using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor
{
    public sealed class CommandDispatcher
    {
        private readonly Dictionary<string, ICommandHandler> _handlers = new();

        public CommandDispatcher(ServiceRegistry services)
        {
            services.AddSingleton(this);
            RegisterClassHandlers(services);
        }

        private void RegisterClassHandlers(ServiceRegistry services)
        {
            var handlerTypes = TypeCache.GetTypesDerivedFrom<ICommandHandler>();

            foreach (var type in handlerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
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

        public CommandInfo[] GetAllCommandInfo()
        {
            var infos = new List<CommandInfo>(_handlers.Count);
            foreach (var handler in _handlers.Values)
            {
                infos.Add(handler.GetCommandInfo());
            }
            return infos.ToArray();
        }

        public async ValueTask<CommandResponse> DispatchAsync(CommandRequest request)
        {
            if (!_handlers.TryGetValue(request.command, out var handler))
            {
                return new CommandResponse
                {
                    success = false,
                    message = $"Unknown command: {request.command}",
                    data = ""
                };
            }

            var wantsText = request.format == "text";

            try
            {
                var result = await handler.ExecuteAsync(request);

                if (result is Unit)
                {
                    return new CommandResponse
                    {
                        success = true,
                        message = $"Command '{request.command}' succeeded",
                        data = "",
                        format = "json"
                    };
                }

                if (wantsText && handler is IResponseFormatter formatter
                    && formatter.TryFormat(result, true, out var formatted))
                {
                    return new CommandResponse
                    {
                        success = true,
                        message = $"Command '{request.command}' succeeded",
                        data = formatted,
                        format = "text"
                    };
                }

                return new CommandResponse
                {
                    success = true,
                    message = $"Command '{request.command}' succeeded",
                    data = JsonUtility.ToJson(result),
                    format = "json"
                };
            }
            catch (CommandFailedException ex)
            {
                if (ex.ResponseData is Unit or null)
                {
                    return new CommandResponse
                    {
                        success = false,
                        message = $"Command failed: {ex.Message}",
                        data = "",
                        format = "json"
                    };
                }

                if (wantsText && handler is IResponseFormatter failFormatter
                    && failFormatter.TryFormat(ex.ResponseData, false, out var failFormatted))
                {
                    return new CommandResponse
                    {
                        success = false,
                        message = $"Command failed: {ex.Message}",
                        data = failFormatted,
                        format = "text"
                    };
                }

                return new CommandResponse
                {
                    success = false,
                    message = $"Command failed: {ex.Message}",
                    data = JsonUtility.ToJson(ex.ResponseData),
                    format = "json"
                };
            }
            catch (Exception ex)
            {
                return new CommandResponse
                {
                    success = false,
                    message = $"Command failed: {ex.Message}",
                    data = "",
                    format = "json"
                };
            }
        }
    }
}
