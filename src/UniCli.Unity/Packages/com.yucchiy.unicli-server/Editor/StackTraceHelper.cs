namespace UniCli.Server.Editor
{
    internal static class StackTraceHelper
    {
        /// <summary>
        /// Truncate a stack trace to the specified number of lines.
        /// lines &lt; 0: return full stack trace, 0: return empty string, N &gt; 0: return first N lines.
        /// </summary>
        public static string Truncate(string stackTrace, int lines)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return "";

            if (lines < 0)
                return stackTrace;

            if (lines == 0)
                return "";

            var count = 0;
            var endIndex = 0;
            for (var i = 0; i < stackTrace.Length; i++)
            {
                if (stackTrace[i] != '\n')
                    continue;

                count++;
                if (count >= lines)
                {
                    endIndex = i;
                    break;
                }
            }

            if (count < lines)
                return stackTrace;

            return stackTrace.Substring(0, endIndex);
        }
    }
}
