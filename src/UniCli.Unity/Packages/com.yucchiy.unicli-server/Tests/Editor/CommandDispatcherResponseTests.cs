using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;
using UnityEditor;

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

        private static ServiceRegistry CreateServiceRegistry()
        {
            var services = new ServiceRegistry();
            var installerTypes = TypeCache.GetTypesDerivedFrom<IServiceInstaller>();
            foreach (var type in installerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var installer = (IServiceInstaller)Activator.CreateInstance(type);
                installer.Install(services);
            }

            return services;
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
            var dispatcher = new CommandDispatcher(CreateServiceRegistry());
            var handler = new StubHandler();
            var response = dispatcher.BuildResponse(true, "ok", Unit.Value, handler, false);
            Assert.IsTrue(response.success);
            Assert.AreEqual("", response.data);
        }

        [Test]
        public void BuildResponse_NullData_ReturnsEmptyData()
        {
            var dispatcher = new CommandDispatcher(CreateServiceRegistry());
            var handler = new StubHandler();
            var response = dispatcher.BuildResponse(true, "ok", null, handler, false);
            Assert.IsTrue(response.success);
            Assert.AreEqual("", response.data);
        }

        [Test]
        public void BuildResponse_JsonMode_ReturnsJsonData()
        {
            var dispatcher = new CommandDispatcher(CreateServiceRegistry());
            var handler = new StubHandler();
            var data = new TestResponse { value = "hello" };
            var response = dispatcher.BuildResponse(true, "ok", data, handler, false);
            Assert.AreEqual("json", response.format);
            StringAssert.Contains("hello", response.data);
        }

        [Test]
        public void BuildResponse_TextMode_WithFormatter_ReturnsTextData()
        {
            var dispatcher = new CommandDispatcher(CreateServiceRegistry());
            var handler = new StubFormatterHandler();
            var data = new TestResponse { value = "world" };
            var response = dispatcher.BuildResponse(true, "ok", data, handler, true);
            Assert.AreEqual("text", response.format);
            StringAssert.Contains("formatted:world", response.data);
        }

        [Test]
        public void BuildResponse_TextMode_WithoutFormatter_FallsBackToJson()
        {
            var dispatcher = new CommandDispatcher(CreateServiceRegistry());
            var handler = new StubHandler();
            var data = new TestResponse { value = "fallback" };
            var response = dispatcher.BuildResponse(true, "ok", data, handler, true);
            Assert.AreEqual("json", response.format);
            StringAssert.Contains("fallback", response.data);
        }

        [Test]
        public void BuildResponse_CommandListResponse_WithFlatTypeDetails_UsesDefaultJsonSerialization()
        {
            var dispatcher = new CommandDispatcher(CreateServiceRegistry());
            var handler = new StubHandler();
            var data = new CommandListResponse
            {
                commands = new[]
                {
                    new CommandInfo
                    {
                        name = "Test.Command",
                        description = "Command metadata",
                        builtIn = true,
                        module = "",
                        requestFields = Array.Empty<CommandFieldInfo>(),
                        responseFields = new[]
                        {
                            new CommandFieldInfo
                            {
                                name = "root",
                                type = "Level0",
                                typeId = "Tests:Level0",
                                defaultValue = ""
                            }
                        },
                        requestTypeDetails = Array.Empty<CommandTypeDetail>(),
                        responseTypeDetails = CreateTypeDetailChain(12)
                    }
                }
            };

            var response = dispatcher.BuildResponse(true, "ok", data, handler, false);

            Assert.AreEqual("json", response.format);

            for (var i = 0; i < 12; i++)
            {
                StringAssert.Contains($"\"typeName\":\"Level{i}\"", response.data);
            }

            StringAssert.Contains("\"responseTypeDetails\":", response.data);
            StringAssert.Contains("\"name\":\"root\",\"type\":\"Level0\",\"typeId\":\"Tests:Level0\",\"defaultValue\":\"\"", response.data);
            Assert.AreEqual(12, CountOccurrences(response.data, "\"typeName\":"));
            Assert.AreEqual(25, CountOccurrences(response.data, "\"typeId\":"));
            Assert.IsFalse(response.data.Contains("\"children\":", StringComparison.Ordinal));
        }

        [Test]
        public void BuildResponse_CommandListResponse_WithCollidingTypeNames_PreservesDistinctTypeIds()
        {
            var dispatcher = new CommandDispatcher(CreateServiceRegistry());
            var handler = new StubHandler();
            var data = new CommandListResponse
            {
                commands = new[]
                {
                    new CommandInfo
                    {
                        name = "Test.Collision",
                        description = "Collision metadata",
                        builtIn = true,
                        module = "",
                        requestFields = new[]
                        {
                            new CommandFieldInfo
                            {
                                name = "alpha",
                                type = "Duplicate",
                                typeId = "Tests:Alpha.Duplicate",
                                defaultValue = ""
                            },
                            new CommandFieldInfo
                            {
                                name = "beta",
                                type = "Duplicate",
                                typeId = "Tests:Beta.Duplicate",
                                defaultValue = ""
                            }
                        },
                        responseFields = Array.Empty<CommandFieldInfo>(),
                        requestTypeDetails = new[]
                        {
                            new CommandTypeDetail
                            {
                                typeName = "Duplicate",
                                typeId = "Tests:Alpha.Duplicate",
                                fields = new[]
                                {
                                    new CommandFieldInfo
                                    {
                                        name = "profile",
                                        type = "string",
                                        typeId = "",
                                        defaultValue = ""
                                    }
                                }
                            },
                            new CommandTypeDetail
                            {
                                typeName = "Duplicate",
                                typeId = "Tests:Beta.Duplicate",
                                fields = new[]
                                {
                                    new CommandFieldInfo
                                    {
                                        name = "retries",
                                        type = "int",
                                        typeId = "",
                                        defaultValue = ""
                                    }
                                }
                            }
                        },
                        responseTypeDetails = Array.Empty<CommandTypeDetail>()
                    }
                }
            };

            var response = dispatcher.BuildResponse(true, "ok", data, handler, false);

            StringAssert.Contains("\"typeName\":\"Duplicate\",\"typeId\":\"Tests:Alpha.Duplicate\"", response.data);
            StringAssert.Contains("\"typeName\":\"Duplicate\",\"typeId\":\"Tests:Beta.Duplicate\"", response.data);
            StringAssert.Contains("\"name\":\"alpha\",\"type\":\"Duplicate\",\"typeId\":\"Tests:Alpha.Duplicate\"", response.data);
            StringAssert.Contains("\"name\":\"beta\",\"type\":\"Duplicate\",\"typeId\":\"Tests:Beta.Duplicate\"", response.data);
        }

        private static CommandTypeDetail[] CreateTypeDetailChain(int depth)
        {
            var result = new CommandTypeDetail[depth];

            for (var i = 0; i < depth; i++)
            {
                result[i] = new CommandTypeDetail
                {
                    typeName = $"Level{i}",
                    typeId = $"Tests:Level{i}",
                    fields = i == depth - 1
                        ? new[]
                        {
                            new CommandFieldInfo
                            {
                                name = "value",
                                type = "string",
                                typeId = "",
                                defaultValue = ""
                            }
                        }
                        : new[]
                        {
                            new CommandFieldInfo
                            {
                                name = $"level{i + 1}",
                                type = $"Level{i + 1}",
                                typeId = $"Tests:Level{i + 1}",
                                defaultValue = ""
                            }
                        }
                };
            }

            return result;
        }

        private static int CountOccurrences(string text, string value)
        {
            var count = 0;
            var startIndex = 0;

            while ((startIndex = text.IndexOf(value, startIndex, StringComparison.Ordinal)) >= 0)
            {
                count++;
                startIndex += value.Length;
            }

            return count;
        }
    }
}
