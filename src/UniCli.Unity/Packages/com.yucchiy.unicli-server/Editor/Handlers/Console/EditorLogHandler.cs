using System.Threading;
using System;
using System.Threading.Tasks;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class EditorLogGetHandler : CommandHandler<EditorLogRequest, EditorLogResponse>
    {
        private readonly EditorLogManager _logManager;

        public EditorLogGetHandler(EditorLogManager logManager)
        {
            _logManager = logManager;
        }

        public override string CommandName => "Console.GetLog";
        public override string Description => "Retrieve Unity Editor console logs with optional filtering";

        protected override ValueTask<EditorLogResponse> ExecuteAsync(EditorLogRequest request, CancellationToken cancellationToken)
        {
            var logs = _logManager.GetLogs(request.logType, request.searchText, request.maxCount);

            if (request.stackTraceLines >= 0)
            {
                for (var i = 0; i < logs.Length; i++)
                {
                    logs[i].stackTrace = StackTraceHelper.Truncate(logs[i].stackTrace, request.stackTraceLines);
                }
            }

            var response = new EditorLogResponse
            {
                logs = logs,
                totalCount = _logManager.GetLogCount(),
                displayedCount = logs.Length
            };

            return new ValueTask<EditorLogResponse>(response);
        }
    }

    [Serializable]
    public class EditorLogRequest
    {
        public string logType = "All";
        public string searchText = "";
        public int maxCount = 100;
        public int stackTraceLines = 0; // 0: no stack trace (default), -1: full, N>0: first N lines
    }
}
