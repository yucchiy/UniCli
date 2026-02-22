using System.Threading;
using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Animation")]
    public sealed class AnimatorSetParameterHandler
        : CommandHandler<AnimatorSetParameterRequest, AnimatorSetParameterResponse>
    {
        public override string CommandName => "Animator.SetParameter";
        public override string Description => "Set an Animator parameter value (requires PlayMode)";

        protected override bool TryWriteFormatted(AnimatorSetParameterResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
                writer.WriteLine(
                    $"Set parameter \"{response.name}\" = {response.value} on \"{response.gameObjectName}\"");
            else
                writer.WriteLine("Failed to set parameter");

            return true;
        }

        protected override ValueTask<AnimatorSetParameterResponse> ExecuteAsync(
            AnimatorSetParameterRequest request, CancellationToken cancellationToken)
        {
            if (!EditorApplication.isPlaying)
            {
                throw new CommandFailedException(
                    "This command requires PlayMode",
                    new AnimatorSetParameterResponse());
            }

            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            if (string.IsNullOrEmpty(request.value))
                throw new ArgumentException("value is required");

            var go = GameObjectResolver.ResolveByIdOrPath(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new AnimatorSetParameterResponse());
            }

            var animator = go.GetComponent<UnityEngine.Animator>();
            if (animator == null)
            {
                throw new CommandFailedException(
                    $"Animator component not found on \"{go.name}\"",
                    new AnimatorSetParameterResponse());
            }

            var param = FindParameter(animator, request.name);
            if (param == null)
            {
                throw new CommandFailedException(
                    $"Parameter \"{request.name}\" not found",
                    new AnimatorSetParameterResponse());
            }

            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    if (!float.TryParse(request.value, NumberStyles.Float, CultureInfo.InvariantCulture,
                            out var f))
                        throw new CommandFailedException($"Invalid float value \"{request.value}\"",
                            new AnimatorSetParameterResponse());
                    animator.SetFloat(request.name, f);
                    break;
                case AnimatorControllerParameterType.Int:
                    if (!int.TryParse(request.value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                            out var n))
                        throw new CommandFailedException($"Invalid int value \"{request.value}\"",
                            new AnimatorSetParameterResponse());
                    animator.SetInteger(request.name, n);
                    break;
                case AnimatorControllerParameterType.Bool:
                    if (!bool.TryParse(request.value, out var b))
                        throw new CommandFailedException($"Invalid bool value \"{request.value}\"",
                            new AnimatorSetParameterResponse());
                    animator.SetBool(request.name, b);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.SetTrigger(request.name);
                    break;
            }

            return new ValueTask<AnimatorSetParameterResponse>(new AnimatorSetParameterResponse
            {
                gameObjectName = go.name,
                name = request.name,
                type = param.type.ToString(),
                value = request.value
            });
        }

        private static AnimatorControllerParameter FindParameter(UnityEngine.Animator animator, string name)
        {
            for (var i = 0; i < animator.parameterCount; i++)
            {
                var p = animator.GetParameter(i);
                if (p.name == name)
                    return p;
            }

            return null;
        }
    }

    [Serializable]
    public class AnimatorSetParameterRequest
    {
        public int instanceId;
        public string path = "";
        public string name = "";
        public string value = "";
    }

    [Serializable]
    public class AnimatorSetParameterResponse
    {
        public string gameObjectName;
        public string name;
        public string type;
        public string value;
    }
}
