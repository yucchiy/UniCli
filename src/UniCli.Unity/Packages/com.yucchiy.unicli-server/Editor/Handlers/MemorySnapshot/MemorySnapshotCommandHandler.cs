#if UNICLI_MEMORY_PROFILER
using System;
using System.IO;
using System.Linq;

namespace UniCli.Server.Editor.Handlers
{
    public abstract class MemorySnapshotCommandHandler<TRequest, TResponse> : CommandHandler<TRequest, TResponse>
    {
        private const string DefaultSnapshotDirectory = "MemoryCaptures";
        private readonly ISnapshotAnalyzer _analyzer;

        private protected MemorySnapshotCommandHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
        {
            Cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        }

        private protected SnapshotAnalysisCache Cache { get; }

        private protected SnapshotAnalysis GetAnalysis(string path, out bool cached, out long analysisMs)
        {
            if (string.IsNullOrEmpty(path))
                throw new CommandFailedException("Snapshot path is required.", null);

            return Cache.GetOrAnalyze(ResolvePath(path), _analyzer, out cached, out analysisMs);
        }

        private protected SnapshotAnalysis GetAnalysisOrDefault(string path, int defaultSnapshotIndex, out bool cached, out long analysisMs)
        {
            return Cache.GetOrAnalyze(
                ResolveSnapshotPathOrDefault(path, defaultSnapshotIndex),
                _analyzer,
                out cached,
                out analysisMs);
        }

        private protected SnapshotAnalysis GetAnalysisByReferenceOrDefault(
            string snapshot,
            string path,
            int defaultSnapshotIndex,
            out bool cached,
            out long analysisMs,
            out MemorySnapshotStatusEntry entry)
        {
            if (!string.IsNullOrEmpty(snapshot))
            {
                if (!string.IsNullOrEmpty(path))
                    throw new CommandFailedException("Use either snapshot or path, not both.", null);

                cached = true;
                analysisMs = 0;
                return Cache.GetLoaded(snapshot, out entry);
            }

            var analysis = Cache.GetOrAnalyze(
                ResolveSnapshotPathOrDefault(path, defaultSnapshotIndex),
                _analyzer,
                out cached,
                out analysisMs);
            entry = Cache.ToStatusEntry(analysis);
            return analysis;
        }

        private protected SnapshotAnalysis GetAnalysisByReference(
            string snapshot,
            string path,
            out bool cached,
            out long analysisMs,
            out MemorySnapshotStatusEntry entry)
        {
            if (!string.IsNullOrEmpty(snapshot))
            {
                if (!string.IsNullOrEmpty(path))
                    throw new CommandFailedException("Use either snapshot or path, not both.", null);

                cached = true;
                analysisMs = 0;
                return Cache.GetLoaded(snapshot, out entry);
            }

            var analysis = GetAnalysis(path, out cached, out analysisMs);
            entry = Cache.ToStatusEntry(analysis);
            return analysis;
        }

        private protected SnapshotAnalysis LoadAnalysis(
            string path,
            string name,
            bool replace,
            out bool cached,
            out long analysisMs,
            out MemorySnapshotStatusEntry entry)
        {
            return Cache.LoadOrAnalyze(
                ResolveSnapshotPathOrDefault(path, 0),
                _analyzer,
                name,
                replace,
                out cached,
                out analysisMs,
                out entry);
        }

        private protected string ResolveSnapshotPathOrDefault(string path, int defaultSnapshotIndex)
        {
            if (!string.IsNullOrEmpty(path))
                return ResolvePath(path);

            return MemorySnapshotPathResolver.ResolveDefaultSnapshot(
                ResolvePath(DefaultSnapshotDirectory),
                defaultSnapshotIndex);
        }

        private protected string ResolveDefaultSnapshotDirectory()
        {
            return ResolvePath(DefaultSnapshotDirectory);
        }

        private protected string GetDefaultCapturePath()
        {
            return ResolvePath(Path.Combine(DefaultSnapshotDirectory, $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.snap"));
        }

        private protected string ToDisplayPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";

            var fullPath = Path.GetFullPath(path);
            if (!string.IsNullOrEmpty(ClientWorkingDirectory))
            {
                var cwd = Path.GetFullPath(ClientWorkingDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var prefix = cwd + Path.DirectorySeparatorChar;
                if (fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return NormalizePath(fullPath.Substring(prefix.Length));
            }

            return NormalizePath(path);
        }

        internal static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }

    internal static class MemorySnapshotPathResolver
    {
        public static string ResolveDefaultSnapshot(string resolvedDirectory, int index)
        {
            var snapshots = ListSnapshots(resolvedDirectory);
            if (index >= 0 && index < snapshots.Length)
                return snapshots[index].FullName;

            throw new CommandFailedException(
                $"Need at least {index + 1} snapshot file(s) in {NormalizePath(resolvedDirectory)} but found {snapshots.Length}.",
                null);
        }

        public static FileInfo[] ListSnapshots(string resolvedDirectory)
        {
            if (string.IsNullOrEmpty(resolvedDirectory) || !Directory.Exists(resolvedDirectory))
                return Array.Empty<FileInfo>();

            return Directory.EnumerateFiles(resolvedDirectory, "*.snap", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .ThenByDescending(info => info.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
#endif
