using System;

namespace UniCli.Protocol
{
    [Serializable]
    public class CommandInfo
    {
        public string name;
        public string description;
        public CommandFieldInfo[] requestFields;
        public CommandFieldInfo[] responseFields;
    }

    [Serializable]
    public class CommandFieldInfo
    {
        public string name;
        public string type;
        public string defaultValue;
    }

    [Serializable]
    public class CommandListResponse
    {
        public CommandInfo[] commands;
    }
}
