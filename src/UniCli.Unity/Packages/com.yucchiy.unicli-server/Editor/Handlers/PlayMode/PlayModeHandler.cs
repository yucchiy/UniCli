using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PlayModeEnterHandler : CommandHandler<Unit, Unit>
    {
        public override string CommandName => "PlayMode.Enter";
        public override string Description => "Enter play mode in Unity Editor";

        protected override ValueTask<Unit> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            EditorApplication.EnterPlaymode();
            return new ValueTask<Unit>(Unit.Value);
        }
    }

    public sealed class PlayModeExitHandler : CommandHandler<Unit, Unit>
    {
        public override string CommandName => "PlayMode.Exit";
        public override string Description => "Exit play mode in Unity Editor";

        protected override ValueTask<Unit> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            EditorApplication.ExitPlaymode();
            return new ValueTask<Unit>(Unit.Value);
        }
    }

    public sealed class PlayModePauseHandler : CommandHandler<Unit, Unit>
    {
        public override string CommandName => "PlayMode.Pause";
        public override string Description => "Toggle pause state in play mode";

        protected override ValueTask<Unit> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
            return new ValueTask<Unit>(Unit.Value);
        }
    }

    public sealed class PlayModeStatusHandler : CommandHandler<Unit, PlayModeStatusResponse>
    {
        public override string CommandName => "PlayMode.Status";
        public override string Description => "Get the current play mode state";

        protected override bool TryWriteFormatted(PlayModeStatusResponse response, bool success, IFormatWriter writer)
        {
            if (!success) return false;

            writer.WriteLine($"isPlaying: {response.isPlaying}");
            writer.WriteLine($"isPaused: {response.isPaused}");
            writer.WriteLine($"isCompiling: {response.isCompiling}");
            return true;
        }

        protected override ValueTask<PlayModeStatusResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            return new ValueTask<PlayModeStatusResponse>(new PlayModeStatusResponse
            {
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused,
                isCompiling = EditorApplication.isCompiling,
            });
        }
    }

    [System.Serializable]
    public class PlayModeStatusResponse
    {
        public bool isPlaying;
        public bool isPaused;
        public bool isCompiling;
    }
}