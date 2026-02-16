#nullable enable
using System;

namespace UniCli.Server.Editor
{
    public sealed class ServerContext
    {
        public string ServerId { get; }
        public DateTime StartedAt { get; }
        public string PipeName { get; }

        public ServerContext(string pipeName)
        {
            ServerId = Guid.NewGuid().ToString("N")[..8];
            StartedAt = DateTime.Now;
            PipeName = pipeName;
        }
    }
}
