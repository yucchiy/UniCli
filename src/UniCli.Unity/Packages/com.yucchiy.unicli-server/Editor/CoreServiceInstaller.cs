namespace UniCli.Server.Editor
{
    public sealed class CoreServiceInstaller : IServiceInstaller
    {
        public void Install(ServiceRegistry services)
        {
            services.AddSingleton(new EditorLogManager(maxBufferSize: 10000));

            var pipeName = ProjectIdentifier.GetPipeName();
            services.AddSingleton(new ServerContext(pipeName));

            services.AddSingleton(UniCliSettings.instance);
            services.AddSingleton<IDispatcherReloader>(new BootstrapDispatcherReloader());
        }

        private sealed class BootstrapDispatcherReloader : IDispatcherReloader
        {
            public void Reload() => UniCliServerBootstrap.ReloadDispatcher();
        }
    }
}
