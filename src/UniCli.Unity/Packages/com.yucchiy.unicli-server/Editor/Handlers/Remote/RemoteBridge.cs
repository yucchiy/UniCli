using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Remote;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace UniCli.Server.Editor.Handlers.Remote
{
    public sealed class RemoteBridge : ScriptableObject
    {
        private readonly Dictionary<string, TaskCompletionSource<string>> _pendingRequests =
            new Dictionary<string, TaskCompletionSource<string>>();

        private readonly Dictionary<string, ChunkAccumulator> _chunkBuffers =
            new Dictionary<string, ChunkAccumulator>();

        private bool _registered;

        public void EnsureRegistered()
        {
            if (_registered)
                return;

            EditorConnection.instance.Register(RuntimeMessageGuids.ChunkedResponse, OnChunkedResponse);
            _registered = true;
        }

        private void OnDestroy()
        {
            if (!_registered)
                return;

            EditorConnection.instance.Unregister(RuntimeMessageGuids.ChunkedResponse, OnChunkedResponse);
            _registered = false;

            foreach (var kvp in _pendingRequests)
                kvp.Value.TrySetCanceled();
            _pendingRequests.Clear();
            _chunkBuffers.Clear();
        }

        public async Task<string> SendCommandAsync(string command, string data, int playerId, CancellationToken cancellationToken)
        {
            EnsureRegistered();

            var requestId = Guid.NewGuid().ToString();
            var request = new RuntimeCommandRequest
            {
                requestId = requestId,
                command = command,
                data = data ?? ""
            };

            var json = JsonUtility.ToJson(request);
            var bytes = Encoding.UTF8.GetBytes(json);

            var tcs = new TaskCompletionSource<string>();
            _pendingRequests[requestId] = tcs;

            using var registration = cancellationToken.Register(() =>
            {
                _pendingRequests.Remove(requestId);
                _chunkBuffers.Remove(requestId);
                tcs.TrySetCanceled();
            });

            EditorConnection.instance.Send(RuntimeMessageGuids.CommandRequest, bytes, playerId);

            return await tcs.Task;
        }

        public async Task<string> SendListAsync(int playerId, CancellationToken cancellationToken)
        {
            EnsureRegistered();

            var requestId = Guid.NewGuid().ToString();
            var request = new RuntimeListRequest
            {
                requestId = requestId
            };

            var json = JsonUtility.ToJson(request);
            var bytes = Encoding.UTF8.GetBytes(json);

            var tcs = new TaskCompletionSource<string>();
            _pendingRequests[requestId] = tcs;

            using var registration = cancellationToken.Register(() =>
            {
                _pendingRequests.Remove(requestId);
                _chunkBuffers.Remove(requestId);
                tcs.TrySetCanceled();
            });

            EditorConnection.instance.Send(RuntimeMessageGuids.ListRequest, bytes, playerId);

            return await tcs.Task;
        }

        private void OnChunkedResponse(MessageEventArgs args)
        {
            var json = Encoding.UTF8.GetString(args.data);
            var chunk = JsonUtility.FromJson<RuntimeChunkedMessage>(json);

            if (!_pendingRequests.ContainsKey(chunk.requestId))
                return;

            if (!_chunkBuffers.TryGetValue(chunk.requestId, out var accumulator))
            {
                accumulator = new ChunkAccumulator(chunk.totalChunks);
                _chunkBuffers[chunk.requestId] = accumulator;
            }

            accumulator.Add(chunk.chunkIndex, chunk.data);

            if (!accumulator.IsComplete)
                return;

            var reassembled = accumulator.Reassemble();
            _chunkBuffers.Remove(chunk.requestId);

            if (_pendingRequests.TryGetValue(chunk.requestId, out var tcs))
            {
                _pendingRequests.Remove(chunk.requestId);
                tcs.TrySetResult(reassembled);
            }
        }

        private sealed class ChunkAccumulator
        {
            private readonly string[] _chunks;
            private int _receivedCount;

            public bool IsComplete => _receivedCount >= _chunks.Length;

            public ChunkAccumulator(int totalChunks)
            {
                _chunks = new string[totalChunks];
            }

            public void Add(int index, string data)
            {
                if (index < 0 || index >= _chunks.Length)
                    return;

                if (_chunks[index] != null)
                    return;

                _chunks[index] = data;
                _receivedCount++;
            }

            public string Reassemble()
            {
                var sb = new StringBuilder();
                for (var i = 0; i < _chunks.Length; i++)
                    sb.Append(_chunks[i]);
                return sb.ToString();
            }
        }
    }
}
