#if UNICLI_MEMORY_PROFILER
using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniCli.Server.Editor.Handlers
{
    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotAllOfMemoryHandler : MemorySnapshotCommandHandler<MemorySnapshotAllOfMemoryRequest, MemorySnapshotAllOfMemoryResponse>
    {
        internal MemorySnapshotAllOfMemoryHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.AllOfMemory";
        public override string Description => "Show a Memory Profiler All Of Memory style report with bounded sections and filters";

        protected override ValueTask<MemorySnapshotAllOfMemoryResponse> ExecuteAsync(MemorySnapshotAllOfMemoryRequest request, CancellationToken cancellationToken)
        {
            var scope = MemorySnapshotAllOfMemoryScopeParser.Parse(request.scope);
            if (scope == MemorySnapshotAllOfMemoryScope.Managed && !string.IsNullOrEmpty(request.nameFilter))
            {
                throw new CommandFailedException(
                    "MemorySnapshot.AllOfMemory nameFilter applies only to native object rows; use scope=all/native with includeNativeObjects=true.",
                    null);
            }

            var limit = MemorySnapshotFormatting.ClampLimit(request.limit);
            var minSize = Math.Max(0, request.minSize);
            var minSizeDelta = Math.Max(0, request.minSizeDelta);
            var includeNativeTypes = request.includeNativeTypes && scope != MemorySnapshotAllOfMemoryScope.Managed;
            var includeManagedTypes = request.includeManagedTypes && scope != MemorySnapshotAllOfMemoryScope.Native;
            var includeNativeObjects = (request.includeNativeObjects || !string.IsNullOrEmpty(request.nameFilter)) &&
                scope != MemorySnapshotAllOfMemoryScope.Managed;
            var includeDiff = request.includeDiff && (!string.IsNullOrEmpty(request.baseSnapshot) || !string.IsNullOrEmpty(request.basePath));

            var analysis = GetAnalysisByReferenceOrDefault(
                request.snapshot,
                request.path,
                0,
                out var cached,
                out var analysisMs,
                out var entry);

            var response = new MemorySnapshotAllOfMemoryResponse
            {
                id = entry.id,
                name = entry.name,
                path = analysis.Path,
                fileSize = analysis.FileSize,
                analysisMs = analysisMs,
                cached = cached,
                metadata = analysis.Metadata,
                total = MemorySnapshotAnalysisQueries.GetTotal(analysis),
                categories = request.includeCategories ? analysis.Categories : Array.Empty<MemorySnapshotCategoryInfo>(),
                filters = new MemorySnapshotAllOfMemoryFilterInfo
                {
                    scope = MemorySnapshotAllOfMemoryScopeParser.Name(scope),
                    limit = limit,
                    typeFilter = request.typeFilter ?? "",
                    nameFilter = request.nameFilter ?? "",
                    minSize = minSize,
                    minSizeDelta = minSizeDelta,
                    includeCategories = request.includeCategories,
                    includeNativeTypes = includeNativeTypes,
                    includeManagedTypes = includeManagedTypes,
                    includeNativeObjects = includeNativeObjects,
                    includeDiff = includeDiff
                }
            };

            if (includeNativeTypes)
                response.nativeTypes = ToTypeSection(analysis, MemorySnapshotScope.Native, request.typeFilter, limit, minSize);

            if (includeManagedTypes)
                response.managedTypes = ToTypeSection(analysis, MemorySnapshotScope.Managed, request.typeFilter, limit, minSize);

            if (includeNativeObjects)
            {
                response.nativeObjects = new MemorySnapshotAllOfMemoryObjectSection
                {
                    included = true,
                    objects = MemorySnapshotAnalysisQueries.GetTopObjects(
                        analysis,
                        request.typeFilter,
                        request.nameFilter,
                        limit,
                        minSize,
                        out var truncated),
                    truncated = truncated
                };
            }

            if (includeDiff)
            {
                var baseAnalysis = GetAnalysisByReference(
                    request.baseSnapshot,
                    request.basePath,
                    out var baseCached,
                    out var baseAnalysisMs,
                    out var baseEntry);

                response.analysisMs += baseAnalysisMs;
                response.cached = response.cached && baseCached;
                response.diff = new MemorySnapshotAllOfMemoryDiffSection
                {
                    included = true,
                    @base = ToEndpoint(baseEntry, baseAnalysis),
                    target = ToEndpoint(entry, analysis)
                };

                if (includeNativeTypes)
                    response.diff.nativeTypes = ToDiffSection(baseAnalysis, analysis, MemorySnapshotScope.Native, request.typeFilter, limit, minSizeDelta);

                if (includeManagedTypes)
                    response.diff.managedTypes = ToDiffSection(baseAnalysis, analysis, MemorySnapshotScope.Managed, request.typeFilter, limit, minSizeDelta);
            }

            return new ValueTask<MemorySnapshotAllOfMemoryResponse>(response);
        }

        private static MemorySnapshotAllOfMemoryTypeSection ToTypeSection(
            SnapshotAnalysis analysis,
            MemorySnapshotScope scope,
            string typeFilter,
            int limit,
            long minSize)
        {
            var result = MemorySnapshotAnalysisQueries.GetTopTypes(analysis, scope, typeFilter, limit, minSize);
            return new MemorySnapshotAllOfMemoryTypeSection
            {
                included = true,
                scope = MemorySnapshotFormatting.ScopeName(scope),
                totalCount = MemorySnapshotAnalysisQueries.GetObjectCount(analysis, scope),
                totalSize = MemorySnapshotAnalysisQueries.GetTotalSize(analysis, scope),
                types = result.Types,
                othersCount = result.OthersCount,
                othersSize = result.OthersSize
            };
        }

        private static MemorySnapshotAllOfMemoryTypeDiffSection ToDiffSection(
            SnapshotAnalysis baseAnalysis,
            SnapshotAnalysis targetAnalysis,
            MemorySnapshotScope scope,
            string typeFilter,
            int limit,
            long minSizeDelta)
        {
            var result = MemorySnapshotAnalysisQueries.GetDiffTypes(baseAnalysis, targetAnalysis, scope, typeFilter, limit, minSizeDelta);
            return new MemorySnapshotAllOfMemoryTypeDiffSection
            {
                included = true,
                scope = MemorySnapshotFormatting.ScopeName(scope),
                totalSizeDelta = MemorySnapshotAnalysisQueries.GetTotalSize(targetAnalysis, scope) -
                    MemorySnapshotAnalysisQueries.GetTotalSize(baseAnalysis, scope),
                types = result.Types,
                othersCount = result.OthersCount,
                othersSize = result.OthersSize
            };
        }

        private static MemorySnapshotAllOfMemoryEndpointInfo ToEndpoint(MemorySnapshotStatusEntry entry, SnapshotAnalysis analysis)
        {
            return new MemorySnapshotAllOfMemoryEndpointInfo
            {
                id = entry.id,
                name = entry.name,
                path = analysis.Path,
                totalNativeSize = analysis.TotalNativeSize,
                totalManagedSize = analysis.TotalManagedObjectSize,
                totalSize = analysis.TotalNativeSize + analysis.TotalManagedObjectSize,
                captureDate = analysis.Metadata.captureDate
            };
        }
    }

    [Serializable]
    public class MemorySnapshotAllOfMemoryRequest
    {
        public string snapshot;
        public string path;
        public string baseSnapshot;
        public string basePath;
        public string scope;
        public int limit = 20;
        public string typeFilter;
        public string nameFilter;
        public long minSize;
        public long minSizeDelta;
        public bool includeCategories = true;
        public bool includeNativeTypes = true;
        public bool includeManagedTypes = true;
        public bool includeNativeObjects;
        public bool includeDiff = true;
    }

    [Serializable]
    public class MemorySnapshotAllOfMemoryResponse
    {
        public string id;
        public string name;
        public string path;
        public long fileSize;
        public long analysisMs;
        public bool cached;
        public MemorySnapshotMetadataInfo metadata;
        public MemorySnapshotTotalInfo total;
        public MemorySnapshotCategoryInfo[] categories;
        public MemorySnapshotAllOfMemoryTypeSection nativeTypes;
        public MemorySnapshotAllOfMemoryTypeSection managedTypes;
        public MemorySnapshotAllOfMemoryObjectSection nativeObjects;
        public MemorySnapshotAllOfMemoryDiffSection diff;
        public MemorySnapshotAllOfMemoryFilterInfo filters;
    }

    [Serializable]
    public class MemorySnapshotAllOfMemoryFilterInfo
    {
        public string scope;
        public int limit;
        public string typeFilter;
        public string nameFilter;
        public long minSize;
        public long minSizeDelta;
        public bool includeCategories;
        public bool includeNativeTypes;
        public bool includeManagedTypes;
        public bool includeNativeObjects;
        public bool includeDiff;
    }

    [Serializable]
    public class MemorySnapshotAllOfMemoryTypeSection
    {
        public bool included;
        public string scope;
        public long totalCount;
        public long totalSize;
        public MemorySnapshotNativeTypeStat[] types;
        public long othersCount;
        public long othersSize;
    }

    [Serializable]
    public class MemorySnapshotAllOfMemoryObjectSection
    {
        public bool included;
        public MemorySnapshotNativeObjectInfo[] objects;
        public bool truncated;
    }

    [Serializable]
    public class MemorySnapshotAllOfMemoryDiffSection
    {
        public bool included;
        public MemorySnapshotAllOfMemoryEndpointInfo @base;
        public MemorySnapshotAllOfMemoryEndpointInfo target;
        public MemorySnapshotAllOfMemoryTypeDiffSection nativeTypes;
        public MemorySnapshotAllOfMemoryTypeDiffSection managedTypes;
    }

    [Serializable]
    public class MemorySnapshotAllOfMemoryEndpointInfo
    {
        public string id;
        public string name;
        public string path;
        public long totalNativeSize;
        public long totalManagedSize;
        public long totalSize;
        public string captureDate;
    }

    [Serializable]
    public class MemorySnapshotAllOfMemoryTypeDiffSection
    {
        public bool included;
        public string scope;
        public long totalSizeDelta;
        public MemorySnapshotNativeTypeDiffInfo[] types;
        public long othersCount;
        public long othersSize;
    }

    internal enum MemorySnapshotAllOfMemoryScope
    {
        All,
        Native,
        Managed
    }

    internal static class MemorySnapshotAllOfMemoryScopeParser
    {
        public static MemorySnapshotAllOfMemoryScope Parse(string scope)
        {
            if (string.IsNullOrEmpty(scope) ||
                string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase))
            {
                return MemorySnapshotAllOfMemoryScope.All;
            }

            if (string.Equals(scope, MemorySnapshotFormatting.NativeScopeName, StringComparison.OrdinalIgnoreCase))
                return MemorySnapshotAllOfMemoryScope.Native;

            if (string.Equals(scope, MemorySnapshotFormatting.ManagedScopeName, StringComparison.OrdinalIgnoreCase))
                return MemorySnapshotAllOfMemoryScope.Managed;

            throw new CommandFailedException(
                "Unknown All Of Memory scope: " + scope + ". Valid values are: all, native, managed.",
                null);
        }

        public static string Name(MemorySnapshotAllOfMemoryScope scope)
        {
            if (scope == MemorySnapshotAllOfMemoryScope.Native)
                return MemorySnapshotFormatting.NativeScopeName;

            return scope == MemorySnapshotAllOfMemoryScope.Managed
                ? MemorySnapshotFormatting.ManagedScopeName
                : "all";
        }
    }
}
#endif
