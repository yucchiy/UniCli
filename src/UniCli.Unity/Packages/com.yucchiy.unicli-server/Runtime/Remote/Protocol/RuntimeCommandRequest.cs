using System;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    [Serializable]
    public class RuntimeCommandRequest
    {
        public string requestId;
        public string command;
        public string data;
    }
}
