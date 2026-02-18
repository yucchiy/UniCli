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
        public override string CommandName => CommandNames.Screenshot.Capture;
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

        protected override ValueTask<ScreenshotCaptureResponse> ExecuteAsync(ScreenshotCaptureRequest request, CancellationToken cancellationToken)
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
                tex = ScreenCapture.CaptureScreenshotAsTexture(superSize);
                if (tex == null)
                    throw new InvalidOperationException("Failed to capture screenshot. Ensure the Game View is visible and rendering.");

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
            return new ValueTask<ScreenshotCaptureResponse>(new ScreenshotCaptureResponse
            {
                path = fullPath,
                width = capturedWidth,
                height = capturedHeight,
                size = fileInfo.Length
            });
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
