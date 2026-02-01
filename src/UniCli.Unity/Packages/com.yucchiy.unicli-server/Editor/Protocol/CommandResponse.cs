using System;

namespace UniCli.Protocol
{
    [Serializable]
    public class CommandResponse
    {
        public bool success;
        public string message;
        public string data;  // Structured data (JSON string) or formatted text
        public string format; // "json" or "text"; empty/null treated as "json"
    }
}
