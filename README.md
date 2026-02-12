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

Download the latest binary from the [Releases](https://github.com/yucchiy/UniCli/releases) page and place it in your PATH.

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

### Common options

These options can be combined with any `exec` command:

| Option      | Description                          |
|-------------|--------------------------------------|
| `--json`    | Output in JSON format                |
| `--timeout` | Set command timeout in milliseconds  |
| `--no-focus`| Don't bring Unity Editor to front    |
| `--help`    | Show command parameters and usage    |

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

# Prefab operations
unicli exec Prefab.GetStatus --path "MyPrefabInstance"
unicli exec Prefab.Instantiate --assetPath "Assets/Prefabs/Enemy.prefab"
unicli exec Prefab.Save --path "Player" --assetPath "Assets/Prefabs/Player.prefab"
unicli exec Prefab.Apply --path "MyPrefabInstance"
unicli exec Prefab.Unpack --path "MyPrefabInstance" --completely

# Manage packages
unicli exec PackageManager.List
unicli exec PackageManager.Add --packageIdOrName com.unity.mathematics
unicli exec PackageManager.Remove --packageIdOrName com.unity.mathematics

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
| Core               | `Compile`                            | Compile scripts and return results |
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
| GameObject         | `GameObject.GetComponents`           | Get components                     |
| GameObject         | `GameObject.SetActive`               | Set active state                   |
| GameObject         | `GameObject.GetHierarchy`            | Get scene hierarchy                |
| GameObject         | `GameObject.AddComponent`            | Add a component                    |
| GameObject         | `GameObject.RemoveComponent`         | Remove a component                 |
| Prefab             | `Prefab.GetStatus`                   | Get prefab instance status         |
| Prefab             | `Prefab.Instantiate`                 | Instantiate a prefab into scene    |
| Prefab             | `Prefab.Save`                        | Save GameObject as prefab          |
| Prefab             | `Prefab.Apply`                       | Apply prefab overrides             |
| Prefab             | `Prefab.Unpack`                      | Unpack a prefab instance           |
| AssetDatabase      | `AssetDatabase.Find`                 | Search assets                      |
| AssetDatabase      | `AssetDatabase.Import`               | Import an asset                    |
| AssetDatabase      | `AssetDatabase.GetPath`              | Get asset path by GUID             |
| Project            | `Project.Inspect`                    | Get project info                   |
| PackageManager     | `PackageManager.List`                | List packages                      |
| PackageManager     | `PackageManager.Add`                 | Add a package                      |
| PackageManager     | `PackageManager.Remove`              | Remove a package                   |
| PackageManager     | `PackageManager.Search`              | Search registry                    |
| AssemblyDefinition | `AssemblyDefinition.List`            | List assembly definitions          |
| AssemblyDefinition | `AssemblyDefinition.Get`             | Get assembly definition            |
| AssemblyDefinition | `AssemblyDefinition.Create`          | Create assembly definition         |
| AssemblyDefinition | `AssemblyDefinition.AddReference`    | Add asmdef reference               |
| AssemblyDefinition | `AssemblyDefinition.RemoveReference` | Remove asmdef reference            |

Use `unicli exec <command> --help` to see parameters for any command.


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
