using System;
using UnityEngine.Scripting;

namespace UniCli.Remote
{
    [Preserve]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DebugCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public DebugCommandAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }
    }
}
