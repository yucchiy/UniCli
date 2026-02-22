using UnityEditor;
using UnityEditor.Networking.PlayerConnection;

namespace UniCli.Server.Editor.Handlers.Remote
{
    internal static class RemoteHelper
    {
        public static bool ShouldExecuteLocally(int requestedPlayerId)
        {
            if (requestedPlayerId > 0)
                return false;

            return EditorApplication.isPlaying;
        }

        public static int ResolvePlayerId(int requestedId)
        {
            if (requestedId > 0)
                return requestedId;

            var players = EditorConnection.instance.ConnectedPlayers;
            if (players.Count == 0)
                throw new System.InvalidOperationException("No runtime player connected. Connect a Development Build first.");

            return players[0].playerId;
        }
    }
}
