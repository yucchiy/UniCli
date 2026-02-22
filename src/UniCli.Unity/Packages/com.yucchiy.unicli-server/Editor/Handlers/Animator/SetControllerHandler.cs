using System.Threading;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("Animation")]
    public sealed class AnimatorSetControllerHandler
        : CommandHandler<AnimatorSetControllerRequest, AnimatorSetControllerResponse>
    {
        public override string CommandName => "Animator.SetController";
        public override string Description => "Assign an AnimatorController to an Animator component";

        protected override bool TryWriteFormatted(AnimatorSetControllerResponse response, bool success,
            IFormatWriter writer)
        {
            if (success)
                writer.WriteLine(
                    $"Set controller \"{response.controllerAssetPath}\" on \"{response.gameObjectName}\"");
            else
                writer.WriteLine("Failed to set controller");

            return true;
        }

        protected override ValueTask<AnimatorSetControllerResponse> ExecuteAsync(
            AnimatorSetControllerRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.controllerAssetPath))
                throw new ArgumentException("controllerAssetPath is required");

            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new AnimatorSetControllerResponse());
            }

            var animator = go.GetComponent<UnityEngine.Animator>();
            if (animator == null)
            {
                throw new CommandFailedException(
                    $"Animator component not found on \"{go.name}\"",
                    new AnimatorSetControllerResponse());
            }

            var controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(request.controllerAssetPath);
            if (controller == null)
            {
                throw new CommandFailedException(
                    $"AnimatorController not found at \"{request.controllerAssetPath}\"",
                    new AnimatorSetControllerResponse());
            }

            Undo.RecordObject(animator, "Set Animator Controller");
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);

            return new ValueTask<AnimatorSetControllerResponse>(new AnimatorSetControllerResponse
            {
                gameObjectName = go.name,
                gameObjectPath = GameObjectResolver.BuildPath(go.transform),
                controllerAssetPath = request.controllerAssetPath
            });
        }
    }

    [Serializable]
    public class AnimatorSetControllerRequest
    {
        public int instanceId;
        public string path = "";
        public string controllerAssetPath = "";
    }

    [Serializable]
    public class AnimatorSetControllerResponse
    {
        public string gameObjectName;
        public string gameObjectPath;
        public string controllerAssetPath;
    }
}
