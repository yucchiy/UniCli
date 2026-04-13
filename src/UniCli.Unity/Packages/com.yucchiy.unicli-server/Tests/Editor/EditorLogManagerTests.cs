using System;
using NUnit.Framework;
using UnityEngine;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class EditorLogManagerTests
    {
        private EditorLogManager _manager;
        private string _prefix;

        [SetUp]
        public void SetUp()
        {
            _manager = new EditorLogManager();
            _prefix = $"EditorLogManagerTests_{Guid.NewGuid():N}";
        }

        [TearDown]
        public void TearDown()
        {
            _manager.ClearLogs();
        }

        [Test]
        public void GetLogs_ReturnsLogsInChronologicalOrder()
        {
            Debug.Log($"{_prefix}_1");
            Debug.Log($"{_prefix}_2");
            Debug.Log($"{_prefix}_3");

            var logs = _manager.GetLogs("All", _prefix, 0);

            Assert.AreEqual(3, logs.Length);
            Assert.AreEqual($"{_prefix}_1", logs[0].message);
            Assert.AreEqual($"{_prefix}_2", logs[1].message);
            Assert.AreEqual($"{_prefix}_3", logs[2].message);
        }

        [Test]
        public void GetLogs_WithMaxCount_ReturnsNewestEntriesInChronologicalOrder()
        {
            Debug.Log($"{_prefix}_1");
            Debug.Log($"{_prefix}_2");
            Debug.Log($"{_prefix}_3");
            Debug.Log($"{_prefix}_4");

            var logs = _manager.GetLogs("All", _prefix, 2);

            Assert.AreEqual(2, logs.Length);
            Assert.AreEqual($"{_prefix}_3", logs[0].message);
            Assert.AreEqual($"{_prefix}_4", logs[1].message);
        }

        [Test]
        public void GetLogs_WithLogTypeFilter_PreservesChronologicalOrder()
        {
            Debug.Log($"{_prefix}_Log1");
            Debug.LogWarning($"{_prefix}_Warn1");
            Debug.Log($"{_prefix}_Log2");
            Debug.LogWarning($"{_prefix}_Warn2");
            Debug.Log($"{_prefix}_Log3");

            var logs = _manager.GetLogs("Warning", _prefix, 0);

            Assert.AreEqual(2, logs.Length);
            Assert.AreEqual($"{_prefix}_Warn1", logs[0].message);
            Assert.AreEqual($"{_prefix}_Warn2", logs[1].message);
        }
    }
}
