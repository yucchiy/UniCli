using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Animations;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AnimatorControllerAddTransitionConditionHandler
        : CommandHandler<AnimatorControllerAddTransitionConditionRequest,
            AnimatorControllerAddTransitionConditionResponse>
    {
        public override string CommandName => CommandNames.AnimatorController.AddTransitionCondition;

        public override string Description =>
            "Add a condition to a transition between two states in an AnimatorController";

        protected override bool TryWriteFormatted(AnimatorControllerAddTransitionConditionResponse response,
            bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine(
                    $"Added condition ({response.parameter} {response.mode} {response.threshold}) to transition {response.sourceStateName} -> {response.destinationStateName}");
            else
                writer.WriteLine("Failed to add transition condition");

            return true;
        }

        protected override ValueTask<AnimatorControllerAddTransitionConditionResponse> ExecuteAsync(
            AnimatorControllerAddTransitionConditionRequest request)
        {
            if (string.IsNullOrEmpty(request.sourceStateName))
                throw new ArgumentException("sourceStateName is required");

            if (string.IsNullOrEmpty(request.destinationStateName))
                throw new ArgumentException("destinationStateName is required");

            if (string.IsNullOrEmpty(request.parameter))
                throw new ArgumentException("parameter is required");

            if (string.IsNullOrEmpty(request.mode))
                throw new ArgumentException("mode is required");

            if (!Enum.TryParse<AnimatorConditionMode>(request.mode, true, out var conditionMode))
            {
                throw new CommandFailedException(
                    $"Invalid condition mode \"{request.mode}\". Valid modes: If, IfNot, Greater, Less, Equals, NotEqual",
                    new AnimatorControllerAddTransitionConditionResponse());
            }

            var controller = AnimatorControllerResolver.Resolve(request.assetPath);

            if (request.layerIndex < 0 || request.layerIndex >= controller.layers.Length)
            {
                throw new CommandFailedException(
                    $"Layer index {request.layerIndex} is out of range (0..{controller.layers.Length - 1})",
                    new AnimatorControllerAddTransitionConditionResponse());
            }

            var stateMachine = controller.layers[request.layerIndex].stateMachine;

            var sourceState = FindState(stateMachine, request.sourceStateName);
            if (sourceState == null)
            {
                throw new CommandFailedException(
                    $"Source state \"{request.sourceStateName}\" not found in layer {request.layerIndex}",
                    new AnimatorControllerAddTransitionConditionResponse());
            }

            var transition = FindTransition(sourceState, request.destinationStateName, request.transitionIndex);
            if (transition == null)
            {
                throw new CommandFailedException(
                    $"Transition from \"{request.sourceStateName}\" to \"{request.destinationStateName}\" not found",
                    new AnimatorControllerAddTransitionConditionResponse());
            }

            transition.AddCondition(conditionMode, request.threshold, request.parameter);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new ValueTask<AnimatorControllerAddTransitionConditionResponse>(
                new AnimatorControllerAddTransitionConditionResponse
                {
                    assetPath = request.assetPath,
                    sourceStateName = request.sourceStateName,
                    destinationStateName = request.destinationStateName,
                    parameter = request.parameter,
                    mode = conditionMode.ToString(),
                    threshold = request.threshold,
                    conditionCount = transition.conditions.Length
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

        private static AnimatorStateTransition FindTransition(AnimatorState sourceState,
            string destinationStateName, int transitionIndex)
        {
            var matchIndex = 0;
            foreach (var transition in sourceState.transitions)
            {
                if (transition.destinationState != null &&
                    transition.destinationState.name == destinationStateName)
                {
                    if (matchIndex == transitionIndex)
                        return transition;
                    matchIndex++;
                }
            }

            return null;
        }
    }

    [Serializable]
    public class AnimatorControllerAddTransitionConditionRequest
    {
        public string assetPath = "";
        public string sourceStateName = "";
        public string destinationStateName = "";
        public int layerIndex;
        public int transitionIndex;
        public string parameter = "";
        public string mode = "";
        public float threshold;
    }

    [Serializable]
    public class AnimatorControllerAddTransitionConditionResponse
    {
        public string assetPath;
        public string sourceStateName;
        public string destinationStateName;
        public string parameter;
        public string mode;
        public float threshold;
        public int conditionCount;
    }
}
