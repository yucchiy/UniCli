using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AnimatorControllerAddTransitionHandler
        : CommandHandler<AnimatorControllerAddTransitionRequest, AnimatorControllerAddTransitionResponse>
    {
        public override string CommandName => CommandNames.AnimatorController.AddTransition;
        public override string Description => "Add a transition between two states in an AnimatorController";

        protected override bool TryWriteFormatted(AnimatorControllerAddTransitionResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
                writer.WriteLine(
                    $"Added transition {response.sourceStateName} -> {response.destinationStateName} in {response.assetPath}");
            else
                writer.WriteLine("Failed to add transition");

            return true;
        }

        protected override ValueTask<AnimatorControllerAddTransitionResponse> ExecuteAsync(
            AnimatorControllerAddTransitionRequest request)
        {
            if (string.IsNullOrEmpty(request.sourceStateName))
                throw new ArgumentException("sourceStateName is required");

            if (string.IsNullOrEmpty(request.destinationStateName))
                throw new ArgumentException("destinationStateName is required");

            var controller = AnimatorControllerResolver.Resolve(request.assetPath);

            if (request.layerIndex < 0 || request.layerIndex >= controller.layers.Length)
            {
                throw new CommandFailedException(
                    $"Layer index {request.layerIndex} is out of range (0..{controller.layers.Length - 1})",
                    new AnimatorControllerAddTransitionResponse());
            }

            var stateMachine = controller.layers[request.layerIndex].stateMachine;

            var sourceState = FindState(stateMachine, request.sourceStateName);
            if (sourceState == null)
            {
                throw new CommandFailedException(
                    $"Source state \"{request.sourceStateName}\" not found in layer {request.layerIndex}",
                    new AnimatorControllerAddTransitionResponse());
            }

            var destinationState = FindState(stateMachine, request.destinationStateName);
            if (destinationState == null)
            {
                throw new CommandFailedException(
                    $"Destination state \"{request.destinationStateName}\" not found in layer {request.layerIndex}",
                    new AnimatorControllerAddTransitionResponse());
            }

            var transition = sourceState.AddTransition(destinationState);
            transition.hasExitTime = request.hasExitTime;
            transition.exitTime = request.exitTime;
            transition.duration = request.duration;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new ValueTask<AnimatorControllerAddTransitionResponse>(
                new AnimatorControllerAddTransitionResponse
                {
                    assetPath = request.assetPath,
                    sourceStateName = request.sourceStateName,
                    destinationStateName = request.destinationStateName,
                    layerIndex = request.layerIndex,
                    hasExitTime = transition.hasExitTime,
                    exitTime = transition.exitTime,
                    duration = transition.duration
                });
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state.name == name)
                    return childState.state;
            }

            return null;
        }
    }

    [Serializable]
    public class AnimatorControllerAddTransitionRequest
    {
        public string assetPath = "";
        public string sourceStateName = "";
        public string destinationStateName = "";
        public int layerIndex;
        public bool hasExitTime;
        public float exitTime = 0.9f;
        public float duration = 0.25f;
    }

    [Serializable]
    public class AnimatorControllerAddTransitionResponse
    {
        public string assetPath;
        public string sourceStateName;
        public string destinationStateName;
        public int layerIndex;
        public bool hasExitTime;
        public float exitTime;
        public float duration;
    }
}
