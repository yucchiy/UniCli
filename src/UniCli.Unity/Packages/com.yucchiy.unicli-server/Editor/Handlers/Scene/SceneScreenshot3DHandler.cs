using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Scene")]
    public sealed class SceneScreenshot3DHandler : CommandHandler<SceneScreenshot3DRequest, SceneScreenshotResponse>
    {
        public override string CommandName => "Scene.Screenshot3D";
        public override string Description => "Capture a screenshot of the SceneView in 3D mode, orbiting a lookAt target by yaw/pitch/distance, and save as PNG";

        protected override bool TryWriteFormatted(SceneScreenshotResponse response, bool success, IFormatWriter writer)
            => SceneViewScreenshotUtility.TryWriteResult(response, success, writer);

        protected override ValueTask<SceneScreenshotResponse> ExecuteAsync(SceneScreenshot3DRequest request, CancellationToken cancellationToken)
        {
            var sceneView = SceneViewScreenshotUtility.GetOrCreateSceneView();

            var currentEuler = sceneView.rotation.eulerAngles;
            var pitch = float.IsNaN(request.pitch) ? currentEuler.x : request.pitch;
            var yaw = float.IsNaN(request.yaw) ? currentEuler.y : request.yaw;

            var options = new SceneViewScreenshotUtility.CaptureOptions
            {
                orthographic = false,
                pivot = SceneViewScreenshotUtility.ResolvePivot(sceneView, request.lookAt, request.offset, allowTwoComponentOffset: false),
                rotation = Quaternion.Euler(pitch, yaw, 0f),
                size = sceneView.size,
                distance = request.distance,
                width = request.width,
                height = request.height
            };

            var path = string.IsNullOrEmpty(request.path)
                ? Path.Combine("Screenshots", $"sceneview3d_{DateTime.Now:yyyyMMdd_HHmmss}.png")
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
    public class SceneScreenshot3DRequest
    {
        public string path;
        public string lookAt;
        public float[] offset;
        public float yaw = float.NaN;
        public float pitch = float.NaN;
        public float distance;
        public int width;
        public int height;
    }
}
