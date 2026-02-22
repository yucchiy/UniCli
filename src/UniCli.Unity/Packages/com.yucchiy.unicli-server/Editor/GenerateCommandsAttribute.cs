using System;

internal enum InstanceResolveMode
{
    Static = 0,
    Guid = 1,
    InstanceId = 2,
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class GenerateCommandsAttribute : Attribute
{
    public string TypeName { get; }
    public string CommandPrefix { get; }
    public InstanceResolveMode ResolveMode { get; set; } = InstanceResolveMode.Static;
    public string Module { get; set; } = "";

    public GenerateCommandsAttribute(string typeName, string commandPrefix)
    {
        TypeName = typeName;
        CommandPrefix = commandPrefix;
    }
}
