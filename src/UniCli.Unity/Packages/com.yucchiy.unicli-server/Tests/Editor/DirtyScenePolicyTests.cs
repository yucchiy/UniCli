using System;
using System.Collections.Generic;
using NUnit.Framework;
using UniCli.Server.Editor.Handlers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class DirtyScenePolicyTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("error")]
        [TestCase("Error")]
        public void Parse_ErrorValues_ReturnsError(string value)
        {
            Assert.That(DirtyScenePolicy.Parse(value, allowDiscard: true, "Scene.Open"), Is.EqualTo(DirtyAction.Error));
        }

        [TestCase("save")]
        [TestCase("Save")]
        public void Parse_Save_ReturnsSave(string value)
        {
            Assert.That(DirtyScenePolicy.Parse(value, allowDiscard: true, "Scene.Open"), Is.EqualTo(DirtyAction.Save));
        }

        [TestCase("discard")]
        [TestCase("Discard")]
        public void Parse_DiscardAllowed_ReturnsDiscard(string value)
        {
            Assert.That(DirtyScenePolicy.Parse(value, allowDiscard: true, "Scene.Open"), Is.EqualTo(DirtyAction.Discard));
        }

        [Test]
        public void Parse_DiscardNotAllowed_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => DirtyScenePolicy.Parse("discard", allowDiscard: false, "TestRunner.RunEditMode"));
            Assert.That(exception.Message, Does.Contain("discard"));
            Assert.That(exception.Message, Does.Contain("save"));
        }

        [Test]
        public void Parse_InvalidValue_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => DirtyScenePolicy.Parse("keep", allowDiscard: true, "Scene.Open"));
            Assert.That(exception.Message, Does.Contain("keep"));
        }

        [Test]
        public void Apply_NoDirtyScenes_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => DirtyScenePolicy.Apply(DirtyAction.Error, new List<Scene>(), "Scene.Open"));
        }

        // EditMode tests run in an untitled, unsaved scene, and Unity rejects
        // NewScene(Additive) while such a scene exists. The tests below therefore
        // dirty the active (untitled) test scene instead of creating one.

        [Test]
        public void Apply_ErrorWithDirtyScene_ThrowsWithSceneName()
        {
            var scene = MakeActiveSceneDirty(out var marker);
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DirtyScenePolicy.Apply(DirtyAction.Error, new List<Scene> { scene }, "Scene.Open"));
                Assert.That(exception.Message, Does.Contain("unsaved changes"));
                Assert.That(exception.Message, Does.Contain("dirtyAction"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(marker);
            }
        }

        [Test]
        public void Apply_ErrorWithDirtyScene_DiscardNotAllowed_MessageOmitsDiscard()
        {
            var scene = MakeActiveSceneDirty(out var marker);
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DirtyScenePolicy.Apply(DirtyAction.Error, new List<Scene> { scene }, "TestRunner.RunEditMode", allowDiscard: false));
                Assert.That(exception.Message, Does.Contain("\"save\""));
                Assert.That(exception.Message, Does.Not.Contain("discard"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(marker);
            }
        }

        [Test]
        public void Apply_SaveWithUntitledDirtyScene_Throws()
        {
            var scene = MakeActiveSceneDirty(out var marker);
            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => DirtyScenePolicy.Apply(DirtyAction.Save, new List<Scene> { scene }, "Scene.Open"));
                Assert.That(exception.Message, Does.Contain("untitled"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(marker);
            }
        }

        [Test]
        public void Apply_DiscardWithDirtyScene_DoesNotThrow()
        {
            var scene = MakeActiveSceneDirty(out var marker);
            try
            {
                Assert.DoesNotThrow(
                    () => DirtyScenePolicy.Apply(DirtyAction.Discard, new List<Scene> { scene }, "Scene.Open"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(marker);
            }
        }

        [Test]
        public void EnsureNoUntitledScenes_WithUntitledScene_Throws()
        {
            // The EditMode test scene itself is untitled (empty path).
            var scene = SceneManager.GetActiveScene();
            Assert.That(scene.path, Is.Empty, "precondition: test scene should be untitled");

            var exception = Assert.Throws<InvalidOperationException>(
                () => DirtyScenePolicy.EnsureNoUntitledScenes(new List<Scene> { scene }, "Scene.Save"));
            Assert.That(exception.Message, Does.Contain("untitled"));
            Assert.That(exception.Message, Does.Contain("saveAsPath"));
        }

        [Test]
        public void EnsureNoUntitledScenes_EmptyList_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => DirtyScenePolicy.EnsureNoUntitledScenes(new List<Scene>(), "Scene.Save"));
        }

        private static Scene MakeActiveSceneDirty(out GameObject marker)
        {
            var scene = SceneManager.GetActiveScene();
            marker = new GameObject("DirtyScenePolicyTests_Marker");
            SceneManager.MoveGameObjectToScene(marker, scene);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            Assert.That(scene.isDirty, Is.True, "precondition: scene should be dirty");
            return scene;
        }
    }
}
