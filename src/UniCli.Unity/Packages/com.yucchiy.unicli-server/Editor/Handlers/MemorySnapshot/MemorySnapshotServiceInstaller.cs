#if UNICLI_MEMORY_PROFILER
namespace UniCli.Server.Editor.Handlers
{
    public sealed class MemorySnapshotServiceInstaller : IServiceInstaller
    {
        public void Install(ServiceRegistry services)
        {
            services.AddSingleton(new SnapshotAnalysisCache());
            services.AddSingleton<ISnapshotAnalyzer>(new MemoryProfilerPackageAnalyzer());
        }
    }
}
#endif
