using System;
using UnityEditor;
using UnityEditor.Animations;

namespace UniCli.Server.Editor.Handlers
{
    internal static class AnimatorControllerResolver
    {
        public static UnityEditor.Animations.AnimatorController Resolve(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentException("assetPath is required");

            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(assetPath);
            if (controller == null)
            {
                throw new CommandFailedException(
                    $"AnimatorController not found at \"{assetPath}\"",
                    new { assetPath });
            }

            return controller;
        }
    }
}
