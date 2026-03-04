# Command Reference

> Auto-generated from `unicli commands --json`. Run `tools/generate-command-reference.sh` to update.


## Core


### Compile

Trigger script compilation and return results with error details

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `errorCount` | `int` |
| `warningCount` | `int` |
| `errors` | `CompileIssue[]` |
| `warnings` | `CompileIssue[]` |

---


### Eval

Compile and execute C# code dynamically in the Unity Editor context

**Parameters:**

| Field | Type |
|---|---|
| `code` | `string` |
| `declarations` | `string` |

**Response:** None

---


## Animation


### Animator.CrossFade

Cross-fade to a state on an Animator (requires PlayMode)

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `stateName` | `string` |
| `layer` | `int` |
| `transitionDuration` | `float` |
| `normalizedTime` | `float` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `stateName` | `string` |
| `layer` | `int` |
| `transitionDuration` | `float` |
| `normalizedTime` | `float` |

---


### Animator.Inspect

Inspect an Animator component (parameters, current state, controller info)

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `gameObjectPath` | `string` |
| `instanceId` | `int` |
| `enabled` | `bool` |
| `controllerAssetPath` | `string` |
| `parameters` | `AnimatorRuntimeParameterInfo[]` |
| `isPlaying` | `bool` |
| `currentStateName` | `string` |
| `currentStateNormalizedTime` | `float` |

---


### Animator.Play

Play a state immediately on an Animator (requires PlayMode)

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `stateName` | `string` |
| `layer` | `int` |
| `normalizedTime` | `float` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `stateName` | `string` |
| `layer` | `int` |
| `normalizedTime` | `float` |

---


### Animator.SetController

Assign an AnimatorController to an Animator component

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `controllerAssetPath` | `string` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `gameObjectPath` | `string` |
| `controllerAssetPath` | `string` |

---


### Animator.SetParameter

Set an Animator parameter value (requires PlayMode)

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `name` | `string` |
| `value` | `string` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `name` | `string` |
| `type` | `string` |
| `value` | `string` |

---


### AnimatorController.AddParameter

Add a parameter to an AnimatorController

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `name` | `string` |
| `type` | `string` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `name` | `string` |
| `type` | `string` |
| `parameterCount` | `int` |

---


### AnimatorController.AddState

Add a state to an AnimatorController layer

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `name` | `string` |
| `layerIndex` | `int` |
| `motionAssetPath` | `string` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `name` | `string` |
| `layerIndex` | `int` |
| `motionName` | `string` |
| `stateCount` | `int` |

---


### AnimatorController.AddTransition

Add a transition between two states in an AnimatorController

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `sourceStateName` | `string` |
| `destinationStateName` | `string` |
| `layerIndex` | `int` |
| `hasExitTime` | `bool` |
| `exitTime` | `float` |
| `duration` | `float` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `sourceStateName` | `string` |
| `destinationStateName` | `string` |
| `layerIndex` | `int` |
| `hasExitTime` | `bool` |
| `exitTime` | `float` |
| `duration` | `float` |

---


### AnimatorController.AddTransitionCondition

Add a condition to a transition between two states in an AnimatorController

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `sourceStateName` | `string` |
| `destinationStateName` | `string` |
| `layerIndex` | `int` |
| `transitionIndex` | `int` |
| `parameter` | `string` |
| `mode` | `string` |
| `threshold` | `float` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `sourceStateName` | `string` |
| `destinationStateName` | `string` |
| `parameter` | `string` |
| `mode` | `string` |
| `threshold` | `float` |
| `conditionCount` | `int` |

---


### AnimatorController.Create

Create a new AnimatorController asset

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `layerCount` | `int` |
| `parameterCount` | `int` |

---


### AnimatorController.Inspect

Inspect an AnimatorController asset (layers, parameters, states)

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `parameters` | `AnimatorParameterInfo[]` |
| `layers` | `AnimatorLayerInfo[]` |

---


### AnimatorController.RemoveParameter

Remove a parameter from an AnimatorController

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `name` | `string` |
| `parameterCount` | `int` |

---


## AssemblyDefinition


### AssemblyDefinition.AddReference

Add an assembly reference to an existing assembly definition

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `reference` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `addedReference` | `string` |
| `references` | `string[]` |

---


### AssemblyDefinition.Create

Create a new assembly definition file

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `directory` | `string` |
| `rootNamespace` | `string` |
| `references` | `string[]` |
| `includePlatforms` | `string[]` |
| `excludePlatforms` | `string[]` |
| `allowUnsafeCode` | `bool` |
| `autoReferenced` | `bool` (default: `true`) |
| `defineConstraints` | `string[]` |
| `noEngineReferences` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |

---


### AssemblyDefinition.Get

Get detailed information about a specific assembly definition

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `rootNamespace` | `string` |
| `references` | `string[]` |
| `includePlatforms` | `string[]` |
| `excludePlatforms` | `string[]` |
| `allowUnsafeCode` | `bool` |
| `autoReferenced` | `bool` |
| `defineConstraints` | `string[]` |
| `noEngineReferences` | `bool` |
| `sourceFiles` | `string[]` |
| `defines` | `string[]` |

---


### AssemblyDefinition.List

List all assembly definitions in the project

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `assemblies` | `AssemblyDefinitionEntry[]` |
| `totalCount` | `int` |

