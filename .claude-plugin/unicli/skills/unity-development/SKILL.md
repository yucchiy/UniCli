---
description: >-
  MUST activate when performing ANY Unity-related task: editing C# scripts
  under Assets/ or Packages/, compiling Unity code, running tests, creating or
  modifying GameObjects/scenes/prefabs/assets, managing Unity packages, changing
  project settings, building the player, inspecting the scene hierarchy, or any
  operation involving the Unity Editor. This skill provides required workflows
  such as .meta file generation after file creation and compilation verification
  after code changes. Always load this skill before starting Unity work.
---

# UniCli — Unity Editor CLI

UniCli lets you interact with Unity Editor directly from the terminal.
The CLI (`unicli`) communicates with the Unity Editor over named pipes, so the Editor must be open with the `com.yucchiy.unicli-server` package installed.

## RULES — Always Follow These

1. **After creating/modifying ANY file under `Assets/` or `Packages/`**: Run `unicli exec AssetDatabase.Import --path "<path>" --json` to generate `.meta` files. **Never create `.meta` files manually** — always let Unity generate them via this command. Unity requires `.meta` files for every asset — skipping this causes missing references, broken imports, and compilation errors. This applies to all file types: `.cs`, `.asmdef`, `.asset`, `.prefab`, directories, etc.
2. **After modifying C# code in the Unity project**: Run `unicli exec Compile --json` to verify compilation. `dotnet build` only checks the client side — server-side code is compiled by Unity's own compiler.
3. **Always use `--json`** when parsing output programmatically.
4. **If connection to Unity Editor fails**: Retry 2–3 times, then ask the user to confirm Unity Editor is running with the project open.
5. **For platform-specific verification**: Use `unicli exec BuildPlayer.Compile --target <platform> --json` to catch platform-specific errors (missing `#if` guards, unsupported APIs, etc.).

## Prerequisites

Before running commands, verify that the CLI is installed and the Editor is reachable:

```bash
unicli check
```

If `unicli check` reports that the server package is not installed, run `unicli install` to install it:

```bash
unicli install
```

If the package is installed but the connection fails, make sure Unity Editor is open with the target project loaded. Retry a few times — the Editor may need a moment to start the server.

## Discovering Commands

The project may have custom commands beyond the built-in set. Always run `unicli commands` to get the latest list:

```bash
unicli commands        # human-readable
unicli commands --json # machine-readable
```

Use `--help` on any command to see its parameters:

```bash
unicli exec GameObject.Find --help
```

## Executing Commands

Run commands with `unicli exec <command>`. Pass parameters as `--key value` flags:

```bash
unicli exec GameObject.Find --name "Main Camera"
unicli exec TestRunner.RunEditMode --testNameFilter MyTest
```

Boolean flags can be passed without a value:

```bash
unicli exec GameObject.Find --includeInactive
```

Array parameters can be passed by repeating the same flag:

```bash
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --options ConnectWithProfiler
```

### Common options

- `--json` — Output in JSON format (recommended for structured processing)
- `--timeout <ms>` — Set command timeout in milliseconds
- `--no-focus` — Don't bring Unity Editor to front
- `--help` — Show command parameters and usage

## Built-in Commands

