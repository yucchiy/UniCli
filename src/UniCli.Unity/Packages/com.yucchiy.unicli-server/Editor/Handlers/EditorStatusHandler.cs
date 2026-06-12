using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class EditorStatusHandler : CommandHandler<Unit, EditorStatusResponse>
    {
        public override string CommandName => "Editor.Status";
        public override string Description => "Get Unity Editor status via EditorApplication, EditorSceneManager, SceneManager, and PrefabStageUtility";

        protected override bool TryWriteFormatted(EditorStatusResponse response, bool success, IFormatWriter writer)
        {
            if (!success)
                return false;

            writer.WriteLine($"Playing: {response.isPlaying}, Paused: {response.isPaused}, WillChangePlaymode: {response.willChangePlaymode}");
            writer.WriteLine($"Compiling: {response.isCompiling}, Updating: {response.isUpdating}");
            writer.WriteLine($"Dirty scenes: {(response.hasDirtyScenes ? "Yes" : "No")}, Untitled scene: {(response.hasUntitledScene ? "Yes" : "No")}");

            if (response.dirtyScenes != null)
            {
                foreach (var scene in response.dirtyScenes)
                {
                    var path = string.IsNullOrEmpty(scene.path) ? "(untitled)" : scene.path;
                    writer.WriteLine($"  {scene.name} ({path}) buildIndex={scene.buildIndex} rootCount={scene.rootCount}");
                }
            }

            if (response.prefabStageOpen)
                writer.WriteLine($"Prefab stage: {response.prefabStageAssetPath} dirty={response.prefabStageDirty}");
            else
                writer.WriteLine("Prefab stage: closed");

            return true;
        }

        protected override ValueTask<EditorStatusResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var dirtyScenes = new List<SceneInfo>();
            var hasUntitledScene = false;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (string.IsNullOrEmpty(scene.path))
                    hasUntitledScene = true;

                if (!scene.isDirty)
                    continue;

                dirtyScenes.Add(new SceneInfo
                {
                    name = scene.name,
                    path = scene.path,
                    buildIndex = scene.buildIndex,
                    isDirty = scene.isDirty,
                    isLoaded = scene.isLoaded,
                    rootCount = scene.rootCount
                });
            }

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var prefabStageOpen = prefabStage != null;

            return new ValueTask<EditorStatusResponse>(new EditorStatusResponse
            {
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused,
                willChangePlaymode = EditorApplication.isPlayingOrWillChangePlaymode,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                hasDirtyScenes = dirtyScenes.Count > 0,
                hasUntitledScene = hasUntitledScene,
                dirtyScenes = dirtyScenes.ToArray(),
                prefabStageOpen = prefabStageOpen,
                prefabStageAssetPath = prefabStageOpen ? prefabStage.assetPath ?? "" : "",
                prefabStageDirty = prefabStageOpen && prefabStage.scene.isDirty
            });
        }
    }

    [Serializable]
    public class EditorStatusResponse
    {
        public bool isPlaying;
        public bool isPaused;
        public bool willChangePlaymode;
        public bool isCompiling;
        public bool isUpdating;
        public bool hasDirtyScenes;
        public bool hasUntitledScene;
        public SceneInfo[] dirtyScenes;
        public bool prefabStageOpen;
        public string prefabStageAssetPath;
        public bool prefabStageDirty;
    }
}
