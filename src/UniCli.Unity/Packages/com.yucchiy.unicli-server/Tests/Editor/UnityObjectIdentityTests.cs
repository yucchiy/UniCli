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

#if UNITY_6000_5_OR_NEWER
        [Test]
        public void GetId_ReturnsUntruncatedEntityId()
        {
            var go = CreateGameObject();

            var id = UnityObjectIdentity.GetId(go);
            var raw = unchecked((long)EntityId.ToULong(go.GetEntityId()));

            Assert.AreEqual(raw, id);
            Assert.AreNotEqual((long)unchecked((int)id), id,
                "EntityId raw values carry meaningful upper 32 bits on Unity 6.5; GetId must not truncate them.");
        }

        [Test]
        public void Resolve_LegacyIntRangeId_ResolvesSameObject()
        {
            var go = CreateGameObject();

            // Compatibility guarantee for clients that stored ids as 32-bit values:
            // the low 32 bits of the id (the legacy instance id, typically negative for
            // runtime-created objects) must still resolve in the current session.
            long legacyId = unchecked((int)UnityObjectIdentity.GetId(go));

            Assert.AreSame(go, UnityObjectIdentity.Resolve(legacyId));
        }
#else
        [Test]
        public void Resolve_OutOfIntRangeId_ReturnsNull()
        {
            Assert.IsNull(UnityObjectIdentity.Resolve(long.MaxValue));
            Assert.IsNull(UnityObjectIdentity.Resolve(long.MinValue));
        }
#endif

        private GameObject CreateGameObject()
        {
            var go = new GameObject($"UnityObjectIdentityTests_{Guid.NewGuid():N}");
            _createdObjects.Add(go);
            return go;
        }
    }
}
