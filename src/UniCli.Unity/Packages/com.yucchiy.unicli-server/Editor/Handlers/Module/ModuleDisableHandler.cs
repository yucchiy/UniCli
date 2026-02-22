using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers.Module
{
    public sealed class ModuleDisableHandler : CommandHandler<ModuleNameRequest, Unit>
    {
        public override string CommandName => "Module.Disable";
        public override string Description => "Disable a module and reload the command dispatcher";

        protected override ValueTask<Unit> ExecuteAsync(ModuleNameRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            UniCliSettings.instance.DisableModule(request.name);
            UniCliServerBootstrap.ReloadDispatcher();

            return new ValueTask<Unit>(Unit.Value);
        }
    }
}
