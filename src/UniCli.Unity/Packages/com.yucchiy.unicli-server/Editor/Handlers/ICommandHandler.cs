using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Server.Editor.Internal;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public interface ICommandHandler
    {
        string CommandName { get; }
        string Description { get; }
        CommandInfo GetCommandInfo();
        ValueTask<object> ExecuteAsync(object request);
    }

    public interface IResponseFormatter
    {
        bool TryWriteFormatted(object response, bool success, IFormatWriter writer);
    }

    public abstract class CommandHandler<TRequest, TResponse> : ICommandHandler, IResponseFormatter
    {
        public abstract string CommandName { get; }
        public abstract string Description { get; }

        public CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                name = CommandName,
                description = Description,
                requestFields = CommandFieldInfoExtractor.ExtractFieldInfos(typeof(TRequest)),
                responseFields = CommandFieldInfoExtractor.ExtractFieldInfos(typeof(TResponse))
            };
        }

        public async ValueTask<object> ExecuteAsync(object request)
        {
            if (request is not CommandRequest commandRequest)
            {
                throw new System.ArgumentException($"Invalid request type. Expected CommandRequest, got {request?.GetType().Name ?? "null"}");
            }

            TRequest typedRequest;
            if (typeof(TRequest) == typeof(Unit))
            {
                typedRequest = (TRequest)(object)Unit.Value;
            }
            else if (string.IsNullOrEmpty(commandRequest.data))
            {
                typedRequest = JsonUtility.FromJson<TRequest>("{}");
            }
            else
            {
                typedRequest = JsonUtility.FromJson<TRequest>(commandRequest.data);
                if (typedRequest == null)
                {
                    throw new System.ArgumentException($"Failed to deserialize request data to {typeof(TRequest).Name}");
                }
            }

            return await ExecuteAsync(typedRequest);
        }

        public bool TryWriteFormatted(object response, bool success, IFormatWriter writer)
        {
            if (response is TResponse typed)
                return TryWriteFormatted(typed, success, writer);
            return false;
        }

        protected virtual bool TryWriteFormatted(TResponse response, bool success, IFormatWriter writer)
        {
            return false;
        }

        protected abstract ValueTask<TResponse> ExecuteAsync(TRequest request);
    }
}
