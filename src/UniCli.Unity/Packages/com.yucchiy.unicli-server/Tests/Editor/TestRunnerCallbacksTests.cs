using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;
using UnityEditor.TestTools.TestRunner.Api;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class TestRunnerCallbacksTests
    {
        [Test]
        public void TestRunnerCallbacks_OnError_FaultsTask()
        {
            var tcs = new TaskCompletionSource<TestRunnerResponse>();
            var callbacks = (IErrorCallbacks)new TestRunnerCallbacks(tcs);

            callbacks.OnError("boom");

            Assert.That(tcs.Task.IsFaulted, Is.True);
            var exception = tcs.Task.Exception?.GetBaseException();
            Assert.That(exception, Is.TypeOf<InvalidOperationException>());
            Assert.That(exception.Message, Is.EqualTo("Test run aborted: boom"));
        }

        [Test]
        public void TestRunnerCallbacks_RunFinishedAfterOnError_DoesNotThrow()
        {
            var tcs = new TaskCompletionSource<TestRunnerResponse>();
            var callbacks = new TestRunnerCallbacks(tcs);
            ((IErrorCallbacks)callbacks).OnError("boom");

            Assert.DoesNotThrow(() => callbacks.RunFinished(null));
            Assert.That(tcs.Task.IsFaulted, Is.True);
        }
    }
}