| Command | Description |
|---|---|
| `BuildPlayer.Build` | Build the player |
| `BuildPlayer.Compile` | Compile player scripts for a build target |
| `BuildProfile.List` | List all build profiles (Unity 6+ only) |
| `BuildProfile.GetActive` | Get the active build profile (Unity 6+ only) |
| `BuildProfile.SetActive` | Set the active build profile (Unity 6+ only) |
| `BuildProfile.Inspect` | Inspect a build profile's details (Unity 6+ only) |
| `Compile` | Compile scripts and return results |
| `Connection.List` | List available connection targets (players/devices) |
| `Connection.Connect` | Connect to a target by ID, IP, or device ID |
| `Connection.Status` | Get current profiler connection status |
| `Console.GetLog` | Get console log entries |
| `Console.Clear` | Clear console |
| `PlayMode.Enter` | Enter play mode |
| `PlayMode.Exit` | Exit play mode |
| `PlayMode.Pause` | Toggle pause |
| `Menu.List` | List menu items |
| `Menu.Execute` | Execute a menu item by path |
| `TestRunner.RunEditMode` | Run EditMode tests |
| `TestRunner.RunPlayMode` | Run PlayMode tests |
| `GameObject.Find` | Find GameObjects by name, tag, or layer |
| `GameObject.Create` | Create a new GameObject in the scene |
| `GameObject.CreatePrimitive` | Create a primitive (Cube, Sphere, etc.) |
| `GameObject.GetComponents` | Get components on a GameObject |
| `GameObject.SetActive` | Set active state |
| `GameObject.GetHierarchy` | Get scene hierarchy tree |
| `GameObject.AddComponent` | Add a component to a GameObject |
| `GameObject.RemoveComponent` | Remove a component from a GameObject |
| `GameObject.Destroy` | Destroy a GameObject from the scene |
| `GameObject.SetTransform` | Set local position/rotation/scale |
| `GameObject.Duplicate` | Duplicate a GameObject |
| `GameObject.Rename` | Rename a GameObject |
| `GameObject.SetParent` | Change parent or move to root |
| `Component.SetProperty` | Set a component property via SerializedProperty (supports ObjectReference via `guid:`, `instanceId:`, asset path) |
| `Material.Create` | Create a new material asset |
| `Material.Inspect` | Read all properties of a Material asset (by GUID) |
| `Material.SetColor` | Set a shader color property on a Material |
| `Material.SetFloat` | Set a shader float property on a Material |
| `Material.GetColor` | Get a shader color property from a Material |
| `Material.GetFloat` | Get a shader float property from a Material |
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
| `Prefab.GetStatus` | Get prefab instance status |
| `Prefab.Instantiate` | Instantiate a prefab into scene |
| `Prefab.Save` | Save GameObject as prefab asset |
| `Prefab.Apply` | Apply prefab overrides |
| `Prefab.Unpack` | Unpack a prefab instance |
| `AssetDatabase.Find` | Search assets |
| `AssetDatabase.Import` | Import an asset |
| `AssetDatabase.GetPath` | Get asset path by GUID |
| `AssetDatabase.Delete` | Delete an asset |
| `Project.Inspect` | Get project info |
| `PackageManager.List` | List installed packages |
| `PackageManager.Add` | Add a package |
| `PackageManager.Remove` | Remove a package |
| `PackageManager.Search` | Search the Unity package registry |
| `PackageManager.GetInfo` | Get detailed info about an installed package |
| `PackageManager.Update` | Update a package to a specific or latest version |
| `AssemblyDefinition.List` | List assembly definitions |
| `AssemblyDefinition.Get` | Get assembly definition details |
| `AssemblyDefinition.Create` | Create a new assembly definition |
| `AssemblyDefinition.AddReference` | Add an asmdef reference |
| `AssemblyDefinition.RemoveReference` | Remove an asmdef reference |
| `Scene.List` | List all loaded scenes |
| `Scene.GetActive` | Get the active scene |
| `Scene.SetActive` | Set the active scene |
| `Scene.Open` | Open a scene by asset path |
| `Scene.Close` | Close a loaded scene |
| `Scene.Save` | Save a scene or all open scenes |
| `Scene.New` | Create a new scene |
| `TypeCache.List` | List types derived from a base type |
| `TypeInspect` | Inspect nested types of a given type |
| `Eval` | Compile and execute C# code dynamically in the Unity Editor context |
| `NuGet.List` | List all installed NuGet packages (requires NuGetForUnity) |
| `NuGet.Install` | Install a NuGet package (requires NuGetForUnity) |
| `NuGet.Uninstall` | Uninstall a NuGet package (requires NuGetForUnity) |
| `NuGet.Restore` | Restore all NuGet packages (requires NuGetForUnity) |
| `Profiler.Inspect` | Get profiler status and memory statistics |
| `Profiler.StartRecording` | Start profiler recording |
| `Profiler.StopRecording` | Stop profiler recording |
| `Profiler.SaveProfile` | Save profiler data to a .raw file |
| `Profiler.LoadProfile` | Load profiler data from a .raw file |
| `Profiler.GetFrameData` | Get CPU profiler sample data for a specific frame |
| `Profiler.TakeSnapshot` | Take a memory snapshot (.snap file) |
| `Profiler.AnalyzeFrames` | Analyze recorded frames and return aggregate statistics |
| `Profiler.FindSpikes` | Find frames exceeding frame time or GC allocation thresholds |
| `Recorder.StartRecording` | Start recording Game View as video (requires Play Mode and com.unity.recorder) |
| `Recorder.StopRecording` | Stop the current video recording |
| `Recorder.Status` | Get the current recording status |
| `Screenshot.Capture` | Capture a screenshot of the Game View and save as PNG (requires Play Mode) |
| `Remote.List` | List debug commands registered on connected runtime player |
| `Remote.Invoke` | Invoke a debug command on connected runtime player |
| `Module.List` | List all available modules and their enabled status |
| `Module.Enable` | Enable a module and reload the command dispatcher |
| `Module.Disable` | Disable a module and reload the command dispatcher |

