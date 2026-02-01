#nullable enable
using System;
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

        static UniCliServerBootstrap()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            RunServiceInstallers(Services);

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.update += OnEditorUpdate;

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
                logger: Debug.Log,
                errorLogger: Debug.LogError
            );
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
                    Debug.LogError($"[UniCli] Failed to run service installer {type.FullName}: {ex.Message}");
                }
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            StopServer();
        }

        private static void OnEditorUpdate()
        {
            _server?.ProcessCommands();
        }
    }
}
