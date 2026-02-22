using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Animation")]
    public sealed class AnimatorInspectHandler
        : CommandHandler<AnimatorInspectRequest, AnimatorInspectResponse>
    {
        public override string CommandName => "Animator.Inspect";
        public override string Description => "Inspect an Animator component (parameters, current state, controller info)";

        protected override bool TryWriteFormatted(AnimatorInspectResponse response, bool success,
            IFormatWriter writer)
        {
            if (!success)
            {
                writer.WriteLine("Failed to inspect Animator");
                return true;
            }

            writer.WriteLine($"Animator on \"{response.gameObjectName}\"");
            writer.WriteLine($"  Controller: {response.controllerAssetPath}");
            writer.WriteLine($"  Enabled: {response.enabled}");

            if (response.parameters?.Length > 0)
            {
                writer.WriteLine($"  Parameters ({response.parameters.Length}):");
                foreach (var p in response.parameters)
                    writer.WriteLine($"    {p.name} ({p.type}) = {p.value}");
            }

            if (response.isPlaying && response.currentStateName != null)
                writer.WriteLine($"  Current State: {response.currentStateName} (layer 0)");

            return true;
        }

        protected override ValueTask<AnimatorInspectResponse> ExecuteAsync(AnimatorInspectRequest request, CancellationToken cancellationToken)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new AnimatorInspectResponse());
            }

            var animator = go.GetComponent<UnityEngine.Animator>();
            if (animator == null)
            {
                throw new CommandFailedException(
                    $"Animator component not found on \"{go.name}\"",
                    new AnimatorInspectResponse());
            }

            var controllerPath = "";
            if (animator.runtimeAnimatorController != null)
                controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);

            var parameters = new List<AnimatorRuntimeParameterInfo>();
            var isPlaying = EditorApplication.isPlaying;

            if (animator.runtimeAnimatorController != null)
            {
                for (var i = 0; i < animator.parameterCount; i++)
                {
                    var param = animator.GetParameter(i);
                    var info = new AnimatorRuntimeParameterInfo
                    {
                        name = param.name,
                        type = param.type.ToString()
                    };

                    if (isPlaying && animator.isActiveAndEnabled)
                    {
                        info.value = param.type switch
                        {
                            AnimatorControllerParameterType.Float =>
                                animator.GetFloat(param.name).ToString("F3"),
                            AnimatorControllerParameterType.Int =>
                                animator.GetInteger(param.name).ToString(),
                            AnimatorControllerParameterType.Bool =>
                                animator.GetBool(param.name).ToString(),
                            AnimatorControllerParameterType.Trigger => "(trigger)",
                            _ => "?"
                        };
                    }
                    else
                    {
                        info.value = param.type switch
                        {
                            AnimatorControllerParameterType.Float => param.defaultFloat.ToString("F3"),
                            AnimatorControllerParameterType.Int => param.defaultInt.ToString(),
                            AnimatorControllerParameterType.Bool => param.defaultBool.ToString(),
                            AnimatorControllerParameterType.Trigger => "(trigger)",
                            _ => "?"
                        };
                    }

                    parameters.Add(info);
                }
            }

            var response = new AnimatorInspectResponse
            {
                gameObjectName = go.name,
                gameObjectPath = GameObjectResolver.BuildPath(go.transform),
                instanceId = animator.GetInstanceID(),
                enabled = animator.enabled,
                controllerAssetPath = controllerPath,
                parameters = parameters.ToArray(),
                isPlaying = isPlaying
            };

            if (isPlaying && animator.isActiveAndEnabled)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                response.currentStateName = GetStateName(animator, stateInfo, 0);
                response.currentStateNormalizedTime = stateInfo.normalizedTime;
            }

            return new ValueTask<AnimatorInspectResponse>(response);
        }

        private static string GetStateName(UnityEngine.Animator animator,
            UnityEngine.AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.runtimeAnimatorController is UnityEditor.Animations.AnimatorController ac)
            {
                if (layerIndex < ac.layers.Length)
                {
                    foreach (var childState in ac.layers[layerIndex].stateMachine.states)
                    {
                        if (stateInfo.IsName(childState.state.name))
                            return childState.state.name;
                    }
                }
            }

            return $"(hash={stateInfo.fullPathHash})";
        }
    }

    [Serializable]
    public class AnimatorInspectRequest
    {
        public int instanceId;
        public string path = "";
    }

    [Serializable]
    public class AnimatorInspectResponse
    {
        public string gameObjectName;
        public string gameObjectPath;
        public int instanceId;
        public bool enabled;
        public string controllerAssetPath;
        public AnimatorRuntimeParameterInfo[] parameters;
        public bool isPlaying;
        public string currentStateName;
        public float currentStateNormalizedTime;
    }

    [Serializable]
    public class AnimatorRuntimeParameterInfo
    {
        public string name;
        public string type;
        public string value;
    }
}
