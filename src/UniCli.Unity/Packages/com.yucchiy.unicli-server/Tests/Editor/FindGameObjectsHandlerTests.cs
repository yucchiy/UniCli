using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;
using UnityEngine;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class FindGameObjectsHandlerTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private FindGameObjectsHandler _handler;
        private string _prefix;

        [SetUp]
        public void SetUp()
        {
            _handler = new FindGameObjectsHandler();
            _prefix = $"FindGameObjectsHandlerTests_{Guid.NewGuid():N}";
        }

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
        }

        [Test]
        public void ExecuteAsync_NameExactMatch_ReturnsOnlyExactName()
        {
            var exactName = CreateName("Target");
            CreateGameObject(exactName);
            CreateGameObject($"{exactName}(Clone)");

            var response = ExecuteAsync(new FindGameObjectsRequest
            {
                name = exactName
            }).GetAwaiter().GetResult();

            Assert.AreEqual(1, response.totalFound);
            Assert.AreEqual(1, response.results.Length);
            Assert.AreEqual(exactName, response.results[0].name);
        }

        [Test]
        public void ExecuteAsync_NameExactMatch_IsCaseSensitive()
        {
            var lowerCaseName = CreateName("target");
            CreateGameObject(lowerCaseName);

            var response = ExecuteAsync(new FindGameObjectsRequest
            {
                name = CreateName("TARGET")
            }).GetAwaiter().GetResult();

            Assert.AreEqual(0, response.totalFound);
            Assert.AreEqual(0, response.results.Length);
        }

        [Test]
        public void ExecuteAsync_NamePattern_KeepsCaseInsensitiveSubstringBehavior()
        {
            CreateGameObject(CreateName("AlphaBeta"));

            var response = ExecuteAsync(new FindGameObjectsRequest
            {
                namePattern = "betA"
            }).GetAwaiter().GetResult();

            Assert.AreEqual(1, response.totalFound);
            Assert.AreEqual(1, response.results.Length);
        }

        [Test]
        public void ExecuteAsync_NameAndNamePattern_RequiresBothToMatch()
        {
            var exactName = CreateName("AlphaBeta");
            CreateGameObject(exactName);
            CreateGameObject(CreateName("AlphaGamma"));

            var response = ExecuteAsync(new FindGameObjectsRequest
            {
                name = exactName,
                namePattern = "Gamma"
            }).GetAwaiter().GetResult();

            Assert.AreEqual(0, response.totalFound);
            Assert.AreEqual(0, response.results.Length);
        }

        private async Task<FindGameObjectsResponse> ExecuteAsync(FindGameObjectsRequest request)
        {
            var result = await ((ICommandHandler)_handler).ExecuteAsync(
                new CommandRequest
                {
                    command = "GameObject.Find",
                    data = JsonUtility.ToJson(request),
                    format = "json"
                },
                CancellationToken.None);

            return (FindGameObjectsResponse)result;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private string CreateName(string suffix) => $"{_prefix}_{suffix}";
    }
}
