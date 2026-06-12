#if UNICLI_MEMORY_PROFILER
using System;
using System.Collections.Generic;
using System.Linq;

namespace UniCli.Server.Editor.Handlers
{
    internal sealed class SnapshotAnalysis
    {
        public string Path;
        public long FileSize;
        public long FileMtimeTicks;
        public string SnapshotId;
        public string SnapshotName;
        public bool Pinned;
        public string AnalyzedAtUtc;
        public string LastAccessedAtUtc;
        public long AnalysisMs;
        public MemorySnapshotMetadataInfo Metadata;
        public MemorySnapshotCategoryInfo[] Categories;
        public MemorySnapshotNativeTypeStat[] NativeTypeStats;
        public MemorySnapshotNativeTypeStat[] ManagedTypeStats;
        public RetainedNativeObjectInfo[] TopNativeObjects;
        public MemorySnapshotBreakdownTreeNode[] BreakdownTree;
        public string BreakdownTreeUnavailableReason;
        public long NativeObjectCount;
        public long TotalNativeSize;
        public long ManagedObjectCount;
        public long TotalManagedObjectSize;
    }

    internal sealed class RetainedNativeObjectInfo
    {
        public long NativeObjectIndex;
        public string Name;
        public string TypeName;
        public long Size;
        public string InstanceId;

        public MemorySnapshotNativeObjectInfo ToResponse()
        {
            return new MemorySnapshotNativeObjectInfo
            {
                name = Name,
                typeName = TypeName,
                size = Size,
                instanceId = InstanceId
            };
        }
    }

    internal sealed class MemorySnapshotBreakdownTreeNode
    {
        public string Path;
        public string Name;
        public int Depth;
        public long Allocated;
        public long Resident;
        public bool ResidentAvailable;
        public int RetainedChildrenCount;
        public bool ChildrenTruncated;
    }

    internal sealed class NativeObjectRetainer
    {
        private readonly TopObjectSet _globalTopObjects;
        private readonly Dictionary<string, TopObjectSet> _topObjectsByType =
            new Dictionary<string, TopObjectSet>(StringComparer.Ordinal);
        private readonly int _perTypeCapacity;

        public NativeObjectRetainer(int globalCapacity, int perTypeCapacity)
        {
            _globalTopObjects = new TopObjectSet(globalCapacity);
            _perTypeCapacity = perTypeCapacity;
        }

        public void Add(RetainedNativeObjectInfo item)
        {
            _globalTopObjects.Add(item);

            if (!_topObjectsByType.TryGetValue(item.TypeName, out var perType))
            {
                perType = new TopObjectSet(_perTypeCapacity);
                _topObjectsByType[item.TypeName] = perType;
            }

            perType.Add(item);
        }

