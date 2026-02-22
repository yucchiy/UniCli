namespace UniCli.Server.Editor
{
    public sealed class ModuleDefinition
    {
        public string Name { get; }
        public string Description { get; }

        public ModuleDefinition(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
