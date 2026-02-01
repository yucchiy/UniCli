using System;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class CommandFailedException : Exception
    {
        public object ResponseData { get; }

        public CommandFailedException(string message, object responseData)
            : base(message)
        {
            ResponseData = responseData;
        }
    }
}
