using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ConsoleLogHandler : CommandHandler<ConsoleLogRequest, Unit>
    {
        public override string CommandName => CommandNames.Console.Log;
        public override string Description => "Output a message to the Unity Editor console";

        protected override ValueTask<Unit> ExecuteAsync(ConsoleLogRequest request)
        {
            if (request.background)
            {
                var count = Math.Max(1, request.count);
                for (var i = 0; i < count; i++)
                {
                    var index = i;
                    Task.Run(() => LogMessage($"{request.message} [bg#{index}]", request.logType));
                }
            }
            else
            {
                var count = Math.Max(1, request.count);
                for (var i = 0; i < count; i++)
                {
                    LogMessage(count > 1 ? $"{request.message} [#{i}]" : request.message, request.logType);
                }
            }

            return new ValueTask<Unit>(new Unit());
        }

        private static void LogMessage(string message, string logType)
        {
            switch (logType?.ToLowerInvariant())
            {
                case "warning":
                    Debug.LogWarning(message);
                    break;
                case "error":
                    Debug.LogError(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }
    }

    [Serializable]
    public class ConsoleLogRequest
    {
        public string message = "";
        public string logType = "Log";
        public bool background;
        public int count = 1;
    }
}
