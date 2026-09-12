using System;

namespace UniCli.Protocol
{
    [Serializable]
    public class CommandInfo
    {
        public string name;
        public string description;
        public bool builtIn;
        public string module;

        // Editor state this command requires, from CommandPreconditionAttribute; null when it
        // declares none, so existing listings and cached completions stay valid.
        public string requiresEditorState;
        public CommandFieldInfo[] requestFields;
        public CommandFieldInfo[] responseFields;
        public CommandTypeDetail[] requestTypeDetails;
        public CommandTypeDetail[] responseTypeDetails;
    }

    [Serializable]
    public class CommandFieldInfo
    {
        public string name;
        public string type;
        public string typeId;
        public string defaultValue;
    }

    [Serializable]
    public class CommandTypeDetail
    {
        public string typeName;
        public string typeId;
        public CommandFieldInfo[] fields;
    }

    [Serializable]
    public class CommandListResponse
    {
        public CommandInfo[] commands;
    }
}
