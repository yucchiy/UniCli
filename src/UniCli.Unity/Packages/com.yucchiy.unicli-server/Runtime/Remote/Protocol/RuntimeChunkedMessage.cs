using System;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    [Serializable]
    public class RuntimeChunkedMessage
    {
        public string requestId;
        public int chunkIndex;
        public int totalChunks;
        public string data;
    }
}
