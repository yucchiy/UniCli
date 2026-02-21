using System;
using UnityEngine;

namespace UniCli.Remote.Commands
{
    [DebugCommand("Debug.GetLogs", "Get recent log entries")]
    public sealed class GetLogsCommand : DebugCommand<GetLogsCommand.Request, GetLogsCommand.Response>
    {
        protected override Response ExecuteCommand(Request request)
        {
            var capture = RuntimeDebugReceiver.RuntimeLogCapture;
            if (capture == null)
                throw new InvalidOperationException("RuntimeLogCapture is not initialized");

            LogType? typeFilter = null;
            if (!string.IsNullOrEmpty(request.type))
            {
                if (Enum.TryParse<LogType>(request.type, true, out var parsed))
                    typeFilter = parsed;
            }

            var entries = capture.GetEntries(request.limit, typeFilter);

            return new Response
            {
                count = entries.Length,
                entries = entries
            };
        }

        [Serializable]
        public class Request
        {
            public int limit = 50;
            public string type;
        }

        [Serializable]
        public class Response
        {
            public int count;
            public RuntimeLogCapture.LogEntry[] entries;
        }
    }
}