        public RetainedNativeObjectInfo[] Build()
        {
            var byIndex = new Dictionary<long, RetainedNativeObjectInfo>();
            AddRange(byIndex, _globalTopObjects.Items);

            foreach (var set in _topObjectsByType.Values)
                AddRange(byIndex, set.Items);

            return byIndex.Values
                .OrderByDescending(item => item.Size)
                .ThenBy(item => item.TypeName, StringComparer.Ordinal)
                .ThenBy(item => item.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static void AddRange(Dictionary<long, RetainedNativeObjectInfo> byIndex, IEnumerable<RetainedNativeObjectInfo> items)
        {
            foreach (var item in items)
                byIndex[item.NativeObjectIndex] = item;
        }

        private sealed class TopObjectSet
        {
            private readonly int _capacity;
            private readonly List<RetainedNativeObjectInfo> _items;
            private int _minIndex = -1;
            private long _minSize = long.MaxValue;

            public TopObjectSet(int capacity)
            {
                _capacity = Math.Max(0, capacity);
                _items = new List<RetainedNativeObjectInfo>(_capacity);
            }

            public IEnumerable<RetainedNativeObjectInfo> Items => _items;

            public void Add(RetainedNativeObjectInfo item)
            {
                if (_capacity <= 0)
                    return;

                if (_items.Count < _capacity)
                {
                    _items.Add(item);
                    if (item.Size < _minSize)
                    {
                        _minSize = item.Size;
                        _minIndex = _items.Count - 1;
                    }
                    return;
                }

                if (item.Size <= _minSize)
                    return;

                _items[_minIndex] = item;
                RecomputeMinimum();
            }

            private void RecomputeMinimum()
            {
                _minIndex = 0;
                _minSize = _items[0].Size;

                for (var i = 1; i < _items.Count; i++)
                {
                    if (_items[i].Size >= _minSize)
                        continue;

                    _minSize = _items[i].Size;
                    _minIndex = i;
                }
            }
        }
    }

    internal static class MemorySnapshotAnalysisQueries
    {
        public static MemorySnapshotNativeObjectInfo[] GetTopObjects(
            SnapshotAnalysis analysis,
            string typeFilter,
            string nameFilter,
            int limit,
            out bool truncated)
        {
            return GetTopObjects(analysis, typeFilter, nameFilter, limit, 0, out truncated);
        }

        public static MemorySnapshotNativeObjectInfo[] GetTopObjects(
            SnapshotAnalysis analysis,
            string typeFilter,
            string nameFilter,
            int limit,
            long minSize,
            out bool truncated)
        {
            var filtered = analysis.TopNativeObjects
                .Where(item => item.Size >= minSize && Matches(item.TypeName, typeFilter) && Matches(item.Name, nameFilter))
                .OrderByDescending(item => item.Size)
                .ThenBy(item => item.TypeName, StringComparer.Ordinal)
                .ThenBy(item => item.Name, StringComparer.Ordinal)
                .ToArray();

            var clampedLimit = MemorySnapshotFormatting.ClampLimit(limit);
            truncated = filtered.Length > clampedLimit || MayHaveUnretainedMatches(analysis, typeFilter, nameFilter, filtered.Length);

            return filtered
                .Take(clampedLimit)
                .Select(item => item.ToResponse())
                .ToArray();
        }

        public static MemorySnapshotNativeTypeStat[] GetTopTypes(SnapshotAnalysis analysis, string typeFilter, int limit)
        {
            return GetTopTypes(analysis, MemorySnapshotScope.Native, typeFilter, limit, 0).Types;
        }

        public static MemorySnapshotTypeStatResult GetTopTypes(
            SnapshotAnalysis analysis,
            MemorySnapshotScope scope,
            string typeFilter,
            int limit,
            long minSize)
        {
            return GetTopTypes(GetTypeStats(analysis, scope), typeFilter, limit, minSize);
        }

        public static MemorySnapshotTypeStatResult GetTopTypes(
            MemorySnapshotNativeTypeStat[] stats,
            string typeFilter,
            int limit,
            long minSize)
        {
            var clampedLimit = MemorySnapshotFormatting.ClampLimit(limit);
            var rows = (stats ?? Array.Empty<MemorySnapshotNativeTypeStat>())
                .Where(stat => stat.count > 0 && Matches(stat.typeName, typeFilter))
                .OrderByDescending(stat => stat.totalSize)
                .ThenBy(stat => stat.typeName, StringComparer.Ordinal)
                .ToArray();

            var included = new List<MemorySnapshotNativeTypeStat>();
            long othersCount = 0;
            long othersSize = 0;

            foreach (var row in rows)
            {
                if (row.totalSize < minSize || included.Count >= clampedLimit)
                {
                    othersCount++;
                    othersSize += row.totalSize;
                    continue;
                }

                included.Add(row);
            }

            return new MemorySnapshotTypeStatResult
            {
                Types = included.ToArray(),
                OthersCount = othersCount,
                OthersSize = othersSize
            };
        }

        public static MemorySnapshotNativeTypeDiffInfo[] GetDiffTypes(
            SnapshotAnalysis baseAnalysis,
            SnapshotAnalysis targetAnalysis,
            string typeFilter,
            int limit)
        {
            return GetDiffTypes(baseAnalysis, targetAnalysis, MemorySnapshotScope.Native, typeFilter, limit, 0).Types;
        }

        public static MemorySnapshotTypeDiffResult GetDiffTypes(
            SnapshotAnalysis baseAnalysis,
            SnapshotAnalysis targetAnalysis,
            MemorySnapshotScope scope,
            string typeFilter,
            int limit,
            long minSizeDelta)
        {
            return GetDiffTypes(
                GetTypeStats(baseAnalysis, scope),
                GetTypeStats(targetAnalysis, scope),
                typeFilter,
                limit,
                minSizeDelta);
        }

        public static MemorySnapshotTypeDiffResult GetDiffTypes(
            MemorySnapshotNativeTypeStat[] baseTypeStats,
            MemorySnapshotNativeTypeStat[] targetTypeStats,
            string typeFilter,
            int limit,
            long minSizeDelta)
        {
            var clampedLimit = MemorySnapshotFormatting.ClampLimit(limit);
            var baseStats = (baseTypeStats ?? Array.Empty<MemorySnapshotNativeTypeStat>())
                .ToDictionary(stat => stat.typeName, StringComparer.Ordinal);
            var targetStats = (targetTypeStats ?? Array.Empty<MemorySnapshotNativeTypeStat>())
                .ToDictionary(stat => stat.typeName, StringComparer.Ordinal);
            var names = new HashSet<string>(baseStats.Keys, StringComparer.Ordinal);
            names.UnionWith(targetStats.Keys);

            var rows = new List<MemorySnapshotNativeTypeDiffInfo>();
            foreach (var name in names)
            {
                if (!Matches(name, typeFilter))
                    continue;

                baseStats.TryGetValue(name, out var baseStat);
                targetStats.TryGetValue(name, out var targetStat);

                var baseCount = baseStat?.count ?? 0;
                var targetCount = targetStat?.count ?? 0;
                var baseSize = baseStat?.totalSize ?? 0;
                var targetSize = targetStat?.totalSize ?? 0;
                var countDelta = targetCount - baseCount;
                var sizeDelta = targetSize - baseSize;

                if (countDelta == 0 && sizeDelta == 0)
                    continue;

                rows.Add(new MemorySnapshotNativeTypeDiffInfo
                {
                    typeName = name,
                    baseCount = baseCount,
                    targetCount = targetCount,
                    countDelta = countDelta,
                    baseSize = baseSize,
                    targetSize = targetSize,
                    sizeDelta = sizeDelta
                });
            }

            var sortedRows = rows
                .OrderByDescending(row => Math.Abs(row.sizeDelta))
                .ThenBy(row => row.typeName, StringComparer.Ordinal)
                .ToArray();

            var included = new List<MemorySnapshotNativeTypeDiffInfo>();
            long othersCount = 0;
            long othersSize = 0;

            foreach (var row in sortedRows)
            {
                var absoluteDelta = SafeAbs(row.sizeDelta);
                if (absoluteDelta < minSizeDelta || included.Count >= clampedLimit)
                {
                    othersCount++;
                    othersSize += absoluteDelta;
                    continue;
                }

                included.Add(row);
            }

            return new MemorySnapshotTypeDiffResult
            {
                Types = included.ToArray(),
                OthersCount = othersCount,
                OthersSize = othersSize
            };
        }

        public static MemorySnapshotTotalInfo GetTotal(SnapshotAnalysis analysis)
        {
            long committed = 0;
            long resident = 0;
            var residentAvailable = false;

            foreach (var category in analysis.Categories)
            {
                committed += category.committed;
                if (category.residentAvailable)
                {
                    resident += category.resident;
                    residentAvailable = true;
                }
            }

            return new MemorySnapshotTotalInfo
            {
                committed = committed,
                resident = resident,
                residentAvailable = residentAvailable
            };
        }

        public static MemorySnapshotBreakdownTreeResult GetBreakdownTree(
            SnapshotAnalysis analysis,
            string pathFilter,
            int pathDepth,
            MemorySnapshotMemoryMetric memoryMetric,
            long minSize,
            int responseLimit)
        {
            var source = analysis.BreakdownTree;
            var hasResident = source != null && source.Any(node => node.ResidentAvailable);
            var effectiveMetric = memoryMetric == MemorySnapshotMemoryMetric.Resident && !hasResident
                ? MemorySnapshotMemoryMetric.Allocated
                : memoryMetric;

            if (source == null)
            {
                return new MemorySnapshotBreakdownTreeResult
                {
                    Included = false,
                    UnavailableReason = string.IsNullOrEmpty(analysis.BreakdownTreeUnavailableReason)
                        ? "All Of Memory breakdown tree is unavailable."
                        : analysis.BreakdownTreeUnavailableReason,
                    Nodes = Array.Empty<MemorySnapshotBreakdownTreeNodeInfo>(),
                    EffectivePathDepth = ClampBreakdownPathDepth(pathDepth, 0),
                    EffectiveMemoryMetric = effectiveMetric
                };
            }

            var clampedMinSize = Math.Max(0, minSize);
            var pathSegments = SplitPath(pathFilter);
            var pathLookup = BuildPathLookup(source);
            var match = pathSegments.Length > 0 ? FindPathMatch(source, pathSegments) : null;
            var maxRelativeDepth = GetMaxRelativeDepth(source, match, pathSegments.Length > 0);
            var effectivePathDepth = ClampBreakdownPathDepth(pathDepth, maxRelativeDepth);

            if (pathSegments.Length > 0 && match == null)
            {
                return new MemorySnapshotBreakdownTreeResult
                {
                    Included = true,
                    Nodes = Array.Empty<MemorySnapshotBreakdownTreeNodeInfo>(),
                    EffectivePathDepth = effectivePathDepth,
                    EffectiveMemoryMetric = effectiveMetric
                };
            }

            var includePaths = BuildBreakdownIncludeSet(
                source,
                pathLookup,
                match,
                pathSegments.Length > 0,
                effectivePathDepth,
                effectiveMetric,
                clampedMinSize);
            var filtered = source
                .Where(node => includePaths.Contains(node.Path))
                .ToArray();
            var ordered = SortBreakdownPreOrder(filtered, effectiveMetric);
            var clampedLimit = MemorySnapshotBreakdownTreeLimits.ClampResponseLimit(responseLimit);
            var limited = ordered.Take(clampedLimit).ToArray();
            var limitedPathSet = new HashSet<string>(limited.Select(node => node.Path), StringComparer.OrdinalIgnoreCase);
            var returnedChildrenCounts = CountReturnedChildren(limited);

            return new MemorySnapshotBreakdownTreeResult
            {
                Included = true,
                Nodes = limited
                    .Select(node => ToBreakdownTreeInfo(node, limitedPathSet, returnedChildrenCounts))
                    .ToArray(),
                Truncated = ordered.Length > clampedLimit || limited.Any(node => HasHiddenChildren(node, limitedPathSet)),
                EffectivePathDepth = effectivePathDepth,
                EffectiveMemoryMetric = effectiveMetric
            };
        }

        public static MemorySnapshotNativeTypeStat[] GetTypeStats(SnapshotAnalysis analysis, MemorySnapshotScope scope)
        {
            return scope == MemorySnapshotScope.Managed
                ? analysis.ManagedTypeStats ?? Array.Empty<MemorySnapshotNativeTypeStat>()
                : analysis.NativeTypeStats ?? Array.Empty<MemorySnapshotNativeTypeStat>();
        }

        public static long GetObjectCount(SnapshotAnalysis analysis, MemorySnapshotScope scope)
        {
            return scope == MemorySnapshotScope.Managed ? analysis.ManagedObjectCount : analysis.NativeObjectCount;
        }

        public static long GetTotalSize(SnapshotAnalysis analysis, MemorySnapshotScope scope)
        {
            return scope == MemorySnapshotScope.Managed ? analysis.TotalManagedObjectSize : analysis.TotalNativeSize;
        }

        private static HashSet<string> BuildBreakdownIncludeSet(
            MemorySnapshotBreakdownTreeNode[] source,
            Dictionary<string, MemorySnapshotBreakdownTreeNode> pathLookup,
            MemorySnapshotBreakdownTreeNode match,
            bool hasPathFilter,
            int effectivePathDepth,
            MemorySnapshotMemoryMetric metric,
            long minSize)
        {
            var includePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in source)
            {
                if (!IsWithinBreakdownDepth(node, match, hasPathFilter, effectivePathDepth))
                    continue;

                var isContextNode = hasPathFilter && IsAncestorOrSelf(node.Path, match.Path);
                if (!isContextNode && MetricValue(node, metric) < minSize)
                    continue;

                AddNodeAndAncestors(includePaths, pathLookup, node.Path);
            }

            return includePaths;
        }

        private static bool IsWithinBreakdownDepth(
            MemorySnapshotBreakdownTreeNode node,
            MemorySnapshotBreakdownTreeNode match,
            bool hasPathFilter,
            int effectivePathDepth)
        {
            if (!hasPathFilter)
                return node.Depth <= effectivePathDepth;

            if (IsAncestorOrSelf(node.Path, match.Path))
                return true;

            return IsAncestorOrSelf(match.Path, node.Path) &&
                node.Depth - match.Depth <= effectivePathDepth;
        }

        private static MemorySnapshotBreakdownTreeNode[] SortBreakdownPreOrder(
            MemorySnapshotBreakdownTreeNode[] nodes,
            MemorySnapshotMemoryMetric metric)
        {
            var nodePaths = new HashSet<string>(nodes.Select(node => node.Path), StringComparer.OrdinalIgnoreCase);
            var roots = new List<MemorySnapshotBreakdownTreeNode>();
            var childrenByParent = new Dictionary<string, List<MemorySnapshotBreakdownTreeNode>>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                var parentPath = GetParentPath(node.Path);
                if (parentPath.Length == 0 || !nodePaths.Contains(parentPath))
                {
                    roots.Add(node);
                    continue;
                }

                if (!childrenByParent.TryGetValue(parentPath, out var children))
                {
                    children = new List<MemorySnapshotBreakdownTreeNode>();
                    childrenByParent[parentPath] = children;
                }

                children.Add(node);
            }

            var result = new List<MemorySnapshotBreakdownTreeNode>(nodes.Length);
            AddSortedBreakdownNodes(roots, childrenByParent, metric, result);
            return result.ToArray();
        }