### Settings Commands (auto-generated)

Inspect commands for `PlayerSettings`, `EditorSettings`, and `EditorUserBuildSettings` are auto-generated at compile time to match your Unity version.

| Command | Description |
|---|---|
| `PlayerSettings.Inspect` | Get all PlayerSettings values |
| `EditorSettings.Inspect` | Get all EditorSettings values |
| `EditorUserBuildSettings.Inspect` | Get all EditorUserBuildSettings values |
| `Material.Inspect` | Read all properties of a material instance (requires `guid`) |

To **modify** settings, use `unicli eval`:

```bash
unicli eval 'PlayerSettings.companyName = "MyCompany";' --json
unicli eval 'PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);' --json
```

## Common Workflows

**Compile and check for errors:**

```bash
unicli exec Compile --json
```

**List connection targets and check status:**

```bash
unicli exec Connection.List --json
unicli exec Connection.Status --json
unicli exec Connection.Connect '{"id":-1}' --json
unicli exec Connection.Connect '{"deviceId":"DEVICE_SERIAL"}' --json
```

**List and invoke runtime debug commands on a connected player:**

```bash
unicli exec Remote.List --json
unicli exec Remote.Invoke '{"command":"Debug.ShowStats"}' --json
unicli exec Remote.Invoke '{"command":"Debug.ToggleHitboxes","data":"{\"enabled\":true}"}' --json
```

**Build the player:**

```bash
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --json
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --json
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --options ConnectWithProfiler --json
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --target Android --json
```

**Compile player scripts for a specific build target:**

```bash
unicli exec BuildPlayer.Compile --json
unicli exec BuildPlayer.Compile --target Android --json
unicli exec BuildPlayer.Compile --target iOS --extraScriptingDefines MY_DEFINE --extraScriptingDefines ANOTHER_DEFINE --json
```

**Manage build profiles (Unity 6+ only):**

```bash
unicli exec BuildProfile.List --json
unicli exec BuildProfile.GetActive --json
unicli exec BuildProfile.SetActive '{"path":"Assets/Settings/MyProfile.asset"}' --json
unicli exec BuildProfile.SetActive '{"path":"none"}' --json
unicli exec BuildProfile.Inspect '{"path":"Assets/Settings/MyProfile.asset"}' --json
```

**Run tests:**

```bash
unicli exec TestRunner.RunEditMode --json
unicli exec TestRunner.RunPlayMode --json
unicli exec TestRunner.RunEditMode --testNameFilter MyTest --json
```

**Inspect the scene:**

```bash
unicli exec GameObject.GetHierarchy --json
unicli exec GameObject.Find --name "Main Camera" --json
unicli exec GameObject.GetComponents --instanceId 1234 --json
```

**Create GameObjects:**

```bash
unicli exec GameObject.Create --name "Enemy" --json
unicli exec GameObject.Create --name "Child" --parent "Enemy" --json
unicli exec GameObject.Create --name "WithCollider" --components BoxCollider --json
unicli exec GameObject.CreatePrimitive --primitiveType Cube --json
unicli exec GameObject.CreatePrimitive --primitiveType Sphere --name "Ball" --json
```

