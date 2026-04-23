using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ScreenshotCaptureHandler : CommandHandler<ScreenshotCaptureRequest, ScreenshotCaptureResponse>
    {
        public override string CommandName => "Screenshot.Capture";
        public override string Description => "Capture a screenshot of the Game View and save as PNG (requires Play Mode)";

        protected override bool TryWriteFormatted(ScreenshotCaptureResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"Screenshot saved to: {response.path}");
                writer.WriteLine($"  Resolution: {response.width}x{response.height}");
                writer.WriteLine($"  Size: {FormatBytes(response.size)}");
            }
            else
            {
                writer.WriteLine("Failed to capture screenshot");
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

        protected override async ValueTask<ScreenshotCaptureResponse> ExecuteAsync(ScreenshotCaptureRequest request, CancellationToken cancellationToken)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Screenshot.Capture requires Play Mode. Use PlayMode.Enter first.");

            var superSize = request.superSize > 0 ? request.superSize : 1;

            var path = string.IsNullOrEmpty(request.path)
                ? Path.Combine("Screenshots", $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png")
                : request.path;
            path = ResolvePath(path);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            int capturedWidth;
            int capturedHeight;
            Texture2D tex = null;
            try
            {
                tex = await CaptureScreenshotAsync(superSize, cancellationToken);
                capturedWidth = tex.width;
                capturedHeight = tex.height;
                var pngBytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, pngBytes);
            }
            finally
            {
                if (tex != null)
                    UnityEngine.Object.DestroyImmediate(tex);
            }

            var fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
                throw new InvalidOperationException($"Failed to save screenshot to: {fullPath}");

            var fileInfo = new FileInfo(fullPath);
            return new ScreenshotCaptureResponse
            {
                path = fullPath,
                width = capturedWidth,
                height = capturedHeight,
                size = fileInfo.Length
            };
        }

        private static async Task<Texture2D> CaptureScreenshotAsync(int superSize, CancellationToken cancellationToken)
        {
            var gameObject = new GameObject("UniCliScreenshotCaptureRunner")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(gameObject);

            var runner = gameObject.AddComponent<ScreenshotCaptureRunner>();

            try
            {
                return await runner.CaptureAsync(superSize, cancellationToken);
            }
            finally
            {
                if (gameObject != null)
                    UnityEngine.Object.Destroy(gameObject);
            }
        }

        private sealed class ScreenshotCaptureRunner : MonoBehaviour
        {
            public Task<Texture2D> CaptureAsync(int superSize, CancellationToken cancellationToken)
            {
                var tcs = new TaskCompletionSource<Texture2D>();
                StartCoroutine(CaptureCoroutine(superSize, tcs, cancellationToken));
                return tcs.Task;
            }

            private System.Collections.IEnumerator CaptureCoroutine(int superSize, TaskCompletionSource<Texture2D> tcs, CancellationToken cancellationToken)
            {
                yield return new WaitForEndOfFrame();

                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled(cancellationToken);
                    yield break;
                }

                var tex = ScreenCapture.CaptureScreenshotAsTexture(superSize);
                if (tex != null)
                {
                    tcs.TrySetResult(tex);
                    yield break;
                }

                tcs.TrySetException(new InvalidOperationException("Failed to capture screenshot. Ensure the Game View is visible and rendering."));
            }
        }
    }

    [Serializable]
    public class ScreenshotCaptureRequest
    {
        public string path;
        public int superSize;
    }

    [Serializable]
    public class ScreenshotCaptureResponse
    {
        public string path;
        public int width;
        public int height;
        public long size;
    }
}
