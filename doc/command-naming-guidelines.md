# Command Naming Guidelines

This document defines the naming conventions for UniCli command handlers.

## Naming Principle

Command names should be **intuitive for CLI users**, not a 1:1 mirror of Unity API class names. The guiding question is:

> If a Unity developer types `unicli commands`, will this name immediately convey what it does?

## Rules

### 1. Use the concept name when the Unity API name is an implementation detail

When the Unity API name includes prefixes like `Editor`, suffixes like `Utility`/`Manager`, or is otherwise verbose, use a shorter concept-based name.

| Command | Unity API | Reason |
|---|---|---|
| `Scene.*` | `EditorSceneManager`, `SceneManager` | "Scene" is the concept; "EditorSceneManager" is an implementation detail |
| `Prefab.*` | `PrefabUtility` | "Prefab" is the concept; "Utility" suffix adds no meaning |
| `PlayMode.*` | `EditorApplication` | "PlayMode" describes the action domain; "EditorApplication" is too broad |
| `TestRunner.*` | `TestRunner.Api` | Simplified from the full namespace |
| `Console.*` | `LogEntry`, `LogEntries` | "Console" matches the Unity Editor window name |
| `Connection.*` | `EditorConnection` | "Connection" is the concept; "Editor" prefix is unnecessary in a CLI |

### 2. Use the Unity API name when it is already a clear, well-known concept

When the Unity class name is universally recognized by Unity developers and is concise enough for CLI use, adopt it directly.

| Command | Unity API | Reason |
|---|---|---|
| `AnimatorController.*` | `AnimatorController` | Distinct concept from `Animator` (runtime component) |
| `AssetDatabase.*` | `AssetDatabase` | Well-known, concise |
| `PackageManager.*` | `PackageManager.Client` | Well-known Unity concept |
| `Profiler.*` | `ProfilerDriver` | "Profiler" is the concept; "Driver" is an implementation detail |
| `BuildProfile.*` | `BuildProfile` | Direct match, Unity 6+ concept |
| `AssemblyDefinition.*` | `AssemblyDefinitionAsset` | "AssemblyDefinition" is well-known; "Asset" suffix is redundant |
| `Material.*` | `Material` | Direct match |

### 3. Distinguish overlapping concepts with separate categories

When one Unity domain spans both editor assets and runtime components, use separate command categories rather than merging them.

| Category | Scope | Example Commands |
|---|---|---|
| `AnimatorController.*` | Editor asset (`.controller` file) | `Create`, `AddState`, `AddTransition` |
| `Animator.*` | Runtime component on a GameObject | `Play`, `SetParameter`, `CrossFade` |

Merging these under a single `Animator.*` would create ambiguity (e.g., `Animator.Inspect` could mean either).

### 4. Keep the category name singular and concise

- Use `Scene`, not `Scenes` or `SceneManagement`
- Use `Prefab`, not `Prefabs` or `PrefabUtility`
- Use `GameObject`, not `GameObjects` (even though operations may return multiple results)

### 5. Use `Verb` or `Adjective+Noun` for action names

| Pattern | Examples |
|---|---|
| CRUD-style | `Create`, `Find`, `Delete`, `List` |
| State changes | `SetActive`, `SetParent`, `SetTransform` |
| Inspection | `Inspect`, `GetComponents`, `GetHierarchy` |
| Domain actions | `Enter`, `Exit`, `Pause`, `Apply`, `Compile` |

Prefer short, imperative verbs. Avoid redundancy with the category: `GameObject.Create`, not `GameObject.CreateGameObject`.

## Decision Checklist for New Commands

When naming a new command, walk through these questions:

1. **Is the Unity API name already concise and well-known?**
   - Yes -> Use it directly (e.g., `AssetDatabase`, `Material`)
   - No -> Simplify to the concept name (e.g., `Scene` instead of `EditorSceneManager`)

2. **Does the name conflict with an existing category?**
   - Yes -> Use the more specific name (e.g., `AnimatorController` to avoid conflicting with `Animator`)
   - No -> Use the simpler name

3. **Would a Unity developer immediately understand the command from `unicli commands` output?**
   - Yes -> Good to go
   - No -> Reconsider the name
