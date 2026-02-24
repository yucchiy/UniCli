using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ModuleDisableHandler : CommandHandler<ModuleNameRequest, Unit>
    {
        private readonly UniCliSettings _settings;
        private readonly IDispatcherReloader _reloader;

        public ModuleDisableHandler(UniCliSettings settings, IDispatcherReloader reloader)
        {
            _settings = settings;
            _reloader = reloader;
        }

        public override string CommandName => "Module.Disable";
        public override string Description => "Disable a module and reload the command dispatcher";

        protected override ValueTask<Unit> ExecuteAsync(ModuleNameRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            _settings.DisableModule(request.name);
            _reloader.Reload();

            return new ValueTask<Unit>(Unit.Value);
        }
    }
}