---


### AssemblyDefinition.RemoveReference

Remove an assembly reference from an existing assembly definition

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `reference` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `removedReference` | `string` |
| `references` | `string[]` |

---


## Assets


### AssetDatabase.Delete

Delete an asset

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `type` | `string` |

---


### AssetDatabase.Find

Find assets by filter (e.g. t:Texture, l:MyLabel)

**Parameters:**

| Field | Type |
|---|---|
| `filter` | `string` |
| `searchInFolders` | `string[]` |
| `maxResults` | `int` (default: `100`) |

**Response:**

| Field | Type |
|---|---|
| `assets` | `AssetInfo[]` |
| `totalFound` | `int` |

---


### AssetDatabase.GetPath

Convert between asset GUID and path

**Parameters:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `path` | `string` |
| `type` | `string` |
| `exists` | `bool` |

---


### AssetDatabase.Import

Reimport an asset or refresh the AssetDatabase

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |
| `forceUpdate` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `refreshed` | `bool` |

---


### Material.Create

Create a new material asset

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `shader` | `string` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `guid` | `string` |
| `shaderName` | `string` |

---


### Material.GetColor

Get a color property from a material (Material.GetColor)

**Parameters:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `name` | `string` |
| `value` | `ColorValue` |

---


### Material.GetFloat

Get a float property from a material (Material.GetFloat)

**Parameters:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `name` | `string` |
| `value` | `float` |

---


### Material.Inspect

Inspect Material instance

**Parameters:**

| Field | Type |
|---|---|
| `guid` | `string` |

**Response:**

| Field | Type |
|---|---|
| `color` | `Color` |
| `mainTextureOffset` | `Vector2` |
| `mainTextureScale` | `Vector2` |
| `renderQueue` | `int` |
| `globalIlluminationFlags` | `string` |
| `doubleSidedGI` | `bool` |
| `enableInstancing` | `bool` |
| `passCount` | `int` |
| `isVariant` | `bool` |

---


### Material.SetColor

Set a color property on a material (Material.SetColor)

**Parameters:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `name` | `string` |
| `value` | `ColorValue` |

**Response:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `name` | `string` |
| `value` | `ColorValue` |

---


### Material.SetFloat

Set a float property on a material (Material.SetFloat)

**Parameters:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `name` | `string` |
| `value` | `float` |

**Response:**

| Field | Type |
|---|---|
| `guid` | `string` |
| `name` | `string` |
| `value` | `float` |

---


### Prefab.Apply

Apply overrides of a prefab instance to the source prefab asset via PrefabUtility

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `assetPath` | `string` |

---


### Prefab.GetStatus

Get prefab instance status for a GameObject via PrefabUtility

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `status` | `string` |
| `assetPath` | `string` |
| `hasOverrides` | `bool` |
| `isPrefabInstance` | `bool` |

---


### Prefab.Instantiate

Instantiate a prefab asset into the scene via PrefabUtility

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `parentInstanceId` | `int` |
| `parentPath` | `string` |

**Response:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `name` | `string` |
| `assetPath` | `string` |

---


### Prefab.Save

Save a GameObject as a prefab asset via PrefabUtility

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `assetPath` | `string` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `assetPath` | `string` |

---


### Prefab.Unpack

Unpack a prefab instance via PrefabUtility, disconnecting it from the source prefab

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `completely` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `unpackMode` | `string` |

---


## BuildPlayer


### BuildPlayer.Build

Build the player using BuildPipeline.BuildPlayer

**Parameters:**

| Field | Type |
|---|---|
| `target` | `string` |
| `locationPathName` | `string` |
| `scenes` | `string[]` |
| `options` | `string[]` |
| `extraScriptingDefines` | `string[]` |

**Response:**

| Field | Type |
|---|---|
| `target` | `string` |
| `targetGroup` | `string` |
| `locationPathName` | `string` |
| `result` | `string` |
| `totalErrorCount` | `int` |
| `totalWarningCount` | `int` |
| `totalBuildTimeSec` | `double` |
| `totalSizeBytes` | `Int64` |
| `steps` | `BuildStepInfo[]` |
| `errors` | `BuildMessageInfo[]` |
| `warnings` | `BuildMessageInfo[]` |

---


### BuildPlayer.Compile

Compile player scripts for a specific build target

**Parameters:**

| Field | Type |
|---|---|
| `target` | `string` |
| `extraScriptingDefines` | `string[]` |

**Response:**

| Field | Type |
|---|---|
| `target` | `string` |
| `targetGroup` | `string` |
| `assemblyCount` | `int` |
| `assemblies` | `string[]` |
| `errorCount` | `int` |
| `warningCount` | `int` |
| `errors` | `CompileIssue[]` |
| `warnings` | `CompileIssue[]` |

---


## BuildTarget


### BuildTarget.GetActive

Get the active build target and build target group via EditorUserBuildSettings

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `buildTarget` | `string` |
| `buildTargetGroup` | `string` |

---


### BuildTarget.Switch

Switch the active build target via EditorUserBuildSettings.SwitchActiveBuildTarget

**Parameters:**

| Field | Type |
|---|---|
| `target` | `string` |

**Response:**

