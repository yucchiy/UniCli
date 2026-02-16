using System;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class AnimatorControllerRemoveParameterHandler
        : CommandHandler<AnimatorControllerRemoveParameterRequest, AnimatorControllerRemoveParameterResponse>
    {
        public override string CommandName => CommandNames.AnimatorController.RemoveParameter;
        public override string Description => "Remove a parameter from an AnimatorController";

        protected override bool TryWriteFormatted(AnimatorControllerRemoveParameterResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Removed parameter \"{response.name}\" from {response.assetPath}");
            else
                writer.WriteLine("Failed to remove parameter");

            return true;
        }

        protected override ValueTask<AnimatorControllerRemoveParameterResponse> ExecuteAsync(
            AnimatorControllerRemoveParameterRequest request)
        {
            if (string.IsNullOrEmpty(request.name))
                throw new ArgumentException("name is required");

            var controller = AnimatorControllerResolver.Resolve(request.assetPath);

            var index = -1;
            for (var i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name == request.name)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                throw new CommandFailedException(
                    $"Parameter \"{request.name}\" not found in \"{request.assetPath}\"",
                    new AnimatorControllerRemoveParameterResponse());
            }

            controller.RemoveParameter(index);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new ValueTask<AnimatorControllerRemoveParameterResponse>(
                new AnimatorControllerRemoveParameterResponse
                {
                    assetPath = request.assetPath,
                    name = request.name,
                    parameterCount = controller.parameters.Length
                });
        }
    }

    [Serializable]
    public class AnimatorControllerRemoveParameterRequest
    {
        public string assetPath = "";
        public string name = "";
    }

    [Serializable]
    public class AnimatorControllerRemoveParameterResponse
    {
        public string assetPath;
        public string name;
        public int parameterCount;
    }
}
