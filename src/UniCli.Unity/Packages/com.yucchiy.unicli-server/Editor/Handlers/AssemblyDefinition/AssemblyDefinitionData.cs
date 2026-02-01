using System;

namespace UniCli.Server.Editor.Handlers
{
    [Serializable]
    public class AssemblyDefinitionData
    {
        public string name = "";
        public string rootNamespace = "";
        public string[] references = Array.Empty<string>();
        public string[] includePlatforms = Array.Empty<string>();
        public string[] excludePlatforms = Array.Empty<string>();
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences = Array.Empty<string>();
        public bool autoReferenced = true;
        public string[] defineConstraints = Array.Empty<string>();
        public VersionDefineEntry[] versionDefines = Array.Empty<VersionDefineEntry>();
        public bool noEngineReferences;
    }

    [Serializable]
    public class VersionDefineEntry
    {
        public string name;
        public string expression;
        public string define;
    }
}
