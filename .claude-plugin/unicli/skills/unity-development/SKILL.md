---
name: unity-development
description: >-
  Use for Unity Editor automation through UniCli in projects where `unicli` is
  available: running `unicli exec`/`unicli eval`, editing files under `Assets/`
  or `Packages/`, compiling Unity code, running EditMode/PlayMode tests, and
  creating or modifying GameObjects, scenes, prefabs, assets, packages, build
  settings, or project settings. Follow required safeguards such as
  `AssetDatabase.Import` after file changes and `Compile` verification after C#
  edits.
license: MIT
compatibility: Requires unicli CLI installed and Unity Editor running with com.yucchiy.unicli-server package
metadata:
  author: yucchiy
  version: "1.5.0"
---

# UniCli — Unity Editor CLI

UniCli lets you interact with Unity Editor directly from the terminal.
The CLI (`unicli`) communicates with the Unity Editor over named pipes, so the Editor must be open with the `com.yucchiy.unicli-server` package installed.

## RULES — Always Follow These

1. **After creating/modifying ANY file under `Assets/` or `Packages/`**: Run `unicli exec AssetDatabase.Import --path "<path>" --json` to generate `.meta` files. **Never create `.meta` files manually** — always let Unity generate them via this command. Unity requires `.meta` files for every asset — skipping this causes missing references, broken imports, and compilation errors. This applies to all file types: `.cs`, `.asmdef`, `.asset`, `.prefab`, directories, etc.
2. **After modifying C# code in the Unity project**: Run `unicli exec Compile --json` to verify compilation.
3. **Always use `--json`** when parsing output programmatically.
4. **If connection to Unity Editor fails**: Retry 2–3 times, then ask the user to confirm Unity Editor is running with the project open.
5. **For platform-specific verification**: Use `unicli exec BuildPlayer.Compile --target <platform> --json` to catch platform-specific errors (missing `#if` guards, unsupported APIs, etc.).
6. **When running tests**: Always use the default `--resultFilter failures` (or `--resultFilter none` for summary-only) to keep output minimal. Only use `--resultFilter all` when you specifically need to inspect individual passed test details. This prevents large test suites from flooding context. Stack traces are omitted by default (`--stackTraceLines 0`); use `--stackTraceLines 3` when you need to diagnose a failure location.
7. **When checking console logs**: Use `Console.GetLog` with `{"logType":"Warning,Error"}` to filter out informational noise and focus on actionable issues. Stack traces are omitted by default; use `{"logType":"Error","stackTraceLines":3}` when debugging errors.
8. **Discover commands dynamically**: Use `unicli commands --json` to list all available commands and `unicli exec <command> --help` to see parameters for any command. Do not rely on memorized command lists — the project may have custom commands.
9. **Dirty scenes and scene-affecting operations**: `Scene.Open`/`Scene.New` (single mode), `Scene.Close`, and `TestRunner.RunEditMode`/`RunPlayMode` fail by default when a dirty scene would lose its unsaved changes, instead of letting Unity discard them silently or show a blocking save dialog. Read the error, then decide explicitly: pass `dirtyAction: "save"` when the dirty scenes are your own intended persistent changes, or `dirtyAction: "discard"` (scene commands only; not available for test runs) to drop temporary probe changes. If the dirty state may be from the user, do not save or discard it; ask the user how to proceed. `Scene.Save` rejects untitled scenes unless `saveAsPath` is given (saving them without a path would open a file panel). Dialogs can still block the editor through `Menu.Execute` (menu items with confirmations or file panels) and `eval` (`EditorUtility.DisplayDialog`, `OpenFilePanel`, ...) — avoid those APIs, and use `unicli exec Editor.Status` (without `--json`) as a compact pre-flight check before `BuildPlayer.Build`, `Menu.Execute`, or entering Play Mode, which do not take `dirtyAction`.
10. **When commands hang or time out**: Suspect a Unity Editor modal dialog such as a scene save prompt. Do not keep retrying blindly; ask the user to close or resolve the dialog, then retry.

## Project Path

By default, `unicli` looks for a Unity project in the current working directory.
If the Unity project is in a subdirectory, set the `UNICLI_PROJECT` environment variable:

```bash
export UNICLI_PROJECT=path/to/unity/project
unicli exec Compile --json
```

Or prefix each command:

```bash
UNICLI_PROJECT=path/to/unity/project unicli exec Compile --json
```

