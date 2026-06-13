using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;
using UnityEngine;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class SceneScreenshotHandlerTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private readonly List<string> _createdFiles = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();

            foreach (var file in _createdFiles)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            _createdFiles.Clear();
        }

        [Test]
        public void Screenshot2D_SavesPngWithRequestedDimensions()
        {
            var path = CreateTempPath();
            var response = ExecuteAsync(new SceneScreenshot2DHandler(), "Scene.Screenshot2D", new SceneScreenshot2DRequest
            {
                path = path,
                offset = new[] { 1f, 2f },
                size = 5f,
                width = 320,
                height = 240
            }).GetAwaiter().GetResult();

            Assert.IsTrue(File.Exists(response.path));
            Assert.AreEqual(320, response.width);
            Assert.AreEqual(240, response.height);
            Assert.Greater(response.size, 0);
        }

        [Test]
        public void Screenshot3D_SavesPngWithRequestedDimensions()
        {
            var path = CreateTempPath();
            var response = ExecuteAsync(new SceneScreenshot3DHandler(), "Scene.Screenshot3D", new SceneScreenshot3DRequest
            {
                path = path,
                yaw = 45f,
                pitch = 30f,
                distance = 10f,
                width = 320,
                height = 240
            }).GetAwaiter().GetResult();

            Assert.IsTrue(File.Exists(response.path));
            Assert.AreEqual(320, response.width);
            Assert.AreEqual(240, response.height);
            Assert.Greater(response.size, 0);
        }

        [Test]
        public void Screenshot3D_LookAtResolvesGameObject()
        {
            var target = new GameObject($"SceneScreenshotHandlerTests_{Guid.NewGuid():N}");
            target.transform.position = new Vector3(10f, 20f, 30f);
            _createdObjects.Add(target);

            var path = CreateTempPath();
            var response = ExecuteAsync(new SceneScreenshot3DHandler(), "Scene.Screenshot3D", new SceneScreenshot3DRequest
            {
                path = path,
                lookAt = target.name,
                offset = new[] { 0f, 1f, 0f },
                yaw = 90f,
                pitch = 45f,
                distance = 5f,
                width = 320,
                height = 240
            }).GetAwaiter().GetResult();

            Assert.IsTrue(File.Exists(response.path));
        }

        [Test]
        public void Screenshot3D_LookAtNotFound_Throws()
        {
            var path = CreateTempPath();
            Assert.Throws<ArgumentException>(() =>
                ExecuteAsync(new SceneScreenshot3DHandler(), "Scene.Screenshot3D", new SceneScreenshot3DRequest
                {
                    path = path,
                    lookAt = $"NonExistentObject_{Guid.NewGuid():N}",
                    yaw = 0f,
                    pitch = 0f
                }).GetAwaiter().GetResult());
        }

        [Test]
        public void Screenshot2D_InvalidOffsetLength_Throws()
        {
            var path = CreateTempPath();
            Assert.Throws<ArgumentException>(() =>
                ExecuteAsync(new SceneScreenshot2DHandler(), "Scene.Screenshot2D", new SceneScreenshot2DRequest
                {
                    path = path,
                    offset = new[] { 1f }
                }).GetAwaiter().GetResult());
        }

        private async Task<SceneScreenshotResponse> ExecuteAsync<TRequest>(ICommandHandler handler, string command, TRequest request)
        {
            var result = await handler.ExecuteAsync(
                new CommandRequest
                {
                    command = command,
                    data = JsonUtility.ToJson(request),
                    format = "json"
                },
                CancellationToken.None);

            return (SceneScreenshotResponse)result;
        }

        private string CreateTempPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"unicli_test_{Guid.NewGuid():N}.png");
            _createdFiles.Add(path);
            return path;
        }
    }
}
