using UnityEngine;

namespace UniCli.Server.Editor.Handlers.Remote
{
    public sealed class RemoteServiceInstaller : IServiceInstaller
    {
        public void Install(ServiceRegistry services)
        {
            var bridge = ScriptableObject.CreateInstance<RemoteBridge>();
            bridge.hideFlags = HideFlags.HideAndDontSave;
            services.AddSingleton(bridge);
        }
    }
}
