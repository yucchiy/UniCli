using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    internal enum DirtyAction
    {
        Error,
        Save,
        Discard,
    }

    /// <summary>
    /// Resolves how commands treat unsaved (dirty) scenes before Unity gets a
    /// chance to discard them silently or show a save-confirmation dialog that
    /// would block the editor main loop (and therefore the UniCli server).
    /// </summary>
    internal static class DirtySceneGuard
    {
        public static DirtyAction Parse(string dirtyAction, bool allowDiscard, string commandName)
        {
            if (string.IsNullOrEmpty(dirtyAction))
                return DirtyAction.Error;

            if (string.Equals(dirtyAction, "error", StringComparison.OrdinalIgnoreCase))
                return DirtyAction.Error;

            if (string.Equals(dirtyAction, "save", StringComparison.OrdinalIgnoreCase))
                return DirtyAction.Save;

            if (string.Equals(dirtyAction, "discard", StringComparison.OrdinalIgnoreCase))
            {
                if (!allowDiscard)
                    throw new ArgumentException(
                        $"dirtyAction \"discard\" is not supported for '{commandName}'. Use \"save\".");
                return DirtyAction.Discard;
            }

            throw new ArgumentException(
                $"Invalid dirtyAction \"{dirtyAction}\" for '{commandName}'. Valid values: \"error\" (default), \"save\"{(allowDiscard ? ", \"discard\"" : "")}.");
        }

        public static void Apply(DirtyAction action, IReadOnlyList<Scene> affectedScenes, string commandName, bool allowDiscard = true)
        {
            var dirtyScenes = affectedScenes.Where(scene => scene.IsValid() && scene.isDirty).ToList();
            if (dirtyScenes.Count == 0)
                return;

            switch (action)
            {
                case DirtyAction.Error:
                    throw new InvalidOperationException(BuildErrorMessage(dirtyScenes, commandName, allowDiscard));

                case DirtyAction.Save:
                    SaveScenes(dirtyScenes);
                    return;

                case DirtyAction.Discard:
                    // Proceed without saving. Scripted EditorSceneManager operations
                    // discard unsaved changes silently (verified on 2022.3/6000.x).
                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, null);
            }
        }

        public static List<Scene> GetLoadedScenes()
        {
            var scenes = new List<Scene>(SceneManager.sceneCount);
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    scenes.Add(scene);
            }

            return scenes;
        }

        private static string BuildErrorMessage(List<Scene> dirtyScenes, string commandName, bool allowDiscard)
        {
            var sceneList = string.Join(", ", dirtyScenes.Select(DescribeScene));
            var actions = allowDiscard ? "\"save\" or \"discard\"" : "\"save\"";
            return $"{sceneList} {(dirtyScenes.Count == 1 ? "has" : "have")} unsaved changes. "
                + $"Specify dirtyAction {actions} for '{commandName}', or save the scene first.";
        }

        private static string DescribeScene(Scene scene)
        {
            var name = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
            return string.IsNullOrEmpty(scene.path)
                ? $"Scene '{name}' (untitled)"
                : $"Scene '{name}' ({scene.path})";
        }

        private static void SaveScenes(List<Scene> dirtyScenes)
        {
            var untitled = dirtyScenes.Where(scene => string.IsNullOrEmpty(scene.path)).ToList();
            if (untitled.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot auto-save untitled scene(s): {string.Join(", ", untitled.Select(DescribeScene))}. "
                    + "Save them to an asset path first with Scene.Save (assetPath), or use dirtyAction \"discard\".");
            }

            foreach (var scene in dirtyScenes)
            {
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException($"Failed to save {DescribeScene(scene)}.");
            }
        }
    }
}
