# UniCli

A command-line interface for controlling Unity Editor from the terminal.
UniCli lets you compile scripts, run tests, manage packages, inspect GameObjects, and more — all without leaving your terminal.

Designed to work with AI coding agents such as [Claude Code](https://docs.anthropic.com/en/docs/claude-code), UniCli gives AI the ability to interact with Unity Editor directly through structured CLI commands with JSON output.

## How It Works

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

The pipe name used for communication is derived from the project path, so each Unity project gets its own connection.


## Dynamic Code Execution (Eval)

`unicli eval` compiles and executes arbitrary C# code in the Unity Editor context using `AssemblyBuilder`. Code has full access to Unity APIs including `UnityEngine` and `UnityEditor`.

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

The result is returned as raw JSON. If the return type is `[Serializable]`, it is serialized with `JsonUtility`. `UnityEngine.Object` types use `EditorJsonUtility`. Primitives and strings are returned directly. Code that doesn't return a value (`void` operations) returns `null`.

Eval code supports `async`/`await` and receives a `cancellationToken` variable (`System.Threading.CancellationToken`) that is cancelled when the client disconnects. Use it for cooperative cancellation of long-running operations:

```bash
# Wait asynchronously with cancellation support
unicli eval 'await Task.Delay(5000, cancellationToken); return "done";' --json
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

# Get/switch the active build target
unicli exec BuildTarget.GetActive
unicli exec BuildTarget.Switch --target Android
unicli exec BuildTarget.Switch --target iOS

# Build profiles (Unity 6+ only)
unicli exec BuildProfile.List
unicli exec BuildProfile.GetActive
unicli exec BuildProfile.SetActive '{"path":"Assets/Settings/MyProfile.asset"}'
unicli exec BuildProfile.Inspect '{"path":"Assets/Settings/MyProfile.asset"}'

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

# Material operations
unicli exec Material.Create --assetPath "Assets/Materials/MyMat.mat"
unicli exec Material.Create --assetPath "Assets/Materials/MyMat.mat" --shader "Standard"
unicli exec Material.Inspect --guid "abc123def456"
unicli exec Material.SetColor --guid "abc123def456" --name "_Color" --value '{"r":1,"g":0,"b":0,"a":1}'
unicli exec Material.GetColor --guid "abc123def456" --name "_Color"
unicli exec Material.SetFloat --guid "abc123def456" --name "_Metallic" --value 0.8
unicli exec Material.GetFloat --guid "abc123def456" --name "_Metallic"

# AnimatorController operations
unicli exec AnimatorController.Create --assetPath "Assets/Animations/Player.controller"
unicli exec AnimatorController.Inspect --assetPath "Assets/Animations/Player.controller"
unicli exec AnimatorController.AddParameter --assetPath "Assets/Animations/Player.controller" --name "Speed" --type Float
unicli exec AnimatorController.AddState --assetPath "Assets/Animations/Player.controller" --name "Idle"
unicli exec AnimatorController.AddState --assetPath "Assets/Animations/Player.controller" --name "Walk"
unicli exec AnimatorController.AddTransition --assetPath "Assets/Animations/Player.controller" --sourceStateName "Idle" --destinationStateName "Walk"
unicli exec AnimatorController.AddTransitionCondition --assetPath "Assets/Animations/Player.controller" --sourceStateName "Idle" --destinationStateName "Walk" --parameter "Speed" --mode Greater --threshold 0.1

# Animator component operations
unicli exec Animator.SetController --path "Player" --controllerAssetPath "Assets/Animations/Player.controller"
unicli exec Animator.Inspect --path "Player"

# Prefab operations
unicli exec Prefab.GetStatus --path "MyPrefabInstance"
unicli exec Prefab.Instantiate --assetPath "Assets/Prefabs/Enemy.prefab"
unicli exec Prefab.Save --path "Player" --assetPath "Assets/Prefabs/Player.prefab"
unicli exec Prefab.Apply --path "MyPrefabInstance"
unicli exec Prefab.Unpack --path "MyPrefabInstance" --completely

# Selection operations
unicli exec Selection.Get
unicli exec Selection.SetGameObject --path "Main Camera"
unicli exec Selection.SetAsset --path "Assets/Materials/MyMat.mat"

# Window operations
unicli exec Window.List
unicli exec Window.Open --typeName "UnityEditor.ConsoleWindow"
unicli exec Window.Focus --typeName "UnityEditor.SceneView"

# Search Unity project using Unity Search API
unicli exec Search --query "t:Material"
unicli exec Search --query "t:Prefab" --maxResults 10

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

# Settings — modify values via eval
unicli eval 'PlayerSettings.companyName = "MyCompany";' --json
unicli eval 'PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);' --json

# Execute menu items
unicli exec Menu.Execute --menuPath "Window/General/Console"

# Console logs
unicli exec Console.GetLog
unicli exec Console.Clear

# Dynamic C# code execution (Eval)
unicli eval 'return Application.unityVersion;' --json
unicli eval 'return PlayerSettings.productName;' --json

# Multi-line code with heredoc
unicli eval "$(cat <<'EOF'
var go = GameObject.Find("Main Camera");
return go.transform.position;
EOF
)" --json

# Void operations (no return value needed)
unicli eval "$(cat <<'EOF'
var go = new GameObject("Created by Eval");
go.AddComponent<BoxCollider>();
EOF
)" --json

# Async/await (the generated code receives a cancellationToken variable)
unicli eval 'await Task.Delay(100, cancellationToken); return "done";' --json

# Custom type declarations with --declarations
unicli eval "$(cat <<'EOF'
var stats = new MyStats();
stats.objectCount = GameObject.FindObjectsOfType<GameObject>().Length;
stats.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
return stats;
EOF
)" --declarations "$(cat <<'EOF'
[System.Serializable]
public class MyStats
{
    public int objectCount;
    public string sceneName;
}
EOF
)" --json
```

**Profiler operations:**

```bash
# Get profiler status and memory statistics
unicli exec Profiler.Inspect --json

# Start profiler recording (clears existing frames by default)
unicli exec Profiler.StartRecording --json
unicli exec Profiler.StartRecording '{"deep":true}' --json
unicli exec Profiler.StartRecording '{"editor":true}' --json

# Stop profiler recording
unicli exec Profiler.StopRecording --json

# Save profiler data to a .raw file
unicli exec Profiler.SaveProfile '{"path":"Profiles/capture.raw"}' --json

# Load profiler data from a .raw file
unicli exec Profiler.LoadProfile '{"path":"Profiles/capture.raw"}' --json

# Get CPU sample data for the last frame (top 20 by default)
unicli exec Profiler.GetFrameData --json
unicli exec Profiler.GetFrameData '{"frame":10,"limit":5}' --json

# Take a memory snapshot (.snap file)
unicli exec Profiler.TakeSnapshot --json
unicli exec Profiler.TakeSnapshot '{"path":"MemoryCaptures/my_snapshot.snap"}' --json

# Analyze recorded frames (aggregate statistics)
unicli exec Profiler.AnalyzeFrames --json
unicli exec Profiler.AnalyzeFrames '{"startFrame":100,"endFrame":200,"topSampleCount":20}' --json

# Find spike frames (frame time or GC threshold)
unicli exec Profiler.FindSpikes '{"frameTimeThresholdMs":16.6}' --json
unicli exec Profiler.FindSpikes '{"gcThresholdBytes":1024,"limit":5}' --json
```

**Screenshot and video recording:**

```bash
# Capture a screenshot (requires Play Mode)
unicli exec Screenshot.Capture --json
unicli exec Screenshot.Capture '{"path":"Screenshots/test.png"}' --json
unicli exec Screenshot.Capture '{"path":"Screenshots/hires.png","superSize":2}' --json

# Record video (requires Play Mode and com.unity.recorder)
unicli exec Recorder.StartRecording --json
unicli exec Recorder.StartRecording '{"path":"Recordings/demo.mp4","format":"MP4","frameRate":60}' --json
unicli exec Recorder.Status --json
unicli exec Recorder.StopRecording --json
```

**Module management:**

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

**NuGet package management (requires [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)):**

```bash
# List installed NuGet packages
unicli exec NuGet.List --json

# Install a NuGet package
unicli exec NuGet.Install '{"id":"Newtonsoft.Json"}' --json
unicli exec NuGet.Install '{"id":"Newtonsoft.Json","version":"13.0.3"}' --json

# Uninstall a NuGet package
unicli exec NuGet.Uninstall '{"id":"Newtonsoft.Json"}' --json

# Restore all NuGet packages
unicli exec NuGet.Restore --json
```


## Available Commands

The following commands are built in. You can also run `unicli commands` to see this list from the terminal.

| Category           | Command                              | Description                        |
|--------------------|--------------------------------------|------------------------------------|
| BuildPlayer        | `BuildPlayer.Build`                  | Build the player                   |
| BuildPlayer        | `BuildPlayer.Compile`                | Compile player scripts for a build target |
| BuildProfile       | `BuildProfile.List`                  | List all build profiles (Unity 6+) |
| BuildProfile       | `BuildProfile.GetActive`             | Get the active build profile (Unity 6+) |
| BuildProfile       | `BuildProfile.SetActive`             | Set the active build profile (Unity 6+) |
| BuildProfile       | `BuildProfile.Inspect`               | Inspect a build profile's details (Unity 6+) |
| BuildTarget        | `BuildTarget.GetActive`              | Get the active build target and target group |
| BuildTarget        | `BuildTarget.Switch`                 | Switch the active build target       |
| Core               | `Compile`                            | Compile scripts and return results |
| Connection         | `Connection.List`                    | List available connection targets  |
| Connection         | `Connection.Connect`                 | Connect to a target by ID, IP, or device ID |
| Connection         | `Connection.Status`                  | Get current connection status      |
| Console            | `Console.GetLog`                     | Get console log entries            |
| Console            | `Console.Clear`                      | Clear console                      |
| PlayMode           | `PlayMode.Enter`                     | Enter play mode                    |
| PlayMode           | `PlayMode.Exit`                      | Exit play mode                     |
| PlayMode           | `PlayMode.Pause`                     | Toggle pause                       |
| PlayMode           | `PlayMode.Status`                    | Get the current play mode state    |
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
| Material           | `Material.Create`                    | Create a new material asset        |
| Material           | `Material.Inspect`                   | Read all properties of a material (auto-generated) |
| Material           | `Material.SetColor`                  | Set a color property on a material |
| Material           | `Material.GetColor`                  | Get a color property from a material |
| Material           | `Material.SetFloat`                  | Set a float property on a material |
| Material           | `Material.GetFloat`                  | Get a float property from a material |
| AnimatorController | `AnimatorController.Create`          | Create a new .controller asset     |
| AnimatorController | `AnimatorController.Inspect`         | Inspect layers, parameters, states |
| AnimatorController | `AnimatorController.AddParameter`    | Add a parameter                    |
| AnimatorController | `AnimatorController.RemoveParameter` | Remove a parameter                 |
| AnimatorController | `AnimatorController.AddState`        | Add a state to a layer             |
| AnimatorController | `AnimatorController.AddTransition`   | Add a transition between states    |
| AnimatorController | `AnimatorController.AddTransitionCondition` | Add a condition to a transition |
| Animator           | `Animator.Inspect`                   | Inspect Animator component         |
| Animator           | `Animator.SetController`             | Assign an AnimatorController       |
| Animator           | `Animator.SetParameter`              | Set a parameter value (PlayMode)   |
| Animator           | `Animator.Play`                      | Play a state immediately (PlayMode)|
| Animator           | `Animator.CrossFade`                 | Cross-fade to a state (PlayMode)   |
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
| Selection          | `Selection.Get`                      | Get the current editor selection   |
| Selection          | `Selection.SetAsset`                 | Select an asset by path            |
| Selection          | `Selection.SetAssets`                | Select multiple assets by paths    |
| Selection          | `Selection.SetGameObject`            | Select a GameObject by path        |
| Selection          | `Selection.SetGameObjects`           | Select multiple GameObjects by paths |
| Window             | `Window.List`                        | List all available EditorWindow types |
| Window             | `Window.Open`                        | Open an EditorWindow by type name  |
| Window             | `Window.Focus`                       | Focus an already-open EditorWindow |
| Window             | `Window.Create`                      | Create a new EditorWindow instance |
| Utility            | `Type.List`                          | List types derived from a base type |
| Utility            | `Type.Inspect`                       | Inspect nested types of a given type |
| Eval               | `Eval`                               | Compile and execute C# code dynamically |
| Search (optional)  | `Search`                             | Search Unity project using Unity Search API |
| NuGet (optional)   | `NuGet.List`                         | List all installed NuGet packages  |
| NuGet (optional)   | `NuGet.Install`                      | Install a NuGet package            |
| NuGet (optional)   | `NuGet.Uninstall`                    | Uninstall a NuGet package          |
| NuGet (optional)   | `NuGet.Restore`                      | Restore all NuGet packages         |
| Profiler           | `Profiler.Inspect`                   | Get profiler status and memory statistics |
| Profiler           | `Profiler.StartRecording`            | Start profiler recording           |
| Profiler           | `Profiler.StopRecording`             | Stop profiler recording            |
| Profiler           | `Profiler.SaveProfile`               | Save profiler data to a .raw file  |
| Profiler           | `Profiler.LoadProfile`               | Load profiler data from a .raw file |
| Profiler           | `Profiler.GetFrameData`              | Get CPU profiler sample data for a specific frame |
| Profiler           | `Profiler.TakeSnapshot`              | Take a memory snapshot (.snap file) |
| Profiler           | `Profiler.AnalyzeFrames`             | Analyze recorded frames and return aggregate statistics |
| Profiler           | `Profiler.FindSpikes`                | Find frames exceeding frame time or GC allocation thresholds |
| Remote             | `Remote.List`                        | List debug commands on connected player |
| Remote             | `Remote.Invoke`                      | Invoke a debug command on connected player |
| Recorder (optional)| `Recorder.StartRecording`            | Start recording Game View as video (requires Play Mode) |
| Recorder (optional)| `Recorder.StopRecording`             | Stop the current video recording   |
| Recorder (optional)| `Recorder.Status`                    | Get the current recording status   |
| Screenshot         | `Screenshot.Capture`                 | Capture Game View screenshot as PNG (requires Play Mode) |
| BuildMagic (optional)| `BuildMagic.List`                  | List all BuildMagic build schemes  |
| BuildMagic (optional)| `BuildMagic.Inspect`               | Inspect a build scheme's configurations |
| BuildMagic (optional)| `BuildMagic.Apply`                 | Apply a build scheme               |
| Module             | `Module.List`                        | List all available modules and their enabled status |
| Module             | `Module.Enable`                      | Enable a module and reload the command dispatcher |
| Module             | `Module.Disable`                     | Disable a module and reload the command dispatcher |

Use `unicli exec <command> --help` to see parameters for any command.

### Settings Commands (auto-generated)

UniCli auto-generates Inspect commands via a Roslyn Source Generator at Unity compile time. Target types are declared with the `[GenerateCommands]` assembly attribute, so the available properties always match your exact Unity version.

| Type | Command | Description |
|---|---|---|
| `PlayerSettings` | `PlayerSettings.Inspect` | Get all PlayerSettings values |
| `EditorSettings` | `EditorSettings.Inspect` | Get all EditorSettings values |
| `EditorUserBuildSettings` | `EditorUserBuildSettings.Inspect` | Get all EditorUserBuildSettings values |
| `Material` | `Material.Inspect` | Read all properties of a material instance (requires `guid`) |

To **modify** settings, use `unicli eval` for direct access to Unity APIs:

```bash
# Set a property
unicli eval 'PlayerSettings.companyName = "MyCompany";' --json

# Call a method with platform target
unicli eval 'PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);' --json

# Read a value
unicli eval 'return PlayerSettings.companyName;' --json
```

For Material operations, use the dedicated commands (`Material.SetColor`, `Material.SetFloat`, etc.) or `unicli eval`.

Run `unicli commands` to see the full list of available commands, including all generated commands.


## Custom Commands

You can extend UniCli by adding custom commands in your Unity project. Commands are auto-discovered — no manual registration required.

### Class-based commands

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

Inherit from `DebugCommand<TRequest, TResponse>` and annotate with `[DebugCommand]`. Commands are auto-discovered at runtime via reflection.

```csharp
using System;
using UniCli.Remote;
using UnityEngine;

[DebugCommand("Debug.ToggleHitboxes", "Toggle hitbox visualization")]
public sealed class ToggleHitboxesCommand : DebugCommand<ToggleHitboxesCommand.Request, ToggleHitboxesCommand.Response>
{
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
[DebugCommand("Debug.ResetState", "Reset game state")]
public sealed class ResetStateCommand : DebugCommand<Unit, Unit>
{
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
- The `[DebugCommand]` attribute takes a name (by convention `Debug.*`) and an optional description
- Place custom commands anywhere in your project — they are discovered automatically via reflection at startup


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
