using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Remote;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers.Remote
{
    [Module("Remote")]
    public sealed class RemoteInvokeHandler : CommandHandler<RemoteInvokeRequest, RemoteInvokeResponse>
    {
        private readonly RemoteBridge _bridge;
        private readonly DebugCommandRegistry _registry;

        public RemoteInvokeHandler(RemoteBridge bridge, DebugCommandRegistry registry)
        {
            _bridge = bridge;
            _registry = registry;
        }

        public override string CommandName => "Remote.Invoke";
        public override string Description => "Invoke a debug command on connected runtime player";

        protected override bool TryWriteFormatted(RemoteInvokeResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                if (string.IsNullOrEmpty(response.data))
                    writer.WriteLine($"Command '{response.command}' succeeded");
                else
                    writer.WriteLine(response.data);
            }
            else
            {
                writer.WriteLine($"Command failed: {response.message}");
            }

            return true;
        }

        protected override async ValueTask<RemoteInvokeResponse> ExecuteAsync(RemoteInvokeRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.command))
                throw new ArgumentException("'command' is required");

            if (RemoteHelper.ShouldExecuteLocally(request.playerId))
                return ExecuteLocally(request);

            var playerId = RemoteHelper.ResolvePlayerId(request.playerId);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var responseJson = await _bridge.SendCommandAsync(request.command, request.data, playerId, cts.Token);
            var cmdResponse = JsonUtility.FromJson<RuntimeCommandResponse>(responseJson);

            if (!cmdResponse.success)
            {
                throw new CommandFailedException(
                    cmdResponse.message,
                    new RemoteInvokeResponse
                    {
                        command = request.command,
                        success = false,
                        message = cmdResponse.message,
                        data = cmdResponse.data
                    });
            }

            return new RemoteInvokeResponse
            {
                command = request.command,
                success = true,
                message = cmdResponse.message,
                data = cmdResponse.data
            };
        }

        private RemoteInvokeResponse ExecuteLocally(RemoteInvokeRequest request)
        {
            if (!_registry.TryGetCommand(request.command, out var command))
            {
                throw new CommandFailedException(
                    $"Unknown debug command: {request.command}",
                    new RemoteInvokeResponse
                    {
                        command = request.command,
                        success = false,
                        message = $"Unknown debug command: {request.command}",
                        data = ""
                    });
            }

            var resultJson = command.Execute(request.data);

            return new RemoteInvokeResponse
            {
                command = request.command,
                success = true,
                message = $"Command '{request.command}' succeeded (local PlayMode)",
                data = resultJson
            };
        }

    }

    [Serializable]
    public class RemoteInvokeRequest
    {
        public string command;
        public string data;
        public int playerId;
    }

    [Serializable]
    public class RemoteInvokeResponse : IRawJsonResponse
    {
        public string command;
        public bool success;
        public string message;
        public string data;

        public string ToJson()
        {
            var dataJson = string.IsNullOrEmpty(data) ? "null" : data;
            var escapedCommand = command?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
            var escapedMessage = message?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
            return $"{{\"command\":\"{escapedCommand}\",\"success\":{(success ? "true" : "false")},\"message\":\"{escapedMessage}\",\"data\":{dataJson}}}";
        }
    }
}
