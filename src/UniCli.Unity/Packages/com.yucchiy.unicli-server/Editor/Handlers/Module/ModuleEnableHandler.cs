using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers.Module
{
    public sealed class ModuleEnableHandler : CommandHandler<ModuleNameRequest, Unit>
    {
        public override string CommandName => "Module.Enable";
        public override string Description => "Enable a module and reload the command dispatcher";

        protected override ValueTask<Unit> ExecuteAsync(ModuleNameRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            UniCliSettings.instance.EnableModule(request.name);
            UniCliServerBootstrap.ReloadDispatcher();

            return new ValueTask<Unit>(Unit.Value);
        }
    }

    [Serializable]
    public class ModuleNameRequest
    {
        public string name;
    }
}
