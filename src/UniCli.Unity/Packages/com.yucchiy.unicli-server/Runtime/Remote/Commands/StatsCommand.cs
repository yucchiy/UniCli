using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace UniCli.Remote.Commands
{
    [DebugCommand("Debug.Stats", "Get runtime performance statistics")]
    public sealed class StatsCommand : DebugCommand<Unit, StatsCommand.Response>
    {
        protected override Response ExecuteCommand(Unit request)
        {
            var resolution = Screen.currentResolution;

            return new Response
            {
                fps = 1f / Time.unscaledDeltaTime,
                unscaledDeltaTime = Time.unscaledDeltaTime,
                frameCount = Time.frameCount,
                timeScale = Time.timeScale,
                fixedDeltaTime = Time.fixedDeltaTime,
                realtimeSinceStartup = Time.realtimeSinceStartup,
                totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong(),
                totalReservedMemory = Profiler.GetTotalReservedMemoryLong(),
                totalUnusedReservedMemory = Profiler.GetTotalUnusedReservedMemoryLong(),
                monoUsedSize = Profiler.GetMonoUsedSizeLong(),
                monoHeapSize = Profiler.GetMonoHeapSizeLong(),
                gcCollectionCount0 = GC.CollectionCount(0),
                gcCollectionCount1 = GC.CollectionCount(1),
                gcCollectionCount2 = GC.CollectionCount(2),
                gameObjectCount = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length,
                loadedSceneCount = SceneManager.sceneCount,
                screenOrientation = Screen.orientation.ToString(),
                currentResolutionWidth = resolution.width,
                currentResolutionHeight = resolution.height
            };
        }

        [Serializable]
        public class Response
        {
            public float fps;
            public float unscaledDeltaTime;
            public int frameCount;
            public float timeScale;
            public float fixedDeltaTime;
            public float realtimeSinceStartup;
            public long totalAllocatedMemory;
            public long totalReservedMemory;
            public long totalUnusedReservedMemory;
            public long monoUsedSize;
            public long monoHeapSize;
            public int gcCollectionCount0;
            public int gcCollectionCount1;
            public int gcCollectionCount2;
            public int gameObjectCount;
            public int loadedSceneCount;
            public string screenOrientation;
            public int currentResolutionWidth;
            public int currentResolutionHeight;
        }
    }
}
