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
        private readonly object _lock = new();
        private readonly Queue<LogEntry> _logBuffer = new();
        private readonly int _maxBufferSize;

        public EditorLogManager(int maxBufferSize = 10000)
        {
            _maxBufferSize = maxBufferSize;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
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

            lock (_lock)
            {
                _logBuffer.Enqueue(entry);
                if (_logBuffer.Count > _maxBufferSize)
                {
                    _logBuffer.Dequeue();
                }
            }
        }

        public int GetLogCount()
        {
            lock (_lock)
            {
                return _logBuffer.Count;
            }
        }

        public LogEntry[] GetLogs()
        {
            lock (_lock)
            {
                return _logBuffer.ToArray();
            }
        }

        public LogEntry[] GetLogs(string logType, string searchText, int maxCount)
        {
            var filterByType = !string.IsNullOrEmpty(logType)
                && !logType.Equals("All", StringComparison.OrdinalIgnoreCase);
            var filterBySearch = !string.IsNullOrEmpty(searchText);

            string[] allowedTypes = null;
            if (filterByType)
            {
                allowedTypes = logType.Split(',');
                for (var i = 0; i < allowedTypes.Length; i++)
                    allowedTypes[i] = allowedTypes[i].Trim();
            }

            var result = new List<LogEntry>();

            lock (_lock)
            {
                foreach (var entry in _logBuffer)
                {
                    if (filterByType && !MatchesAnyType(entry.type, allowedTypes))
                        continue;

                    if (filterBySearch && !entry.message.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    result.Add(entry);
                }
            }

            if (maxCount > 0 && result.Count > maxCount)
            {
                result.RemoveRange(0, result.Count - maxCount);
            }

            return result.ToArray();
        }

        private static bool MatchesAnyType(string entryType, string[] allowedTypes)
        {
            foreach (var type in allowedTypes)
            {
                if (entryType.Equals(type, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public void ClearLogs()
        {
            lock (_lock)
            {
                _logBuffer.Clear();
            }
        }
    }
}