## Prerequisites

Before running commands, verify that the CLI is installed and the Editor is reachable:

```bash
unicli check
```

If `unicli check` reports that the server package is not installed, run `unicli install` to install it:

```bash
unicli install
```

If the server package version does not match the CLI version, run `unicli install --update` to update it:

```bash
unicli install --update
```

If the package is installed but the connection fails, make sure Unity Editor is open with the target project loaded. Retry a few times — the Editor may need a moment to start the server.

## Executing Commands

Run commands with `unicli exec <command>`. Pass parameters as `--key value` flags or raw JSON. Use JSON for arrays and nested values:

```bash
unicli exec GameObject.Find '{"name":"Main Camera"}' --json
```

Boolean flags can be passed without a value:

```bash
unicli exec GameObject.Find --includeInactive --json
```

Array parameters can be passed by repeating the same flag:

```bash
unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --options ConnectWithProfiler --json
```

### Common options

- `--json` — Output in JSON format (recommended for structured processing)
- `--timeout <ms>` — Set command timeout in milliseconds
- `--no-focus` — Don't bring Unity Editor to front
- `--help` — Show command parameters and usage

## Key Workflows

**Dirty-scene policy for scene-affecting commands:**

```bash
# Fails by default if a dirty scene would lose unsaved changes
unicli exec Scene.Open '{"path":"Assets/Scenes/Level1.unity"}' --json
# Decide explicitly: save first, or discard (scene commands only)
unicli exec Scene.Open '{"path":"Assets/Scenes/Level1.unity","dirtyAction":"save"}' --json
unicli exec Scene.New '{"empty":true,"dirtyAction":"discard"}' --json
unicli exec TestRunner.RunPlayMode '{"dirtyAction":"save"}' --json
```

**Compile and run tests:**

```bash
unicli exec Compile --json
unicli exec TestRunner.RunEditMode --json
unicli exec TestRunner.RunPlayMode --json
```

**Capture SceneView screenshots (works in Edit Mode, no Play Mode required):**

```bash
# 2D mode (orthographic, facing the XY plane); lookAt is a GameObject path, offset shifts the capture center
unicli exec Scene.Screenshot2D '{"lookAt":"Player","offset":[1,0],"size":5,"path":"Screenshots/map.png"}' --json
# 3D mode: orbit the lookAt point by yaw/pitch at the given distance
unicli exec Scene.Screenshot3D '{"lookAt":"Player","yaw":45,"pitch":30,"distance":10,"path":"Screenshots/shot.png"}' --json
```

**Inspect and modify settings:**

```bash
unicli exec PlayerSettings.Inspect --json
unicli eval 'PlayerSettings.companyName = "MyCompany";' --json
```

**Dynamic C# code execution (Eval):**

`unicli eval` compiles and executes arbitrary C# code in the Unity Editor context. Use shell heredocs for multi-line code:

```bash
unicli eval 'return Application.unityVersion;' --json

unicli eval "$(cat <<'EOF'
var go = GameObject.Find("Main Camera");
return go.transform.position;
EOF
)" --json
```

Options:
- `--declarations '<code>'` — Additional type declarations (classes, structs, enums) included outside the Execute method
- The generated eval code receives a `cancellationToken` variable for cooperative cancellation with `async`/`await`

## Running Custom Code

When built-in commands don't cover what you need, choose the right approach:

1. **One-shot tasks → Eval**: Use `unicli eval` for ad-hoc operations, quick inspections, prototyping, and tasks that don't need to be reused. No files to create or compile — just pass the code directly.
2. **Reusable project commands → CommandHandler**: Use `CommandHandler` when the operation will be called repeatedly or is part of the project's workflow. This provides type-safe parameters, structured responses, and discoverability via `unicli commands`.

**Connect to a running player:**

`Connection.*` commands manage the Unity Editor's PlayerConnection — used to connect to Development Builds running on devices or the local machine. This connection is required for remote debug commands and profiler data collection.

```bash
unicli exec Connection.List --json                          # List available targets
unicli exec Connection.Connect '{"id":-1}' --json           # Connect by player ID
unicli exec Connection.Connect '{"ip":"192.168.1.100"}' --json  # Connect by IP
unicli exec Connection.Connect '{"deviceId":"SERIAL"}' --json   # Connect by device serial
unicli exec Connection.Status --json                        # Check connection status
```

**Invoke remote debug commands on connected player:**

