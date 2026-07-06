using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class UnityObjectIdentityTests
    {
        private readonly List<GameObject> _createdObjects = new();

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
        public void GetId_ThenResolve_ReturnsSameGameObject()
        {
            var go = CreateGameObject();

            var id = UnityObjectIdentity.GetId(go);
            var resolved = UnityObjectIdentity.Resolve(id);

            Assert.AreSame(go, resolved);
        }

        [Test]
        public void GetId_ThenResolveGeneric_ReturnsTypedGameObject()
        {
            var go = CreateGameObject();

            var id = UnityObjectIdentity.GetId(go);
            var resolved = UnityObjectIdentity.Resolve<GameObject>(id);

            Assert.AreSame(go, resolved);
        }

        [Test]
        public void GetId_ThenResolve_RoundTripsComponent()
        {
            var go = CreateGameObject();
            var collider = go.AddComponent<BoxCollider>();

            var id = UnityObjectIdentity.GetId(collider);
            var resolved = UnityObjectIdentity.Resolve(id);

            Assert.AreSame(collider, resolved);
        }

        [Test]
        public void Resolve_Zero_ReturnsNull()
        {
            Assert.IsNull(UnityObjectIdentity.Resolve(0));
        }

        [Test]
        public void ResolveGeneric_MismatchedType_ReturnsNull()
        {
            var go = CreateGameObject();
            var collider = go.AddComponent<BoxCollider>();

            var resolved = UnityObjectIdentity.Resolve<Rigidbody>(UnityObjectIdentity.GetId(collider));

            Assert.IsNull(resolved);
        }

        private GameObject CreateGameObject()
        {
            var go = new GameObject($"UnityObjectIdentityTests_{Guid.NewGuid():N}");
            _createdObjects.Add(go);
            return go;
        }
    }
}
