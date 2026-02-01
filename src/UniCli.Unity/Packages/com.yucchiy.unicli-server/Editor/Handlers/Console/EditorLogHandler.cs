using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class EditorLogGetHandler : CommandHandler<EditorLogRequest, EditorLogResponse>
    {
        private readonly EditorLogManager _logManager;

        public EditorLogGetHandler(EditorLogManager logManager)
        {
            _logManager = logManager;
        }

        public override string CommandName => CommandNames.Console.GetLog;
        public override string Description => "Retrieve Unity Editor console logs with optional filtering";

        protected override ValueTask<EditorLogResponse> ExecuteAsync(EditorLogRequest request)
        {
            var logs = _logManager.GetLogs(request.logType, request.searchText, request.maxCount);

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
    }
}