        private static void AddSortedBreakdownNodes(
            List<MemorySnapshotBreakdownTreeNode> nodes,
            Dictionary<string, List<MemorySnapshotBreakdownTreeNode>> childrenByParent,
            MemorySnapshotMemoryMetric metric,
            List<MemorySnapshotBreakdownTreeNode> result)
        {
            foreach (var node in SortBreakdownSiblings(nodes, metric))
            {
                result.Add(node);
                if (childrenByParent.TryGetValue(node.Path, out var children))
                    AddSortedBreakdownNodes(children, childrenByParent, metric, result);
            }
        }

        private static IEnumerable<MemorySnapshotBreakdownTreeNode> SortBreakdownSiblings(
            IEnumerable<MemorySnapshotBreakdownTreeNode> nodes,
            MemorySnapshotMemoryMetric metric)
        {
            return nodes
                .OrderByDescending(node => MetricValue(node, metric))
                .ThenBy(node => node.Name, StringComparer.Ordinal)
                .ThenBy(node => node.Path, StringComparer.Ordinal);
        }

        private static MemorySnapshotBreakdownTreeNodeInfo ToBreakdownTreeInfo(
            MemorySnapshotBreakdownTreeNode node,
            HashSet<string> returnedPathSet,
            Dictionary<string, int> returnedChildrenCounts)
        {
            returnedChildrenCounts.TryGetValue(node.Path, out var childrenCount);
            return new MemorySnapshotBreakdownTreeNodeInfo
            {
                path = node.Path,
                name = node.Name,
                depth = node.Depth,
                allocated = node.Allocated,
                resident = node.Resident,
                residentAvailable = node.ResidentAvailable,
                childrenCount = childrenCount,
                childrenTruncated = HasHiddenChildren(node, returnedPathSet)
            };
        }

