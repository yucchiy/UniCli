using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UniCli.Server.Editor.Handlers
{
    internal static class SceneViewScreenshotUtility
    {
        public const int DefaultWidth = 1920;
        public const int DefaultHeight = 1080;

        internal struct CaptureOptions
        {
            public bool orthographic;
            public Vector3 pivot;
            public Quaternion rotation;
            public float size;
            public float distance;
            public int width;
            public int height;
        }

        public static SceneView GetOrCreateSceneView()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                sceneView = EditorWindow.GetWindow<SceneView>();
            if (sceneView == null || sceneView.camera == null)
                throw new InvalidOperationException("No SceneView is available");
            return sceneView;
        }

        public static Vector3 ResolvePivot(SceneView sceneView, string lookAt, float[] offset, bool allowTwoComponentOffset)
        {
            var pivot = sceneView.pivot;
            if (!string.IsNullOrEmpty(lookAt))
            {
                var target = GameObjectResolver.ResolveByPath(lookAt);
                if (target == null)
                    throw new ArgumentException($"GameObject not found: \"{lookAt}\"");
                pivot = target.transform.position;
            }

            if (offset != null && offset.Length > 0)
            {
                var valid = offset.Length == 3 || (allowTwoComponentOffset && offset.Length == 2);
                if (!valid)
                {
                    throw new ArgumentException(allowTwoComponentOffset
                        ? $"offset must have 2 or 3 components, got {offset.Length}"
                        : $"offset must have 3 components, got {offset.Length}");
                }

                pivot += new Vector3(offset[0], offset[1], offset.Length > 2 ? offset[2] : 0f);
            }

            return pivot;
        }

        public static Texture2D Capture(SceneView sceneView, CaptureOptions options)
        {
            var width = options.width > 0 ? options.width : DefaultWidth;
            var height = options.height > 0 ? options.height : DefaultHeight;

            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var cameraObject = new GameObject("UniCliSceneViewCapture") { hideFlags = HideFlags.HideAndDontSave };
            var previousActive = RenderTexture.active;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.CopyFrom(sceneView.camera);
                camera.cameraType = CameraType.Game;
                // The SceneView camera may use an explicit projection matrix baked with the
                // window's aspect ratio; reset so the requested resolution drives projection.
                camera.ResetWorldToCameraMatrix();
                camera.ResetProjectionMatrix();
                camera.targetTexture = renderTexture;
                camera.aspect = (float)width / height;
                camera.orthographic = options.orthographic;

                float distance;
                if (options.orthographic)
                {
                    camera.orthographicSize = options.size;
                    distance = options.distance > 0f ? options.distance : options.size * 2f;
                }
                else
                {
                    distance = options.distance > 0f
                        ? options.distance
                        : options.size / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                }

                camera.transform.SetPositionAndRotation(
                    options.pivot - options.rotation * Vector3.forward * distance,
                    options.rotation);
                camera.nearClipPlane = Mathf.Max(0.01f, distance * 0.0005f);
                camera.farClipPlane = Mathf.Max(1000f, distance * 100f);

                Render(camera, renderTexture);

                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void Render(Camera camera, RenderTexture destination)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                // Camera.Render is unsupported on scriptable render pipelines (URP/HDRP).
                var request = new RenderPipeline.StandardRequest { destination = destination };
                if (RenderPipeline.SupportsRenderRequest(camera, request))
                {
                    RenderPipeline.SubmitRenderRequest(camera, request);
                    return;
                }
            }

            camera.Render();
        }

        public static SceneScreenshotResponse SavePng(Texture2D texture, string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(path, texture.EncodeToPNG());

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                throw new InvalidOperationException($"Failed to save screenshot to: {fullPath}");

            var fileInfo = new FileInfo(fullPath);
            return new SceneScreenshotResponse
            {
                path = fullPath,
                width = texture.width,
                height = texture.height,
                size = fileInfo.Length
            };
        }

        public static bool TryWriteResult(SceneScreenshotResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"Screenshot saved to: {response.path}");
                writer.WriteLine($"  Resolution: {response.width}x{response.height}");
                writer.WriteLine($"  Size: {FormatBytes(response.size)}");
            }
            else
            {
                writer.WriteLine("Failed to capture SceneView screenshot");
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
    }

    [Serializable]
    public class SceneScreenshotResponse
    {
        public string path;
        public int width;
        public int height;
        public long size;
    }
}
