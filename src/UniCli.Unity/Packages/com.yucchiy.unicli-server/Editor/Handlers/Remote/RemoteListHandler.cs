using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Remote;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers.Remote
{
    [Module("Remote")]
    public sealed class RemoteListHandler : CommandHandler<RemoteListRequest, RemoteListResponse>
    {
        private readonly RemoteBridge _bridge;
        private readonly DebugCommandRegistry _registry;

        public RemoteListHandler(RemoteBridge bridge, DebugCommandRegistry registry)
        {
            _bridge = bridge;
            _registry = registry;
        }

        public override string CommandName => "Remote.List";
        public override string Description => "List debug commands registered on connected runtime player";

        protected override bool TryWriteFormatted(RemoteListResponse response, bool success, IFormatWriter writer)
        {
            if (!success || response.commands == null || response.commands.Length == 0)
            {
                writer.WriteLine("No debug commands available on connected player.");
                return true;
            }

            writer.WriteLine($"Debug commands ({response.commands.Length}):");
            foreach (var cmd in response.commands)
            {
                var desc = string.IsNullOrEmpty(cmd.description) ? "" : $" - {cmd.description}";
                writer.WriteLine($"  {cmd.name}{desc}");
            }

            return true;
        }

        protected override async ValueTask<RemoteListResponse> ExecuteAsync(RemoteListRequest request, CancellationToken cancellationToken)
        {
            if (RemoteHelper.ShouldExecuteLocally(request.playerId))
                return ExecuteListLocally();

            var playerId = RemoteHelper.ResolvePlayerId(request.playerId);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var responseJson = await _bridge.SendListAsync(playerId, cts.Token);
            var listResponse = JsonUtility.FromJson<RuntimeListResponse>(responseJson);

            return new RemoteListResponse
            {
                commands = listResponse.commands
            };
        }

        private RemoteListResponse ExecuteListLocally()
        {
            return new RemoteListResponse
            {
                commands = _registry.GetCommandInfos()
            };
        }

    }

    [Serializable]
    public class RemoteListRequest
    {
        public int playerId;
    }

    [Serializable]
    public class RemoteListResponse
    {
        public RuntimeCommandInfo[] commands;
    }
}
