using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class TestRunEditModeHandler : CommandHandler<TestRunRequest, TestRunnerResponse>
    {
        public override string CommandName => CommandNames.TestRunner.RunEditMode;
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
        public override string CommandName => CommandNames.TestRunner.RunPlayMode;
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

            if (!string.IsNullOrEmpty(request.testNameFilter))
            {
                filter.testNames = new[] { request.testNameFilter };
            }

            if (!string.IsNullOrEmpty(request.assemblyFilter))
            {
                filter.assemblyNames = new[] { request.assemblyFilter };
            }

            var callbacks = new TestRunnerCallbacks(tcs);
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
        public string testNameFilter = "";
        public string assemblyFilter = "";
    }

    internal class TestRunnerCallbacks : ICallbacks
    {
        private readonly TaskCompletionSource<TestRunnerResponse> _tcs;
        private readonly System.Collections.Generic.List<TestResult> _testResults = new();
        private int _passedCount;
        private int _failedCount;
        private int _skippedCount;

        public TestRunnerCallbacks(TaskCompletionSource<TestRunnerResponse> tcs)
        {
            _tcs = tcs;
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

            _testResults.Add(new TestResult
            {
                name = result.Test.FullName,
                status = status,
                duration = result.Duration,
                message = result.Message ?? "",
                stackTrace = result.StackTrace ?? ""
            });

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
        }
    }
}
