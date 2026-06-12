#if UNICLI_MEMORY_PROFILER
using System;
using System.Reflection;

namespace UniCli.Server.Editor.Handlers
{
    internal sealed class MemoryProfilerPackageAnalyzer : ISnapshotAnalyzer
    {
        public bool IsAvailable(out string unavailableReason)
        {
            return MemoryProfilerReflection.TryGet(out _, out unavailableReason);
        }

        public SnapshotAnalysis Analyze(string snapshotPath)
        {
            if (!MemoryProfilerReflection.TryGet(out var reflection, out var unavailableReason))
                throw new CommandFailedException(unavailableReason, null);

            object reader = null;
            object snapshot = null;

            try
            {
                reader = reflection.CreateFileReader();
                var openResult = reflection.OpenFileReader(reader, snapshotPath);
                if (!string.Equals(openResult, "Success", StringComparison.Ordinal))
                    throw new CommandFailedException($"Failed to open snapshot '{snapshotPath}': {openResult}", null);

                snapshot = reflection.CreateCachedSnapshot(reader);
                reader = null;

                reflection.CrawlManagedData(snapshot);
                return reflection.ExtractAnalysis(snapshot, snapshotPath);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw new CommandFailedException(ex.InnerException.Message, null);
            }
            finally
            {
                if (snapshot != null)
                    reflection.DisposeCachedSnapshot(snapshot);
                else if (reader != null)
                    reflection.CloseFileReader(reader);
            }
        }
    }
}
#endif
