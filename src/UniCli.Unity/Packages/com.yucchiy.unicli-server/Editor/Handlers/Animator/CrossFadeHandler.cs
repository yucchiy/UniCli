using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AnimatorCrossFadeHandler
        : CommandHandler<AnimatorCrossFadeRequest, AnimatorCrossFadeResponse>
    {
        public override string CommandName => CommandNames.Animator.CrossFade;
        public override string Description => "Cross-fade to a state on an Animator (requires PlayMode)";

        protected override bool TryWriteFormatted(AnimatorCrossFadeResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
                writer.WriteLine(
                    $"Cross-fading to \"{response.stateName}\" (duration={response.transitionDuration}) on \"{response.gameObjectName}\"");
            else
                writer.WriteLine("Failed to cross-fade");

            return true;
        }

        protected override ValueTask<AnimatorCrossFadeResponse> ExecuteAsync(AnimatorCrossFadeRequest request)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new CommandFailedException(
                    "This command requires PlayMode",
                    new AnimatorCrossFadeResponse());
            }

            if (string.IsNullOrEmpty(request.stateName))
                throw new ArgumentException("stateName is required");

            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new AnimatorCrossFadeResponse());
            }

            var animator = go.GetComponent<UnityEngine.Animator>();
            if (animator == null)
            {
                throw new CommandFailedException(
                    $"Animator component not found on \"{go.name}\"",
                    new AnimatorCrossFadeResponse());
            }

            animator.CrossFade(request.stateName, request.transitionDuration, request.layer,
                request.normalizedTime);

            return new ValueTask<AnimatorCrossFadeResponse>(new AnimatorCrossFadeResponse
            {
                gameObjectName = go.name,
                stateName = request.stateName,
                layer = request.layer,
                transitionDuration = request.transitionDuration,
                normalizedTime = request.normalizedTime
            });
        }
    }

    [Serializable]
    public class AnimatorCrossFadeRequest
    {
        public int instanceId;
        public string path = "";
        public string stateName = "";
        public int layer;
        public float transitionDuration = 0.25f;
        public float normalizedTime;
    }

    [Serializable]
    public class AnimatorCrossFadeResponse
    {
        public string gameObjectName;
        public string stateName;
        public int layer;
        public float transitionDuration;
        public float normalizedTime;
    }
}
