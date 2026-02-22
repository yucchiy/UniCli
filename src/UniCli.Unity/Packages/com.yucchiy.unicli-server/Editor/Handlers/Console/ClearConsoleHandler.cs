using System.Threading;
using System;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ClearConsoleHandler : CommandHandler<Unit, ClearConsoleResponse>
    {
        private readonly EditorLogManager _logManager;

        public ClearConsoleHandler(EditorLogManager logManager)
        {
            _logManager = logManager;
        }

        public override string CommandName => "Console.Clear";
        public override string Description => "Clear Unity Editor console logs";

        protected override ValueTask<ClearConsoleResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var count = _logManager.GetLogCount();
            _logManager.ClearLogs();

            return new ValueTask<ClearConsoleResponse>(new ClearConsoleResponse
            {
                clearedCount = count
            });
        }
    }

    [Serializable]
    public class ClearConsoleResponse
    {
        public int clearedCount;
    }
}
