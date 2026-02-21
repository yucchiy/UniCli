using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    public sealed class LogCapture
    {
        private const int DefaultCapacity = 256;

        private readonly LogEntry[] _buffer;
        private int _head;
        private int _count;

        public LogCapture(int capacity = DefaultCapacity)
        {
            _buffer = new LogEntry[capacity];
        }

        public void Start()
        {
            Application.logMessageReceived += OnLogMessage;
        }

        public void Stop()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        public LogEntry[] GetEntries(int limit, LogType? typeFilter)
        {
            var resultCount = Mathf.Min(limit > 0 ? limit : _count, _count);
            var entries = new LogEntry[resultCount];

            if (typeFilter.HasValue)
            {
                var filtered = new System.Collections.Generic.List<LogEntry>(resultCount);
                var startIndex = (_head - _count + _buffer.Length) % _buffer.Length;
                for (var i = 0; i < _count && filtered.Count < resultCount; i++)
                {
                    var entry = _buffer[(startIndex + i) % _buffer.Length];
                    if (entry.type == typeFilter.Value.ToString())
                        filtered.Add(entry);
                }
                return filtered.ToArray();
            }

            var offset = _count - resultCount;
            var start = (_head - _count + _buffer.Length) % _buffer.Length;
            for (var i = 0; i < resultCount; i++)
                entries[i] = _buffer[(start + offset + i) % _buffer.Length];

            return entries;
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            _buffer[_head] = new LogEntry
            {
                message = condition,
                stackTrace = stackTrace,
                type = type.ToString(),
                timestamp = Time.realtimeSinceStartup
            };
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length)
                _count++;
        }

        [Serializable]
        public struct LogEntry
        {
            public string message;
            public string stackTrace;
            public string type;
            public float timestamp;
        }
    }
}
