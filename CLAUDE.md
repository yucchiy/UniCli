# UniCli

## Project Structure

- `src`: Source code directory
    - `src/UniCli.Client`: CLI project for `unicli`
    - `src/UniCli.Unity`: Unity server implementation for `unicli` (Unity project)
        - `src/UniCli.Unity/Packages`: Server package
        - `src/UniCli.Unity/Assets/Samples`: Sample implementations for the server package
    - `src/UniCli.SourceGenerator`: Roslyn Source Generator for auto-generating Settings command handlers
- `src/UniCli.Protocol`: Shared type definitions between `UniCli.Client` and `UniCli.Unity`
- `doc`: Documentation directory

## Quick Commands

```bash
# Build Protocol (must be built first to trigger file copy)
dotnet build src/UniCli.Protocol

# Build Client
dotnet build src/UniCli.Client

# Publish Client and test with the built binary
dotnet publish src/UniCli.Client -o .build
UNICLI_PROJECT=src/UniCli.Unity .build/unicli commands --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Compile --json

# GameObject operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.Create '{"name":"TestObject"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.Create '{"name":"Child","parent":"TestObject"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.Create '{"name":"WithCollider","components":["BoxCollider"]}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.CreatePrimitive '{"primitiveType":"Cube"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.CreatePrimitive '{"primitiveType":"Sphere","name":"Ball","parent":"Parent"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.GetComponents '{"path":"Main Camera"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.AddComponent '{"path":"Main Camera","typeName":"BoxCollider"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.RemoveComponent '{"componentInstanceId":12345}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.Destroy '{"path":"TestObject"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.SetTransform '{"path":"TestObject","position":[1,2,3]}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.Duplicate '{"path":"TestObject"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.Rename '{"path":"TestObject","name":"Renamed"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec GameObject.SetParent '{"path":"Child","parentPath":"Parent"}' --json

# Component operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Component.SetProperty '{"componentInstanceId":12345,"propertyPath":"m_IsKinematic","value":"true"}' --json

# AnimatorController operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec AnimatorController.Create '{"assetPath":"Assets/Test.controller"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec AnimatorController.Inspect '{"assetPath":"Assets/Test.controller"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec AnimatorController.AddParameter '{"assetPath":"Assets/Test.controller","name":"Speed","type":"Float"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec AnimatorController.RemoveParameter '{"assetPath":"Assets/Test.controller","name":"Speed"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec AnimatorController.AddState '{"assetPath":"Assets/Test.controller","name":"Idle"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec AnimatorController.AddTransition '{"assetPath":"Assets/Test.controller","sourceStateName":"Idle","destinationStateName":"Walk"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec AnimatorController.AddTransitionCondition '{"assetPath":"Assets/Test.controller","sourceStateName":"Idle","destinationStateName":"Walk","parameter":"Speed","mode":"Greater","threshold":0.1}' --json

# Animator operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Animator.Inspect '{"path":"SomeGameObject"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Animator.SetController '{"path":"SomeGameObject","controllerAssetPath":"Assets/Test.controller"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Animator.SetParameter '{"path":"SomeGameObject","name":"Speed","value":"1.5"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Animator.Play '{"path":"SomeGameObject","stateName":"Idle"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Animator.CrossFade '{"path":"SomeGameObject","stateName":"Walk","transitionDuration":0.25}' --json

# Prefab operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Prefab.GetStatus '{"path":"Main Camera"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Prefab.Instantiate '{"assetPath":"Assets/Prefabs/Enemy.prefab"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Prefab.Save '{"path":"Main Camera","assetPath":"Assets/TestPrefab.prefab"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Prefab.Apply '{"path":"MyPrefabInstance"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Prefab.Unpack '{"path":"MyPrefabInstance"}' --json

# Scene operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Scene.List --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Scene.GetActive --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Scene.Open '{"path":"Assets/Scenes/SampleScene.unity"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Scene.Open '{"path":"Assets/Scenes/Additive.unity","additive":true}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Scene.SetActive '{"name":"SampleScene"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Scene.Save '{"all":true}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Scene.Close '{"name":"Additive"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Scene.New '{"empty":true,"additive":true}' --json

# PackageManager operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec PackageManager.GetInfo '{"name":"com.unity.test-framework"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec PackageManager.Update '{"name":"com.unity.test-framework"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec PackageManager.Update '{"name":"com.unity.test-framework","version":"1.4.5"}' --json

# AssetDatabase operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec AssetDatabase.Delete '{"path":"Assets/Prefabs/Old.prefab"}' --json

# Run Unity EditMode tests
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec TestRunner.RunEditMode --json

# Run Unity PlayMode tests
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec TestRunner.RunPlayMode --json

# Build player
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --options Development --options ConnectWithProfiler --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec BuildPlayer.Build --locationPathName "Builds/Test.app" --target Android --scenes "Assets/Scenes/SampleScene.unity" --json

# Compile player scripts for a specific build target
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec BuildPlayer.Compile --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec BuildPlayer.Compile --target Android --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec BuildPlayer.Compile --target iOS --extraScriptingDefines MY_DEFINE --extraScriptingDefines ANOTHER_DEFINE --json

# Connection operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Connection.List --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Connection.Status --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Connection.Connect '{"id":-1}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Connection.Connect '{"ip":"192.168.1.100"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Connection.Connect '{"deviceId":"DEVICE_SERIAL"}' --json

# Material operations
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Material.Create '{"assetPath":"Assets/Materials/MyMat.mat"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Material.Create '{"assetPath":"Assets/Materials/MyMat.mat","shader":"Standard"}' --json

# Settings operations (auto-generated by Source Generator)
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec PlayerSettings.Inspect --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec PlayerSettings.companyName '{"value":"MyCompany"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec PlayerSettings.Android.minSdkVersion '{"value":"AndroidApiLevel28"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec PlayerSettings.SetScriptingBackend '{"buildTarget":"Android","value":"IL2CPP"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec EditorSettings.Inspect --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec EditorUserBuildSettings.Inspect --json

# TypeCache and type inspection
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec TypeCache.List '{"baseType":"UniCli.Server.Editor.Handlers.ICommandHandler"}' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec TypeInspect '{"typeName":"UnityEditor.PlayerSettings"}' --json

# Dynamic C# code execution (Eval)
UNICLI_PROJECT=src/UniCli.Unity .build/unicli eval 'return Application.unityVersion;' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli eval 'return PlayerSettings.productName;' --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli eval "$(cat <<'EOF'
var go = GameObject.Find("Main Camera");
return go.transform.position;
EOF
)" --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli eval "$(cat <<'EOF'
var stats = new MyStats();
stats.objectCount = GameObject.FindObjectsOfType<GameObject>().Length;
return stats;
EOF
)" --declarations "$(cat <<'EOF'
[System.Serializable]
public class MyStats { public int objectCount; }
EOF
)" --json

# Compile Unity project (also serves as a build verification for the server)
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Compile --json
```

