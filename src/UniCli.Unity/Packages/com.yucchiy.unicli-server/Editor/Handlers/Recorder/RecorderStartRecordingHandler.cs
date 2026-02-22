#if UNICLI_RECORDER
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class RecorderStartRecordingHandler : CommandHandler<RecorderStartRecordingRequest, RecorderStartRecordingResponse>
    {
        public override string CommandName => "Recorder.StartRecording";
        public override string Description => "Start recording the Game View as a video (requires Play Mode)";

        protected override bool TryWriteFormatted(RecorderStartRecordingResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"Recording started: {response.path}");
                writer.WriteLine($"  Format: {response.format}");
                writer.WriteLine($"  Resolution: {response.width}x{response.height}");
                writer.WriteLine($"  Frame Rate: {response.frameRate} fps");
            }
            else
            {
                writer.WriteLine("Failed to start recording");
            }
            return true;
        }

        protected override ValueTask<RecorderStartRecordingResponse> ExecuteAsync(RecorderStartRecordingRequest request, CancellationToken cancellationToken)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Recorder.StartRecording requires Play Mode. Use PlayMode.Enter first.");

            if (RecorderState.Controller != null && RecorderState.Controller.IsRecording())
                throw new InvalidOperationException("Recording is already in progress. Use Recorder.StopRecording first.");

            var format = string.IsNullOrEmpty(request.format) ? "MP4" : request.format.ToUpperInvariant();
            var frameRate = request.frameRate > 0 ? request.frameRate : 30f;
            var quality = string.IsNullOrEmpty(request.quality) ? "High" : request.quality;

            var path = string.IsNullOrEmpty(request.path)
                ? Path.Combine("Recordings", $"recording_{DateTime.Now:yyyyMMdd_HHmmss}")
                : request.path;
            // Strip extension since Recorder appends it automatically
            if (path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.LastIndexOf('.'));
            path = ResolvePath(path);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = frameRate;

            var movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.name = "UniCli Movie Recorder";
            movieSettings.Enabled = true;
            movieSettings.OutputFile = path;

            if (format == "WEBM")
            {
                movieSettings.EncoderSettings = new CoreEncoderSettings
                {
                    Codec = CoreEncoderSettings.OutputCodec.WEBM,
                    EncodingQuality = ParseQuality(quality),
                };
            }
            else
            {
                movieSettings.EncoderSettings = new CoreEncoderSettings
                {
                    Codec = CoreEncoderSettings.OutputCodec.MP4,
                    EncodingQuality = ParseQuality(quality),
                };
            }

            movieSettings.CaptureAudio = request.captureAudio;

            var imageInputSettings = movieSettings.ImageInputSettings as GameViewInputSettings;
            if (imageInputSettings != null && request.width > 0 && request.height > 0)
            {
                imageInputSettings.OutputWidth = request.width;
                imageInputSettings.OutputHeight = request.height;
            }

            controllerSettings.AddRecorderSettings(movieSettings);

            var controller = new RecorderController(controllerSettings);
            controller.PrepareRecording();
            controller.StartRecording();

            if (!controller.IsRecording())
            {
                UnityEngine.Object.DestroyImmediate(movieSettings);
                UnityEngine.Object.DestroyImmediate(controllerSettings);
                throw new InvalidOperationException("Failed to start recording. Ensure the Game View is visible.");
            }

            var actualWidth = request.width > 0 ? request.width : Screen.width;
            var actualHeight = request.height > 0 ? request.height : Screen.height;

            RecorderState.Controller = controller;
            RecorderState.OutputPath = path + (format == "WEBM" ? ".webm" : ".mp4");

            return new ValueTask<RecorderStartRecordingResponse>(new RecorderStartRecordingResponse
            {
                path = RecorderState.OutputPath,
                format = format == "WEBM" ? "WebM" : "MP4",
                width = actualWidth,
                height = actualHeight,
                frameRate = frameRate,
            });
        }

        private static CoreEncoderSettings.VideoEncodingQuality ParseQuality(string quality)
        {
            switch (quality.ToLowerInvariant())
            {
                case "low": return CoreEncoderSettings.VideoEncodingQuality.Low;
                case "medium": return CoreEncoderSettings.VideoEncodingQuality.Medium;
                case "high": return CoreEncoderSettings.VideoEncodingQuality.High;
                default: return CoreEncoderSettings.VideoEncodingQuality.High;
            }
        }
    }

    [Serializable]
    public class RecorderStartRecordingRequest
    {
        public string path;
        public string format;
        public int width;
        public int height;
        public float frameRate;
        public string quality;
        public bool captureAudio;
    }

    [Serializable]
    public class RecorderStartRecordingResponse
    {
        public string path;
        public string format;
        public int width;
        public int height;
        public float frameRate;
    }
}
#endif
