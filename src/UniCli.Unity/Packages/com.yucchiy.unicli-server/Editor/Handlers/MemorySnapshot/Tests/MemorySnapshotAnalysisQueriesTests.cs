#if UNICLI_MEMORY_PROFILER
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UniCli.Server.Editor.Handlers;

namespace UniCli.Server.Editor.MemorySnapshot.Tests
{
    public sealed class MemorySnapshotAnalysisQueriesTests
    {
        [Test]
        public void Unit_NativeObjectRetainer_PerTypeObjectsBelowGlobalTop_RetainsPerTypeTopObjects()
        {
            var retainer = new NativeObjectRetainer(globalCapacity: 2, perTypeCapacity: 2);
            retainer.Add(ObjectInfo(1, "Mesh", "huge-a", 1000));
            retainer.Add(ObjectInfo(2, "Mesh", "huge-b", 900));
            retainer.Add(ObjectInfo(3, "Texture2D", "small-a", 30));
            retainer.Add(ObjectInfo(4, "Texture2D", "small-b", 20));
            retainer.Add(ObjectInfo(5, "Texture2D", "small-c", 10));

            var retained = retainer.Build();

            CollectionAssert.AreEquivalent(new[] { 1L, 2L, 3L, 4L }, retained.Select(item => item.NativeObjectIndex));
        }

        [Test]
        public void Unit_GetTopObjects_TypeFilterHasMoreObjectsThanRetained_MarksTruncated()
        {
            var analysis = new SnapshotAnalysis
            {
                NativeObjectCount = 3,
                NativeTypeStats = new[]
                {
                    TypeStat("Texture2D", count: 3, totalSize: 60)
                },
                TopNativeObjects = new[]
                {
                    ObjectInfo(1, "Texture2D", "tex-a", 30),
                    ObjectInfo(2, "Texture2D", "tex-b", 20)
                }
            };

            var objects = MemorySnapshotAnalysisQueries.GetTopObjects(
                analysis,
                typeFilter: "Texture",
                nameFilter: "",
                limit: 20,
                out var truncated);

            Assert.AreEqual(2, objects.Length);
            Assert.IsTrue(truncated);
        }

        [Test]
        public void Unit_GetTopTypes_FilteredTypes_SortsByTotalSize()
        {
            var analysis = new SnapshotAnalysis
            {
                NativeTypeStats = new[]
                {
                    TypeStat("Texture2D", count: 2, totalSize: 20),
                    TypeStat("RenderTexture", count: 1, totalSize: 50),
                    TypeStat("Mesh", count: 4, totalSize: 100)
                },
                TopNativeObjects = new RetainedNativeObjectInfo[0]
            };

            var types = MemorySnapshotAnalysisQueries.GetTopTypes(
                analysis,
                typeFilter: "Texture",
                limit: 20);

            Assert.AreEqual(new[] { "RenderTexture", "Texture2D" }, types.Select(type => type.typeName).ToArray());
        }

        [Test]
        public void Unit_GetTopTypes_LimitAndMinSize_RollsUpHiddenTypes()
        {
            var analysis = Analysis(
                TypeStat("Texture2D", count: 2, totalSize: 100),
                TypeStat("Mesh", count: 4, totalSize: 80),
                TypeStat("AudioClip", count: 3, totalSize: 10));

            var result = MemorySnapshotAnalysisQueries.GetTopTypes(
                analysis,
                MemorySnapshotScope.Native,
                typeFilter: "",
                limit: 1,
                minSize: 20);

            Assert.AreEqual(new[] { "Texture2D" }, result.Types.Select(type => type.typeName).ToArray());
            Assert.AreEqual(2, result.OthersCount);
            Assert.AreEqual(90, result.OthersSize);
        }

        [Test]
        public void Unit_GetTopTypes_ManagedScope_UsesManagedTypeStats()
        {
            var analysis = new SnapshotAnalysis
            {
                NativeTypeStats = new[]
                {
                    TypeStat("Texture2D", count: 2, totalSize: 100)
                },
                ManagedTypeStats = new[]
                {
                    TypeStat("System.String", count: 5, totalSize: 50),
                    TypeStat("Game.Inventory", count: 1, totalSize: 80)
                },
                TopNativeObjects = new RetainedNativeObjectInfo[0]
            };

            var result = MemorySnapshotAnalysisQueries.GetTopTypes(
                analysis,
                MemorySnapshotScope.Managed,
                typeFilter: "",
                limit: 20,
                minSize: 0);

            Assert.AreEqual(new[] { "Game.Inventory", "System.String" }, result.Types.Select(type => type.typeName).ToArray());
        }