## Testing

When testing CLI behavior, always publish first with `dotnet publish src/UniCli.Client -o .build`, then test with `.build/unicli` directly. Do not use `dotnet run`.

### Server-side verification (required)

`dotnet build` only verifies the client-side compilation. When modifying server-side code (`Packages/com.yucchiy.unicli-server/`), **always verify with Unity compilation and tests**.

```bash
# 1. Build Source Generator (if modified)
dotnet build src/UniCli.SourceGenerator

# 2. Publish the client first
dotnet publish src/UniCli.Client -o .build

# 3. Verify server-side compilation (required)
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec Compile --json

# 3. Run tests
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec TestRunner.RunEditMode --json
UNICLI_PROJECT=src/UniCli.Unity .build/unicli exec TestRunner.RunPlayMode --json
```

### Maintaining documentation

When adding or modifying commands, update the following files to keep them in sync:

- `README.md` — Available Commands table and Examples section
- `.claude-plugin/unicli/skills/unity-development/SKILL.md` — Built-in Commands table and Common Workflows section

### Releasing a new version

1. Create a `release/vX.Y.Z` branch from `main`
2. Bump version in the following 3 files:
   - `src/UniCli.Client/UniCli.Client.csproj` (`<Version>`)
   - `src/UniCli.Unity/Packages/com.yucchiy.unicli-server/package.json` (`"version"`)
   - `.claude-plugin/marketplace.json` (`"version"`)
3. Build and verify: `dotnet build src/UniCli.Protocol && dotnet publish src/UniCli.Client -o .build`
4. Create a PR to `main` with a changelog summary
5. After merge: `git tag vX.Y.Z && git push origin vX.Y.Z`
   - GitHub Actions (`.github/workflows/release.yml`) will automatically build binaries and create a GitHub Release

### Tests requiring Unity connection

The `exec` and `commands` subcommands require a connection to the Unity Editor. If the connection fails, retry a few times. If it still fails, ask the user to confirm that Unity Editor is running with the project open.
