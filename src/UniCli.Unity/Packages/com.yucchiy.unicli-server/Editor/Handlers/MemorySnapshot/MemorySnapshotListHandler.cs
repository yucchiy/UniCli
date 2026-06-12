#if UNICLI_MEMORY_PROFILER
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling.Memory;

namespace UniCli.Server.Editor.Handlers
{
    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotListHandler : MemorySnapshotCommandHandler<MemorySnapshotListRequest, MemorySnapshotListResponse>
    {
        internal MemorySnapshotListHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.List";
        public override string Description => "List memory snapshot (.snap) files captured by MemorySnapshot.Capture, Profiler.TakeSnapshot, or the Memory Profiler window";

        protected override ValueTask<MemorySnapshotListResponse> ExecuteAsync(MemorySnapshotListRequest request, CancellationToken cancellationToken)
        {
            var directory = string.IsNullOrEmpty(request.directory) ? "MemoryCaptures" : request.directory;
            var resolvedDirectory = ResolvePath(directory);

            if (!Directory.Exists(resolvedDirectory))
            {
                return new ValueTask<MemorySnapshotListResponse>(new MemorySnapshotListResponse
                {
                    entries = Array.Empty<MemorySnapshotListEntry>()
                });
            }

            var entries = MemorySnapshotPathResolver.ListSnapshots(resolvedDirectory)
                .Select(info => new MemorySnapshotListEntry
                {
                    path = ToDisplayPath(info.FullName),
                    size = info.Length,
                    lastModified = info.LastWriteTimeUtc.ToString("o")
                })
                .ToArray();

            return new ValueTask<MemorySnapshotListResponse>(new MemorySnapshotListResponse
            {
                entries = entries
            });
        }
    }

    [Serializable]
    public class MemorySnapshotListRequest
    {
        public string directory = "MemoryCaptures";
    }

    [Serializable]
    public class MemorySnapshotListResponse
    {
        public MemorySnapshotListEntry[] entries;
    }

    [Serializable]
    public class MemorySnapshotListEntry
    {
        public string path;
        public long size;
        public string lastModified;
    }

    [Module("MemoryProfiler")]
    public sealed class MemorySnapshotCaptureHandler : MemorySnapshotCommandHandler<MemorySnapshotCaptureRequest, MemorySnapshotCaptureResponse>
    {
        internal MemorySnapshotCaptureHandler(SnapshotAnalysisCache cache, ISnapshotAnalyzer analyzer)
            : base(cache, analyzer)
        {
        }

        public override string CommandName => "MemorySnapshot.Capture";
        public override string Description => "Capture a memory snapshot for MemorySnapshot analysis; Profiler.TakeSnapshot remains available as the generic profiler capture command";

        protected override async ValueTask<MemorySnapshotCaptureResponse> ExecuteAsync(MemorySnapshotCaptureRequest request, CancellationToken cancellationToken)
        {
            var path = string.IsNullOrEmpty(request.path) ? GetDefaultCapturePath() : ResolvePath(request.path);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var captureFlags = ParseCaptureFlags(request.flags);
            var tcs = new TaskCompletionSource<string>();

            MemoryProfiler.TakeSnapshot(path, (filePath, success) =>
            {
                if (success)
                    tcs.TrySetResult(filePath);
                else
                    tcs.TrySetException(new InvalidOperationException($"Failed to capture memory snapshot at: {path}"));
            }, captureFlags);

            var resultPath = await tcs.Task.WithCancellation(cancellationToken);

            long size = 0;
            if (File.Exists(resultPath))
                size = new FileInfo(resultPath).Length;

            return new MemorySnapshotCaptureResponse
            {
                path = resultPath,
                size = size
            };
        }

        private static CaptureFlags ParseCaptureFlags(string[] flags)
        {
            if (flags == null || flags.Length == 0)
                return CaptureFlags.ManagedObjects | CaptureFlags.NativeObjects | CaptureFlags.NativeAllocations;

            var result = (CaptureFlags)0;
            foreach (var flag in flags)
            {
                if (string.IsNullOrWhiteSpace(flag))
                    continue;

                if (!Enum.TryParse(typeof(CaptureFlags), flag, ignoreCase: true, out var parsed))
                {
                    throw new CommandFailedException(
                        $"Unknown memory snapshot capture flag: {flag}. Valid values are: {string.Join(", ", Enum.GetNames(typeof(CaptureFlags)))}.",
                        null);
                }

                result |= (CaptureFlags)parsed;
            }

            return result;
        }
    }

    [Serializable]
    public class MemorySnapshotCaptureRequest
    {
        public string path;
        public string[] flags;
    }

    [Serializable]
    public class MemorySnapshotCaptureResponse
    {
        public string path;
        public long size;
    }
}
#endif
