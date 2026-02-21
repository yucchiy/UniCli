using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;
using UniCli.Server.Editor.Internal;
using UnityEngine;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class CommandDispatcherResponseTests
    {
        [Serializable]
        private class TestResponse
        {
            public string value;
        }

        private class StubHandler : ICommandHandler
        {
            public string CommandName => "Test.Stub";
            public string Description => "Stub handler for tests";
            public CommandInfo GetCommandInfo() => new() { name = CommandName, description = Description };
            public ValueTask<object> ExecuteAsync(object request, CancellationToken cancellationToken) => default;
        }

        private class StubFormatterHandler : ICommandHandler, IResponseFormatter
        {
            public string CommandName => "Test.Formatter";
            public string Description => "Stub formatter handler";
            public CommandInfo GetCommandInfo() => new() { name = CommandName, description = Description };
            public ValueTask<object> ExecuteAsync(object request, CancellationToken cancellationToken) => default;

            public bool TryWriteFormatted(object response, bool success, IFormatWriter writer)
            {
                writer.WriteLine($"formatted:{((TestResponse)response).value}");
                return true;
            }
        }

        [Test]
        public void MakeResponse_Success_SetsFieldsCorrectly()
        {
            var response = CommandDispatcher.MakeResponse(true, "ok", "{}", "json");
            Assert.IsTrue(response.success);
            Assert.AreEqual("ok", response.message);
            Assert.AreEqual("{}", response.data);
            Assert.AreEqual("json", response.format);
        }

        [Test]
        public void MakeResponse_Error_SetsFieldsCorrectly()
        {
            var response = CommandDispatcher.MakeResponse(false, "fail");
            Assert.IsFalse(response.success);
            Assert.AreEqual("fail", response.message);
            Assert.AreEqual("", response.data);
            Assert.AreEqual("json", response.format);
        }

        [Test]
        public void BuildResponse_UnitData_ReturnsEmptyData()
        {
            var dispatcher = new CommandDispatcher(new ServiceRegistry());
            var handler = new StubHandler();
            var response = dispatcher.BuildResponse(true, "ok", Unit.Value, handler, false);
            Assert.IsTrue(response.success);
            Assert.AreEqual("", response.data);
        }

        [Test]
        public void BuildResponse_NullData_ReturnsEmptyData()
        {
            var dispatcher = new CommandDispatcher(new ServiceRegistry());
            var handler = new StubHandler();
            var response = dispatcher.BuildResponse(true, "ok", null, handler, false);
            Assert.IsTrue(response.success);
            Assert.AreEqual("", response.data);
        }

        [Test]
        public void BuildResponse_JsonMode_ReturnsJsonData()
        {
            var dispatcher = new CommandDispatcher(new ServiceRegistry());
            var handler = new StubHandler();
            var data = new TestResponse { value = "hello" };
            var response = dispatcher.BuildResponse(true, "ok", data, handler, false);
            Assert.AreEqual("json", response.format);
            StringAssert.Contains("hello", response.data);
        }

        [Test]
        public void BuildResponse_TextMode_WithFormatter_ReturnsTextData()
        {
            var dispatcher = new CommandDispatcher(new ServiceRegistry());
            var handler = new StubFormatterHandler();
            var data = new TestResponse { value = "world" };
            var response = dispatcher.BuildResponse(true, "ok", data, handler, true);
            Assert.AreEqual("text", response.format);
            StringAssert.Contains("formatted:world", response.data);
        }

        [Test]
        public void BuildResponse_TextMode_WithoutFormatter_FallsBackToJson()
        {
            var dispatcher = new CommandDispatcher(new ServiceRegistry());
            var handler = new StubHandler();
            var data = new TestResponse { value = "fallback" };
            var response = dispatcher.BuildResponse(true, "ok", data, handler, true);
            Assert.AreEqual("json", response.format);
            StringAssert.Contains("fallback", response.data);
        }
    }
}
