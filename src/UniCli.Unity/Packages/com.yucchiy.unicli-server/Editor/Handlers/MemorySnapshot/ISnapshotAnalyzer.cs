#if UNICLI_MEMORY_PROFILER
namespace UniCli.Server.Editor.Handlers
{
    internal interface ISnapshotAnalyzer
    {
        bool IsAvailable(out string unavailableReason);
        SnapshotAnalysis Analyze(string snapshotPath);
    }
}
#endif
