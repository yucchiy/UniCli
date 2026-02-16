using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AnimatorControllerInspectHandler
        : CommandHandler<AnimatorControllerInspectRequest, AnimatorControllerInspectResponse>
    {
        public override string CommandName => CommandNames.AnimatorController.Inspect;
        public override string Description => "Inspect an AnimatorController asset (layers, parameters, states)";

        protected override bool TryWriteFormatted(AnimatorControllerInspectResponse response, bool success,
            IFormatWriter writer)
        {
            if (!success)
            {
                writer.WriteLine("Failed to inspect AnimatorController");
                return true;
            }

            writer.WriteLine($"AnimatorController: {response.assetPath}");

            if (response.parameters?.Length > 0)
            {
                writer.WriteLine($"Parameters ({response.parameters.Length}):");
                foreach (var p in response.parameters)
                    writer.WriteLine($"  {p.name} ({p.type}) = {FormatDefaultValue(p)}");
            }

            if (response.layers?.Length > 0)
            {
                foreach (var layer in response.layers)
                {
                    writer.WriteLine($"Layer: {layer.name} (weight={layer.defaultWeight})");
                    if (layer.states?.Length > 0)
                    {
                        foreach (var state in layer.states)
                        {
                            var defaultMarker = state.isDefault ? " [default]" : "";
                            writer.WriteLine(
                                $"  State: {state.name}{defaultMarker} motion={state.motionName} speed={state.speed} transitions={state.transitionCount}");
                        }
                    }
                }
            }

            return true;
        }

        private static string FormatDefaultValue(AnimatorParameterInfo p)
        {
            return p.type switch
            {
                "Float" => p.defaultFloat.ToString("F2"),
                "Int" => p.defaultInt.ToString(),
                "Bool" => p.defaultBool.ToString(),
                "Trigger" => "(trigger)",
                _ => "?"
            };
        }

        protected override ValueTask<AnimatorControllerInspectResponse> ExecuteAsync(
            AnimatorControllerInspectRequest request)
        {
            var controller = AnimatorControllerResolver.Resolve(request.assetPath);

            var parameters = new List<AnimatorParameterInfo>();
            foreach (var param in controller.parameters)
            {
                parameters.Add(new AnimatorParameterInfo
                {
                    name = param.name,
                    type = param.type.ToString(),
                    defaultFloat = param.defaultFloat,
                    defaultInt = param.defaultInt,
                    defaultBool = param.defaultBool
                });
            }

            var layers = new List<AnimatorLayerInfo>();
            foreach (var layer in controller.layers)
            {
                var states = new List<AnimatorStateInfo>();
                var defaultState = layer.stateMachine.defaultState;

                foreach (var childState in layer.stateMachine.states)
                {
                    var state = childState.state;
                    states.Add(new AnimatorStateInfo
                    {
                        name = state.name,
                        motionName = state.motion != null ? state.motion.name : "(none)",
                        isDefault = defaultState == state,
                        speed = state.speed,
                        transitionCount = state.transitions.Length
                    });
                }

                layers.Add(new AnimatorLayerInfo
                {
                    name = layer.name,
                    defaultWeight = layer.defaultWeight,
                    blendingMode = layer.blendingMode.ToString(),
                    states = states.ToArray()
                });
            }

            return new ValueTask<AnimatorControllerInspectResponse>(new AnimatorControllerInspectResponse
            {
                assetPath = request.assetPath,
                parameters = parameters.ToArray(),
                layers = layers.ToArray()
            });
        }
    }

    [Serializable]
    public class AnimatorControllerInspectRequest
    {
        public string assetPath = "";
    }

    [Serializable]
    public class AnimatorControllerInspectResponse
    {
        public string assetPath;
        public AnimatorParameterInfo[] parameters;
        public AnimatorLayerInfo[] layers;
    }

    [Serializable]
    public class AnimatorParameterInfo
    {
        public string name;
        public string type;
        public float defaultFloat;
        public int defaultInt;
        public bool defaultBool;
    }

    [Serializable]
    public class AnimatorLayerInfo
    {
        public string name;
        public float defaultWeight;
        public string blendingMode;
        public AnimatorStateInfo[] states;
    }

    [Serializable]
    public class AnimatorStateInfo
    {
        public string name;
        public string motionName;
        public bool isDefault;
        public float speed;
        public int transitionCount;
    }
}