**Modify GameObjects:**

```bash
unicli exec GameObject.Rename --path "Enemy" --name "Boss" --json
unicli exec GameObject.SetTransform --path "Boss" --position 1,2,3 --json
unicli exec GameObject.Duplicate --path "Boss" --json
unicli exec GameObject.SetParent --path "Child" --parentPath "Boss" --json
unicli exec GameObject.Destroy --path "OldObject" --json
```

**Manage components:**

```bash
unicli exec GameObject.AddComponent --path "Player" --typeName BoxCollider --json
unicli exec GameObject.RemoveComponent --componentInstanceId 1234 --json
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_IsKinematic" --value "true" --json

# ObjectReference: assign a material to a renderer by GUID
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_Materials.Array.data[0]" --value "guid:abc123def456" --json
# ObjectReference: by asset path
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_Mesh" --value "Assets/Meshes/Custom.mesh" --json
# ObjectReference: clear a reference
unicli exec Component.SetProperty --componentInstanceId 1234 --propertyPath "m_Material" --value "null" --json
```

**Material operations:**

```bash
# Create a new material
unicli exec Material.Create --assetPath "Assets/Materials/MyMat.mat" --json
unicli exec Material.Create --assetPath "Assets/Materials/MyMat.mat" --shader "Standard" --json
# Inspect all properties of a material
unicli exec Material.Inspect --guid "abc123def456" --json
# Set shader properties
unicli exec Material.SetColor --guid "abc123def456" --name "_Color" --value '{"r":1,"g":0,"b":0,"a":1}' --json
unicli exec Material.SetFloat --guid "abc123def456" --name "_Metallic" --value 0.8 --json
unicli exec Material.GetColor --guid "abc123def456" --name "_Color" --json
unicli exec Material.GetFloat --guid "abc123def456" --name "_Metallic" --json
```

**AnimatorController operations:**

```bash
unicli exec AnimatorController.Create --assetPath "Assets/Animations/Player.controller" --json
unicli exec AnimatorController.Inspect --assetPath "Assets/Animations/Player.controller" --json
unicli exec AnimatorController.AddParameter --assetPath "Assets/Animations/Player.controller" --name "Speed" --type Float --json
unicli exec AnimatorController.AddState --assetPath "Assets/Animations/Player.controller" --name "Idle" --json
unicli exec AnimatorController.AddState --assetPath "Assets/Animations/Player.controller" --name "Walk" --json
unicli exec AnimatorController.AddTransition --assetPath "Assets/Animations/Player.controller" --sourceStateName "Idle" --destinationStateName "Walk" --json
unicli exec AnimatorController.AddTransitionCondition --assetPath "Assets/Animations/Player.controller" --sourceStateName "Idle" --destinationStateName "Walk" --parameter "Speed" --mode Greater --threshold 0.1 --json
```

**Animator component operations:**

```bash
unicli exec Animator.SetController --path "Player" --controllerAssetPath "Assets/Animations/Player.controller" --json
unicli exec Animator.Inspect --path "Player" --json
```

**Prefab operations:**

```bash
unicli exec Prefab.GetStatus --path "MyPrefabInstance" --json
unicli exec Prefab.Instantiate --assetPath "Assets/Prefabs/Enemy.prefab" --json
unicli exec Prefab.Save --path "Player" --assetPath "Assets/Prefabs/Player.prefab" --json
unicli exec Prefab.Apply --path "MyPrefabInstance" --json
unicli exec Prefab.Unpack --path "MyPrefabInstance" --json
```

**Scene operations:**

```bash
unicli exec Scene.List --json
unicli exec Scene.GetActive --json
unicli exec Scene.Open --path "Assets/Scenes/Level1.unity" --json
unicli exec Scene.Open --path "Assets/Scenes/Additive.unity" --additive --json
unicli exec Scene.SetActive --name "Level1" --json
unicli exec Scene.Save --all --json
unicli exec Scene.Close --name "Additive" --json
unicli exec Scene.New --empty --additive --json
```

**Delete an asset:**

```bash
unicli exec AssetDatabase.Delete --path "Assets/Prefabs/Old.prefab" --json
```