| Field | Type |
|---|---|
| `buildTarget` | `string` |
| `buildTargetGroup` | `string` |

---


## Commands


### Commands.List

List all available commands with their metadata

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `commands` | `CommandInfo[]` |

---


## Console


### Console.Clear

Clear Unity Editor console logs

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `clearedCount` | `int` |

---


### Console.GetLog

Retrieve Unity Editor console logs with optional filtering

**Parameters:**

| Field | Type |
|---|---|
| `logType` | `string` (default: `All`) |
| `searchText` | `string` |
| `maxCount` | `int` (default: `100`) |
| `stackTraceLines` | `int` |

**Response:**

| Field | Type |
|---|---|
| `logs` | `LogEntry[]` |
| `totalCount` | `int` |
| `displayedCount` | `int` |

---


## EditorSettings


### EditorSettings.Inspect

Inspect all EditorSettings values

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `unityRemoteDevice` | `string` |
| `unityRemoteCompression` | `string` |
| `unityRemoteResolution` | `string` |
| `unityRemoteJoystickSource` | `string` |
| `serializationMode` | `string` |
| `lineEndingsForNewScripts` | `string` |
| `defaultBehaviorMode` | `string` |
| `prefabModeAllowAutoSave` | `bool` |
| `spritePackerMode` | `string` |
| `spritePackerPaddingPower` | `int` |
| `etcTextureCompressorBehavior` | `int` |
| `etcTextureFastCompressor` | `int` |
| `etcTextureNormalCompressor` | `int` |
| `etcTextureBestCompressor` | `int` |
| `enableTextureStreamingInEditMode` | `bool` |
| `enableTextureStreamingInPlayMode` | `bool` |
| `asyncShaderCompilation` | `bool` |
| `cachingShaderPreprocessor` | `bool` |
| `projectGenerationRootNamespace` | `string` |
| `useLegacyProbeSampleCount` | `bool` |
| `enableCookiesInLightmapper` | `bool` |
| `enableEnlightenBakedGI` | `bool` |
| `enterPlayModeOptionsEnabled` | `bool` |
| `enterPlayModeOptions` | `string` |
| `serializeInlineMappingsOnOneLine` | `bool` |
| `assetPipelineMode` | `string` |
| `cacheServerMode` | `string` |
| `refreshImportMode` | `string` |
| `cacheServerEndpoint` | `string` |
| `cacheServerNamespacePrefix` | `string` |
| `cacheServerEnableDownload` | `bool` |
| `cacheServerEnableUpload` | `bool` |
| `cacheServerEnableAuth` | `bool` |
| `cacheServerEnableTls` | `bool` |
| `cacheServerValidationMode` | `string` |
| `cacheServerDownloadBatchSize` | `int` |
| `gameObjectNamingDigits` | `int` |
| `gameObjectNamingScheme` | `string` |
| `assetNamingUsesSpace` | `bool` |

---


## EditorUserBuildSettings


### EditorUserBuildSettings.Inspect

Inspect all EditorUserBuildSettings values

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `selectedBuildTargetGroup` | `string` |
| `selectedQnxOsVersion` | `string` |
| `selectedQnxArchitecture` | `string` |
| `selectedEmbeddedLinuxArchitecture` | `string` |
| `remoteDeviceInfo` | `bool` |
| `remoteDeviceAddress` | `string` |
| `remoteDeviceUsername` | `string` |
| `remoteDeviceExports` | `string` |
| `pathOnRemoteDevice` | `string` |
| `selectedStandaloneTarget` | `string` |
| `standaloneBuildSubtarget` | `string` |
| `ps4BuildSubtarget` | `string` |
| `ps4HardwareTarget` | `string` |
| `explicitNullChecks` | `bool` |
| `explicitDivideByZeroChecks` | `bool` |
| `explicitArrayBoundsChecks` | `bool` |
| `needSubmissionMaterials` | `bool` |
| `forceInstallation` | `bool` |
| `movePackageToDiscOuterEdge` | `bool` |
| `compressFilesInPackage` | `bool` |
| `buildScriptsOnly` | `bool` |
| `xboxBuildSubtarget` | `string` |
| `streamingInstallLaunchRange` | `int` |
| `xboxOneDeployMethod` | `string` |
| `xboxOneDeployDrive` | `string` |
| `xboxOneAdditionalDebugPorts` | `string` |
| `xboxOneRebootIfDeployFailsAndRetry` | `bool` |
| `androidBuildSubtarget` | `string` |
| `webGLBuildSubtarget` | `string` |
| `androidETC2Fallback` | `string` |
| `androidBuildSystem` | `string` |
| `androidBuildType` | `string` |
| `androidCreateSymbols` | `string` |
| `wsaUWPBuildType` | `string` |
| `wsaUWPSDK` | `string` |
| `wsaMinUWPSDK` | `string` |
| `wsaArchitecture` | `string` |
| `wsaUWPVisualStudioVersion` | `string` |
| `windowsDevicePortalAddress` | `string` |
| `windowsDevicePortalUsername` | `string` |
| `windowsDevicePortalPassword` | `string` |
| `wsaBuildAndRunDeployTarget` | `string` |
| `overrideMaxTextureSize` | `int` |
| `overrideTextureCompression` | `string` |
| `activeBuildTarget` | `string` |
| `development` | `bool` |
| `connectProfiler` | `bool` |
| `buildWithDeepProfilingSupport` | `bool` |
| `allowDebugging` | `bool` |
| `waitForPlayerConnection` | `bool` |
| `exportAsGoogleAndroidProject` | `bool` |
| `buildAppBundle` | `bool` |
| `symlinkSources` | `bool` |
| `iOSXcodeBuildConfig` | `string` |
| `macOSXcodeBuildConfig` | `string` |
| `switchCreateRomFile` | `bool` |
| `switchEnableRomCompression` | `bool` |
| `switchSaveADF` | `bool` |
| `switchRomCompressionType` | `string` |
| `switchRomCompressionLevel` | `int` |
| `switchRomCompressionConfig` | `string` |
| `switchNVNGraphicsDebugger` | `bool` |
| `generateNintendoSwitchShaderInfo` | `bool` |
| `switchNVNShaderDebugging` | `bool` |
| `switchNVNAftermath` | `bool` |
| `switchNVNDrawValidation_Light` | `bool` |
| `switchNVNDrawValidation_Heavy` | `bool` |
| `switchEnableMemoryTracker` | `bool` |
| `switchWaitForMemoryTrackerOnStartup` | `bool` |
| `switchEnableDebugPad` | `bool` |
| `switchRedirectWritesToHostMount` | `bool` |
| `switchHTCSScriptDebugging` | `bool` |
| `switchUseLegacyNvnPoolAllocator` | `bool` |
| `switchEnableUnpublishableErrors` | `bool` |
| `installInBuildFolder` | `bool` |
| `waitForManagedDebugger` | `bool` |
| `managedDebuggerFixedPort` | `int` |

