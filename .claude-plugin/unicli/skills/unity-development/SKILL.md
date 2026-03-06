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
  version: "1.1.0"
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
7. **When checking console logs**: Use `--logType "Warning,Error"` to filter out informational noise and focus on actionable issues. Stack traces are omitted by default; use `--stackTraceLines 3` when debugging errors.
8. **Discover commands dynamically**: Use `unicli commands --json` to list all available commands and `unicli exec <command> --help` to see parameters for any command. Do not rely on memorized command lists — the project may have custom commands.

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

Run commands with `unicli exec <command>`. Pass parameters as `--key value` flags:

```bash
unicli exec GameObject.Find --name "Main Camera" --json
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

**Compile and run tests:**

```bash
unicli exec Compile --json
unicli exec TestRunner.RunEditMode --json
unicli exec TestRunner.RunPlayMode --json
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
