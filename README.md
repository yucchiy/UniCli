# UniCli

A command-line interface for controlling Unity Editor from the terminal.
UniCli lets you compile scripts, run tests, manage packages, inspect GameObjects, and more — all without leaving your terminal.

Designed to work with AI coding agents such as [Claude Code](https://docs.anthropic.com/en/docs/claude-code), UniCli gives AI the ability to interact with Unity Editor directly through structured CLI commands with JSON output.

## How It Works

UniCli consists of two components:

- **CLI** (`unicli`) — A NativeAOT-compiled binary that you run from the terminal
- **Unity Package** (`com.yucchiy.unicli-server`) — An Editor plugin that receives and executes commands inside Unity

The CLI communicates with the Unity Editor over named pipes using length-prefixed JSON messages, providing fast local IPC without network overhead.

## Requirements

- Unity 2022.3 or later
- macOS (arm64 / x64) or Windows (x64)

## Installation

### CLI

**Homebrew (macOS):**

```bash
brew tap yucchiy/tap
brew install unicli
```

**Manual:** Download the latest binary from the [Releases](https://github.com/yucchiy/UniCli/releases) page and place it in your PATH.

### Unity Package

The UniCli package must be installed in your Unity project. You can install it using the CLI:

```bash
unicli install
```

Or add it manually via Unity Package Manager using the git URL:

```
https://github.com/yucchiy/UniCli.git?path=src/UniCli.Unity/Packages/com.yucchiy.unicli-server
```

## CLI Subcommands

The `unicli` binary provides the following subcommands:

| Subcommand    | Description                                                 |
|---------------|-------------------------------------------------------------|
| `check`       | Check package installation and Unity Editor connection      |
| `install`     | Install the UniCli package into a Unity project             |
| `exec`        | Execute a command on the Unity Editor                       |
| `commands`    | List all available commands                                 |
| `status`      | Show connection status and project info                     |
| `completions` | Generate shell completion scripts (bash / zsh / fish)       |

```
unicli check             # verify installation and editor connection
unicli install           # install the Unity package
unicli commands          # list all available commands
unicli status            # show connection details
unicli completions bash  # generate shell completions
```

Add `--json` to `check`, `commands`, or `status` for machine-readable JSON output.


## Executing Commands

Use `unicli exec <command>` to run commands on the Unity Editor.

### Parameter syntax

Parameters can be passed as `--key value` flags (recommended) or as a raw JSON string:

```bash
# --key value syntax (recommended)
unicli exec GameObject.Find --name "Main Camera"
unicli exec TestRunner.RunEditMode --testNameFilter MyTest

# Raw JSON syntax
unicli exec GameObject.Find '{"name":"Main Camera"}'
```

Boolean flags can be passed without a value:

```bash
unicli exec GameObject.Find --includeInactive
```

Array parameters can be passed by repeating the same flag:

```bash
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --options ConnectWithProfiler
unicli exec BuildPlayer.Compile --target iOS --extraScriptingDefines MY_DEFINE --extraScriptingDefines ANOTHER_DEFINE
```

### Common options

These options can be combined with any `exec` command:

| Option      | Description                          |
|-------------|--------------------------------------|
| `--json`    | Output in JSON format                |
| `--timeout` | Set command timeout in milliseconds  |
| `--no-focus`| Don't bring Unity Editor to front    |
| `--help`    | Show command parameters and usage    |

By default, when the server is not responding (e.g., after an assembly reload), the CLI automatically brings Unity Editor to the foreground using a PID file (`Library/UniCli/server.pid`) and restores focus to the original application once the command completes. Use `--no-focus` to disable this behavior, or set the `UNICLI_FOCUS` environment variable to `0` or `false` to disable it globally.

For example:

```bash
unicli exec Compile --json
unicli exec Compile --timeout 30000
unicli exec GameObject.Find --help
```

### Examples

```bash
# Compile scripts
unicli exec Compile

# Build the player
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app"
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --options ConnectWithProfiler
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --target Android --scenes "Assets/Scenes/Main.unity"

# Compile player scripts for a specific build target
unicli exec BuildPlayer.Compile
unicli exec BuildPlayer.Compile --target Android
unicli exec BuildPlayer.Compile --target iOS --extraScriptingDefines MY_DEFINE --extraScriptingDefines ANOTHER_DEFINE

# List available connection targets (players/devices)
unicli exec Connection.List

# Get current connection status
unicli exec Connection.Status

# Connect to a target by ID, IP, or device ID
unicli exec Connection.Connect '{"id":-1}'
unicli exec Connection.Connect '{"ip":"192.168.1.100"}'
unicli exec Connection.Connect '{"deviceId":"DEVICE_SERIAL"}'

# Run tests
unicli exec TestRunner.RunEditMode
unicli exec TestRunner.RunPlayMode
unicli exec TestRunner.RunEditMode --testNameFilter MyTest

# Find GameObjects
unicli exec GameObject.Find --name "Main Camera"
unicli exec GameObject.Find --tag Player --includeInactive
unicli exec GameObject.GetHierarchy
unicli exec GameObject.GetComponents --instanceId 1234
unicli exec GameObject.AddComponent --path "Player" --typeName BoxCollider
unicli exec GameObject.RemoveComponent --componentInstanceId 1234

# Create GameObjects
unicli exec GameObject.Create --name "Enemy"
unicli exec GameObject.Create --name "Child" --parent "Enemy"
unicli exec GameObject.Create --name "WithCollider" --components BoxCollider
unicli exec GameObject.CreatePrimitive --primitiveType Cube
unicli exec GameObject.CreatePrimitive --primitiveType Sphere --name "Ball" --parent "Enemy"

# Modify GameObjects
unicli exec GameObject.Rename --path "Enemy" --name "Boss"
unicli exec GameObject.SetTransform --path "Boss" --position 1,2,3 --rotation 0,90,0
unicli exec GameObject.Duplicate --path "Boss"
unicli exec GameObject.SetParent --path "Boss(Clone)" --parentPath "Boss"
unicli exec GameObject.Destroy --path "Boss(Clone)"

# Set component properties
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_IsKinematic" --value "true"

# Set ObjectReference properties (e.g. assign a material to a renderer)
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_Materials.Array.data[0]" --value "guid:abc123def456"
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_Mesh" --value "Assets/Meshes/Custom.mesh"
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_Material" --value "null"

# Material operations (requires asset GUID)
unicli exec Material.Inspect --guid "abc123def456"
unicli exec Material.SetColor --guid "abc123def456" --name "_Color" --value '{"r":1,"g":0,"b":0,"a":1}'
unicli exec Material.SetFloat --guid "abc123def456" --name "_Metallic" --value 0.8

# Prefab operations
unicli exec Prefab.GetStatus --path "MyPrefabInstance"
unicli exec Prefab.Instantiate --assetPath "Assets/Prefabs/Enemy.prefab"
unicli exec Prefab.Save --path "Player" --assetPath "Assets/Prefabs/Player.prefab"
unicli exec Prefab.Apply --path "MyPrefabInstance"
unicli exec Prefab.Unpack --path "MyPrefabInstance" --completely

# Delete an asset
unicli exec AssetDatabase.Delete --path "Assets/Prefabs/Old.prefab"

# Manage packages
unicli exec PackageManager.List
unicli exec PackageManager.Add --packageIdOrName com.unity.mathematics
unicli exec PackageManager.Remove --packageIdOrName com.unity.mathematics
unicli exec PackageManager.GetInfo --name com.unity.test-framework
unicli exec PackageManager.Update --name com.unity.test-framework
unicli exec PackageManager.Update --name com.unity.test-framework --version 1.4.5

# Scene operations
unicli exec Scene.List
unicli exec Scene.GetActive
unicli exec Scene.Open --path "Assets/Scenes/Level1.unity"
unicli exec Scene.Open --path "Assets/Scenes/Additive.unity" --additive
unicli exec Scene.SetActive --name "Level1"
unicli exec Scene.Save --all
unicli exec Scene.Save --name "Level1" --saveAsPath "Assets/Scenes/Level1_backup.unity"
unicli exec Scene.Close --name "Additive"
unicli exec Scene.New --empty --additive

# Settings — inspect all values
unicli exec PlayerSettings.Inspect
unicli exec EditorSettings.Inspect

# Settings — set a property
unicli exec PlayerSettings.SetCompanyName --value "MyCompany"
unicli exec PlayerSettings.Android.SetMinSdkVersion --value AndroidApiLevel28

# Settings — call Set/Get methods (with platform target)
unicli exec PlayerSettings.SetScriptingBackend --buildTarget Android --value IL2CPP
unicli exec PlayerSettings.GetScriptingBackend --buildTarget Android

# Execute menu items
unicli exec Menu.Execute --menuPath "Window/General/Console"

# Console logs
unicli exec Console.GetLog
unicli exec Console.Clear
```


## Available Commands

The following commands are built in. You can also run `unicli commands` to see this list from the terminal.

| Category           | Command                              | Description                        |
|--------------------|--------------------------------------|------------------------------------|
| BuildPlayer        | `BuildPlayer.Build`                  | Build the player                   |
| BuildPlayer        | `BuildPlayer.Compile`                | Compile player scripts for a build target |
| Core               | `Compile`                            | Compile scripts and return results |
| Connection         | `Connection.List`                    | List available connection targets  |
| Connection         | `Connection.Connect`                 | Connect to a target by ID, IP, or device ID |
| Connection         | `Connection.Status`                  | Get current connection status      |
| Console            | `Console.GetLog`                     | Get console log entries            |
| Console            | `Console.Clear`                      | Clear console                      |
| PlayMode           | `PlayMode.Enter`                     | Enter play mode                    |
| PlayMode           | `PlayMode.Exit`                      | Exit play mode                     |
| PlayMode           | `PlayMode.Pause`                     | Toggle pause                       |
| Menu               | `Menu.List`                          | List menu items                    |
| Menu               | `Menu.Execute`                       | Execute a menu item                |
| TestRunner         | `TestRunner.RunEditMode`             | Run EditMode tests                 |
| TestRunner         | `TestRunner.RunPlayMode`             | Run PlayMode tests                 |
| GameObject         | `GameObject.Find`                    | Find GameObjects                   |
| GameObject         | `GameObject.Create`                  | Create a new GameObject            |
| GameObject         | `GameObject.CreatePrimitive`         | Create a primitive GameObject      |
| GameObject         | `GameObject.GetComponents`           | Get components                     |
| GameObject         | `GameObject.SetActive`               | Set active state                   |
| GameObject         | `GameObject.GetHierarchy`            | Get scene hierarchy                |
| GameObject         | `GameObject.AddComponent`            | Add a component                    |
| GameObject         | `GameObject.RemoveComponent`         | Remove a component                 |
| GameObject         | `GameObject.Destroy`                 | Destroy a GameObject               |
| GameObject         | `GameObject.SetTransform`            | Set local transform                |
| GameObject         | `GameObject.Duplicate`               | Duplicate a GameObject             |
| GameObject         | `GameObject.Rename`                  | Rename a GameObject                |
| GameObject         | `GameObject.SetParent`               | Change parent or move to root      |
| Component          | `Component.SetProperty`              | Set a component property (supports ObjectReference via `guid:`, `instanceId:`, asset path) |
| Prefab             | `Prefab.GetStatus`                   | Get prefab instance status         |
| Prefab             | `Prefab.Instantiate`                 | Instantiate a prefab into scene    |
| Prefab             | `Prefab.Save`                        | Save GameObject as prefab          |
| Prefab             | `Prefab.Apply`                       | Apply prefab overrides             |
| Prefab             | `Prefab.Unpack`                      | Unpack a prefab instance           |
| AssetDatabase      | `AssetDatabase.Find`                 | Search assets                      |
| AssetDatabase      | `AssetDatabase.Import`               | Import an asset                    |
| AssetDatabase      | `AssetDatabase.GetPath`              | Get asset path by GUID             |
| AssetDatabase      | `AssetDatabase.Delete`               | Delete an asset                    |
| Project            | `Project.Inspect`                    | Get project info                   |
| PackageManager     | `PackageManager.List`                | List packages                      |
| PackageManager     | `PackageManager.Add`                 | Add a package                      |
| PackageManager     | `PackageManager.Remove`              | Remove a package                   |
| PackageManager     | `PackageManager.Search`              | Search registry                    |
| PackageManager     | `PackageManager.GetInfo`             | Get package details                |
| PackageManager     | `PackageManager.Update`              | Update a package                   |
| AssemblyDefinition | `AssemblyDefinition.List`            | List assembly definitions          |
| AssemblyDefinition | `AssemblyDefinition.Get`             | Get assembly definition            |
| AssemblyDefinition | `AssemblyDefinition.Create`          | Create assembly definition         |
| AssemblyDefinition | `AssemblyDefinition.AddReference`    | Add asmdef reference               |
| AssemblyDefinition | `AssemblyDefinition.RemoveReference` | Remove asmdef reference            |
| Scene              | `Scene.List`                         | List all loaded scenes             |
| Scene              | `Scene.GetActive`                    | Get the active scene               |
| Scene              | `Scene.SetActive`                    | Set the active scene               |
| Scene              | `Scene.Open`                         | Open a scene by asset path         |
| Scene              | `Scene.Close`                        | Close a loaded scene               |
| Scene              | `Scene.Save`                         | Save a scene or all open scenes    |
| Scene              | `Scene.New`                          | Create a new scene                 |
| Utility            | `TypeCache.List`                     | List types derived from a base type |
| Utility            | `TypeInspect`                        | Inspect nested types of a given type |

Use `unicli exec <command> --help` to see parameters for any command.

### Settings Commands (auto-generated)

UniCli auto-generates commands via a Roslyn Source Generator at Unity compile time. Target types are declared with the `[GenerateCommands]` assembly attribute, so the available commands always match your exact Unity version.

#### Static types (Settings)

Commands for `PlayerSettings`, `EditorSettings`, and `EditorUserBuildSettings`:

| Pattern | Example | Description |
|---|---|---|
| `<Settings>.Inspect` | `PlayerSettings.Inspect` | Get all property values at once |
| `<Settings>.Set<Property>` | `PlayerSettings.SetCompanyName` | Set a single property |
| `<Settings>.<Nested>.Set<Property>` | `PlayerSettings.Android.SetMinSdkVersion` | Set a nested type property |
| `<Settings>.<Method>` | `PlayerSettings.SetScriptingBackend` | Call a Set/Get method |

Enum values are passed as strings (e.g., `"IL2CPP"`, `"AndroidApiLevel28"`). Invalid values return an error with the list of valid options.

#### Instance types (asset-based)

Commands for instance types like `Material` require a `guid` parameter to identify the target asset:

| Pattern | Example | Description |
|---|---|---|
| `<Type>.Inspect` | `Material.Inspect` | Read all properties of an instance |
| `<Type>.Set<Property>` | `Material.SetRenderQueue` | Set a property on the instance |
| `<Type>.<Method>` | `Material.SetColor`, `Material.GetFloat` | Call a Set/Get method on the instance |

Instance type commands automatically call `EditorUtility.SetDirty()` after mutations to ensure changes are saved.

Run `unicli commands` to see the full list of available commands, including all generated commands.


## Custom Commands

You can extend UniCli by adding custom commands in your Unity project. Commands are auto-discovered — no manual registration required.

### Class-based commands

Inherit from `CommandHandler<TRequest, TResponse>` and define `[Serializable]` request/response types:

```csharp
using System;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;

public sealed class GreetHandler : CommandHandler<GreetRequest, GreetResponse>
{
    public override string CommandName => "MyApp.Greet";
    public override string Description => "Returns a greeting message";

    protected override ValueTask<GreetResponse> ExecuteAsync(GreetRequest request)
    {
        return new ValueTask<GreetResponse>(new GreetResponse
        {
            message = $"Hello, {request.name}!"
        });
    }
}

[Serializable]
public class GreetRequest
{
    public string name;
}

[Serializable]
public class GreetResponse
{
    public string message;
}
```

Once the handler is placed anywhere in your Unity project, it becomes immediately available:

```bash
unicli exec MyApp.Greet --name "World"
```

For commands that require no input or produce no output, use `Unit` as the type parameter:

```csharp
public sealed class PingHandler : CommandHandler<Unit, PingResponse>
{
    public override string CommandName => "MyApp.Ping";
    public override string Description => "Health check";

    protected override ValueTask<PingResponse> ExecuteAsync(Unit request)
    {
        return new ValueTask<PingResponse>(new PingResponse { ok = true });
    }
}
```

### Text formatting

Override `TryFormat` to provide human-readable output (used when `--json` is not specified):

```csharp
protected override bool TryFormat(GreetResponse response, bool success, out string formatted)
{
    formatted = response.message;
    return true;
}
```

### Error handling

Throw `CommandFailedException` to report failures while still returning structured data:

```csharp
if (hasErrors)
    throw new CommandFailedException("Validation failed", response);
```

## Claude Code Integration

UniCli provides a [Claude Code](https://docs.anthropic.com/en/docs/claude-code) plugin via the marketplace. This plugin gives Claude Code the ability to interact with Unity Editor — compiling scripts, running tests, inspecting GameObjects, managing packages, and more — as part of its coding workflow.

With the plugin installed, Claude Code can:

- **Compile & verify** — catch compilation errors immediately after code changes
- **Run tests** — execute EditMode / PlayMode tests and read results
- **Inspect the scene** — find GameObjects, check components, and navigate the hierarchy
- **Manage packages** — add, remove, and search Unity packages
- **Discover commands** — automatically find all available commands, including project-specific custom commands

The plugin also handles server package setup: if the `com.yucchiy.unicli-server` package is not yet installed in the Unity project, Claude Code will run `unicli install` to set it up automatically.

### Install the plugin

The UniCli CLI must be installed beforehand. See [Installation — CLI](#cli) above.

```bash
# 1. Add the UniCli marketplace
/plugin marketplace add yucchiy/UniCli

# 2. Install the plugin
/plugin install unicli@unicli
```

## License

[MIT](./LICENSE)