---


## GameObject


### Component.SetProperty

Set a component property value via SerializedProperty

**Parameters:**

| Field | Type |
|---|---|
| `componentInstanceId` | `int` |
| `propertyPath` | `string` |
| `value` | `string` |

**Response:**

| Field | Type |
|---|---|
| `componentInstanceId` | `int` |
| `propertyPath` | `string` |
| `previousValue` | `string` |
| `currentValue` | `string` |

---


### GameObject.AddComponent

Add a component to a GameObject by type name

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `typeName` | `string` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `typeName` | `string` |
| `instanceId` | `int` |
| `enabled` | `bool` |

---


### GameObject.Create

Create a new GameObject in the scene

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `parent` | `string` |
| `components` | `string[]` |

**Response:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `name` | `string` |
| `path` | `string` |
| `isActive` | `bool` |
| `components` | `string[]` |

---


### GameObject.CreatePrimitive

Create a primitive GameObject (Cube, Sphere, Capsule, Cylinder, Plane, Quad)

**Parameters:**

| Field | Type |
|---|---|
| `primitiveType` | `string` |
| `name` | `string` |
| `parent` | `string` |

**Response:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `name` | `string` |
| `path` | `string` |
| `isActive` | `bool` |
| `components` | `string[]` |

---


### GameObject.Destroy

Destroy a GameObject from the scene

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `instanceId` | `int` |

---


### GameObject.Duplicate

Duplicate an existing GameObject in the scene

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `name` | `string` |
| `path` | `string` |
| `isActive` | `bool` |
| `components` | `string[]` |

---


### GameObject.Find

Find GameObjects by name, tag, layer, or component

**Parameters:**

| Field | Type |
|---|---|
| `namePattern` | `string` |
| `tag` | `string` |
| `layer` | `int` (default: `-1`) |
| `requiredComponents` | `string[]` |
| `includeInactive` | `bool` |
| `maxResults` | `int` (default: `100`) |

**Response:**

| Field | Type |
|---|---|
| `results` | `GameObjectResult[]` |
| `totalFound` | `int` |

---


### GameObject.GetComponents

Get detailed component information for a GameObject

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `components` | `ComponentDetail[]` |

---


### GameObject.GetHierarchy

Get the scene hierarchy of GameObjects

**Parameters:**

| Field | Type |
|---|---|
| `includeInactive` | `bool` |
| `maxDepth` | `int` (default: `-1`) |
| `includeComponents` | `bool` (default: `true`) |

**Response:**

| Field | Type |
|---|---|
| `scenes` | `HierarchyScene[]` |

---


### GameObject.RemoveComponent

Remove a component from a GameObject by instance ID

**Parameters:**

| Field | Type |
|---|---|
| `componentInstanceId` | `int` |

**Response:**

| Field | Type |
|---|---|
| `gameObjectName` | `string` |
| `typeName` | `string` |
| `componentInstanceId` | `int` |

---


### GameObject.Rename

Rename a GameObject

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `previousName` | `string` |
| `name` | `string` |
| `instanceId` | `int` |

---


### GameObject.SetActive

Set active state of a GameObject

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `active` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `previousState` | `bool` |
| `currentState` | `bool` |

---


### GameObject.SetParent

Change the parent of a GameObject (or move to root)

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `parentInstanceId` | `int` |
| `parentPath` | `string` |
| `worldPositionStays` | `bool` (default: `true`) |

**Response:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `name` | `string` |
| `path` | `string` |

---


### GameObject.SetTransform

Set the local transform (position, rotation, scale) of a GameObject

