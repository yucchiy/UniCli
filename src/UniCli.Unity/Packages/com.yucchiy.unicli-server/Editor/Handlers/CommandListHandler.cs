using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class CommandListHandler : CommandHandler<Unit, CommandListResponse>
    {
        private readonly CommandDispatcher _dispatcher;

        public CommandListHandler(CommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public override string CommandName => "Commands.List";
        public override string Description => "List all available commands with their metadata";

        protected override ValueTask<CommandListResponse> ExecuteAsync(Unit request)
        {
            var response = new CommandListResponse
            {
                commands = _dispatcher.GetAllCommandInfo()
            };

            return new ValueTask<CommandListResponse>(response);
        }
    }
}