        [Test]
        public void Unit_GetDiffTypes_MixedDeltas_SortsByAbsoluteSizeDelta()
        {
            var before = Analysis(
                TypeStat("Texture2D", count: 10, totalSize: 100),
                TypeStat("Mesh", count: 4, totalSize: 80),
                TypeStat("AudioClip", count: 2, totalSize: 50));
            var after = Analysis(
                TypeStat("Texture2D", count: 12, totalSize: 130),
                TypeStat("Mesh", count: 4, totalSize: 70),
                TypeStat("AudioClip", count: 2, totalSize: 50));

            var diff = MemorySnapshotAnalysisQueries.GetDiffTypes(
                before,
                after,
                typeFilter: "",
                limit: 20);

            Assert.AreEqual(new[] { "Texture2D", "Mesh" }, diff.Select(type => type.typeName).ToArray());
            Assert.AreEqual(30, diff[0].sizeDelta);
            Assert.AreEqual(-10, diff[1].sizeDelta);
        }

        [Test]
        public void Unit_GetDiffTypes_MinSizeDelta_RollsUpHiddenDeltas()
        {
            var before = Analysis(
                TypeStat("Texture2D", count: 10, totalSize: 100),
                TypeStat("Mesh", count: 4, totalSize: 80),
                TypeStat("AudioClip", count: 2, totalSize: 50),
                TypeStat("Shader", count: 1, totalSize: 10));
            var after = Analysis(
                TypeStat("Texture2D", count: 12, totalSize: 150),
                TypeStat("Mesh", count: 5, totalSize: 100),
                TypeStat("AudioClip", count: 2, totalSize: 55),
                TypeStat("Shader", count: 1, totalSize: 10));

            var result = MemorySnapshotAnalysisQueries.GetDiffTypes(
                before,
                after,
                MemorySnapshotScope.Native,
                typeFilter: "",
                limit: 1,
                minSizeDelta: 10);

            Assert.AreEqual(new[] { "Texture2D" }, result.Types.Select(type => type.typeName).ToArray());
            Assert.AreEqual(2, result.OthersCount);
            Assert.AreEqual(25, result.OthersSize);
        }

