using System;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    [Serializable]
    public class RuntimeCommandResponse
    {
        public string requestId;
        public bool success;
        public string message;
        public string data;
    }
}
