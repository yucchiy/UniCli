using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling.Memory;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ProfilerTakeSnapshotHandler : CommandHandler<ProfilerTakeSnapshotRequest, ProfilerTakeSnapshotResponse>
    {
        public override string CommandName => CommandNames.Profiler.TakeSnapshot;
        public override string Description => "Take a memory snapshot (.snap file)";

        protected override bool TryWriteFormatted(ProfilerTakeSnapshotResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"Memory snapshot saved to: {response.path}");
                if (response.size > 0)
                    writer.WriteLine($"  Size: {FormatBytes(response.size)}");
            }
            else
            {
                writer.WriteLine("Failed to take memory snapshot");
            }
            return true;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024L) return $"{bytes} B";
            if (bytes < 1024L * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        protected override async ValueTask<ProfilerTakeSnapshotResponse> ExecuteAsync(ProfilerTakeSnapshotRequest request, CancellationToken cancellationToken)
        {
            var path = request.path;
            if (string.IsNullOrEmpty(path))
            {
                var dir = "MemoryCaptures";
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                path = Path.Combine(dir, $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.snap");
            }
            else
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            }

            var tcs = new TaskCompletionSource<string>();
            var captureFlags = CaptureFlags.ManagedObjects | CaptureFlags.NativeObjects | CaptureFlags.NativeAllocations;

            MemoryProfiler.TakeSnapshot(path, (filePath, success) =>
            {
                if (success)
                    tcs.TrySetResult(filePath);
                else
                    tcs.TrySetException(new InvalidOperationException($"Failed to take memory snapshot at: {path}"));
            }, captureFlags);

            var resultPath = await tcs.Task.WithCancellation(cancellationToken);

            long size = 0;
            if (File.Exists(resultPath))
                size = new FileInfo(resultPath).Length;

            return new ProfilerTakeSnapshotResponse
            {
                path = resultPath,
                size = size
            };
        }
    }

    [Serializable]
    public class ProfilerTakeSnapshotRequest
    {
        public string path;
    }

    [Serializable]
    public class ProfilerTakeSnapshotResponse
    {
        public string path;
        public long size;
    }
}
