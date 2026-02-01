using System;

namespace UniCli.Protocol
{
    [Serializable]
    public class TestRunnerResponse
    {
        public int passed;
        public int failed;
        public int skipped;
        public int total;
        public TestResult[] results;
    }

    [Serializable]
    public class TestResult
    {
        public string name;
        public string status; // "Passed", "Failed", "Skipped"
        public double duration; // In seconds
        public string message; // Error message or skip reason
        public string stackTrace; // Stack trace on failure
    }
}

