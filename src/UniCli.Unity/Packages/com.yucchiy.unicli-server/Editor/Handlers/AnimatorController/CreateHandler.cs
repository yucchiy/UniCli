using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor.Animations;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Animation")]
    public sealed class AnimatorControllerCreateHandler
        : CommandHandler<AnimatorControllerCreateRequest, AnimatorControllerCreateResponse>
    {
        public override string CommandName => "AnimatorController.Create";
        public override string Description => "Create a new AnimatorController asset";

        protected override bool TryWriteFormatted(AnimatorControllerCreateResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Created AnimatorController at {response.assetPath}");
            else
                writer.WriteLine("Failed to create AnimatorController");

            return true;
        }

        protected override ValueTask<AnimatorControllerCreateResponse> ExecuteAsync(
            AnimatorControllerCreateRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.assetPath))
                throw new ArgumentException("assetPath is required");

            if (!request.assetPath.EndsWith(".controller"))
            {
                throw new CommandFailedException(
                    $"assetPath must end with .controller (got \"{request.assetPath}\")",
                    new AnimatorControllerCreateResponse());
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(request.assetPath);
            if (controller == null)
            {
                throw new CommandFailedException(
                    $"Failed to create AnimatorController at \"{request.assetPath}\"",
                    new AnimatorControllerCreateResponse());
            }

            return new ValueTask<AnimatorControllerCreateResponse>(new AnimatorControllerCreateResponse
            {
                assetPath = request.assetPath,
                layerCount = controller.layers.Length,
                parameterCount = controller.parameters.Length
            });
        }
    }

    [Serializable]
    public class AnimatorControllerCreateRequest
    {
        public string assetPath = "";
    }

    [Serializable]
    public class AnimatorControllerCreateResponse
    {
        public string assetPath;
        public int layerCount;
        public int parameterCount;
    }
}
