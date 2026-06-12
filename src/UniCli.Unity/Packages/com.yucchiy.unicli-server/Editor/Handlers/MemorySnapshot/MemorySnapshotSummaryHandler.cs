#if UNICLI_MEMORY_PROFILER
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniCli.Server.Editor.Handlers
{
    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotSummaryHandler : MemorySnapshotCommandHandler<MemorySnapshotSummaryRequest, MemorySnapshotSummaryResponse>
    {
        internal MemorySnapshotSummaryHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.Summary";
        public override string Description => "Summarize a memory snapshot captured by MemorySnapshot.Capture, Profiler.TakeSnapshot, or the Memory Profiler window";

        protected override ValueTask<MemorySnapshotSummaryResponse> ExecuteAsync(MemorySnapshotSummaryRequest request, CancellationToken cancellationToken)
        {
            var analysis = GetAnalysisByReferenceOrDefault(
                request.snapshot,
                request.path,
                0,
                out var cached,
                out var analysisMs,
                out var entry);

            return new ValueTask<MemorySnapshotSummaryResponse>(new MemorySnapshotSummaryResponse
            {
                id = entry.id,
                name = entry.name,
                path = analysis.Path,
                fileSize = analysis.FileSize,
                analysisMs = analysisMs,
                cached = cached,
                metadata = analysis.Metadata,
                categories = analysis.Categories,
                total = MemorySnapshotAnalysisQueries.GetTotal(analysis)
            });
        }
    }

    [Serializable]
    public class MemorySnapshotSummaryRequest
    {
        public string snapshot;
        public string path;
    }

    [Serializable]
    public class MemorySnapshotSummaryResponse
    {
        public string id;
        public string name;
        public string path;
        public long fileSize;
        public long analysisMs;
        public bool cached;
        public MemorySnapshotMetadataInfo metadata;
        public MemorySnapshotCategoryInfo[] categories;
        public MemorySnapshotTotalInfo total;
    }

    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotAnalyzeHandler : MemorySnapshotCommandHandler<MemorySnapshotAnalyzeRequest, MemorySnapshotAnalyzeResponse>
    {
        internal MemorySnapshotAnalyzeHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.Analyze";
        public override string Description => "Produce a compact MemorySnapshot report from the latest or specified snapshot, optionally diffed against a base snapshot";

        protected override ValueTask<MemorySnapshotAnalyzeResponse> ExecuteAsync(MemorySnapshotAnalyzeRequest request, CancellationToken cancellationToken)
        {
            var limit = MemorySnapshotFormatting.ClampAnalyzeLimit(request.limit);
            var analysis = GetAnalysisByReferenceOrDefault(
                request.snapshot,
                request.path,
                0,
                out var cached,
                out var analysisMs,
                out var entry);
            var nativeTypes = MemorySnapshotAnalysisQueries.GetTopTypes(
                analysis,
                MemorySnapshotScope.Native,
                typeFilter: "",
                limit: limit,
                minSize: 0);
            var managedTypes = MemorySnapshotAnalysisQueries.GetTopTypes(
                analysis,
                MemorySnapshotScope.Managed,
                typeFilter: "",
                limit: limit,
                minSize: 0);
            var topObjects = MemorySnapshotAnalysisQueries.GetTopObjects(
                analysis,
                typeFilter: "",
                nameFilter: "",
                limit: limit,
                minSize: 0,
                out var topObjectsTruncated);

            var response = new MemorySnapshotAnalyzeResponse
            {
                id = entry.id,
                name = entry.name,
                path = analysis.Path,
                fileSize = analysis.FileSize,
                analysisMs = analysisMs,
                cached = cached,
                summary = new MemorySnapshotAnalyzeSummarySection
                {
                    metadata = analysis.Metadata,
                    categories = analysis.Categories,
                    total = MemorySnapshotAnalysisQueries.GetTotal(analysis)
                },
                topNativeTypes = ToTypeSection(nativeTypes),
                topManagedTypes = ToTypeSection(managedTypes),
                topObjects = new MemorySnapshotAnalyzeObjectSection
                {
                    objects = topObjects,
                    truncated = topObjectsTruncated
                }
            };

            if (!string.IsNullOrEmpty(request.baseSnapshot) || !string.IsNullOrEmpty(request.basePath))
            {
                var baseAnalysis = GetAnalysisByReference(
                    request.baseSnapshot,
                    request.basePath,
                    out var baseCached,
                    out var baseAnalysisMs,
                    out var baseEntry);
                var nativeDiff = MemorySnapshotAnalysisQueries.GetDiffTypes(
                    baseAnalysis,
                    analysis,
                    MemorySnapshotScope.Native,
                    typeFilter: "",
                    limit: limit,
                    minSizeDelta: 0);
                var managedDiff = MemorySnapshotAnalysisQueries.GetDiffTypes(
                    baseAnalysis,
                    analysis,
                    MemorySnapshotScope.Managed,
                    typeFilter: "",
                    limit: limit,
                    minSizeDelta: 0);

                response.analysisMs += baseAnalysisMs;
                response.cached = response.cached && baseCached;
                response.diff = new MemorySnapshotAnalyzeDiffSection
                {
                    @base = new MemorySnapshotDiffEndpointInfo
                    {
                        id = baseEntry.id,
                        name = baseEntry.name,
                        path = baseAnalysis.Path,
                        totalNativeSize = baseAnalysis.TotalNativeSize,
                        totalSize = baseAnalysis.TotalNativeSize,
                        captureDate = baseAnalysis.Metadata.captureDate
                    },
                    target = new MemorySnapshotDiffEndpointInfo
                    {
                        id = entry.id,
                        name = entry.name,
                        path = analysis.Path,
                        totalNativeSize = analysis.TotalNativeSize,
                        totalSize = analysis.TotalNativeSize,
                        captureDate = analysis.Metadata.captureDate
                    },
                    nativeTypes = ToDiffSection(nativeDiff),
                    managedTypes = ToDiffSection(managedDiff)
                };
            }

            return new ValueTask<MemorySnapshotAnalyzeResponse>(response);
        }

        private static MemorySnapshotAnalyzeTypeSection ToTypeSection(MemorySnapshotTypeStatResult result)
        {
            return new MemorySnapshotAnalyzeTypeSection
            {
                types = result.Types,
                othersCount = result.OthersCount,
                othersSize = result.OthersSize
            };
        }

        private static MemorySnapshotAnalyzeTypeDiffSection ToDiffSection(MemorySnapshotTypeDiffResult result)
        {
            return new MemorySnapshotAnalyzeTypeDiffSection
            {
                types = result.Types,
                othersCount = result.OthersCount,
                othersSize = result.OthersSize
            };
        }
    }

    [Serializable]
    public class MemorySnapshotAnalyzeRequest
    {
        public string snapshot;
        public string path;
        public string baseSnapshot;
        public string basePath;
        public int limit = 10;
    }

    [Serializable]
    public class MemorySnapshotAnalyzeResponse
    {
        public string id;
        public string name;
        public string path;
        public long fileSize;
        public long analysisMs;
        public bool cached;
        public MemorySnapshotAnalyzeSummarySection summary;
        public MemorySnapshotAnalyzeTypeSection topNativeTypes;
        public MemorySnapshotAnalyzeTypeSection topManagedTypes;
        public MemorySnapshotAnalyzeObjectSection topObjects;
        public MemorySnapshotAnalyzeDiffSection diff;
    }

    [Serializable]
    public class MemorySnapshotAnalyzeSummarySection
    {
        public MemorySnapshotMetadataInfo metadata;
        public MemorySnapshotCategoryInfo[] categories;
        public MemorySnapshotTotalInfo total;
    }

    [Serializable]
    public class MemorySnapshotAnalyzeTypeSection
    {
        public MemorySnapshotNativeTypeStat[] types;
        public long othersCount;
        public long othersSize;
    }

    [Serializable]
    public class MemorySnapshotAnalyzeObjectSection
    {
        public MemorySnapshotNativeObjectInfo[] objects;
        public bool truncated;
    }

    [Serializable]
    public class MemorySnapshotAnalyzeDiffSection
    {
        public MemorySnapshotDiffEndpointInfo @base;
        public MemorySnapshotDiffEndpointInfo target;
        public MemorySnapshotAnalyzeTypeDiffSection nativeTypes;
        public MemorySnapshotAnalyzeTypeDiffSection managedTypes;
    }

    [Serializable]
    public class MemorySnapshotAnalyzeTypeDiffSection
    {
        public MemorySnapshotNativeTypeDiffInfo[] types;
        public long othersCount;
        public long othersSize;
    }
}
#endif
