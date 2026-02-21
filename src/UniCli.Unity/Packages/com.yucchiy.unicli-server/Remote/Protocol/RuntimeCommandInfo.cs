using System;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    [Serializable]
    public class RuntimeCommandInfo
    {
        public string name;
        public string description;
    }

    [Preserve]
    [Serializable]
    public class RuntimeListRequest
    {
        public string requestId;
    }

    [Preserve]
    [Serializable]
    public class RuntimeListResponse
    {
        public string requestId;
        public RuntimeCommandInfo[] commands;
    }
}