**Parameters:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `path` | `string` |
| `position` | `Single[]` |
| `rotation` | `Single[]` |
| `localScale` | `Single[]` |

**Response:**

| Field | Type |
|---|---|
| `instanceId` | `int` |
| `name` | `string` |
| `position` | `Single[]` |
| `rotation` | `Single[]` |
| `localScale` | `Single[]` |

---


## Menu


### Menu.Execute

Execute a Unity Editor menu item by path

**Parameters:**

| Field | Type |
|---|---|
| `menuItemPath` | `string` |

**Response:**

| Field | Type |
|---|---|
| `executed` | `bool` |
| `menuItemPath` | `string` |

---


### Menu.List

List available Unity Editor menu items with filtering

**Parameters:**

| Field | Type |
|---|---|
| `filterText` | `string` |
| `filterType` | `string` (default: `contains`) |
| `maxCount` | `int` (default: `200`) |

**Response:**

| Field | Type |
|---|---|
| `items` | `MenuItemInfo[]` |
| `totalCount` | `int` |
| `filteredCount` | `int` |

---


## Module


### Module.Disable

Disable a module and reload the command dispatcher

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |

**Response:** None

---


### Module.Enable

Enable a module and reload the command dispatcher

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |

**Response:** None

---


### Module.List

List all available modules and their enabled status

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `modules` | `ModuleInfo[]` |

---


## NuGet


### NuGet.AddSource

Add a NuGet package source

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |

---


### NuGet.Install

Install a NuGet package by id and optional version

**Parameters:**

| Field | Type |
|---|---|
| `id` | `string` |
| `version` | `string` |
| `source` | `string` |

**Response:**

| Field | Type |
|---|---|
| `id` | `string` |
| `version` | `string` |

---


### NuGet.List

List all installed NuGet packages

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `packages` | `NuGetPackageEntry[]` |
| `totalCount` | `int` |

---


### NuGet.ListSources

List all configured NuGet package sources

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `sources` | `NuGetSourceEntry[]` |
| `totalCount` | `int` |

---


### NuGet.RemoveSource

Remove a NuGet package source

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |

---


### NuGet.Restore

Restore all NuGet packages from packages.config

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `packageCount` | `int` |

---


### NuGet.Uninstall

Uninstall a NuGet package by id

**Parameters:**

| Field | Type |
|---|---|
| `id` | `string` |

**Response:**

| Field | Type |
|---|---|
| `id` | `string` |

---


## PackageManager


### PackageManager.Add

Add a package by identifier (e.g., com.unity.foo@1.2.3 or git URL)

**Parameters:**

| Field | Type |
|---|---|
| `identifier` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `displayName` | `string` |
| `version` | `string` |
| `source` | `string` |

---


### PackageManager.GetInfo

Get detailed information about a specific installed package

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `displayName` | `string` |
| `version` | `string` |
| `source` | `string` |
| `description` | `string` |
| `isDirectDependency` | `bool` |
| `latestVersion` | `string` |
| `dependencies` | `string[]` |

---


### PackageManager.List

List all installed packages in the project

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `packages` | `PackageEntry[]` |
| `totalCount` | `int` |

---


### PackageManager.Remove

Remove a package by name (e.g., com.unity.cinemachine)

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |

---


### PackageManager.Search

Search for packages in the Unity registry

**Parameters:**

| Field | Type |
|---|---|
| `query` | `string` |

**Response:**

| Field | Type |
|---|---|
| `packages` | `PackageSearchEntry[]` |
| `totalCount` | `int` |

---


### PackageManager.Update

Update a package to a specific version or the latest version

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `version` | `string` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `displayName` | `string` |
| `previousVersion` | `string` |
| `version` | `string` |
| `source` | `string` |

---


## PlayMode


### PlayMode.Enter

Enter play mode in Unity Editor

**Parameters:** None

**Response:** None

---


### PlayMode.Exit

Exit play mode in Unity Editor

**Parameters:** None

**Response:** None

---


### PlayMode.Pause

Toggle pause state in play mode

**Parameters:** None

**Response:** None

---


### PlayMode.Status

Get the current play mode state

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `isPlaying` | `bool` |
| `isPaused` | `bool` |
| `isCompiling` | `bool` |

---


## PlayerSettings


### PlayerSettings.Inspect

