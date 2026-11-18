# AgawanBase Project Hardening

## Objective
Clear the seven non-audio structural problems found in the project audit so new gameplay features can be added safely.

## Changes
- `Packages/manifest.json` - remove `com.unity.ai.inference`, `com.unity.timeline`, `com.unity.visualscripting`, `com.unity.pipeline`
- `Assets/Readme.asset`, `Assets/TutorialInfo/` - (delete) template readme demo, referenced by nothing
- `ProjectSettings/TagManager.asset` - add layers `Ground` (3), `Environment` (6), `Characters` (7), `Interactable` (8) and tag `Player`
- `Assets/Scenes/Game.unity`, `Assets/Prefabs/BluePlayer.prefab`, `Assets/Prefabs/RedEnemy.prefab` - layer assignment; `Player` tag on the Blue player
- `ProjectSettings/DynamicsManager.asset` - collision matrix for the new layers
- `Assets/Scripts/UI/HudController.cs` - remove per-frame string allocation in `UpdateFreshness`, `UpdateStamina`, `UpdateTeamStatus`
- `Assets/Scripts/AgawanBase.Runtime.asmdef` - (create) runtime assembly, references `Unity.InputSystem` + `UnityEngine.UI`
- `Assets/Editor/AgawanBase.Editor.asmdef` - (create) editor assembly, references `AgawanBase.Runtime`, editor-only
- `Assets/Tests/EditMode/AgawanBase.Tests.EditMode.asmdef` + `TeamRulesTests.cs` - (create) 3 EditMode smoke tests
- `Assets/Scripts/Gameplay/EnemyAI.cs` - becomes `public partial class EnemyAI`, core only
- `Assets/Scripts/Gameplay/EnemyAI.Scoring.cs`, `EnemyAI.Actions.cs`, `EnemyAI.Perception.cs` - (create) same class, split by concern
- `Assets/Settings/Build Profiles/Android™.asset` - release output switched from APK to AAB

## Relevant Assets and Quirks
- Engine/versions: Unity `6000.6.0f1`, URP `17.6.0`, Input System `1.20.0`, uGUI `2.6.0`, test-framework `1.8.0`. Input handling is Input-System-only (`activeInputHandler: 1`).
- **Three assets are NOT leftovers, despite looking like it — never delete them:**
  - `Assets/shandyicon.jpg` is the app icon, referenced 6× in `ProjectSettings/ProjectSettings.asset` (`m_Icon` at :308, icon arrays :413-428).
  - `Assets/InputSystem_Actions.inputactions` is used by the scene's `InputSystemUIInputModule` (`Game.unity:16263-16273`) and registered in `ProjectSettings.asset:154` / `EditorBuildSettings.asset:13`.
  - `Assets/Settings/SampleSceneProfile.asset` is the `m_VolumeProfile` of BOTH `Mobile_RPAsset.asset:95` and `PC_RPAsset.asset:95`.
- Only `Assets/Readme.asset` + `Assets/TutorialInfo/` (Readme.cs, Editor/ReadmeEditor.cs, USS, URP.png) are safe to delete; no scene or code references them.
- Layers 0-5 are Unity defaults (5 = `UI`); 3 is free, 6-31 empty. Quality level 0 `Mobile` (→ `Mobile_RPAsset`, shadows off, render scale 0.8) is the Android default; the PC level is excluded from Android.
- Enter-play-mode has domain reload disabled (`m_EnterPlayModeOptions: 3`), so any static state added later must be reset in `Awake` — `TeamManager.Awake` already does this.
- Runtime scripts use `UnityEngine.InputSystem` (MatchManager, PlayerController, FollowCamera) and `UnityEngine.UI` (6 UI scripts), so the runtime asmdef must reference both.
- Partial-class split is safe: Unity serializes `[SerializeField]` fields across partial files of one type — keep every serialized field and `AiState` in `EnemyAI.cs`.
- `AndroidTargetSdkVersion: 0` means "highest installed SDK" (correct for Play); the only shipping change needed is AAB output.

## Steps
- [x] 1. Remove the four unused packages with `remove_unity_package`, then delete `Assets/Readme.asset` and `Assets/TutorialInfo/` (`manage_asset` delete) [depends: none]
- [x] 2. Add layers `Ground`=3, `Environment`=6, `Characters`=7, `Interactable`=8 and tag `Player` via `execute_script` (`TagManager` / `manage_editor` add_layer/add_tag) [depends: none]
- [x] 3. Assign the new layers in bulk with `execute_script` — `Ground`/court slabs → Ground, other `Game/Environment/*` → Environment, character roots (scene + both prefabs) → Characters, `Flag_*`/`Base_*`/`Prison*` → Interactable, `Game/UI/*` → `UI`; tag the Blue player `Player`; then set the collision matrix (Characters ↔ Ground/Environment on, Interactable ↔ Characters off) and `Debug.Log` the per-layer object counts [depends: 2]
- [x] 4. In `HudController.cs` cache the last formatted value in `UpdateFreshness` (tenths of `fieldTime`), `UpdateStamina` (percent int + sprint/captured flag) and `UpdateTeamStatus` (free count + flag state, with static `"BLUE"`/`"RED"` strings), rebuilding a label string only when its value changes [depends: none]
- [x] 5. Create `AgawanBase.Runtime.asmdef` (references `Unity.InputSystem`, `UnityEngine.UI`) and `AgawanBase.Editor.asmdef` (Editor-only, references `AgawanBase.Runtime`), then add `Assets/Tests/EditMode/` with an asmdef referencing `AgawanBase.Runtime` + TestRunner and 3 tests: `TeamUtil.Opponent` symmetry, `TeamUtil.All` order, `CharacterStatus.Freshness01` clamping; run them with `run_tests` [depends: 1,4]
- [x] 6. Split `EnemyAI.cs` into `public partial class EnemyAI` + `EnemyAI.Scoring.cs` (`Score*`, `Consider`, `RoleBonus`, `BestAction`), `EnemyAI.Actions.cs` (`Act` + the state actions), `EnemyAI.Perception.cs` (`PickTarget`, `RefreshFacts`, flat-math helpers), moving code verbatim [depends: 5]
- [x] 7. Set `EditorUserBuildSettings.buildAppBundle = true` via `execute_script`, rebuild Android to `Builds/AgawanBase.aab` with `manage_build` and confirm the build succeeds with 0 errors [depends: 1]

## Verify
- `check_compile_errors` clean and `read_console` shows no new errors/warnings after steps 4-6; the Sentis `Hidden/Sentis/*` shader warnings disappear from the next build after step 1.
- `run_tests` (EditMode) reports 3/3 passing.
- `get_tags_and_layers` lists Ground/Environment/Characters/Interactable and the `Player` tag; spot-check `Game/Environment/Wall_N`, `Game/Characters/BluePlayer`, `Game/Environment/Flag_Red`, `Game/UI` each carry the expected layer.
- `unity_verification_agent`: play a match in `Assets/Scenes/Game.unity` — tag a Red, get captured, get rescued, steal and return the flag, and win — with zero console errors, and confirm the HUD freshness/stamina/team labels still update correctly (values change, no missing text).
- Android build reports `Build Succeeded` with 0 errors and `Builds/AgawanBase.aab` exists; APK size stays near the current 37.4 MB.
