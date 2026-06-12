#if UNICLI_MEMORY_PROFILER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace UniCli.Server.Editor.Handlers
{
    internal sealed class SnapshotAnalysisCache
    {
        private const int Capacity = 8;

        private readonly object _gate = new object();
        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _entries =
            new Dictionary<string, LinkedListNode<CacheEntry>>();
        private readonly Dictionary<string, string> _entryKeysById =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _entryKeysByName =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly LinkedList<CacheEntry> _lru = new LinkedList<CacheEntry>();

        public SnapshotAnalysis GetOrAnalyze(
            string snapshotPath,
            ISnapshotAnalyzer analyzer,
            out bool cached,
            out long analysisMs)
        {
            return GetOrAnalyze(
                snapshotPath,
                analyzer,
                name: "",
                pin: false,
                replace: false,
                out cached,
                out analysisMs,
                out _);
        }

        public SnapshotAnalysis LoadOrAnalyze(
            string snapshotPath,
            ISnapshotAnalyzer analyzer,
            string name,
            bool replace,
            out bool cached,
            out long analysisMs,
            out MemorySnapshotStatusEntry entry)
        {
            return GetOrAnalyze(
                snapshotPath,
                analyzer,
                name,
                pin: true,
                replace,
                out cached,
                out analysisMs,
                out entry);
        }

        public SnapshotAnalysis GetLoaded(string snapshot, out MemorySnapshotStatusEntry entry)
        {
            if (string.IsNullOrEmpty(snapshot))
                throw new CommandFailedException("Snapshot id or name is required.", null);

            lock (_gate)
            {
                if (!TryGetNodeBySnapshot(snapshot, out var node))
                {
                    throw new CommandFailedException(
                        $"Loaded memory snapshot not found: {snapshot}. Use MemorySnapshot.Status to list loaded snapshots, or MemorySnapshot.Load to name one.",
                        null);
                }

                Touch(node);
                entry = ToStatusEntry(node.Value.Analysis);
                return node.Value.Analysis;
            }
        }

        public int Remove(string snapshot)
        {
            if (string.IsNullOrEmpty(snapshot))
                return Clear();

            lock (_gate)
            {
                if (!TryGetNodeBySnapshot(snapshot, out var node))
                    return 0;

                RemoveNode(node);
                return 1;
            }
        }

        private SnapshotAnalysis GetOrAnalyze(
            string snapshotPath,
            ISnapshotAnalyzer analyzer,
            string name,
            bool pin,
            bool replace,
            out bool cached,
            out long analysisMs,
            out MemorySnapshotStatusEntry entry)
        {
            if (string.IsNullOrEmpty(snapshotPath))
                throw new CommandFailedException("Snapshot path is required.", null);

            if (!File.Exists(snapshotPath))
                throw new CommandFailedException($"Snapshot file not found: {snapshotPath}", null);

            if (!analyzer.IsAvailable(out var unavailableReason))
            {
                throw new CommandFailedException(
                    "Memory Profiler package internals are not accessible (com.unity.memoryprofiler 1.1.x). " +
                    "UniCli MemorySnapshot commands are verified against 1.1.x. Reason: " + unavailableReason,
                    null);
            }

            var fileInfo = new FileInfo(snapshotPath);
            var key = MakeKey(fileInfo);
            var id = MakeId(key);
            var resolvedName = pin ? ResolveName(name, fileInfo) : "";

            lock (_gate)
            {
                if (_entries.TryGetValue(key, out var node))
                {
                    if (pin)
                        ApplyName(node.Value.Analysis, key, resolvedName, replace);

                    Touch(node);
                    cached = true;
                    analysisMs = 0;
                    entry = ToStatusEntry(node.Value.Analysis);
                    return node.Value.Analysis;
                }

                EnsureCacheCanAcceptNewEntry(pin, resolvedName, replace);
            }

            var stopwatch = Stopwatch.StartNew();
            var analysis = analyzer.Analyze(fileInfo.FullName);
            stopwatch.Stop();
            analysisMs = stopwatch.ElapsedMilliseconds;

            analysis.Path = fileInfo.FullName;
            analysis.FileSize = fileInfo.Length;
            analysis.FileMtimeTicks = fileInfo.LastWriteTimeUtc.Ticks;
            analysis.SnapshotId = id;
            analysis.SnapshotName = resolvedName;
            analysis.Pinned = pin;
            analysis.AnalyzedAtUtc = DateTime.UtcNow.ToString("o");
            analysis.LastAccessedAtUtc = analysis.AnalyzedAtUtc;
            analysis.AnalysisMs = analysisMs;

            lock (_gate)
            {
                if (pin)
                    EnsureNameAvailable(resolvedName, key, replace);

                var node = _lru.AddFirst(new CacheEntry(key, analysis));
                _entries[key] = node;
                _entryKeysById[id] = key;
                if (pin)
                    _entryKeysByName[resolvedName] = key;

                EvictIfNeeded();
                entry = ToStatusEntry(analysis);
            }

            cached = false;
            return analysis;
        }

        public MemorySnapshotStatusEntry[] GetEntries()
        {
            lock (_gate)
            {
                return _lru
                    .Select(entry => new MemorySnapshotStatusEntry
                    {
                        id = entry.Analysis.SnapshotId,
                        name = entry.Analysis.SnapshotName,
                        pinned = entry.Analysis.Pinned,
                        path = entry.Analysis.Path,
                        fileSize = entry.Analysis.FileSize,
                        analyzedAtUtc = entry.Analysis.AnalyzedAtUtc,
                        lastAccessedAtUtc = entry.Analysis.LastAccessedAtUtc,
                        analysisMs = entry.Analysis.AnalysisMs
                    })
                    .ToArray();
            }
        }

        public int GetCapacity()
        {
            return Capacity;
        }

        public int Clear()
        {
            lock (_gate)
            {
                var count = _entries.Count;
                _entries.Clear();
                _entryKeysById.Clear();
                _entryKeysByName.Clear();
                _lru.Clear();
                return count;
            }
        }

        private void Touch(LinkedListNode<CacheEntry> node)
        {
            node.Value.Analysis.LastAccessedAtUtc = DateTime.UtcNow.ToString("o");
            _lru.Remove(node);
            _lru.AddFirst(node);
        }

        private bool TryGetNodeBySnapshot(string snapshot, out LinkedListNode<CacheEntry> node)
        {
            if (_entryKeysById.TryGetValue(snapshot, out var key) ||
                _entryKeysByName.TryGetValue(snapshot, out key))
            {
                return _entries.TryGetValue(key, out node);
            }

            node = null;
            return false;
        }

        private void ApplyName(SnapshotAnalysis analysis, string key, string name, bool replace)
        {
            EnsureNameAvailable(name, key, replace);

            if (!string.Equals(analysis.SnapshotName, name, StringComparison.OrdinalIgnoreCase) &&
                _entryKeysByName.TryGetValue(analysis.SnapshotName, out var existingKey) &&
                string.Equals(existingKey, key, StringComparison.Ordinal))
            {
                _entryKeysByName.Remove(analysis.SnapshotName);
            }

            analysis.SnapshotName = name;
            analysis.Pinned = true;
            _entryKeysByName[name] = key;
        }

        private void EnsureNameAvailable(string name, string key, bool replace)
        {
            if (string.IsNullOrEmpty(name))
                throw new CommandFailedException("Snapshot name is required.", null);

            if (!_entryKeysByName.TryGetValue(name, out var existingKey) ||
                string.Equals(existingKey, key, StringComparison.Ordinal))
            {
                return;
            }

            if (!replace)
            {
                throw new CommandFailedException(
                    $"Loaded memory snapshot name is already in use: {name}. Pass replace=true to reassign it.",
                    null);
            }

            if (_entries.TryGetValue(existingKey, out var existingNode))
            {
                existingNode.Value.Analysis.Pinned = false;
                existingNode.Value.Analysis.SnapshotName = "";
            }

            _entryKeysByName.Remove(name);
        }

        private void EnsureCacheCanAcceptNewEntry(bool pin, string name, bool replace)
        {
            if (_entries.Count < Capacity)
                return;

            if (_lru.Any(entry => !entry.Analysis.Pinned))
                return;

            if (pin && replace && _entryKeysByName.ContainsKey(name))
                return;

            var reason = pin
                ? "Cannot load named memory snapshot because all cache slots are pinned."
                : "Cannot analyze memory snapshot because all cache slots are pinned.";
            throw new CommandFailedException($"{reason} Use MemorySnapshot.Unload to release one.", null);
        }

        private void EvictIfNeeded()
        {
            while (_lru.Count > Capacity)
            {
                var last = _lru.Last;
                while (last != null && last.Value.Analysis.Pinned)
                    last = last.Previous;

                if (last == null)
                    throw new CommandFailedException("MemorySnapshot cache is full of pinned entries. Use MemorySnapshot.Unload to release one.", null);

                RemoveNode(last);
            }
        }

        private void RemoveNode(LinkedListNode<CacheEntry> node)
        {
            _entries.Remove(node.Value.Key);
            _entryKeysById.Remove(node.Value.Analysis.SnapshotId);
            if (_entryKeysByName.TryGetValue(node.Value.Analysis.SnapshotName, out var key) &&
                string.Equals(key, node.Value.Key, StringComparison.Ordinal))
            {
                _entryKeysByName.Remove(node.Value.Analysis.SnapshotName);
            }

            _lru.Remove(node);
        }

        public MemorySnapshotStatusEntry ToStatusEntry(SnapshotAnalysis analysis)
        {
            return new MemorySnapshotStatusEntry
            {
                id = analysis.SnapshotId,
                name = analysis.SnapshotName,
                pinned = analysis.Pinned,
                path = analysis.Path,
                fileSize = analysis.FileSize,
                analyzedAtUtc = analysis.AnalyzedAtUtc,
                lastAccessedAtUtc = analysis.LastAccessedAtUtc,
                analysisMs = analysis.AnalysisMs
            };
        }

        private static string ResolveName(string name, FileInfo fileInfo)
        {
            return string.IsNullOrWhiteSpace(name) ? GetDefaultName(fileInfo.FullName) : name.Trim();
        }

        private static string GetDefaultName(string path)
        {
            return Path.GetFileNameWithoutExtension(path) ?? "";
        }

        private static string MakeKey(FileInfo fileInfo)
        {
            return fileInfo.FullName + "|" + fileInfo.LastWriteTimeUtc.Ticks;
        }

        private static string MakeId(string key)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            unchecked
            {
                var hash = offset;
                foreach (var ch in key)
                {
                    hash ^= ch;
                    hash *= prime;
                }

                return "ms_" + hash.ToString("x16");
            }
        }

        private sealed class CacheEntry
        {
            public readonly string Key;
            public readonly SnapshotAnalysis Analysis;

            public CacheEntry(string key, SnapshotAnalysis analysis)
            {
                Key = key;
                Analysis = analysis;
            }
        }
    }
}
#endif
