using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UniCli.Server.Editor.Tests")]
[assembly: InternalsVisibleTo("UniCli.Server.Editor.NuGetForUnity")]
[assembly: InternalsVisibleTo("UniCli.Server.Editor.BuildMagic")]

[assembly: GenerateCommands("UnityEditor.PlayerSettings", "PlayerSettings", Module = "Settings")]
[assembly: GenerateCommands("UnityEditor.EditorSettings", "EditorSettings", Module = "Settings")]
[assembly: GenerateCommands("UnityEditor.EditorUserBuildSettings", "EditorUserBuildSettings", Module = "Settings")]
[assembly: GenerateCommands("UnityEngine.Material", "Material", ResolveMode = InstanceResolveMode.Guid, Module = "Assets")]
