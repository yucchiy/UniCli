#if UNICLI_MEMORY_PROFILER
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniCli.Server.Editor.Handlers
{
    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotDiffHandler : MemorySnapshotCommandHandler<MemorySnapshotDiffRequest, MemorySnapshotDiffResponse>
    {
        internal MemorySnapshotDiffHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.Diff";
        public override string Description => "Compare native or managed type memory deltas between two memory snapshots";

        protected override ValueTask<MemorySnapshotDiffResponse> ExecuteAsync(MemorySnapshotDiffRequest request, CancellationToken cancellationToken)
        {
            var scope = MemorySnapshotScopeParser.Parse(request.scope);
            var minSizeDelta = Math.Max(0, request.minSizeDelta);
            var baseAnalysis = GetAnalysisByReferenceOrDefault(
                request.baseSnapshot,
                request.basePath,
                1,
                out var baseCached,
                out var baseAnalysisMs,
                out var baseEntry);
            var targetAnalysis = GetAnalysisByReferenceOrDefault(
                request.targetSnapshot,
                request.targetPath,
                0,
                out var targetCached,
                out var targetAnalysisMs,
                out var targetEntry);
            var diff = MemorySnapshotAnalysisQueries.GetDiffTypes(
                baseAnalysis,
                targetAnalysis,
                scope,
                request.typeFilter,
                request.limit,
                minSizeDelta);

            return new ValueTask<MemorySnapshotDiffResponse>(new MemorySnapshotDiffResponse
            {
                @base = new MemorySnapshotDiffEndpointInfo
                {
                    id = baseEntry.id,
                    name = baseEntry.name,
                    path = baseAnalysis.Path,
                    totalNativeSize = baseAnalysis.TotalNativeSize,
                    totalSize = MemorySnapshotAnalysisQueries.GetTotalSize(baseAnalysis, scope),
                    captureDate = baseAnalysis.Metadata.captureDate
                },
                target = new MemorySnapshotDiffEndpointInfo
                {
                    id = targetEntry.id,
                    name = targetEntry.name,
                    path = targetAnalysis.Path,
                    totalNativeSize = targetAnalysis.TotalNativeSize,
                    totalSize = MemorySnapshotAnalysisQueries.GetTotalSize(targetAnalysis, scope),
                    captureDate = targetAnalysis.Metadata.captureDate
                },
                analysisMs = baseAnalysisMs + targetAnalysisMs,
                cached = baseCached && targetCached,
                scope = MemorySnapshotFormatting.ScopeName(scope),
                minSizeDelta = minSizeDelta,
                types = diff.Types,
                othersCount = diff.OthersCount,
                othersSize = diff.OthersSize
            });
        }
    }

    [Serializable]
    public class MemorySnapshotDiffRequest
    {
        public string baseSnapshot;
        public string basePath;
        public string targetSnapshot;
        public string targetPath;
        public int limit = 20;
        public string typeFilter;
        public string scope;
        public long minSizeDelta;
    }

    [Serializable]
    public class MemorySnapshotDiffResponse
    {
        public MemorySnapshotDiffEndpointInfo @base;
        public MemorySnapshotDiffEndpointInfo target;
        public long analysisMs;
        public bool cached;
        public string scope;
        public long minSizeDelta;
        public MemorySnapshotNativeTypeDiffInfo[] types;
        public long othersCount;
        public long othersSize;
    }

    [Serializable]
    public class MemorySnapshotDiffEndpointInfo
    {
        public string id;
        public string name;
        public string path;
        public long totalNativeSize;
        public long totalSize;
        public string captureDate;
    }
}
#endif
