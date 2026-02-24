using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ModuleEnableHandler : CommandHandler<ModuleNameRequest, Unit>
    {
        private readonly UniCliSettings _settings;
        private readonly IDispatcherReloader _reloader;

        public ModuleEnableHandler(UniCliSettings settings, IDispatcherReloader reloader)
        {
            _settings = settings;
            _reloader = reloader;
        }

        public override string CommandName => "Module.Enable";
        public override string Description => "Enable a module and reload the command dispatcher";

        protected override ValueTask<Unit> ExecuteAsync(ModuleNameRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            _settings.EnableModule(request.name);
            _reloader.Reload();

            return new ValueTask<Unit>(Unit.Value);
        }
    }

    [Serializable]
    public class ModuleNameRequest
    {
        public string name;
    }
}
