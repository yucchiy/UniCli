using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AnimatorControllerAddStateHandler
        : CommandHandler<AnimatorControllerAddStateRequest, AnimatorControllerAddStateResponse>
    {
        public override string CommandName => CommandNames.AnimatorController.AddState;
        public override string Description => "Add a state to an AnimatorController layer";

        protected override bool TryWriteFormatted(AnimatorControllerAddStateResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
            {
                var motion = string.IsNullOrEmpty(response.motionName) ? "" : $" (motion={response.motionName})";
                writer.WriteLine(
                    $"Added state \"{response.name}\" to layer {response.layerIndex} of {response.assetPath}{motion}");
            }
            else
            {
                writer.WriteLine("Failed to add state");
            }

            return true;
        }

        protected override ValueTask<AnimatorControllerAddStateResponse> ExecuteAsync(
            AnimatorControllerAddStateRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var controller = AnimatorControllerResolver.Resolve(request.assetPath);

            if (request.layerIndex < 0 || request.layerIndex >= controller.layers.Length)
            {
                throw new CommandFailedException(
                    $"Layer index {request.layerIndex} is out of range (0..{controller.layers.Length - 1})",
                    new AnimatorControllerAddStateResponse());
            }

            var stateMachine = controller.layers[request.layerIndex].stateMachine;

            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == request.name)
                {
                    throw new CommandFailedException(
                        $"State \"{request.name}\" already exists in layer {request.layerIndex}",
                        new AnimatorControllerAddStateResponse());
                }
            }

            var state = stateMachine.AddState(request.name);

            if (!string.IsNullOrEmpty(request.motionAssetPath))
            {
                var motion = AssetDatabase.LoadAssetAtPath<UnityEngine.Motion>(request.motionAssetPath);
                if (motion == null)
                {
                    throw new CommandFailedException(
                        $"Motion asset not found at \"{request.motionAssetPath}\"",
                        new AnimatorControllerAddStateResponse());
                }

                state.motion = motion;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new ValueTask<AnimatorControllerAddStateResponse>(new AnimatorControllerAddStateResponse
            {
                assetPath = request.assetPath,
                name = state.name,
                layerIndex = request.layerIndex,
                motionName = state.motion != null ? state.motion.name : "",
                stateCount = stateMachine.states.Length
            });
        }
    }

    [Serializable]
    public class AnimatorControllerAddStateRequest
    {
        public string assetPath = "";
        public string name = "";
        public int layerIndex;
        public string motionAssetPath = "";
    }

    [Serializable]
    public class AnimatorControllerAddStateResponse
    {
        public string assetPath;
        public string name;
        public int layerIndex;
        public string motionName;
        public int stateCount;
    }
}
