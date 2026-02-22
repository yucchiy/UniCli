using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Remote;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers.Remote
{
    [Module("Remote")]
    public sealed class RemoteListHandler : CommandHandler<RemoteListRequest, RemoteListResponse>
    {
        private readonly RemoteBridge _bridge;

        public RemoteListHandler(RemoteBridge bridge)
        {
            _bridge = bridge;
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
            if (ShouldExecuteLocally(request.playerId))
                return ExecuteListLocally();

            var playerId = ResolvePlayerId(request.playerId);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var responseJson = await _bridge.SendListAsync(playerId, cts.Token);
            var listResponse = JsonUtility.FromJson<RuntimeListResponse>(responseJson);

            return new RemoteListResponse
            {
                commands = listResponse.commands
            };
        }

        private static RemoteListResponse ExecuteListLocally()
        {
            var registry = new DebugCommandRegistry();
            registry.DiscoverCommands();

            return new RemoteListResponse
            {
                commands = registry.GetCommandInfos()
            };
        }

        private static bool ShouldExecuteLocally(int requestedPlayerId)
        {
            if (requestedPlayerId > 0)
                return false;

            return EditorApplication.isPlaying;
        }

        private static int ResolvePlayerId(int requestedId)
        {
            if (requestedId > 0)
                return requestedId;

            var players = EditorConnection.instance.ConnectedPlayers;
            if (players.Count == 0)
                throw new InvalidOperationException("No runtime player connected. Connect a Development Build first.");

            return players[0].playerId;
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
