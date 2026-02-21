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

        private bool _registered;

        public void EnsureRegistered()
        {
            if (_registered)
                return;

            EditorConnection.instance.Register(RuntimeMessageGuids.CommandResponse, OnCommandResponse);
            EditorConnection.instance.Register(RuntimeMessageGuids.ListResponse, OnListResponse);
            _registered = true;
        }

        private void OnDestroy()
        {
            if (!_registered)
                return;

            EditorConnection.instance.Unregister(RuntimeMessageGuids.CommandResponse, OnCommandResponse);
            EditorConnection.instance.Unregister(RuntimeMessageGuids.ListResponse, OnListResponse);
            _registered = false;

            foreach (var kvp in _pendingRequests)
                kvp.Value.TrySetCanceled();
            _pendingRequests.Clear();
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
                tcs.TrySetCanceled();
            });

            EditorConnection.instance.Send(RuntimeMessageGuids.ListRequest, bytes, playerId);

            return await tcs.Task;
        }

        private void OnCommandResponse(MessageEventArgs args)
        {
            var json = Encoding.UTF8.GetString(args.data);
            var response = JsonUtility.FromJson<RuntimeCommandResponse>(json);

            if (_pendingRequests.TryGetValue(response.requestId, out var tcs))
            {
                _pendingRequests.Remove(response.requestId);
                tcs.TrySetResult(json);
            }
        }

        private void OnListResponse(MessageEventArgs args)
        {
            var json = Encoding.UTF8.GetString(args.data);
            var response = JsonUtility.FromJson<RuntimeListResponse>(json);

            if (_pendingRequests.TryGetValue(response.requestId, out var tcs))
            {
                _pendingRequests.Remove(response.requestId);
                tcs.TrySetResult(json);
            }
        }
    }
}
