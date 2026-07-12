using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Scene")]
    public sealed class SceneScreenshot2DHandler : CommandHandler<SceneScreenshot2DRequest, SceneScreenshotResponse>
    {
        public override string CommandName => "Scene.Screenshot2D";
        public override string Description => "Capture a screenshot of the SceneView in 2D mode (orthographic, facing the XY plane) and save as PNG";

        protected override bool TryWriteFormatted(SceneScreenshotResponse response, bool success, IFormatWriter writer)
            => SceneViewScreenshotUtility.TryWriteResult(response, success, writer);

        protected override ValueTask<SceneScreenshotResponse> ExecuteAsync(SceneScreenshot2DRequest request, CancellationToken cancellationToken)
        {
            var sceneView = SceneViewScreenshotUtility.GetOrCreateSceneView();
            var options = new SceneViewScreenshotUtility.CaptureOptions
            {
                orthographic = true,
                pivot = SceneViewScreenshotUtility.ResolvePivot(sceneView, request.lookAt, request.offset, allowTwoComponentOffset: true),
                rotation = Quaternion.identity,
                size = request.size > 0f ? request.size : sceneView.size,
                width = request.width,
                height = request.height
            };

            var path = string.IsNullOrEmpty(request.path)
                ? Path.Combine("Screenshots", $"sceneview2d_{DateTime.Now:yyyyMMdd_HHmmss}.png")
                : request.path;
            path = ResolvePath(path);

            Texture2D texture = null;
            try
            {
                texture = SceneViewScreenshotUtility.Capture(sceneView, options);
                return new ValueTask<SceneScreenshotResponse>(SceneViewScreenshotUtility.SavePng(texture, path));
            }
            finally
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }

    [Serializable]
    public class SceneScreenshot2DRequest
    {
        public string path;
        public string lookAt;
        public float[] offset;
        public float size;
        public int width;
        public int height;
    }
}
