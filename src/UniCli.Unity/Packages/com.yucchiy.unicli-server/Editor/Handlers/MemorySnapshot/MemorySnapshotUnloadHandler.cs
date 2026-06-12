#if UNICLI_MEMORY_PROFILER
using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotUnloadHandler : CommandHandler<MemorySnapshotUnloadRequest, MemorySnapshotUnloadResponse>
    {
        private readonly SnapshotAnalysisCache _cache;

        internal MemorySnapshotUnloadHandler(SnapshotAnalysisCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public override string CommandName => "MemorySnapshot.Unload";
        public override string Description => "Clear cached MemorySnapshot analysis results, or release one loaded snapshot by id/name";

        protected override ValueTask<MemorySnapshotUnloadResponse> ExecuteAsync(MemorySnapshotUnloadRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<MemorySnapshotUnloadResponse>(new MemorySnapshotUnloadResponse
            {
                releasedCount = _cache.Remove(request.snapshot)
            });
        }
    }

    [Serializable]
    public class MemorySnapshotUnloadRequest
    {
        public string snapshot;
    }

    [Serializable]
    public class MemorySnapshotUnloadResponse
    {
        public int releasedCount;
    }

    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotLoadHandler : MemorySnapshotCommandHandler<MemorySnapshotLoadRequest, MemorySnapshotLoadResponse>
    {
        internal MemorySnapshotLoadHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.Load";
        public override string Description => "Analyze and name a memory snapshot so later MemorySnapshot commands can refer to it by id or name";

        protected override ValueTask<MemorySnapshotLoadResponse> ExecuteAsync(MemorySnapshotLoadRequest request, CancellationToken cancellationToken)
        {
            LoadAnalysis(
                request.path,
                request.name,
                request.replace,
                out var cached,
                out var analysisMs,
                out var entry);

            return new ValueTask<MemorySnapshotLoadResponse>(new MemorySnapshotLoadResponse
            {
                id = entry.id,
                name = entry.name,
                pinned = entry.pinned,
                path = entry.path,
                fileSize = entry.fileSize,
                analyzedAtUtc = entry.analyzedAtUtc,
                lastAccessedAtUtc = entry.lastAccessedAtUtc,
                analysisMs = analysisMs,
                cached = cached
            });
        }
    }

    [Serializable]
    public class MemorySnapshotLoadRequest
    {
        public string path;
        public string name;
        public bool replace;
    }

    [Serializable]
    public class MemorySnapshotLoadResponse
    {
        public string id;
        public string name;
        public bool pinned;
        public string path;
        public long fileSize;
        public string analyzedAtUtc;
        public string lastAccessedAtUtc;
        public long analysisMs;
        public bool cached;
    }

    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotStatusHandler : MemorySnapshotCommandHandler<Unit, MemorySnapshotStatusResponse>
    {
        internal MemorySnapshotStatusHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.Status";
        public override string Description => "Show cached MemorySnapshot analysis results";

        protected override ValueTask<MemorySnapshotStatusResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            return new ValueTask<MemorySnapshotStatusResponse>(new MemorySnapshotStatusResponse
            {
                entries = Cache.GetEntries(),
                capacity = Cache.GetCapacity()
            });
        }
    }

    [Serializable]
    public class MemorySnapshotStatusResponse
    {
        public MemorySnapshotStatusEntry[] entries;
        public int capacity;
    }

    [Serializable]
    public class MemorySnapshotStatusEntry
    {
        public string id;
        public string name;
        public bool pinned;
        public string path;
        public long fileSize;
        public string analyzedAtUtc;
        public string lastAccessedAtUtc;
        public long analysisMs;
    }
}
#endif
