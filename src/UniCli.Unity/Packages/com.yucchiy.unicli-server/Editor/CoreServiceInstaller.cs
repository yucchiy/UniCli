namespace UniCli.Server.Editor
{
    public sealed class CoreServiceInstaller : IServiceInstaller
    {
        public void Install(ServiceRegistry services)
        {
            services.AddSingleton(new EditorLogManager(maxBufferSize: 10000));

            var pipeName = ProjectIdentifier.GetPipeName();
            services.AddSingleton(new ServerContext(pipeName));
        }
    }
}
