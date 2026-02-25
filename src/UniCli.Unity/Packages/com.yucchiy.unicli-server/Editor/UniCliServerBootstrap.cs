#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor
{
    [InitializeOnLoad]
    public static class UniCliServerBootstrap
    {
        private static UniCliServer? _server;
        private static CommandDispatcher? _dispatcher;
        private static bool _originalRunInBackground;

        public static ServiceRegistry Services { get; } = new();
        public static CommandDispatcher? Dispatcher => _dispatcher;
        public static bool IsRunning => _server != null;
        public static string? CurrentCommandName => _server?.CurrentCommandName;
        public static DateTime? CurrentCommandStartTime => _server?.CurrentCommandStartTime;
        public static string[] QueuedCommandNames => _server?.QueuedCommandNames ?? Array.Empty<string>();

        static UniCliServerBootstrap()
        {
            EnsurePidFile();
            EditorApplication.update += InitializeOnce;
        }

        private static void InitializeOnce()
        {
            EditorApplication.update -= InitializeOnce;
            Initialize();
        }

        private static void Initialize()
        {
            RunServiceInstallers(Services);

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.quitting += OnEditorQuitting;

            StartServer();
        }

        public static void StartServer()
        {
            if (_server != null)
                return;

            _originalRunInBackground = Application.runInBackground;
            Application.runInBackground = true;

            var pipeName = ProjectIdentifier.GetPipeName();
            _dispatcher = new CommandDispatcher(Services);
            _server = new UniCliServer(
                pipeName,
                _dispatcher,
                logger: UnityEngine.Debug.Log,
                errorLogger: UnityEngine.Debug.LogError
            );
        }

        public static void ReloadDispatcher()
        {
            if (_server == null)
                return;

            _dispatcher = new CommandDispatcher(Services);
            _server.ReplaceDispatcher(_dispatcher);
            UnityEngine.Debug.Log("[UniCli] Dispatcher reloaded");
        }

        public static void StopServer()
        {
            if (_server == null)
                return;

            Application.runInBackground = _originalRunInBackground;
            _server.Dispose();
            _server = null;
            _dispatcher = null;
        }

        private static string GetPidFilePath()
        {
            return Path.Combine(Application.dataPath, "..", "Library", "UniCli", "server.pid");
        }

        private static void EnsurePidFile()
        {
            try
            {
                var path = GetPidFilePath();
                var pid = Process.GetCurrentProcess().Id.ToString();

                if (File.Exists(path) && File.ReadAllText(path).Trim() == pid)
                    return;

                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, pid);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[UniCli] Failed to write PID file: {ex.Message}");
            }
        }

        private static void DeletePidFile()
        {
            try
            {
                var path = GetPidFilePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort deletion
            }
        }

        private static void RunServiceInstallers(ServiceRegistry services)
        {
            var installerTypes = TypeCache.GetTypesDerivedFrom<IServiceInstaller>();

            foreach (var type in installerTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                try
                {
                    var installer = (IServiceInstaller)Activator.CreateInstance(type);
                    installer.Install(services);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[UniCli] Failed to run service installer {type.FullName}: {ex.Message}");
                }
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            StopServer();
        }

        private static void OnEditorQuitting()
        {
            DeletePidFile();
        }

        private static void OnEditorUpdate()
        {
            _server?.ProcessCommands();
        }
    }
}
