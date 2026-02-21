using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace UniCli.Remote.Commands
{
    [Preserve]
    [DebugCommand("Debug.GetScenes", "List all loaded scenes")]
    public sealed class GetScenesCommand : DebugCommand<RuntimeUnit, GetScenesCommand.Response>
    {
        protected override Response ExecuteCommand(RuntimeUnit request)
        {
            var activeScene = SceneManager.GetActiveScene();
            var scenes = new List<SceneInfo>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes.Add(new SceneInfo
                {
                    name = scene.name,
                    path = scene.path,
                    buildIndex = scene.buildIndex,
                    isLoaded = scene.isLoaded,
                    isActive = scene == activeScene,
                    rootCount = scene.isLoaded ? scene.rootCount : 0
                });
            }

            return new Response
            {
                scenes = scenes.ToArray()
            };
        }

        [Serializable]
        public class Response
        {
            public SceneInfo[] scenes;
        }

        [Serializable]
        public class SceneInfo
        {
            public string name;
            public string path;
            public int buildIndex;
            public bool isLoaded;
            public bool isActive;
            public int rootCount;
        }
    }
}
