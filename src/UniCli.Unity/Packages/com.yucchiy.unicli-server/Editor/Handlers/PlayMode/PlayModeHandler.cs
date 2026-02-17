using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class PlayModeEnterHandler : CommandHandler<Unit, Unit>
    {
        public override string CommandName => CommandNames.PlayMode.Enter;
        public override string Description => "Enter play mode in Unity Editor";

        protected override ValueTask<Unit> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            EditorApplication.EnterPlaymode();
            return new ValueTask<Unit>(Unit.Value);
        }
    }

    public sealed class PlayModeExitHandler : CommandHandler<Unit, Unit>
    {
        public override string CommandName => CommandNames.PlayMode.Exit;
        public override string Description => "Exit play mode in Unity Editor";

        protected override ValueTask<Unit> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            EditorApplication.ExitPlaymode();
            return new ValueTask<Unit>(Unit.Value);
        }
    }

    public sealed class PlayModePauseHandler : CommandHandler<Unit, Unit>
    {
        public override string CommandName => CommandNames.PlayMode.Pause;
        public override string Description => "Toggle pause state in play mode";

        protected override ValueTask<Unit> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
            return new ValueTask<Unit>(Unit.Value);
        }
    }
}