**Inspect and modify Unity settings:**

```bash
# Inspect all settings values
unicli exec PlayerSettings.Inspect --json
unicli exec EditorSettings.Inspect --json

# Modify settings via eval
unicli eval 'PlayerSettings.companyName = "MyCompany";' --json
unicli eval 'PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);' --json
unicli eval 'return PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android).ToString();' --json
```

**Check console output:**

```bash
unicli exec Console.GetLog --json
```

**Dynamic C# code execution (Eval):**

`unicli eval` compiles and executes arbitrary C# code in the Unity Editor context. Use shell heredocs for multi-line code:

```bash
# Simple expression
unicli eval 'return Application.unityVersion;' --json

# Multi-line code with heredoc
unicli eval "$(cat <<'EOF'
var go = GameObject.Find("Main Camera");
return go.transform.position;
EOF
)" --json

# Void operations (no return needed)
unicli eval "$(cat <<'EOF'
var go = new GameObject("Created by Eval");
go.AddComponent<BoxCollider>();
EOF
)" --json

# Async/await (a cancellationToken variable is available in eval code)
unicli eval 'await Task.Delay(100, cancellationToken); return "done";' --json

# With custom type declarations (--declarations)
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

Options:
- `--json` — JSON output
- `--declarations '<code>'` — Additional type declarations (classes, structs, enums) included outside the Execute method
- `--timeout <ms>` — Set command timeout

The generated eval code receives a `cancellationToken` variable (`System.Threading.CancellationToken`) that is cancelled when the client disconnects. Use it with `async`/`await` for cooperative cancellation (e.g., `await Task.Delay(1000, cancellationToken)`).

**NuGet package management (requires NuGetForUnity):**

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

**Profiler operations:**

```bash
# Get profiler status and memory statistics
unicli exec Profiler.Inspect --json

# Record profiler data, then save
unicli exec Profiler.StartRecording '{"editor":true}' --json
# ... do work ...
unicli exec Profiler.StopRecording --json
unicli exec Profiler.SaveProfile '{"path":"Profiles/capture.raw"}' --json

# Load profiler data from a .raw file
unicli exec Profiler.LoadProfile '{"path":"Profiles/capture.raw"}' --json

# Get CPU sample data for the last recorded frame
unicli exec Profiler.GetFrameData --json
unicli exec Profiler.GetFrameData '{"frame":10,"limit":5}' --json

# Take a memory snapshot (.snap file for Memory Profiler)
unicli exec Profiler.TakeSnapshot --json
unicli exec Profiler.TakeSnapshot '{"path":"MemoryCaptures/my_snapshot.snap"}' --json

# Analyze recorded frames (aggregate statistics)
unicli exec Profiler.AnalyzeFrames --json
unicli exec Profiler.AnalyzeFrames '{"startFrame":100,"endFrame":200,"topSampleCount":20}' --json

# Find spike frames (frame time or GC threshold)
unicli exec Profiler.FindSpikes '{"frameTimeThresholdMs":16.6}' --json
unicli exec Profiler.FindSpikes '{"gcThresholdBytes":1024,"limit":5}' --json
```

**Module management:**

```bash
# List all modules and their enabled status
unicli exec Module.List --json

# Enable a module
unicli exec Module.Enable '{"name":"Settings"}' --json

# Disable a module
unicli exec Module.Disable '{"name":"Profiler"}' --json
```

All modules are enabled by default. Module settings are saved in `ProjectSettings/UniCliSettings.asset`.

**Screenshot operations (requires Play Mode):**

```bash
# Capture the Game View with default settings
unicli exec Screenshot.Capture --json

# Capture with a specific output path
unicli exec Screenshot.Capture '{"path":"Screenshots/test.png"}' --json

# Capture with super-sampling (2x resolution)
unicli exec Screenshot.Capture '{"path":"Screenshots/hires.png","superSize":2}' --json
```

**Video recording (requires Play Mode and com.unity.recorder):**

```bash
# Start recording with default settings (MP4, 30fps)
unicli exec Recorder.StartRecording --json

