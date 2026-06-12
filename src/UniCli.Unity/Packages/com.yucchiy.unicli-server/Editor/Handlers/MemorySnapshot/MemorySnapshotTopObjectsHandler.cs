#if UNICLI_MEMORY_PROFILER
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniCli.Server.Editor.Handlers
{
    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotTopObjectsHandler : MemorySnapshotCommandHandler<MemorySnapshotTopObjectsRequest, MemorySnapshotTopObjectsResponse>
    {
        internal MemorySnapshotTopObjectsHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.TopObjects";
        public override string Description =>
            "List largest retained native objects or native/managed type totals from a memory snapshot; object filters see at most the top 50 objects per native type";

        protected override ValueTask<MemorySnapshotTopObjectsResponse> ExecuteAsync(MemorySnapshotTopObjectsRequest request, CancellationToken cancellationToken)
        {
            var scope = MemorySnapshotScopeParser.Parse(request.scope);
            if (scope == MemorySnapshotScope.Managed && !string.IsNullOrEmpty(request.nameFilter))
                throw new CommandFailedException("MemorySnapshot.TopObjects does not support nameFilter with scope=managed; managed scope is type aggregation only.", null);

            var minSize = Math.Max(0, request.minSize);
            var analysis = GetAnalysisByReferenceOrDefault(
                request.snapshot,
                request.path,
                0,
                out var cached,
                out var analysisMs,
                out var entry);
            var groupByType = request.groupByType || scope == MemorySnapshotScope.Managed;

            var response = new MemorySnapshotTopObjectsResponse
            {
                id = entry.id,
                name = entry.name,
                path = analysis.Path,
                analysisMs = analysisMs,
                cached = cached,
                scope = MemorySnapshotFormatting.ScopeName(scope),
                groupByType = groupByType,
                objects = Array.Empty<MemorySnapshotNativeObjectInfo>(),
                types = Array.Empty<MemorySnapshotNativeTypeStat>(),
                truncated = false,
                minSize = minSize
            };

            if (groupByType)
            {
                var result = MemorySnapshotAnalysisQueries.GetTopTypes(
                    analysis,
                    scope,
                    request.typeFilter,
                    request.limit,
                    minSize);
                response.types = result.Types;
                response.othersCount = result.OthersCount;
                response.othersSize = result.OthersSize;
            }
            else
            {
                response.objects = MemorySnapshotAnalysisQueries.GetTopObjects(
                    analysis,
                    request.typeFilter,
                    request.nameFilter,
                    request.limit,
                    minSize,
                    out var truncated);
                response.truncated = truncated;
            }

            return new ValueTask<MemorySnapshotTopObjectsResponse>(response);
        }
    }

    [Serializable]
    public class MemorySnapshotTopObjectsRequest
    {
        public string snapshot;
        public string path;
        public int limit = 20;
        public string typeFilter;
        public string nameFilter;
        public string scope;
        public long minSize;
        public bool groupByType;
    }

    [Serializable]
    public class MemorySnapshotTopObjectsResponse
    {
        public string id;
        public string name;
        public string path;
        public long analysisMs;
        public bool cached;
        public string scope;
        public bool groupByType;
        public long minSize;
        public MemorySnapshotNativeObjectInfo[] objects;
        public MemorySnapshotNativeTypeStat[] types;
        public long othersCount;
        public long othersSize;
        public bool truncated;
    }
}
#endif