Inspect all PlayerSettings values

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `companyName` | `string` |
| `productName` | `string` |
| `colorSpace` | `string` |
| `defaultScreenWidth` | `int` |
| `defaultScreenHeight` | `int` |
| `defaultWebScreenWidth` | `int` |
| `defaultWebScreenHeight` | `int` |
| `defaultIsNativeResolution` | `bool` |
| `macRetinaSupport` | `bool` |
| `runInBackground` | `bool` |
| `captureSingleScreen` | `bool` |
| `usePlayerLog` | `bool` |
| `resizableWindow` | `bool` |
| `resetResolutionOnWindowResize` | `bool` |
| `bakeCollisionMeshes` | `bool` |
| `useMacAppStoreValidation` | `bool` |
| `dedicatedServerOptimizations` | `bool` |
| `fullScreenMode` | `string` |
| `enable360StereoCapture` | `bool` |
| `stereoRenderingPath` | `string` |
| `enableFrameTimingStats` | `bool` |
| `enableOpenGLProfilerGPURecorders` | `bool` |
| `allowHDRDisplaySupport` | `bool` |
| `useHDRDisplay` | `bool` |
| `hdrBitDepth` | `string` |
| `visibleInBackground` | `bool` |
| `allowFullscreenSwitch` | `bool` |
| `forceSingleInstance` | `bool` |
| `useFlipModelSwapchain` | `bool` |
| `openGLRequireES31` | `bool` |
| `openGLRequireES31AEP` | `bool` |
| `openGLRequireES32` | `bool` |
| `spriteBatchVertexThreshold` | `int` |
| `suppressCommonWarnings` | `bool` |
| `allowUnsafeCode` | `bool` |
| `gcIncremental` | `bool` |
| `keystorePass` | `string` |
| `keyaliasPass` | `string` |
| `gpuSkinning` | `bool` |
| `graphicsJobs` | `bool` |
| `graphicsJobMode` | `string` |
| `xboxPIXTextureCapture` | `bool` |
| `xboxEnableAvatar` | `bool` |
| `xboxOneResolution` | `int` |
| `enableInternalProfiler` | `bool` |
| `actionOnDotNetUnhandledException` | `string` |
| `logObjCUncaughtExceptions` | `bool` |
| `enableCrashReportAPI` | `bool` |
| `applicationIdentifier` | `string` |
| `visionOSBundleVersion` | `string` |
| `tvOSBundleVersion` | `string` |
| `bundleVersion` | `string` |
| `statusBarHidden` | `bool` |
| `stripEngineCode` | `bool` |
| `defaultInterfaceOrientation` | `string` |
| `allowedAutorotateToPortrait` | `bool` |
| `allowedAutorotateToPortraitUpsideDown` | `bool` |
| `allowedAutorotateToLandscapeRight` | `bool` |
| `allowedAutorotateToLandscapeLeft` | `bool` |
| `useAnimatedAutorotation` | `bool` |
| `use32BitDisplayBuffer` | `bool` |
| `preserveFramebufferAlpha` | `bool` |
| `stripUnusedMeshComponents` | `bool` |
| `strictShaderVariantMatching` | `bool` |
| `mipStripping` | `bool` |
| `advancedLicense` | `bool` |
| `aotOptions` | `string` |
| `cursorHotspot` | `Vector2` |
| `accelerometerFrequency` | `int` |
| `mTRendering` | `bool` |
| `muteOtherAudioSources` | `bool` |
| `audioSpatialExperience` | `string` |
| `legacyClampBlendShapeWeights` | `bool` |
| `enableMetalAPIValidation` | `bool` |
| `windowsGamepadBackendHint` | `string` |
| `insecureHttpOption` | `string` |
| `vulkanEnableSetSRGBWrite` | `bool` |
| `vulkanNumSwapchainBuffers` | `UInt32` |
| `vulkanEnableLateAcquireNextImage` | `bool` |
| `vulkanEnablePreTransform` | `bool` |
| `android` | `AndroidSettings` |
| `iOS` | `iOSSettings` |
| `embeddedLinux` | `EmbeddedLinuxSettings` |
| `lumin` | `LuminSettings` |
| `macOS` | `macOSSettings` |
| `pS4` | `PS4Settings` |
| `qNX` | `QNXSettings` |
| `splashScreen` | `SplashScreenSettings` |
| `switch` | `SwitchSettings` |
| `tvOS` | `tvOSSettings` |
| `visionOS` | `VisionOSSettings` |
| `webGL` | `WebGLSettings` |
| `wSA` | `WSASettings` |
| `xboxOne` | `XboxOneSettings` |

---


## Profiler


### Profiler.AnalyzeFrames

Analyze recorded frames and return aggregate statistics

**Parameters:**

| Field | Type |
|---|---|
| `startFrame` | `int` (default: `-1`) |
| `endFrame` | `int` (default: `-1`) |
| `topSampleCount` | `int` (default: `10`) |
| `sampleNameFilter` | `string` |

**Response:**

| Field | Type |
|---|---|
| `analyzedFrameCount` | `int` |
| `startFrame` | `int` |
| `endFrame` | `int` |
| `frameTime` | `FrameTimeStats` |
| `gcAlloc` | `GcAllocStats` |
| `topSamples` | `SampleStats[]` |

---


### Profiler.FindSpikes

Find frames exceeding frame time or GC allocation thresholds

**Parameters:**

| Field | Type |
|---|---|
| `startFrame` | `int` (default: `-1`) |
| `endFrame` | `int` (default: `-1`) |
| `frameTimeThresholdMs` | `float` |
| `gcThresholdBytes` | `Int64` |
| `limit` | `int` (default: `20`) |
| `samplesPerFrame` | `int` (default: `5`) |

**Response:**

| Field | Type |
|---|---|
| `searchedFrameCount` | `int` |
| `startFrame` | `int` |
| `endFrame` | `int` |
| `totalSpikeCount` | `int` |
| `spikes` | `SpikeFrame[]` |

---


### Profiler.GetFrameData

Get CPU profiler sample data for a specific frame

**Parameters:**

| Field | Type |
|---|---|
| `frame` | `int` (default: `-1`) |
| `limit` | `int` |

**Response:**

