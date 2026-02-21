using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    public sealed class RuntimeDebugReceiver : MonoBehaviour
    {
        private static RuntimeDebugReceiver _instance;

        private DebugCommandRegistry _registry;
        private LogCapture _logCapture;

        public static LogCapture LogCapture => _instance != null ? _instance._logCapture : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (_instance != null)
                return;

            var go = new GameObject("[UniCli.Remote]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<RuntimeDebugReceiver>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            _logCapture = new LogCapture();
            _logCapture.Start();

            _registry = new DebugCommandRegistry();
            _registry.DiscoverCommands();

            PlayerConnection.instance.Register(RuntimeMessageGuids.CommandRequest, OnCommandRequest);
            PlayerConnection.instance.Register(RuntimeMessageGuids.ListRequest, OnListRequest);

            UnityEngine.Debug.Log("[UniCli.Remote] RuntimeDebugReceiver initialized");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                PlayerConnection.instance.Unregister(RuntimeMessageGuids.CommandRequest, OnCommandRequest);
                PlayerConnection.instance.Unregister(RuntimeMessageGuids.ListRequest, OnListRequest);
                _logCapture?.Stop();
                _instance = null;
            }
        }

        private void OnCommandRequest(MessageEventArgs args)
        {
            var json = Encoding.UTF8.GetString(args.data);
            var request = JsonUtility.FromJson<RuntimeCommandRequest>(json);

            var response = new RuntimeCommandResponse
            {
                requestId = request.requestId
            };

            try
            {
                if (!_registry.TryGetCommand(request.command, out var command))
                {
                    response.success = false;
                    response.message = $"Unknown debug command: {request.command}";
                    response.data = "";
                }
                else
                {
                    var resultJson = command.Execute(request.data);
                    response.success = true;
                    response.message = $"Command '{request.command}' succeeded";
                    response.data = resultJson;
                }
            }
            catch (Exception ex)
            {
                response.success = false;
                response.message = $"Command '{request.command}' failed: {ex.Message}";
                response.data = "";
            }

            var responseJson = JsonUtility.ToJson(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            PlayerConnection.instance.Send(RuntimeMessageGuids.CommandResponse, responseBytes);
        }

        private void OnListRequest(MessageEventArgs args)
        {
            var json = Encoding.UTF8.GetString(args.data);
            var request = JsonUtility.FromJson<RuntimeListRequest>(json);

            var response = new RuntimeListResponse
            {
                requestId = request.requestId,
                commands = _registry.GetCommandInfos()
            };

            var responseJson = JsonUtility.ToJson(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            PlayerConnection.instance.Send(RuntimeMessageGuids.ListResponse, responseBytes);
        }
    }
}
