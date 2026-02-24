using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UniCli.Server.Editor.Tests")]
[assembly: InternalsVisibleTo("UniCli.Server.Editor.NuGetForUnity")]
[assembly: InternalsVisibleTo("UniCli.Server.Editor.BuildMagic")]
[assembly: InternalsVisibleTo("UniCli.Server.Editor.Search")]

[assembly: GenerateCommands("UnityEditor.PlayerSettings", "PlayerSettings")]
[assembly: GenerateCommands("UnityEditor.EditorSettings", "EditorSettings")]
[assembly: GenerateCommands("UnityEditor.EditorUserBuildSettings", "EditorUserBuildSettings")]
[assembly: GenerateCommands("UnityEngine.Material", "Material", ResolveMode = InstanceResolveMode.Guid, Module = "Assets")]