| Field | Type |
|---|---|
| `frameIndex` | `int` |
| `frameTimeMs` | `float` |
| `totalSampleCount` | `int` |
| `samples` | `ProfilerSampleInfo[]` |

---


### Profiler.Inspect

Get profiler status and memory statistics

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `enabled` | `bool` |
| `deepProfiling` | `bool` |
| `profileEditor` | `bool` |
| `firstFrameIndex` | `int` |
| `lastFrameIndex` | `int` |
| `frameCount` | `int` |
| `totalAllocatedMemory` | `Int64` |
| `totalReservedMemory` | `Int64` |
| `monoHeapSize` | `Int64` |
| `monoUsedSize` | `Int64` |
| `graphicsMemory` | `Int64` |

---


### Profiler.LoadProfile

Load profiler data from a .raw file

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |
| `keepExistingData` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `firstFrameIndex` | `int` |
| `lastFrameIndex` | `int` |
| `frameCount` | `int` |

---


### Profiler.SaveProfile

Save profiler data to a .raw file

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `size` | `Int64` |

---


### Profiler.StartRecording

Start profiler recording

**Parameters:**

| Field | Type |
|---|---|
| `deep` | `bool` |
| `editor` | `bool` |
| `keepFrames` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `enabled` | `bool` |
| `deepProfiling` | `bool` |
| `profileEditor` | `bool` |

---


### Profiler.StopRecording

Stop profiler recording

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `firstFrameIndex` | `int` |
| `lastFrameIndex` | `int` |
| `frameCount` | `int` |

---


### Profiler.TakeSnapshot

Take a memory snapshot (.snap file)

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `size` | `Int64` |

---


## Project


### Project.Inspect

Get Unity project information

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `unityVersion` | `string` |
| `projectPath` | `string` |
| `productName` | `string` |
| `companyName` | `string` |
| `buildTarget` | `string` |
| `isPlaying` | `bool` |
| `processId` | `int` |
| `serverId` | `string` |
| `serverVersion` | `string` |
| `startedAt` | `string` |
| `uptimeSeconds` | `double` |

---


## Recorder


### Recorder.StartRecording

Start recording the Game View as a video (requires Play Mode)

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |
| `format` | `string` |
| `width` | `int` |
| `height` | `int` |
| `frameRate` | `float` |
| `quality` | `string` |
| `captureAudio` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `format` | `string` |
| `width` | `int` |
| `height` | `int` |
| `frameRate` | `float` |

---


### Recorder.Status

Get the current recording status

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `isRecording` | `bool` |

---


### Recorder.StopRecording

Stop the current video recording

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `size` | `Int64` |

---


## Remote


### Connection.Connect

Connect to a target player/device by ID, IP address, or device ID

**Parameters:**

| Field | Type |
|---|---|
| `id` | `int` |
| `ip` | `string` |
| `deviceId` | `string` |

**Response:**

| Field | Type |
|---|---|
| `id` | `int` |
| `name` | `string` |
| `directConnectionUrl` | `string` |

---


### Connection.List

List available connection targets (players/devices)

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `targets` | `ConnectionTarget[]` |

---


### Connection.Status

Get current profiler connection status

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `id` | `int` |
| `name` | `string` |
| `directConnectionUrl` | `string` |

---


### Remote.Invoke

Invoke a debug command on connected runtime player

**Parameters:**

| Field | Type |
|---|---|
| `command` | `string` |
| `data` | `string` |
| `playerId` | `int` |

**Response:**

| Field | Type |
|---|---|
| `command` | `string` |
| `success` | `bool` |
| `message` | `string` |
| `data` | `string` |

---


### Remote.List

List debug commands registered on connected runtime player

**Parameters:**

| Field | Type |
|---|---|
| `playerId` | `int` |

**Response:**

| Field | Type |
|---|---|
| `commands` | `RuntimeCommandInfo[]` |

---


## Scene


### Scene.Close

Close a loaded scene via EditorSceneManager

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `sceneIndex` | `int` (default: `-1`) |
| `removeScene` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `removed` | `bool` |

---


### Scene.GetActive

Get the active scene via SceneManager

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `buildIndex` | `int` |
| `isDirty` | `bool` |
| `isLoaded` | `bool` |
| `rootCount` | `int` |

---


### Scene.List

List all loaded scenes via SceneManager

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `scenes` | `SceneInfo[]` |
| `activeSceneName` | `string` |

---


### Scene.New

Create a new scene via EditorSceneManager

**Parameters:**

| Field | Type |
|---|---|
| `empty` | `bool` |
| `additive` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `buildIndex` | `int` |
| `isDirty` | `bool` |
| `isLoaded` | `bool` |
| `rootCount` | `int` |

---


### Scene.Open

Open a scene by asset path via EditorSceneManager

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |
| `additive` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `buildIndex` | `int` |
| `isDirty` | `bool` |
| `isLoaded` | `bool` |
| `rootCount` | `int` |

---


### Scene.Save

Save a scene or all open scenes via EditorSceneManager

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `sceneIndex` | `int` (default: `-1`) |
| `saveAsPath` | `string` |
| `all` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `savedScenePaths` | `string[]` |
| `savedCount` | `int` |

---


### Scene.SetActive

Set the active scene via SceneManager

**Parameters:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `sceneIndex` | `int` (default: `-1`) |

