using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Remote")]
    public sealed class ConnectionConnectHandler : CommandHandler<ConnectionConnectRequest, ConnectionStatusResponse>
    {
        public override string CommandName => "Connection.Connect";
        public override string Description => "Connect to a target player/device by ID, IP address, or device ID";

        protected override bool TryWriteFormatted(ConnectionStatusResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Connected to: {response.name} (id={response.id})");
            else
                writer.WriteLine("Failed to connect");

            return true;
        }

        protected override ValueTask<ConnectionStatusResponse> ExecuteAsync(ConnectionConnectRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(request.deviceId))
            {
                ProfilerDriver.DirectURLConnect("device://" + request.deviceId);
            }
            else if (request.id != 0)
            {
                ProfilerDriver.connectedProfiler = request.id;
            }
            else if (!string.IsNullOrEmpty(request.ip))
            {
                ProfilerDriver.DirectIPConnect(request.ip);
            }
            else
            {
                throw new ArgumentException("Either deviceId, id, or ip must be specified");
            }

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
    public class ConnectionConnectRequest
    {
        public int id;
        public string ip;
        public string deviceId;
    }
}
