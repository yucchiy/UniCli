using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Animation")]
    public sealed class AnimatorPlayHandler
        : CommandHandler<AnimatorPlayRequest, AnimatorPlayResponse>
    {
        public override string CommandName => "Animator.Play";
        public override string Description => "Play a state immediately on an Animator (requires PlayMode)";

        protected override bool TryWriteFormatted(AnimatorPlayResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Playing state \"{response.stateName}\" on \"{response.gameObjectName}\"");
            else
                writer.WriteLine("Failed to play state");

            return true;
        }

        protected override ValueTask<AnimatorPlayResponse> ExecuteAsync(AnimatorPlayRequest request, CancellationToken cancellationToken)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new CommandFailedException(
                    "This command requires PlayMode",
                    new AnimatorPlayResponse());
            }

            if (string.IsNullOrEmpty(request.stateName))
                throw new ArgumentException("stateName is required");

            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new AnimatorPlayResponse());
            }

            var animator = go.GetComponent<UnityEngine.Animator>();
            if (animator == null)
            {
                throw new CommandFailedException(
                    $"Animator component not found on \"{go.name}\"",
                    new AnimatorPlayResponse());
            }

            animator.Play(request.stateName, request.layer, request.normalizedTime);

            return new ValueTask<AnimatorPlayResponse>(new AnimatorPlayResponse
            {
                gameObjectName = go.name,
                stateName = request.stateName,
                layer = request.layer,
                normalizedTime = request.normalizedTime
            });
        }
    }

    [Serializable]
    public class AnimatorPlayRequest
    {
        public int instanceId;
        public string path = "";
        public string stateName = "";
        public int layer;
        public float normalizedTime;
    }

    [Serializable]
    public class AnimatorPlayResponse
    {
        public string gameObjectName;
        public string stateName;
        public int layer;
        public float normalizedTime;
    }
}
