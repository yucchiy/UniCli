using System;

namespace UniCli.Protocol
{
    [Serializable]
    public class CommandRequest
    {
        public string command;
        public string data;
        public string format; // "json" or "text"; empty/null treated as "json"
        public string cwd; // client's working directory for resolving relative paths
    }
}
