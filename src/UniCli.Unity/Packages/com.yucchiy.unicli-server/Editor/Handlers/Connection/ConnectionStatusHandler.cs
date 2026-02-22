using System.Threading;
using System;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Remote")]
    public sealed class ConnectionStatusHandler : CommandHandler<Unit, ConnectionStatusResponse>
    {
        public override string CommandName => "Connection.Status";
        public override string Description => "Get current profiler connection status";

        protected override bool TryWriteFormatted(ConnectionStatusResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                var url = !string.IsNullOrEmpty(response.directConnectionUrl) ? $", url={response.directConnectionUrl}" : "";
                writer.WriteLine($"Connected to: {response.name} (id={response.id}{url})");
            }
            else
            {
                writer.WriteLine("Failed to get connection status");
            }

            return true;
        }

        protected override ValueTask<ConnectionStatusResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var currentId = ProfilerDriver.connectedProfiler;
            return new ValueTask<ConnectionStatusResponse>(new ConnectionStatusResponse
            {
                id = currentId,
                name = ProfilerDriver.GetConnectionIdentifier(currentId),
                directConnectionUrl = ProfilerDriver.directConnectionUrl
            });
        }
    }

    [Serializable]
    public class ConnectionStatusResponse
    {
        public int id;
        public string name;
        public string directConnectionUrl;
    }
}
