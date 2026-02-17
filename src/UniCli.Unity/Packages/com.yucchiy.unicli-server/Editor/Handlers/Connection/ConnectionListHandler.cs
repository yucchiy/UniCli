using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor.Hardware;
using UnityEditorInternal;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ConnectionListHandler : CommandHandler<Unit, ConnectionListResponse>
    {
        const int PlayerDirectUrlConnectGuid = 0xFEEE;

        public override string CommandName => CommandNames.Connection.List;
        public override string Description => "List available connection targets (players/devices)";

        protected override bool TryWriteFormatted(ConnectionListResponse response, bool success, IFormatWriter writer)
        {
            if (!success || response.targets == null || response.targets.Length == 0)
            {
                writer.WriteLine("No connection targets available.");
                return true;
            }

            writer.WriteLine($"Connection targets ({response.targets.Length}):");
            foreach (var target in response.targets)
            {
                var connected = target.isConnected ? " [connected]" : "";
                var source = !string.IsNullOrEmpty(target.deviceId) ? $" device={target.deviceId}" : "";
                writer.WriteLine($"  id={target.id} {target.name} ({target.type}){source}{connected}");
            }

            return true;
        }

        protected override ValueTask<ConnectionListResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var connectedId = ProfilerDriver.connectedProfiler;
            var connectedUrl = ProfilerDriver.directConnectionUrl;
            var targets = new List<ConnectionTarget>();

            var ids = ProfilerDriver.GetAvailableProfilers();
            foreach (var id in ids)
            {
                targets.Add(new ConnectionTarget
                {
                    id = id,
                    name = ProfilerDriver.GetConnectionIdentifier(id),
                    type = "Profiler",
                    isConnected = id == connectedId
                });
            }

            foreach (var device in DevDeviceList.GetDevices())
            {
                if (!device.isConnected || (device.features & DevDeviceFeatures.PlayerConnection) == 0)
                    continue;

                var url = "device://" + device.id;
                var isConnected = connectedId == PlayerDirectUrlConnectGuid && connectedUrl == url;

                targets.Add(new ConnectionTarget
                {
                    id = PlayerDirectUrlConnectGuid,
                    name = device.name,
                    type = device.type,
                    deviceId = device.id,
                    isConnected = isConnected
                });
            }

            return new ValueTask<ConnectionListResponse>(new ConnectionListResponse
            {
                targets = targets.ToArray()
            });
        }
    }

    [Serializable]
    public class ConnectionListResponse
    {
        public ConnectionTarget[] targets;
    }

    [Serializable]
    public class ConnectionTarget
    {
        public int id;
        public string name;
        public string type;
        public string deviceId;
        public bool isConnected;
    }
}