`Remote.*` commands invoke debug commands on a connected Development Build. Requires: `UNICLI_REMOTE` scripting define symbol + Development Build with Autoconnect Profiler enabled.

```bash
unicli exec Remote.List --json
unicli exec Remote.Invoke '{"command":"Debug.Stats"}' --json
unicli exec Remote.Invoke '{"command":"Debug.GetPlayerPref","data":"{\"key\":\"HighScore\",\"type\":\"int\"}"}' --json
```

Built-in debug commands: `Debug.SystemInfo`, `Debug.Stats`, `Debug.GetLogs`, `Debug.GetHierarchy`, `Debug.FindGameObjects`, `Debug.GetScenes`, `Debug.GetPlayerPref`

**Investigate a memory leak:**

`MemorySnapshot.*` commands are available when the project has `com.unity.memoryprofiler`. Captures can come from `MemorySnapshot.Capture`, `Profiler.TakeSnapshot`, or the Memory Profiler window; commands read `.snap` paths or named loaded snapshots and keep heavy snapshot data out of the CLI response.

```bash
unicli exec MemorySnapshot.Capture '{"path":"MemoryCaptures/before.snap"}' --json
# Reproduce the suspected leak in Play Mode or a connected player.
unicli exec MemorySnapshot.Capture '{"path":"MemoryCaptures/after.snap"}' --json
unicli exec MemorySnapshot.Load '{"path":"MemoryCaptures/before.snap","name":"before"}' --json
unicli exec MemorySnapshot.Load '{"path":"MemoryCaptures/after.snap","name":"after"}' --json
unicli exec MemorySnapshot.AllOfMemory '{"snapshot":"after","baseSnapshot":"before","limit":20,"minSize":1048576,"minSizeDelta":1048576}' --json
unicli exec MemorySnapshot.AllOfMemory '{"snapshot":"after","includeBreakdownTree":true,"pathFilter":"Native/Unity Subsystems","pathDepth":1,"memoryMetric":"both"}' --json
unicli exec MemorySnapshot.Diff '{"baseSnapshot":"before","targetSnapshot":"after","scope":"native","minSizeDelta":1048576}' --json
unicli exec MemorySnapshot.TopObjects '{"snapshot":"after","typeFilter":"Texture2D","limit":20}' --json
unicli exec MemorySnapshot.TopObjects '{"snapshot":"after","scope":"managed","groupByType":true,"limit":20}' --json
unicli exec MemorySnapshot.Analyze '{"snapshot":"after","baseSnapshot":"before","limit":10}' --json
```

Useful MemorySnapshot commands:

- `MemorySnapshot.Capture` — capture a `.snap` file; optional `flags` are `Unity.Profiling.Memory.CaptureFlags` names.
- `MemorySnapshot.List` / `MemorySnapshot.Load` — list files, then pin a snapshot under a stable `name`/`id`.
- `MemorySnapshot.Status` / `MemorySnapshot.Unload` — inspect loaded/cached analyses, release one by id/name, or clear all cached entries.
- `MemorySnapshot.Summary` — category totals and metadata; use `snapshot` or `path`, omitted `path` uses the latest `MemoryCaptures/*.snap`.
- `MemorySnapshot.AllOfMemory` — Memory Profiler All Of Memory style report; default is bounded type sections. Use `includeBreakdownTree`/`pathFilter`/`pathDepth` for All Of Memory tree paths, and `memoryMetric` (`allocated`, `resident`, `both`) for tree sorting and `minSize`; type/object sections remain allocated-based.
- `MemorySnapshot.TopObjects` — largest native objects or native/managed type totals; use `scope:"managed"` for managed type aggregation.
- `MemorySnapshot.Diff` — native/managed type deltas; use `baseSnapshot`/`targetSnapshot` or paths, omitted paths use latest and second latest snapshots.
- `MemorySnapshot.Analyze` — compact one-shot report combining summary, top types/objects, and optional diff.

## Custom Command Handlers

The server auto-discovers all `ICommandHandler` implementations via `TypeCache`, so no manual registration is required.

Place custom handlers under `Assets/Editor/UniCli/` with a dedicated asmdef:

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

## Tips

- Run `unicli commands --json` to discover all available commands, including project-specific custom commands.
- Run `unicli exec <command> --help` to see parameters, types, and default values for any command.
- If a command times out, increase the timeout: `unicli exec Compile --timeout 60000`.
