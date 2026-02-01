using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniCli.Server.Editor
{
    [Serializable]
    public sealed class LogEntry
    {
        public string message;
        public string stackTrace;
        public string type;
        public string timestamp;
    }

    [Serializable]
    public sealed class EditorLogResponse
    {
        public LogEntry[] logs;
        public int totalCount;
        public int displayedCount;
    }

    public sealed class EditorLogManager
    {
        private readonly Queue<LogEntry> _logBuffer = new();
        private readonly int _maxBufferSize;

        public EditorLogManager(int maxBufferSize = 10000)
        {
            _maxBufferSize = maxBufferSize;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                message = message,
                stackTrace = stackTrace,
                type = type.ToString(),
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            };

            _logBuffer.Enqueue(entry);
            if (_logBuffer.Count > _maxBufferSize)
            {
                _logBuffer.Dequeue();
            }
        }

        public int GetLogCount()
        {
            return _logBuffer.Count;
        }

        public LogEntry[] GetLogs()
        {
            return _logBuffer.ToArray();
        }

        public LogEntry[] GetLogs(string logType, string searchText, int maxCount)
        {
            var filterByType = !string.IsNullOrEmpty(logType)
                && !logType.Equals("All", StringComparison.OrdinalIgnoreCase);
            var filterBySearch = !string.IsNullOrEmpty(searchText);

            var result = new List<LogEntry>();

            // Filter all entries first, then take the last maxCount items
            foreach (var entry in _logBuffer)
            {
                if (filterByType && !entry.type.Equals(logType, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (filterBySearch && !entry.message.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    continue;

                result.Add(entry);
            }

            if (maxCount > 0 && result.Count > maxCount)
            {
                result.RemoveRange(0, result.Count - maxCount);
            }

            return result.ToArray();
        }

        public void ClearLogs()
        {
            _logBuffer.Clear();
        }
    }
}
