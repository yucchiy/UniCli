#if UNICLI_RECORDER
using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class RecorderStatusHandler : CommandHandler<Unit, RecorderStatusResponse>
    {
        public override string CommandName => "Recorder.Status";
        public override string Description => "Get the current recording status";

        protected override bool TryWriteFormatted(RecorderStatusResponse response, bool success, IFormatWriter writer)
        {
            if (success)
            {
                writer.WriteLine(response.isRecording ? "Recording: active" : "Recording: inactive");
            }
            else
            {
                writer.WriteLine("Failed to get recording status");
            }
            return true;
        }

        protected override ValueTask<RecorderStatusResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var isRecording = RecorderState.Controller != null && RecorderState.Controller.IsRecording();

            return new ValueTask<RecorderStatusResponse>(new RecorderStatusResponse
            {
                isRecording = isRecording,
            });
        }
    }

    [Serializable]
    public class RecorderStatusResponse
    {
        public bool isRecording;
    }
}
#endif
