# UniCli

[![GitHub Release](https://img.shields.io/github/v/release/yucchiy/UniCli)](https://github.com/yucchiy/UniCli/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A command-line interface for controlling Unity Editor from the terminal.
UniCli lets you compile scripts, run tests, manage packages, inspect GameObjects, and more — all without leaving your terminal.

Designed to work with AI coding agents such as [Claude Code](https://docs.anthropic.com/en/docs/claude-code), UniCli gives AI the ability to interact with Unity Editor directly through structured CLI commands with JSON output.

- **80+ built-in commands**: compile, test, build, inspect GameObjects, manage scenes/prefabs/packages, and more
- **Dynamic C# execution**: run arbitrary C# code in Unity via `unicli eval`
- **Extensible**: add custom commands with a single C# class
- **AI-agent ready**: structured JSON output, Claude Code plugin, and Agent Skills support
- **Cross-platform**: NativeAOT binaries for macOS (arm64/x64), Linux (x64), and Windows (x64)

## Table of Contents

- [Getting Started](#getting-started)
  - [Requirements](#requirements)
  - [CLI](#cli)
  - [Unity Package](#unity-package)
  - [Quick Usage](#quick-usage)
- [CLI Usage](#cli-usage)
  - [Project Discovery](#project-discovery)
- [Executing Commands](#executing-commands)
  - [Parameter syntax](#parameter-syntax)
  - [Common options](#common-options)
  - [Examples](#examples)
- [Dynamic Code Execution (Eval)](#dynamic-code-execution-eval)
- [Custom Commands](#custom-commands)
  - [Text formatting](#text-formatting)
  - [Async handlers and cancellation](#async-handlers-and-cancellation)
  - [Error handling](#error-handling)
- [Built-in Commands](#built-in-commands)
- [Module Management](#module-management)
- [Architecture](#architecture)
- [Remote Commands](#remote-commands)
  - [Prerequisites](#prerequisites)
  - [Editor-side commands](#editor-side-commands)
  - [Built-in debug commands](#built-in-debug-commands)
  - [Creating custom debug commands](#creating-custom-debug-commands)
- [AI Agent Integration](#ai-agent-integration)
  - [Claude Code Plugin](#claude-code-plugin)
  - [Agent Skills / Codex](#agent-skills--codex)
- [License](#license)

## Getting Started

UniCli requires two components: the **CLI** (`unicli`) installed on your machine, and the **Unity Package** (`com.yucchiy.unicli-server`) installed in your Unity project. Both must be set up for UniCli to work.

### Requirements

- Unity 2022.3 or later
- macOS (arm64 / x64), Linux (x64), or Windows (x64)

### CLI

**Homebrew (macOS):**

```bash
brew tap yucchiy/tap
brew install unicli
```

**Scoop (Windows):**

```powershell
scoop bucket add unicli https://github.com/yucchiy/scoop-bucket
scoop install unicli
```

**Linux / Manual:** Download the latest binary from the [Releases](https://github.com/yucchiy/UniCli/releases) page and place it in your PATH.

### Unity Package

The UniCli package must be installed in your Unity project. You can install it using the CLI:

```bash
unicli install
```

Or add it manually via Unity Package Manager using the git URL:

```
https://github.com/yucchiy/UniCli.git?path=src/UniCli.Unity/Packages/com.yucchiy.unicli-server
```

### Quick Usage

Open your Unity project in the Editor, then run `unicli` from the Unity project directory (or any subdirectory). UniCli automatically detects the project by looking for an `Assets` folder in the current directory and its ancestors.

```bash
# Verify connection
unicli check

# List all available commands
unicli commands

# Compile scripts
unicli exec Compile

# Run EditMode tests
unicli exec TestRunner.RunEditMode

# Execute arbitrary C# code (--json for JSON output)
unicli eval 'return Application.unityVersion;' --json
```

## CLI Usage

The `unicli` binary provides the following subcommands:

| Subcommand    | Description                                                 |
|---------------|-------------------------------------------------------------|
| `check`       | Check package installation and Unity Editor connection      |
| `install`     | Install the UniCli package into a Unity project             |
| `exec`        | Execute a command on the Unity Editor                       |
| `eval`        | Compile and execute C# code dynamically in the Unity Editor |
| `commands`    | List all available commands                                 |
| `status`      | Show connection status and project info                     |
| `completions` | Generate shell completion scripts (bash / zsh / fish)       |

```
unicli check             # verify installation and editor connection
unicli install           # install the Unity package
unicli commands          # list all available commands
unicli eval '<code>'     # compile and execute C# code dynamically
unicli status            # show connection details
unicli completions bash  # generate shell completions
```

Add `--json` to `check`, `commands`, or `status` for machine-readable JSON output.

### Project Discovery

By default, `unicli` searches the current directory and its ancestors for a Unity project (a directory containing an `Assets` folder). If you run `unicli` from outside a Unity project, or want to target a specific project, set the `UNICLI_PROJECT` environment variable:

```bash
# Run from anywhere by specifying the project path
UNICLI_PROJECT=/path/to/my/unity-project unicli exec Compile --json

# Useful when the current directory is not inside the Unity project
UNICLI_PROJECT=src/UniCli.Unity unicli commands --json
```


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
| `--help`    | Show command parameters and nested type details |

By default, when the server is not responding (e.g., after an assembly reload), the CLI automatically brings Unity Editor to the foreground using a PID file (`Library/UniCli/server.pid`) and restores focus to the original application once the command completes on macOS and Windows. Linux currently skips foreground activation. Use `--no-focus` to disable this behavior, or set the `UNICLI_FOCUS` environment variable to `0` or `false` to disable it globally.

### Examples

```bash
# Compile scripts (--json for JSON output, --timeout to set deadline)
unicli exec Compile --json
unicli exec Compile --timeout 30000

# Build the player
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app"
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --target Android

# Run tests
unicli exec TestRunner.RunEditMode
unicli exec TestRunner.RunEditMode --testNameFilter MyTest --stackTraceLines 3
unicli exec TestRunner.RunEditMode --resultFilter all

# Find and inspect GameObjects
unicli exec GameObject.Find --name "Main Camera"
unicli exec GameObject.GetHierarchy
unicli exec GameObject.GetComponents --instanceId 1234

# Create and modify GameObjects
unicli exec GameObject.Create --name "Enemy"
unicli exec GameObject.SetTransform --path "Enemy" --position 1,2,3 --rotation 0,90,0
unicli exec GameObject.AddComponent --path "Enemy" --typeName BoxCollider

# Scene operations
unicli exec Scene.Open --path "Assets/Scenes/Level1.unity"
unicli exec Scene.Save --all

# Set component properties
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_IsKinematic" --value "true"

# Console logs
unicli exec Console.GetLog --logType "Warning,Error"

# Show command parameters and usage
unicli exec GameObject.Find --help
```

### Inspect nested request/response types

`unicli exec <command> --help` shows top-level fields and, when available, nested type details.
For machine-readable schemas, use `unicli commands --json`: nested types are exposed via each command's `requestTypeDetails` and `responseTypeDetails` arrays.
Match nested type details by `typeId`; `type` and `typeName` stay human-friendly and may not be unique.

```bash
# Human-friendly schema (includes nested type details)
unicli exec AssetDatabase.Find --help

# Machine-readable schema (commands include "requestTypeDetails" / "responseTypeDetails")
unicli commands --json
```

See [Built-in Commands](#built-in-commands) for the full list of available commands.


## Dynamic Code Execution (Eval)

`unicli eval` compiles and executes arbitrary C# code in the Unity Editor context using `AssemblyBuilder`.
Code has full access to Unity APIs (`UnityEngine`, `UnityEditor`) as well as any packages and libraries referenced by the project.

```bash
unicli eval '<code>' [--json] [--declarations '<decl>'] [--timeout <ms>]
```

| Option | Description |
|---|---|
| `--json` | Output in JSON format |
| `--declarations` | Additional type declarations (classes, structs, enums) |
| `--timeout` | Timeout in milliseconds |

For multi-line code, use shell heredocs:

```bash
unicli eval "$(cat <<'EOF'
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var objects = GameObject.FindObjectsOfType<GameObject>(true);
return $"{scene.name}: {objects.Length} objects";
EOF
)" --json
```

The result is returned as raw JSON. If the return type is `[Serializable]`, it is serialized with `JsonUtility`.
`UnityEngine.Object` types use `EditorJsonUtility`. Primitives and strings are returned directly. Code that doesn't return a value (`void` operations) returns `null`.

Use `--declarations` to define custom types for structured return values:

```bash
unicli eval "$(cat <<'EOF'
var stats = new SceneStats();
stats.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
stats.objectCount = GameObject.FindObjectsOfType<GameObject>().Length;
return stats;
EOF
)" --declarations "$(cat <<'EOF'
[System.Serializable]
public class SceneStats
{
    public string sceneName;
    public int objectCount;
}
EOF
)" --json
```

Eval code supports `async`/`await` and receives a `cancellationToken` variable (`System.Threading.CancellationToken`) that is cancelled when the client disconnects.

Use it for cooperative cancellation of long-running operations:

```bash
# Wait asynchronously with cancellation support
unicli eval 'await Task.Delay(5000, cancellationToken); return "done";' --json
```


## Custom Commands

You can extend UniCli by adding custom commands in your Unity project. Commands are auto-discovered — no manual registration required.

Inherit from `CommandHandler<TRequest, TResponse>` and define `[Serializable]` request/response types:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;

public sealed class GreetHandler : CommandHandler<GreetRequest, GreetResponse>
{
    public override string CommandName => "MyApp.Greet";
    public override string Description => "Returns a greeting message";

    protected override ValueTask<GreetResponse> ExecuteAsync(GreetRequest request, CancellationToken cancellationToken)
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

For naming conventions (when to use concept-based names like `Scene.*` vs API-direct names like `AssetDatabase.*`), see [`doc/command-naming-guidelines.md`](doc/command-naming-guidelines.md).

For commands that require no input or produce no output, use `Unit` as the type parameter:

```csharp
public sealed class PingHandler : CommandHandler<Unit, PingResponse>
{
    public override string CommandName => "MyApp.Ping";
    public override string Description => "Health check";

    protected override ValueTask<PingResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
    {
        return new ValueTask<PingResponse>(new PingResponse { ok = true });
    }
}
```

### Text formatting

Override `TryWriteFormatted` to provide human-readable output (used when `--json` is not specified):

```csharp
protected override bool TryWriteFormatted(GreetResponse response, bool success, IFormatWriter writer)
{
    writer.WriteLine(response.message);
    return true;
}
```

With this override, the output changes depending on whether `--json` is used:

```bash
$ unicli exec MyApp.Greet --name "World"
Hello, World!

$ unicli exec MyApp.Greet --name "World" --json
{"message":"Hello, World!"}
```

### Async handlers and cancellation

All command handlers receive a `CancellationToken` that is cancelled when the client disconnects (e.g., Ctrl+C). For long-running async operations, pass the token through to ensure prompt cancellation:

```csharp
using System.Threading;
using System.Threading.Tasks;
using UniCli.Server.Editor;
using UniCli.Server.Editor.Handlers;

public sealed class LongRunningHandler : CommandHandler<MyRequest, MyResponse>
{
    public override string CommandName => "MyApp.LongTask";
    public override string Description => "A long-running async operation";

    protected override async ValueTask<MyResponse> ExecuteAsync(MyRequest request, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<string>();

        // Start an async Unity operation
        SomeAsyncUnityApi.Start(result => tcs.SetResult(result));

        // Use WithCancellation to abort the wait if the client disconnects
        var result = await tcs.Task.WithCancellation(cancellationToken);

        return new MyResponse { value = result };
    }
}
```

The `WithCancellation` extension method (on `Task` / `Task<T>`) races the task against the cancellation token. If the client disconnects, the await throws `OperationCanceledException` and the server immediately becomes available for the next command.

For synchronous handlers that complete instantly, the `cancellationToken` parameter can be ignored.

### Error handling

Throw `CommandFailedException` to report failures while still returning structured data:

```csharp
if (hasErrors)
    throw new CommandFailedException("Validation failed", response);
```


## Built-in Commands

The following commands are built in. Run `unicli commands` to see this list from the terminal, or `unicli exec <command> --help` to see parameters for any command. For a full reference with parameter and response details, see [`doc/commands.md`](doc/commands.md).

### Core

| Command | Description |
|---|---|
| `Compile` | Compile scripts and return results |
| `Eval` | Compile and execute C# code dynamically |

### Console

| Command | Description |
|---|---|
| `Console.GetLog` | Get console log entries (supports comma-separated `logType` filter, e.g. `"Warning,Error"`) |
| `Console.Clear` | Clear console |

### PlayMode

| Command | Description |
|---|---|
| `PlayMode.Enter` | Enter play mode |
| `PlayMode.Exit` | Exit play mode |
| `PlayMode.Pause` | Toggle pause |
| `PlayMode.Step` | Advance one frame in play mode |
| `PlayMode.Status` | Get the current play mode state |

### TestRunner

| Command | Description |
|---|---|
| `TestRunner.RunEditMode` | Run EditMode tests (`resultFilter`: `"failures"` (default), `"all"`, `"none"`) |
| `TestRunner.RunPlayMode` | Run PlayMode tests (`resultFilter`: `"failures"` (default), `"all"`, `"none"`) |

### Build

| Command | Description |
|---|---|
| `BuildPlayer.Build` | Build the player |
| `BuildPlayer.Compile` | Compile player scripts for a build target |
| `BuildTarget.GetActive` | Get the active build target and target group |
| `BuildTarget.Switch` | Switch the active build target |
| `BuildProfile.List` | List all build profiles (Unity 6+) |
| `BuildProfile.GetActive` | Get the active build profile (Unity 6+) |
| `BuildProfile.SetActive` | Set the active build profile (Unity 6+) |
| `BuildProfile.Inspect` | Inspect a build profile's details (Unity 6+) |

### GameObject / Component

| Command | Description |
|---|---|
| `GameObject.Find` | Find GameObjects |
| `GameObject.Create` | Create a new GameObject |
| `GameObject.CreatePrimitive` | Create a primitive GameObject |
| `GameObject.GetComponents` | Get components |
| `GameObject.SetActive` | Set active state |
| `GameObject.GetHierarchy` | Get scene hierarchy |
| `GameObject.AddComponent` | Add a component |
| `GameObject.RemoveComponent` | Remove a component |
| `GameObject.Destroy` | Destroy a GameObject |
| `GameObject.SetTransform` | Set local transform |
| `GameObject.Duplicate` | Duplicate a GameObject |
| `GameObject.Rename` | Rename a GameObject |
| `GameObject.SetParent` | Change parent or move to root |
| `Component.SetProperty` | Set a component property (supports ObjectReference via `guid:`, `instanceId:`, asset path) |

### Scene

| Command | Description |
|---|---|
| `Scene.List` | List all loaded scenes |
| `Scene.GetActive` | Get the active scene |
| `Scene.SetActive` | Set the active scene |
| `Scene.Open` | Open a scene by asset path |
| `Scene.Close` | Close a loaded scene |
| `Scene.Save` | Save a scene or all open scenes |
| `Scene.New` | Create a new scene |

### Asset

| Command | Description |
|---|---|
| `AssetDatabase.Find` | Search assets |
| `AssetDatabase.Import` | Import an asset |
| `AssetDatabase.GetPath` | Get asset path by GUID |
| `AssetDatabase.Delete` | Delete an asset |
| `Prefab.GetStatus` | Get prefab instance status |
| `Prefab.Instantiate` | Instantiate a prefab into scene |
| `Prefab.Save` | Save GameObject as prefab |
| `Prefab.Apply` | Apply prefab overrides |
| `Prefab.Unpack` | Unpack a prefab instance |
| `Material.Create` | Create a new material asset |
| `Material.Inspect` | Read all properties of a material (auto-generated) |
| `Material.SetColor` | Set a color property on a material |
| `Material.GetColor` | Get a color property from a material |
| `Material.SetFloat` | Set a float property on a material |
| `Material.GetFloat` | Get a float property from a material |

### Animation

| Command | Description |
|---|---|
| `AnimatorController.Create` | Create a new .controller asset |
| `AnimatorController.Inspect` | Inspect layers, parameters, states |
| `AnimatorController.AddParameter` | Add a parameter |
| `AnimatorController.RemoveParameter` | Remove a parameter |
| `AnimatorController.AddState` | Add a state to a layer |
| `AnimatorController.AddTransition` | Add a transition between states |
| `AnimatorController.AddTransitionCondition` | Add a condition to a transition |
| `Animator.Inspect` | Inspect Animator component |
| `Animator.SetController` | Assign an AnimatorController |
| `Animator.SetParameter` | Set a parameter value (PlayMode) |
| `Animator.Play` | Play a state immediately (PlayMode) |
| `Animator.CrossFade` | Cross-fade to a state (PlayMode) |

### PackageManager

| Command | Description |
|---|---|
| `PackageManager.List` | List packages |
| `PackageManager.Add` | Add a package |
| `PackageManager.Remove` | Remove a package |
| `PackageManager.Search` | Search registry |
| `PackageManager.GetInfo` | Get package details |
| `PackageManager.Update` | Update a package |

### Project / Settings

| Command | Description |
|---|---|
| `Project.Inspect` | Get project info |
| `PlayerSettings.Inspect` | Get all PlayerSettings values (auto-generated) |
| `EditorSettings.Inspect` | Get all EditorSettings values (auto-generated) |
| `EditorUserBuildSettings.Inspect` | Get all EditorUserBuildSettings values (auto-generated) |

### AssemblyDefinition

| Command | Description |
|---|---|
| `AssemblyDefinition.List` | List assembly definitions |
| `AssemblyDefinition.Get` | Get assembly definition |
| `AssemblyDefinition.Create` | Create assembly definition |
| `AssemblyDefinition.AddReference` | Add asmdef reference |
| `AssemblyDefinition.RemoveReference` | Remove asmdef reference |

### Selection / Window / Menu

| Command | Description |
|---|---|
| `Selection.Get` | Get the current editor selection |
| `Selection.SetAsset` | Select an asset by path |
| `Selection.SetAssets` | Select multiple assets by paths |
| `Selection.SetGameObject` | Select a GameObject by path |
| `Selection.SetGameObjects` | Select multiple GameObjects by paths |
| `Window.List` | List all available EditorWindow types |
| `Window.Open` | Open an EditorWindow by type name |
| `Window.Focus` | Focus an already-open EditorWindow |
| `Window.Create` | Create a new EditorWindow instance |
| `Menu.List` | List menu items |
| `Menu.Execute` | Execute a menu item |

### Utility

| Command | Description |
|---|---|
| `Type.List` | List types derived from a base type |
| `Type.Inspect` | Inspect nested types of a given type |

### Connection / Remote

| Command | Description |
|---|---|
| `Connection.List` | List available connection targets |
| `Connection.Connect` | Connect to a target by ID, IP, or device ID |
| `Connection.Status` | Get current connection status |
| `Remote.List` | List debug commands on connected player |
| `Remote.Invoke` | Invoke a debug command on connected player |

### Profiler

| Command | Description |
|---|---|
| `Profiler.Inspect` | Get profiler status and memory statistics |
| `Profiler.StartRecording` | Start profiler recording |
| `Profiler.StopRecording` | Stop profiler recording |
| `Profiler.SaveProfile` | Save profiler data to a .raw file |
| `Profiler.LoadProfile` | Load profiler data from a .raw file |
| `Profiler.GetFrameData` | Get CPU profiler sample data for a specific frame |
| `Profiler.TakeSnapshot` | Take a memory snapshot (.snap file) |
| `Profiler.AnalyzeFrames` | Analyze recorded frames and return aggregate statistics |
| `Profiler.FindSpikes` | Find frames exceeding frame time or GC allocation thresholds |

### Screenshot / Recorder

| Command | Description |
|---|---|
| `Screenshot.Capture` | Capture Game View screenshot as PNG (requires Play Mode) |
| `Recorder.StartRecording` | Start recording Game View as video (requires Play Mode) |
| `Recorder.StopRecording` | Stop the current video recording |
| `Recorder.Status` | Get the current recording status |

### Module

| Command | Description |
|---|---|
| `Module.List` | List all available modules and their enabled status |
| `Module.Enable` | Enable a module and reload the command dispatcher |
| `Module.Disable` | Disable a module and reload the command dispatcher |

### Optional: Search

| Command | Description |
|---|---|
| `Search` | Search Unity project using Unity Search API |

### Optional: NuGet

Requires [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

| Command | Description |
|---|---|
| `NuGet.List` | List all installed NuGet packages |
| `NuGet.Install` | Install a NuGet package |
| `NuGet.Uninstall` | Uninstall a NuGet package |
| `NuGet.Restore` | Restore all NuGet packages |
| `NuGet.ListSources` | List all configured package sources |
| `NuGet.AddSource` | Add a NuGet package source |
| `NuGet.RemoveSource` | Remove a NuGet package source |

### Optional: BuildMagic

Requires [BuildMagic](https://github.com/AnnulusGames/BuildMagic) (`jp.co.cyberagent.buildmagic`).

| Command | Description |
|---|---|
| `BuildMagic.List` | List all BuildMagic build schemes |
| `BuildMagic.Inspect` | Inspect a build scheme's configurations |
| `BuildMagic.Apply` | Apply a build scheme |

Settings Inspect commands (`PlayerSettings.Inspect`, `EditorSettings.Inspect`, etc.) are auto-generated via a Roslyn Source Generator, so the available properties always match your exact Unity version. To **modify** settings, use `unicli eval`:

```bash
unicli eval 'PlayerSettings.companyName = "MyCompany";' --json
```


## Module Management

UniCli groups optional commands into **modules** that can be toggled on or off per project. Core commands (Compile, Eval, Console, PlayMode, Menu, Build, TestRunner, Settings, etc.) are always available and cannot be disabled.

The following modules are available:

| Module | Description |
|---|---|
| Scene | Scene operations |
| GameObject | GameObject and Component operations |
| Assets | AssetDatabase, Prefab, Material operations |
| Profiler | Profiler operations |
| Animation | Animator and AnimatorController operations |
| Remote | Remote debug and Connection operations |
| Recorder | Video recording operations (requires `com.unity.recorder`) |
| Search | Unity Search API operations |
| NuGet | NuGet package management (requires NuGetForUnity) |
| BuildMagic | BuildMagic build scheme operations (requires `jp.co.cyberagent.buildmagic`) |

All modules are enabled by default. To disable a module, use the CLI or the Unity settings UI (**Edit > Project Settings > UniCli**):

```bash
# List all modules and their enabled status
unicli exec Module.List --json

# Enable a module
unicli exec Module.Enable '{"name":"Search"}' --json

# Disable a module
unicli exec Module.Disable '{"name":"Profiler"}' --json
```

Module settings are saved in `ProjectSettings/UniCliSettings.asset`.

`unicli commands --json` includes `builtIn` and `module` fields for each command, so you can programmatically identify whether a command is built-in or user-defined and which module it belongs to.


## Architecture

UniCli consists of two components:

- **CLI** (`unicli`) — A NativeAOT-compiled binary that you run from the terminal
- **Unity Package** (`com.yucchiy.unicli-server`) — An Editor plugin that receives and executes commands inside Unity

### Architecture

```
┌──────────┐  Named Pipe     ┌────────────────┐  PlayerConnection  ┌────────────────┐
│  unicli  │◄───────────────►│  Unity Editor  │◄──────────────────►│     Device     │
│  (CLI)   │ Length-prefixed │  (Server)      │ Chunked messages   │  (Dev Build)   │
│          │ JSON messages   │                │                    │                │
└──────────┘                 └────────────────┘                    └────────────────┘
```

**CLI ↔ Editor (Named Pipe):**
The CLI and Unity Editor communicate over a named pipe. The pipe name is derived from a SHA256 hash of the project's `Assets` path, so each project gets its own connection. Messages use a length-prefixed JSON framing protocol with a handshake (magic bytes `UCLI` + protocol version). The server plugin initializes via `[InitializeOnLoad]`, creates a background listener on the named pipe, and enqueues incoming commands to a `ConcurrentQueue`. Commands are dequeued and executed on Unity's main thread every frame via `EditorApplication.update`.

**Editor ↔ Device (PlayerConnection):**
For remote debugging, the Editor relays commands to a running Development Build via Unity's `PlayerConnection`. The runtime module (`UniCli.Remote`) auto-initializes a `RuntimeDebugReceiver` on the device, which discovers debug commands via reflection and registers message handlers. Responses are split into 16 KB chunks to work around PlayerConnection's undocumented message size limits. The Editor's `RemoteBridge` reassembles chunks and returns the complete response to the CLI.


## Remote Commands

UniCli can invoke debug commands on a running Development Build via Unity's `PlayerConnection`. This lets you inspect runtime state, query performance stats, and execute custom debug operations on a connected device — all from the terminal.

**Communication path:** CLI → Unity Editor (Named Pipe) → Device (PlayerConnection)

### Prerequisites

1. **Define symbol** — Add `UNICLI_REMOTE` to your project's Scripting Define Symbols (Player Settings → Other Settings).
   - The remote module's asmdef has two define constraints: `UNICLI_REMOTE || UNITY_EDITOR` and `DEVELOPMENT_BUILD || UNITY_EDITOR`.
   - In the Editor, both constraints are satisfied automatically — no additional setup needed for development.
   - In player builds, `UNICLI_REMOTE` must be defined **and** the build must be a Development Build for the module to be included.
   - This means release builds and builds without `UNICLI_REMOTE` will never contain the remote module code.
2. **Development Build** — Build with the "Development Build" and "Autoconnect Profiler" options enabled to allow PlayerConnection communication.
3. **Connect** — Use `Connection.Connect` to connect the Editor to the running player before sending remote commands.

### Editor-side commands

| Command | Description |
|---|---|
| `Remote.List` | List all debug commands registered on the connected player |
| `Remote.Invoke` | Invoke a debug command on the connected player |

```bash
# List debug commands on connected runtime player
unicli exec Remote.List

# Invoke a debug command
unicli exec Remote.Invoke '{"command":"Debug.Stats"}'

# Invoke with parameters
unicli exec Remote.Invoke '{"command":"Debug.GetPlayerPref","data":"{\"key\":\"HighScore\",\"type\":\"int\"}"}'

# Specify a particular player (when multiple are connected)
unicli exec Remote.Invoke '{"command":"Debug.SystemInfo","playerId":1}'
```

### Built-in debug commands

The following debug commands are included in the package and available on any Development Build with `UNICLI_REMOTE` defined:

| Command | Description |
|---|---|
| `Debug.SystemInfo` | Device model, OS, CPU, GPU, memory, battery, screen, quality settings |
| `Debug.Stats` | FPS, frame time, memory usage, GC collection counts, scene/object counts |
| `Debug.GetLogs` | Recent log entries from a ring buffer (supports limit and type filter) |
| `Debug.GetHierarchy` | Active scene hierarchy tree with depth, active state, component names |
| `Debug.FindGameObjects` | Substring search across all GameObjects (including inactive) |
| `Debug.GetScenes` | All loaded scenes with name, path, build index, root count |
| `Debug.GetPlayerPref` | Read a PlayerPrefs value by key (string, int, or float) |

### Creating custom debug commands

Inherit from `DebugCommand<TRequest, TResponse>` and override `CommandName` / `Description`. Commands are auto-discovered at runtime via reflection.

```csharp
using System;
using UniCli.Remote;
using UnityEngine;

public sealed class ToggleHitboxesCommand : DebugCommand<ToggleHitboxesCommand.Request, ToggleHitboxesCommand.Response>
{
    public override string CommandName => "Debug.ToggleHitboxes";
    public override string Description => "Toggle hitbox visualization";

    protected override Response ExecuteCommand(Request request)
    {
        HitboxVisualizer.Enabled = request.enabled;
        return new Response { enabled = HitboxVisualizer.Enabled };
    }

    [Serializable]
    public class Request
    {
        public bool enabled;
    }

    [Serializable]
    public class Response
    {
        public bool enabled;
    }
}
```

Use `Unit` as the type parameter when no input or output is needed:

```csharp
public sealed class ResetStateCommand : DebugCommand<Unit, Unit>
{
    public override string CommandName => "Debug.ResetState";
    public override string Description => "Reset game state";

    protected override Unit ExecuteCommand(Unit request)
    {
        GameManager.ResetAll();
        return Unit.Value;
    }
}
```

Key points:

- Request/Response types must be `[Serializable]` with **public fields** (required by `JsonUtility`)
- The base class uses `[RequireDerived]` to protect all subclasses from Managed Stripping automatically
- Commands run synchronously on the main thread
- Override `CommandName` (by convention `Debug.*`) and optionally `Description`
- Place custom commands anywhere in your project — they are discovered automatically via reflection at startup


## AI Agent Integration

### Claude Code Plugin

UniCli provides a [Claude Code](https://docs.anthropic.com/en/docs/claude-code) plugin via the marketplace. This plugin gives Claude Code the ability to interact with Unity Editor — compiling scripts, running tests, inspecting GameObjects, managing packages, and more — as part of its coding workflow.

With the plugin installed, Claude Code can:

- **Compile & verify** — catch compilation errors immediately after code changes
- **Run tests** — execute EditMode / PlayMode tests and read results
- **Inspect the scene** — find GameObjects, check components, and navigate the hierarchy
- **Manage packages** — add, remove, and search Unity packages
- **Discover commands** — automatically find all available commands, including project-specific custom commands

The plugin also handles server package setup: if the `com.yucchiy.unicli-server` package is not yet installed in the Unity project, Claude Code will run `unicli install` to set it up automatically.

#### Install the plugin

The UniCli CLI must be installed beforehand. See [Getting Started — CLI](#cli) above.

```bash
# 1. Add the UniCli marketplace
/plugin marketplace add yucchiy/UniCli

# 2. Install the plugin
/plugin install unicli@unicli
```

### Agent Skills / Codex

UniCli's skill definition follows the [Agent Skills](https://github.com/anthropics/agent-skills) specification, making it compatible with multiple AI coding agents:

- **Codex (OpenAI)**: Automatically detected via `.agents/skills/unity-development/`
- **Claude Code**: Installed as a plugin via `.claude-plugin/`
- **Other agents**: Any tool that supports the Agent Skills spec can load the skill from `.agents/skills/`

#### Install via Codex `$skill-installer`

If you're using [Codex](https://openai.com/index/introducing-codex/), install the UniCli skill directly from this repository:

```
$skill-installer install https://github.com/yucchiy/UniCli/tree/main/.agents/skills/unity-development
```

Once installed, Codex automatically detects the skill and gains the ability to interact with Unity Editor.

#### Manual setup for other projects

To use UniCli's skill in another project, copy the skill directory:

```bash
mkdir -p .agents/skills
cp -R /path/to/UniCli/.agents/skills/unity-development .agents/skills/unity-development
```

## License

[MIT](./LICENSE)
