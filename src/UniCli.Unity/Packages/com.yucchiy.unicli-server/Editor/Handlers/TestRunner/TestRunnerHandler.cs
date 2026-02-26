using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class TestListHandler : CommandHandler<TestListRequest, TestListResponse>
    {
        public override string CommandName => "TestRunner.List";
        public override string Description => "List available tests for EditMode or PlayMode";

        protected override async ValueTask<TestListResponse> ExecuteAsync(TestListRequest request, CancellationToken cancellationToken)
        {
            var testMode = string.Equals(request.mode, "PlayMode", StringComparison.OrdinalIgnoreCase)
                ? TestMode.PlayMode
                : TestMode.EditMode;

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var tcs = new TaskCompletionSource<ITestAdaptor>();
            api.RetrieveTestList(testMode, adaptor => tcs.TrySetResult(adaptor));
            var root = await tcs.Task.WithCancellation(cancellationToken);

            var tests = new List<TestListEntry>();
            CollectTests(root, tests);

            return new TestListResponse
            {
                mode = testMode == TestMode.EditMode ? "EditMode" : "PlayMode",
                total = tests.Count,
                tests = tests.ToArray()
            };
        }

        private static void CollectTests(ITestAdaptor node, List<TestListEntry> tests)
        {
            if (!node.IsSuite)
            {
                var categories = node.Categories != null ? new List<string>(node.Categories).ToArray() : Array.Empty<string>();
                tests.Add(new TestListEntry
                {
                    fullName = node.FullName,
                    name = node.Name,
                    categories = categories
                });
                return;
            }

            if (!node.HasChildren) return;
            foreach (var child in node.Children)
                CollectTests(child, tests);
        }
    }

    [Serializable]
    public class TestListRequest
    {
        public string mode = "EditMode";
    }

    [Serializable]
    public class TestListResponse
    {
        public string mode;
        public int total;
        public TestListEntry[] tests;
    }

    [Serializable]
    public class TestListEntry
    {
        public string fullName;
        public string name;
        public string[] categories;
    }

    public sealed class TestRunEditModeHandler : CommandHandler<TestRunRequest, TestRunnerResponse>
    {
        public override string CommandName => "TestRunner.RunEditMode";
        public override string Description => "Run EditMode tests with optional name/assembly filter";

        protected override bool TryWriteFormatted(TestRunnerResponse response, bool success, IFormatWriter writer)
            => TestRunnerResponseFormatter.TryWriteFormatted(response, success, writer);

        protected override async ValueTask<TestRunnerResponse> ExecuteAsync(TestRunRequest request, CancellationToken cancellationToken)
        {
            return await TestRunnerHelper.RunTestsAsync(TestMode.EditMode, request, cancellationToken);
        }
    }

    public sealed class TestRunPlayModeHandler : CommandHandler<TestRunRequest, TestRunnerResponse>
    {
        public override string CommandName => "TestRunner.RunPlayMode";
        public override string Description => "Run PlayMode tests with optional name/assembly filter";

        protected override bool TryWriteFormatted(TestRunnerResponse response, bool success, IFormatWriter writer)
            => TestRunnerResponseFormatter.TryWriteFormatted(response, success, writer);

        protected override async ValueTask<TestRunnerResponse> ExecuteAsync(TestRunRequest request, CancellationToken cancellationToken)
        {
            return await TestRunnerHelper.RunTestsAsync(TestMode.PlayMode, request, cancellationToken);
        }
    }

    internal static class TestRunnerResponseFormatter
    {
        public static bool TryWriteFormatted(TestRunnerResponse response, bool success, IFormatWriter writer)
        {
            var status = success ? "passed" : "failed";
            writer.WriteLine($"Tests {status}: {response.passed} passed, {response.failed} failed, {response.skipped} skipped ({response.total} total)");

            if (response.results != null)
            {
                foreach (var result in response.results)
                {
                    if (result.status == "Passed")
                        continue;

                    var label = result.status == "Failed" ? "FAIL" : result.status.ToUpperInvariant();
                    var line = !string.IsNullOrEmpty(result.message)
                        ? $"  {label} {result.name} - {result.message}"
                        : $"  {label} {result.name}";
                    writer.WriteLine(line);
                }
            }

            return true;
        }
    }

    internal static class TestRunnerHelper
    {
        public static async ValueTask<TestRunnerResponse> RunTestsAsync(TestMode testMode, TestRunRequest request, CancellationToken cancellationToken)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var tcs = new TaskCompletionSource<TestRunnerResponse>();
            var filter = new Filter
            {
                testMode = testMode
            };

            if (request.testNames.Length > 0)
            {
                filter.testNames = request.testNames;
            }

            if (request.groupNames.Length > 0)
            {
                filter.groupNames = request.groupNames;
            }

            if (request.categories.Length > 0)
            {
                filter.categoryNames = request.categories;
            }

            if (request.assemblies.Length > 0)
            {
                filter.assemblyNames = request.assemblies;
            }

            var callbacks = new TestRunnerCallbacks(tcs, request.resultFilter);
            api.RegisterCallbacks(callbacks);
            try
            {
                api.Execute(new ExecutionSettings(filter));
                var response = await tcs.Task.WithCancellation(cancellationToken);
                if (response.failed > 0)
                    throw new CommandFailedException($"{response.failed} test(s) failed", response);
                return response;
            }
            finally
            {
                api.UnregisterCallbacks(callbacks);
            }
        }
    }

    [Serializable]
    public class TestRunRequest
    {
        public string[] testNames = Array.Empty<string>();
        public string[] groupNames = Array.Empty<string>();
        public string[] categories = Array.Empty<string>();
        public string[] assemblies = Array.Empty<string>();
        public string resultFilter = "failures"; // "failures" (default): failed+skipped only, "all": everything, "none": summary only
    }

    internal class TestRunnerCallbacks : ICallbacks
    {
        private readonly TaskCompletionSource<TestRunnerResponse> _tcs;
        private readonly System.Collections.Generic.List<TestResult> _testResults = new();
        private readonly string _resultFilter;
        private int _passedCount;
        private int _failedCount;
        private int _skippedCount;

        public TestRunnerCallbacks(TaskCompletionSource<TestRunnerResponse> tcs, string resultFilter = "failures")
        {
            _tcs = tcs;
            _resultFilter = string.IsNullOrEmpty(resultFilter) ? "failures" : resultFilter;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            _passedCount = 0;
            _failedCount = 0;
            _skippedCount = 0;
            _testResults.Clear();
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            _tcs.SetResult(new TestRunnerResponse
            {
                passed = _passedCount,
                failed = _failedCount,
                skipped = _skippedCount,
                total = _passedCount + _failedCount + _skippedCount,
                results = _testResults.ToArray()
            });
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.IsSuite) return;

            var status = result.TestStatus switch
            {
                TestStatus.Passed => "Passed",
                TestStatus.Failed => "Failed",
                TestStatus.Skipped => "Skipped",
                _ => "Unknown"
            };

            if (result.TestStatus == TestStatus.Passed)
            {
                _passedCount++;
            }
            else if (result.TestStatus == TestStatus.Failed)
            {
                _failedCount++;
            }
            else if (result.TestStatus == TestStatus.Skipped)
            {
                _skippedCount++;
            }

            var shouldInclude = _resultFilter switch
            {
                "all" => true,
                "none" => false,
                _ => result.TestStatus != TestStatus.Passed // "failures": failed + skipped
            };

            if (shouldInclude)
            {
                _testResults.Add(new TestResult
                {
                    name = result.Test.FullName,
                    status = status,
                    duration = result.Duration,
                    message = result.Message ?? "",
                    stackTrace = result.StackTrace ?? ""
                });
            }
        }
    }
}
