using NUnit.Framework;
using UniCli.Server.Editor;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class CommandPreconditionsTests
    {
        private sealed class FakeProbe : IEditorStateProbe
        {
            public bool IsPlaying { get; set; }
            public bool IsCompiling { get; set; }
        }

        // Plain classes rather than handlers: the attribute is what is under test, and
        // CommandDispatcher skips .Tests assemblies when registering, so a stub handler here
        // would never be dispatched anyway.
        private sealed class Undeclared { }

        [CommandPrecondition(EditorState = GuardCondition.NotPlaying)]
        private sealed class NeedsEditMode { }

        [CommandPrecondition(EditorState = GuardCondition.NotCompiling)]
        private sealed class NeedsIdleCompiler { }

        [CommandPrecondition(EditorState = GuardCondition.NotPlayingOrCompiling)]
        private sealed class NeedsBoth { }

        [SetUp]
        public void ClearCache() => CommandPreconditions.ClearCacheForTesting();

        [Test]
        public void Resolve_NoAttribute_ReturnsEmpty()
        {
            var precondition = CommandPreconditions.Resolve(typeof(Undeclared));

            Assert.That(precondition.IsEmpty, Is.True);
            Assert.That(precondition.EditorStateName, Is.Null);
        }

        [Test]
        public void Resolve_NullType_ReturnsEmptyRatherThanThrowing()
        {
            // Reached when a handler implements ICommandHandler directly; must not break dispatch.
            Assert.That(CommandPreconditions.Resolve(null).IsEmpty, Is.True);
        }

        [Test]
        public void Resolve_IsCached_ReturnsSameValueOnRepeatedCalls()
        {
            var first = CommandPreconditions.Resolve(typeof(NeedsBoth));
            var second = CommandPreconditions.Resolve(typeof(NeedsBoth));

            Assert.That(second.EditorState, Is.EqualTo(first.EditorState));
        }

        [Test]
        public void EditorStateName_IsTheDeclaredConditionName()
        {
            // Goes into command metadata, so clients can see the requirement before calling.
            Assert.That(CommandPreconditions.Resolve(typeof(NeedsBoth)).EditorStateName,
                Is.EqualTo("NotPlayingOrCompiling"));
        }

        [Test]
        public void Check_Undeclared_AllowsAnyState()
        {
            var precondition = CommandPreconditions.Resolve(typeof(Undeclared));
            var probe = new FakeProbe { IsPlaying = true, IsCompiling = true };

            Assert.That(CommandPreconditions.Check(precondition, "Some.Command", probe), Is.Null);
        }

        [Test]
        public void Check_NotPlaying_BlocksInPlayMode_AndSaysHowToRecover()
        {
            var precondition = CommandPreconditions.Resolve(typeof(NeedsEditMode));
            var probe = new FakeProbe { IsPlaying = true };

            var reason = CommandPreconditions.Check(precondition, "Scene.Open", probe);

            Assert.That(reason, Is.Not.Null);
            Assert.That(reason, Does.Contain("Scene.Open"), "the caller needs to know which command was refused");
            Assert.That(reason, Does.Contain("PlayMode.Exit"), "a refusal should say how to proceed");
        }

        [Test]
        public void Check_NotPlaying_IgnoresCompiling()
        {
            var precondition = CommandPreconditions.Resolve(typeof(NeedsEditMode));
            var probe = new FakeProbe { IsCompiling = true };

            Assert.That(CommandPreconditions.Check(precondition, "Scene.Open", probe), Is.Null);
        }

        [Test]
        public void Check_NotCompiling_BlocksWhileCompiling()
        {
            var precondition = CommandPreconditions.Resolve(typeof(NeedsIdleCompiler));
            var probe = new FakeProbe { IsCompiling = true };

            Assert.That(CommandPreconditions.Check(precondition, "eval", probe), Does.Contain("compiling"));
        }

        [Test]
        public void Check_NotCompiling_IgnoresPlayMode()
        {
            var precondition = CommandPreconditions.Resolve(typeof(NeedsIdleCompiler));
            var probe = new FakeProbe { IsPlaying = true };

            Assert.That(CommandPreconditions.Check(precondition, "eval", probe), Is.Null);
        }

        [TestCase(true, false, "Play Mode")]
        [TestCase(false, true, "compiling")]
        public void Check_NotPlayingOrCompiling_BlocksEither(bool playing, bool compiling, string expected)
        {
            var precondition = CommandPreconditions.Resolve(typeof(NeedsBoth));
            var probe = new FakeProbe { IsPlaying = playing, IsCompiling = compiling };

            Assert.That(CommandPreconditions.Check(precondition, "BuildPlayer.Build", probe), Does.Contain(expected));
        }

        [Test]
        public void Check_NotPlayingOrCompiling_AllowsIdleEditor()
        {
            var precondition = CommandPreconditions.Resolve(typeof(NeedsBoth));

            Assert.That(CommandPreconditions.Check(precondition, "BuildPlayer.Build", new FakeProbe()), Is.Null);
        }

        [Test]
        public void Check_NullProbe_Throws()
        {
            var precondition = CommandPreconditions.Resolve(typeof(NeedsEditMode));

            Assert.Throws<System.ArgumentNullException>(
                () => CommandPreconditions.Check(precondition, "Scene.Open", null));
        }
    }
}