**Response:**

| Field | Type |
|---|---|
| `name` | `string` |
| `path` | `string` |
| `buildIndex` | `int` |
| `isDirty` | `bool` |
| `isLoaded` | `bool` |
| `rootCount` | `int` |

---


## Screenshot


### Screenshot.Capture

Capture a screenshot of the Game View and save as PNG (requires Play Mode)

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |
| `superSize` | `int` |

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `width` | `int` |
| `height` | `int` |
| `size` | `Int64` |

---


## Search


### Search

Search Unity project using Unity Search API

**Parameters:**

| Field | Type |
|---|---|
| `query` | `string` |
| `provider` | `string` |
| `maxResults` | `int` (default: `50`) |
| `includePackages` | `bool` |

**Response:**

| Field | Type |
|---|---|
| `results` | `SearchResultItem[]` |
| `totalCount` | `int` |
| `displayedCount` | `int` |
| `query` | `string` |

---


## Selection


### Selection.Get

Get the current selection in the editor

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `gameObjects` | `SelectedGameObjectInfo[]` |
| `assets` | `SelectedAssetInfo[]` |

---


### Selection.SetAsset

Select an asset by path

**Parameters:**

| Field | Type |
|---|---|
| `assetPath` | `string` |

**Response:**

| Field | Type |
|---|---|
| `assetPath` | `string` |
| `typeName` | `string` |
| `name` | `string` |

---


### Selection.SetAssets

Select multiple assets by paths

**Parameters:**

| Field | Type |
|---|---|
| `assetPaths` | `string[]` |

**Response:**

| Field | Type |
|---|---|
| `selected` | `SelectedAssetInfo[]` |
| `notFound` | `string[]` |

---


### Selection.SetGameObject

Select a GameObject by path

**Parameters:**

| Field | Type |
|---|---|
| `path` | `string` |

**Response:**

| Field | Type |
|---|---|
| `path` | `string` |
| `instanceId` | `int` |

---


### Selection.SetGameObjects

Select multiple GameObjects by paths

**Parameters:**

| Field | Type |
|---|---|
| `paths` | `string[]` |

**Response:**

| Field | Type |
|---|---|
| `selected` | `SelectedGameObjectInfo[]` |
| `notFound` | `string[]` |

---


## TestRunner


### TestRunner.List

List available tests for EditMode or PlayMode

**Parameters:**

| Field | Type |
|---|---|
| `mode` | `string` (default: `EditMode`) |

**Response:**

| Field | Type |
|---|---|
| `mode` | `string` |
| `total` | `int` |
| `tests` | `TestListEntry[]` |

---


### TestRunner.RunEditMode

Run EditMode tests with optional name/assembly filter

**Parameters:**

| Field | Type |
|---|---|
| `testNames` | `string[]` |
| `groupNames` | `string[]` |
| `categories` | `string[]` |
| `assemblies` | `string[]` |
| `resultFilter` | `string` (default: `failures`) |
| `stackTraceLines` | `int` |

**Response:**

| Field | Type |
|---|---|
| `passed` | `int` |
| `failed` | `int` |
| `skipped` | `int` |
| `total` | `int` |
| `results` | `TestResult[]` |

---


### TestRunner.RunPlayMode

Run PlayMode tests with optional name/assembly filter

**Parameters:**

| Field | Type |
|---|---|
| `testNames` | `string[]` |
| `groupNames` | `string[]` |
| `categories` | `string[]` |
| `assemblies` | `string[]` |
| `resultFilter` | `string` (default: `failures`) |
| `stackTraceLines` | `int` |

**Response:**

| Field | Type |
|---|---|
| `passed` | `int` |
| `failed` | `int` |
| `skipped` | `int` |
| `total` | `int` |
| `results` | `TestResult[]` |

---


## Type


### Type.Inspect

Inspect nested types of a given type

**Parameters:**

| Field | Type |
|---|---|
| `typeName` | `string` |

**Response:**

| Field | Type |
|---|---|
| `typeName` | `string` |
| `nestedTypes` | `TypeInspectNestedInfo[]` |
| `count` | `int` |

---


### Type.List

List types derived from a base type or matching a pattern

**Parameters:**

| Field | Type |
|---|---|
| `baseType` | `string` |
| `filter` | `string` |

**Response:**

| Field | Type |
|---|---|
| `types` | `string[]` |
| `count` | `int` |

---


## Window


### Window.Create

Create a new EditorWindow instance by type name

**Parameters:**

| Field | Type |
|---|---|
| `typeName` | `string` |

**Response:**

| Field | Type |
|---|---|
| `typeName` | `string` |
| `instanceId` | `int` |

---


### Window.Focus

Focus an already-open EditorWindow by type name

**Parameters:**

| Field | Type |
|---|---|
| `typeName` | `string` |

**Response:**

| Field | Type |
|---|---|
| `typeName` | `string` |

---


### Window.List

List all available EditorWindow types

**Parameters:** None

**Response:**

| Field | Type |
|---|---|
| `windows` | `WindowInfo[]` |

---


### Window.Open

Open an EditorWindow by type name

**Parameters:**

| Field | Type |
|---|---|
| `typeName` | `string` |

**Response:**

| Field | Type |
|---|---|
| `typeName` | `string` |

---

