using NUnit.Framework;
using UniCli.Server.Editor.Handlers.Remote;

namespace UniCli.Server.Editor.Tests
{
    [TestFixture]
    public class RemoteInvokeResponseTests
    {
        [Test]
        public void ToJson_ProducesValidJson()
        {
            Assert.That(new RemoteInvokeResponse
            {
                command = "Debug.Test", success = true, message = "say \"hello\"", data = "{\"value\":1}",
            }.ToJson(), Is.EqualTo("{\"command\":\"Debug.Test\",\"success\":true,\"message\":\"say \\\"hello\\\"\",\"data\":{\"value\":1}}"));

            Assert.That(new RemoteInvokeResponse
            {
                command = "Debug.Test", success = true, message = @"path\to\file", data = "{\"value\":1}",
            }.ToJson(), Is.EqualTo("{\"command\":\"Debug.Test\",\"success\":true,\"message\":\"path\\\\to\\\\file\",\"data\":{\"value\":1}}"));

            Assert.That(new RemoteInvokeResponse
            {
                command = "Debug.Test", success = true, message = "a\tb\nc", data = "{\"value\":1}",
            }.ToJson(), Is.EqualTo("{\"command\":\"Debug.Test\",\"success\":true,\"message\":\"a\tb\nc\",\"data\":{\"value\":1}}"));
        }
    }
}
