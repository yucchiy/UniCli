#if UNICLI_RECORDER
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Recorder")]
    public sealed class RecorderStopRecordingHandler : CommandHandler<Unit, RecorderStopRecordingResponse>
    {
        public override string CommandName => "Recorder.StopRecording";
        public override string Description => "Stop the current video recording";

        protected override bool TryWriteFormatted(RecorderStopRecordingResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine($"Recording saved to: {response.path}");
                writer.WriteLine($"  Size: {FormatBytes(response.size)}");
            }
            else
            {
                writer.WriteLine("Failed to stop recording");
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

        protected override ValueTask<RecorderStopRecordingResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            if (RecorderState.Controller == null || !RecorderState.Controller.IsRecording())
                throw new InvalidOperationException("No recording is in progress. Use Recorder.StartRecording first.");

            var controller = RecorderState.Controller;
            var outputPath = RecorderState.OutputPath;

            controller.StopRecording();

            var controllerSettings = controller.Settings;
            if (controllerSettings != null)
            {
                foreach (var recorder in controllerSettings.RecorderSettings)
                {
                    if (recorder != null)
                        UnityEngine.Object.DestroyImmediate(recorder);
                }
                UnityEngine.Object.DestroyImmediate(controllerSettings);
            }

            RecorderState.Controller = null;
            RecorderState.OutputPath = null;

            long fileSize = 0;
            if (File.Exists(outputPath))
                fileSize = new FileInfo(outputPath).Length;

            return new ValueTask<RecorderStopRecordingResponse>(new RecorderStopRecordingResponse
            {
                path = outputPath,
                size = fileSize,
            });
        }
    }

    [Serializable]
    public class RecorderStopRecordingResponse
    {
        public string path;
        public long size;
    }
}
#endif
