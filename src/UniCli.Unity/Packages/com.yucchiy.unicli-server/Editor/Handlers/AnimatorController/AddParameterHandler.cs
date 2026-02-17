using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AnimatorControllerAddParameterHandler
        : CommandHandler<AnimatorControllerAddParameterRequest, AnimatorControllerAddParameterResponse>
    {
        public override string CommandName => CommandNames.AnimatorController.AddParameter;
        public override string Description => "Add a parameter to an AnimatorController";

        protected override bool TryWriteFormatted(AnimatorControllerAddParameterResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Added parameter \"{response.name}\" ({response.type}) to {response.assetPath}");
            else
                writer.WriteLine("Failed to add parameter");

            return true;
        }

        protected override ValueTask<AnimatorControllerAddParameterResponse> ExecuteAsync(
            AnimatorControllerAddParameterRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            if (string.IsNullOrEmpty(request.type))
                throw new ArgumentException("type is required");

            if (!Enum.TryParse<AnimatorControllerParameterType>(request.type, true, out var paramType))
            {
                throw new CommandFailedException(
                    $"Invalid parameter type \"{request.type}\". Valid types: Float, Int, Bool, Trigger",
                    new AnimatorControllerAddParameterResponse());
            }

            var controller = AnimatorControllerResolver.Resolve(request.assetPath);

            foreach (var existing in controller.parameters)
            {
                if (existing.name == request.name)
                {
                    throw new CommandFailedException(
                        $"Parameter \"{request.name}\" already exists",
                        new AnimatorControllerAddParameterResponse());
                }
            }

            controller.AddParameter(request.name, paramType);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new ValueTask<AnimatorControllerAddParameterResponse>(
                new AnimatorControllerAddParameterResponse
                {
                    assetPath = request.assetPath,
                    name = request.name,
                    type = paramType.ToString(),
                    parameterCount = controller.parameters.Length
                });
        }
    }

    [Serializable]
    public class AnimatorControllerAddParameterRequest
    {
        public string assetPath = "";
        public string name = "";
        public string type = "";
    }

    [Serializable]
    public class AnimatorControllerAddParameterResponse
    {
        public string assetPath;
        public string name;
        public string type;
        public int parameterCount;
    }
}