        [Test]
        public void Unit_ResolveDefaultSnapshot_SelectsLatestAndSecondLatestByMtime()
        {
            var directory = Path.Combine(Path.GetTempPath(), "unicli-memorysnapshot-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var oldest = Path.Combine(directory, "oldest.snap");
                var second = Path.Combine(directory, "second.snap");
                var latest = Path.Combine(directory, "latest.snap");
                File.WriteAllText(oldest, "");
                File.WriteAllText(second, "");
                File.WriteAllText(latest, "");

                File.SetLastWriteTimeUtc(oldest, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                File.SetLastWriteTimeUtc(second, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
                File.SetLastWriteTimeUtc(latest, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

                Assert.AreEqual(latest, MemorySnapshotPathResolver.ResolveDefaultSnapshot(directory, 0));
                Assert.AreEqual(second, MemorySnapshotPathResolver.ResolveDefaultSnapshot(directory, 1));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void Unit_SnapshotAnalysisCache_LoadOrAnalyze_AssignsNameAndResolvesLoadedSnapshot()
        {
            var path = CreateTempSnapshotPath();

            try
            {
                var cache = new SnapshotAnalysisCache();
                var analyzer = new FakeSnapshotAnalyzer();
                var analysis = cache.LoadOrAnalyze(
                    path,
                    analyzer,
                    "before",
                    replace: false,
                    out var cached,
                    out _,
                    out var entry);

                Assert.IsFalse(cached);
                Assert.AreEqual(1, analyzer.AnalyzeCount);
                Assert.AreEqual("before", entry.name);
                Assert.IsTrue(entry.pinned);
                StringAssert.StartsWith("ms_", entry.id);

                var loadedByName = cache.GetLoaded("before", out var byNameEntry);
                var loadedById = cache.GetLoaded(entry.id, out var byIdEntry);

                Assert.AreSame(analysis, loadedByName);
                Assert.AreSame(analysis, loadedById);
                Assert.AreEqual(entry.id, byNameEntry.id);
                Assert.AreEqual("before", byIdEntry.name);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Unit_SnapshotAnalysisCache_LoadOrAnalyze_NameCollisionRequiresReplace()
        {
            var firstPath = CreateTempSnapshotPath();
            var secondPath = CreateTempSnapshotPath();

            try
            {
                var cache = new SnapshotAnalysisCache();
                var analyzer = new FakeSnapshotAnalyzer();
                cache.LoadOrAnalyze(firstPath, analyzer, "focus", replace: false, out _, out _, out var firstEntry);

                Assert.Throws<CommandFailedException>(() =>
                    cache.LoadOrAnalyze(secondPath, analyzer, "focus", replace: false, out _, out _, out _));

                cache.LoadOrAnalyze(secondPath, analyzer, "focus", replace: true, out _, out _, out var replacedEntry);
                var loaded = cache.GetLoaded("focus", out var loadedEntry);

                Assert.AreEqual(secondPath, loaded.Path);
                Assert.AreEqual(replacedEntry.id, loadedEntry.id);
                Assert.AreNotEqual(firstEntry.id, loadedEntry.id);
            }
            finally
            {
                File.Delete(firstPath);
                File.Delete(secondPath);
            }
        }

        [Test]
        public void Unit_SnapshotAnalysisCache_Remove_Name_ReleasesLoadedSnapshot()
        {
            var path = CreateTempSnapshotPath();

            try
            {
                var cache = new SnapshotAnalysisCache();
                var analyzer = new FakeSnapshotAnalyzer();
                cache.LoadOrAnalyze(path, analyzer, "release-me", replace: false, out _, out _, out _);

                Assert.AreEqual(1, cache.Remove("release-me"));
                Assert.AreEqual(0, cache.Remove("release-me"));
                Assert.Throws<CommandFailedException>(() => cache.GetLoaded("release-me", out _));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void Unit_AllOfMemoryRequest_Defaults_KeepsObjectRowsOptIn()
        {
            var request = new MemorySnapshotAllOfMemoryRequest();

            Assert.AreEqual(20, request.limit);
            Assert.IsTrue(request.includeCategories);
            Assert.IsTrue(request.includeNativeTypes);
            Assert.IsTrue(request.includeManagedTypes);
            Assert.IsFalse(request.includeNativeObjects);
            Assert.IsTrue(request.includeDiff);
        }

        [Test]
        public void Unit_AllOfMemoryScopeParser_VariousInputs_ReturnsExpected()
        {
            Assert.AreEqual(MemorySnapshotAllOfMemoryScope.All, MemorySnapshotAllOfMemoryScopeParser.Parse(""));
            Assert.AreEqual(MemorySnapshotAllOfMemoryScope.All, MemorySnapshotAllOfMemoryScopeParser.Parse("all"));
            Assert.AreEqual(MemorySnapshotAllOfMemoryScope.Native, MemorySnapshotAllOfMemoryScopeParser.Parse("native"));
            Assert.AreEqual(MemorySnapshotAllOfMemoryScope.Managed, MemorySnapshotAllOfMemoryScopeParser.Parse("managed"));
            Assert.Throws<CommandFailedException>(() => MemorySnapshotAllOfMemoryScopeParser.Parse("objects"));
        }

        private static SnapshotAnalysis Analysis(params MemorySnapshotNativeTypeStat[] typeStats)
        {
            return new SnapshotAnalysis
            {
                NativeTypeStats = typeStats,
                ManagedTypeStats = new MemorySnapshotNativeTypeStat[0],
                TopNativeObjects = new RetainedNativeObjectInfo[0]
            };
        }

        private static MemorySnapshotNativeTypeStat TypeStat(string typeName, long count, long totalSize)
        {
            return new MemorySnapshotNativeTypeStat
            {
                typeName = typeName,
                count = count,
                totalSize = totalSize
            };
        }

        private static RetainedNativeObjectInfo ObjectInfo(long index, string typeName, string name, long size)
        {
            return new RetainedNativeObjectInfo
            {
                NativeObjectIndex = index,
                TypeName = typeName,
                Name = name,
                Size = size,
                InstanceId = index.ToString()
            };
        }

        private static string CreateTempSnapshotPath()
        {
            var path = Path.Combine(Path.GetTempPath(), "unicli-memorysnapshot-" + Guid.NewGuid().ToString("N") + ".snap");
            File.WriteAllText(path, "");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            return path;
        }

        private sealed class FakeSnapshotAnalyzer : ISnapshotAnalyzer
        {
            public int AnalyzeCount { get; private set; }

            public bool IsAvailable(out string unavailableReason)
            {
                unavailableReason = "";
                return true;
            }

            public SnapshotAnalysis Analyze(string snapshotPath)
            {
                AnalyzeCount++;
                return new SnapshotAnalysis
                {
                    Metadata = new MemorySnapshotMetadataInfo(),
                    Categories = new MemorySnapshotCategoryInfo[0],
                    NativeTypeStats = new MemorySnapshotNativeTypeStat[0],
                    ManagedTypeStats = new MemorySnapshotNativeTypeStat[0],
                    TopNativeObjects = new RetainedNativeObjectInfo[0]
                };
            }
        }
    }
}
#endif