# Start with custom settings
unicli exec Recorder.StartRecording '{"path":"Recordings/demo.mp4","format":"MP4","frameRate":60,"width":1920,"height":1080}' --json

# Check recording status
unicli exec Recorder.Status --json

# Stop recording and save file
unicli exec Recorder.StopRecording --json
```

## Running Custom Code

When built-in commands don't cover what you need, choose the right approach based on the use case:

1. **One-shot tasks**: Use Eval (`unicli eval`) for ad-hoc operations, quick inspections, prototyping, and tasks that don't need to be reused. No files to create or compile — just pass the code directly.
2. **Reusable project commands**: Use `CommandHandler` when the operation will be called repeatedly or is part of the project's workflow. This provides type-safe parameters, structured responses, and discoverability via `unicli commands`.

## Custom Command Handlers

The server auto-discovers all `ICommandHandler` implementations via `TypeCache`, so no manual registration is required.

### Directory structure

Place custom handlers under `Assets/Editor/UniCli/` with a dedicated asmdef:

```
Assets/
└── Editor/
    └── UniCli/
        ├── MyProject.UniCli.Editor.asmdef
        └── Handlers/
            └── MyCustomHandler.cs
```

### Workflow

```bash
# 1. Create asmdef and add reference to UniCli.Server.Editor
unicli exec AssemblyDefinition.Create --path "Assets/Editor/UniCli/MyProject.UniCli.Editor.asmdef" --name "MyProject.UniCli.Editor" --editorOnly --json
unicli exec AssemblyDefinition.AddReference --path "Assets/Editor/UniCli/MyProject.UniCli.Editor.asmdef" --reference "UniCli.Server.Editor" --json

# 2. Create handler script, then import and compile
unicli exec AssetDatabase.Import --path "Assets/Editor/UniCli" --json
unicli exec Compile --json

# 3. Verify registration and execute
unicli commands --json
unicli exec MyCategory.MyAction --targetName "test" --json
```

### Handler implementation

```csharp
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UniCli.Server.Editor.Handlers;

namespace MyProject.UniCli.Editor.Handlers
{
    [System.Serializable]
    public class MyRequest
    {
        public string targetName = "";
    }

    [System.Serializable]
    public class MyResponse
    {
        public string result;
    }

    public sealed class MyCustomHandler : CommandHandler<MyRequest, MyResponse>
    {
        public override string CommandName => "MyCategory.MyAction";
        public override string Description => "Description shown in unicli commands";

        protected override ValueTask<MyResponse> ExecuteAsync(MyRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<MyResponse>(new MyResponse
            {
                result = $"Processed {request.targetName}"
            });
        }
    }
}
```

Key rules:
- Request/Response types must be `[Serializable]` with **public fields** (not properties) — required by `JsonUtility`
- Use `Unit` as `TRequest` when no input is needed, or as `TResponse` when no output is needed
- Throw `CommandFailedException` with response data on failure
- For async operations, use `TaskCompletionSource` + `await` with `WithCancellation(cancellationToken)` to wait for Unity callbacks
- Constructor parameters are resolved from `ServiceRegistry` for dependency injection

### Async handlers and cancellation

All handlers receive a `CancellationToken` that is cancelled when the client disconnects (e.g., Ctrl+C). For long-running async operations, use `WithCancellation` to ensure the server releases the command slot immediately on disconnect:

```csharp
protected override async ValueTask<MyResponse> ExecuteAsync(MyRequest request, CancellationToken cancellationToken)
{
    var tcs = new TaskCompletionSource<string>();
    SomeAsyncUnityApi.Start(result => tcs.SetResult(result));

    // Cancels the await if the client disconnects
    var result = await tcs.Task.WithCancellation(cancellationToken);

    return new MyResponse { value = result };
}
```

For synchronous handlers that complete instantly, the `cancellationToken` parameter can be ignored.

## Tips

- Always use `--json` when you need to parse the output programmatically.
- Run `unicli commands --json` first to discover all available commands, including project-specific custom commands.
- If a command times out, increase the timeout: `unicli exec Compile --timeout 60000`.
- If the connection to Unity Editor fails, retry a few times. If it still fails, confirm the Editor is running.