        private static bool HasHiddenChildren(
            MemorySnapshotBreakdownTreeNode node,
            HashSet<string> returnedPathSet)
        {
            var returnedChildrenCount = 0;
            foreach (var path in returnedPathSet)
            {
                if (IsDirectChildPath(node.Path, path))
                    returnedChildrenCount++;
            }

            return node.ChildrenTruncated || node.RetainedChildrenCount > returnedChildrenCount;
        }

        private static Dictionary<string, int> CountReturnedChildren(MemorySnapshotBreakdownTreeNode[] nodes)
        {
            var returnedPathSet = new HashSet<string>(nodes.Select(node => node.Path), StringComparer.OrdinalIgnoreCase);
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in nodes)
            {
                var parentPath = GetParentPath(node.Path);
                if (parentPath.Length == 0 || !returnedPathSet.Contains(parentPath))
                    continue;

                counts.TryGetValue(parentPath, out var count);
                counts[parentPath] = count + 1;
            }

            return counts;
        }

        private static int GetMaxRelativeDepth(
            MemorySnapshotBreakdownTreeNode[] source,
            MemorySnapshotBreakdownTreeNode match,
            bool hasPathFilter)
        {
            if (source.Length == 0)
                return 0;

            if (!hasPathFilter)
                return source.Max(node => node.Depth);

            if (match == null)
                return source.Max(node => node.Depth);

            return source
                .Where(node => IsAncestorOrSelf(match.Path, node.Path))
                .Select(node => node.Depth - match.Depth)
                .DefaultIfEmpty(0)
                .Max();
        }

