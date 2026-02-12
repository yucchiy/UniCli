---
description: Use UniCli CLI to control Unity Editor — compile scripts, run tests, find GameObjects, manage packages, and more. Activate when working with Unity projects.
---

# UniCli — Unity Editor CLI

UniCli lets you interact with Unity Editor directly from the terminal.
The CLI (`unicli`) communicates with the Unity Editor over named pipes, so the Editor must be open with the `com.yucchiy.unicli-server` package installed.

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

### Common options

- `--json` — Output in JSON format (recommended for structured processing)
- `--timeout <ms>` — Set command timeout in milliseconds
- `--no-focus` — Don't bring Unity Editor to front
- `--help` — Show command parameters and usage

## Built-in Commands

| Command | Description |
|---|---|
| `Compile` | Compile scripts and return results |
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
| `GameObject.GetComponents` | Get components on a GameObject |
| `GameObject.SetActive` | Set active state |
| `GameObject.GetHierarchy` | Get scene hierarchy tree |
| `GameObject.AddComponent` | Add a component to a GameObject |
| `GameObject.RemoveComponent` | Remove a component from a GameObject |
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
| `AssemblyDefinition.List` | List assembly definitions |
| `AssemblyDefinition.Get` | Get assembly definition details |
| `AssemblyDefinition.Create` | Create a new assembly definition |
| `AssemblyDefinition.AddReference` | Add an asmdef reference |
| `AssemblyDefinition.RemoveReference` | Remove an asmdef reference |

## Common Workflows

**Compile and check for errors:**

```bash
unicli exec Compile --json
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

**Manage components:**

```bash
unicli exec GameObject.AddComponent --path "Player" --typeName BoxCollider --json
unicli exec GameObject.RemoveComponent --componentInstanceId 1234 --json
```

**Prefab operations:**

```bash
unicli exec Prefab.GetStatus --path "MyPrefabInstance" --json
unicli exec Prefab.Instantiate --assetPath "Assets/Prefabs/Enemy.prefab" --json
unicli exec Prefab.Save --path "Player" --assetPath "Assets/Prefabs/Player.prefab" --json
unicli exec Prefab.Apply --path "MyPrefabInstance" --json
unicli exec Prefab.Unpack --path "MyPrefabInstance" --json
```

**Delete an asset:**

```bash
unicli exec AssetDatabase.Delete --path "Assets/Prefabs/Old.prefab" --json
```

**Check console output:**

```bash
unicli exec Console.GetLog --json
```

## Tips

- Always use `--json` when you need to parse the output programmatically.
- Run `unicli commands --json` first to discover all available commands, including project-specific custom commands.
- If a command times out, increase the timeout: `unicli exec Compile --timeout 60000`.
- If the connection to Unity Editor fails, retry a few times. If it still fails, confirm the Editor is running.