        private static int ClampBreakdownPathDepth(int pathDepth, int maxDepth)
        {
            var requested = pathDepth <= 0 ? MemorySnapshotBreakdownTreeLimits.DefaultPathDepth : pathDepth;
            return Math.Max(0, Math.Min(requested, Math.Max(0, maxDepth)));
        }

        private static MemorySnapshotBreakdownTreeNode FindPathMatch(
            MemorySnapshotBreakdownTreeNode[] nodes,
            string[] pathSegments)
        {
            return nodes.FirstOrDefault(node => MatchesPathSegments(node.Path, pathSegments));
        }

        private static bool MatchesPathSegments(string path, string[] pathSegments)
        {
            var nodeSegments = SplitPath(path);
            if (nodeSegments.Length != pathSegments.Length)
                return false;

            for (var i = 0; i < nodeSegments.Length; i++)
            {
                if (!string.Equals(nodeSegments[i], pathSegments[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static string[] SplitPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Array.Empty<string>();

            return path
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Trim())
                .Where(segment => segment.Length > 0)
                .ToArray();
        }

        private static Dictionary<string, MemorySnapshotBreakdownTreeNode> BuildPathLookup(
            MemorySnapshotBreakdownTreeNode[] nodes)
        {
            var lookup = new Dictionary<string, MemorySnapshotBreakdownTreeNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in nodes)
            {
                if (!lookup.ContainsKey(node.Path))
                    lookup.Add(node.Path, node);
            }

            return lookup;
        }

        private static void AddNodeAndAncestors(
            HashSet<string> includePaths,
            Dictionary<string, MemorySnapshotBreakdownTreeNode> pathLookup,
            string path)
        {
            while (!string.IsNullOrEmpty(path))
            {
                if (!pathLookup.ContainsKey(path))
                    break;

                includePaths.Add(path);
                path = GetParentPath(path);
            }
        }

        private static string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";

            var index = path.LastIndexOf('/');
            return index <= 0 ? "" : path.Substring(0, index);
        }

        private static bool IsAncestorOrSelf(string ancestorPath, string path)
        {
            if (string.Equals(ancestorPath, path, StringComparison.OrdinalIgnoreCase))
                return true;

            return path.StartsWith(ancestorPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDirectChildPath(string parentPath, string path)
        {
            if (!IsAncestorOrSelf(parentPath, path) || string.Equals(parentPath, path, StringComparison.OrdinalIgnoreCase))
                return false;

            return string.Equals(GetParentPath(path), parentPath, StringComparison.OrdinalIgnoreCase);
        }

        private static long MetricValue(MemorySnapshotBreakdownTreeNode node, MemorySnapshotMemoryMetric metric)
        {
            return metric == MemorySnapshotMemoryMetric.Resident ? node.Resident : node.Allocated;
        }

        private static bool Matches(string value, string filter)
        {
            return string.IsNullOrEmpty(filter)
                || (value ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static long SafeAbs(long value)
        {
            return value == long.MinValue ? long.MaxValue : Math.Abs(value);
        }

        private static bool MayHaveUnretainedMatches(
            SnapshotAnalysis analysis,
            string typeFilter,
            string nameFilter,
            int retainedMatchingCount)
        {
            if (analysis.NativeObjectCount <= analysis.TopNativeObjects.Length)
                return false;

            if (!string.IsNullOrEmpty(nameFilter))
                return true;

            var matchingObjectCount = analysis.NativeTypeStats
                .Where(stat => Matches(stat.typeName, typeFilter))
                .Sum(stat => stat.count);

            return matchingObjectCount > retainedMatchingCount;
        }
    }

    internal static class MemorySnapshotFormatting
    {
        public const string NativeScopeName = "native";
        public const string ManagedScopeName = "managed";

        public static int ClampLimit(int limit)
        {
            if (limit <= 0)
                return 20;
            if (limit > 200)
                return 200;
            return limit;
        }

        public static int ClampAnalyzeLimit(int limit)
        {
            if (limit <= 0)
                return 10;
            if (limit > 50)
                return 50;
            return limit;
        }

        public static string ScopeName(MemorySnapshotScope scope)
        {
            return scope == MemorySnapshotScope.Managed ? ManagedScopeName : NativeScopeName;
        }
    }

    internal static class MemorySnapshotBreakdownTreeLimits
    {
        public const int DefaultPathDepth = 2;
        public const int MaxRetainedDepth = 4;
        public const int MaxAnalysisNodes = 5000;
        public const int MaxChildrenPerNode = 50;
        public const int MaxResponseNodes = 500;

        public static int ClampResponseLimit(int limit)
        {
            if (limit <= 0)
                return MaxResponseNodes;
            if (limit > MaxResponseNodes)
                return MaxResponseNodes;
            return limit;
        }
    }

    internal enum MemorySnapshotScope
    {
        Native,
        Managed
    }

    internal static class MemorySnapshotScopeParser
    {
        public static MemorySnapshotScope Parse(string scope)
        {
            if (string.IsNullOrEmpty(scope) ||
                string.Equals(scope, MemorySnapshotFormatting.NativeScopeName, StringComparison.OrdinalIgnoreCase))
            {
                return MemorySnapshotScope.Native;
            }

            if (string.Equals(scope, MemorySnapshotFormatting.ManagedScopeName, StringComparison.OrdinalIgnoreCase))
                return MemorySnapshotScope.Managed;

            throw new CommandFailedException(
                "Unknown memory snapshot scope: " + scope + ". Valid values are: native, managed.",
                null);
        }
    }

    internal enum MemorySnapshotMemoryMetric
    {
        Allocated,
        Resident,
        Both
    }

    internal static class MemorySnapshotMemoryMetricParser
    {
        public static MemorySnapshotMemoryMetric Parse(string metric)
        {
            if (string.IsNullOrEmpty(metric) ||
                string.Equals(metric, "allocated", StringComparison.OrdinalIgnoreCase))
            {
                return MemorySnapshotMemoryMetric.Allocated;
            }

            if (string.Equals(metric, "resident", StringComparison.OrdinalIgnoreCase))
                return MemorySnapshotMemoryMetric.Resident;

            if (string.Equals(metric, "both", StringComparison.OrdinalIgnoreCase))
                return MemorySnapshotMemoryMetric.Both;

            throw new CommandFailedException(
                "Unknown memory metric: " + metric + ". Valid values are: allocated, resident, both.",
                null);
        }

        public static string Name(MemorySnapshotMemoryMetric metric)
        {
            if (metric == MemorySnapshotMemoryMetric.Resident)
                return "resident";

            return metric == MemorySnapshotMemoryMetric.Both ? "both" : "allocated";
        }
    }

    internal sealed class MemorySnapshotTypeStatResult
    {
        public MemorySnapshotNativeTypeStat[] Types;
        public long OthersCount;
        public long OthersSize;
    }

    internal sealed class MemorySnapshotTypeDiffResult
    {
        public MemorySnapshotNativeTypeDiffInfo[] Types;
        public long OthersCount;
        public long OthersSize;
    }

    internal sealed class MemorySnapshotBreakdownTreeResult
    {
        public bool Included;
        public string UnavailableReason;
        public MemorySnapshotBreakdownTreeNodeInfo[] Nodes;
        public bool Truncated;
        public int EffectivePathDepth;
        public MemorySnapshotMemoryMetric EffectiveMemoryMetric;
    }

    [Serializable]
    public sealed class MemorySnapshotMetadataInfo
    {
        public string unityVersion;
        public string platform;
        public bool isEditorCapture;
        public string captureDate;
        public string captureFlags;
    }

    [Serializable]
    public sealed class MemorySnapshotCategoryInfo
    {
        public string name;
        public long committed;
        public long resident;
        public bool residentAvailable;
    }

    [Serializable]
    public sealed class MemorySnapshotTotalInfo
    {
        public long committed;
        public long resident;
        public bool residentAvailable;
    }

    [Serializable]
    public sealed class MemorySnapshotBreakdownTreeNodeInfo
    {
        public string path;
        public string name;
        public int depth;
        public long allocated;
        public long resident;
        public bool residentAvailable;
        public int childrenCount;
        public bool childrenTruncated;
    }

    [Serializable]
    public sealed class MemorySnapshotNativeTypeStat
    {
        public string typeName;
        public long count;
        public long totalSize;
    }

    [Serializable]
    public sealed class MemorySnapshotNativeObjectInfo
    {
        public string name;
        public string typeName;
        public long size;
        public string instanceId;
    }

    [Serializable]
    public sealed class MemorySnapshotNativeTypeDiffInfo
    {
        public string typeName;
        public long baseCount;
        public long targetCount;
        public long countDelta;
        public long baseSize;
        public long targetSize;
        public long sizeDelta;
    }
}
#endif
