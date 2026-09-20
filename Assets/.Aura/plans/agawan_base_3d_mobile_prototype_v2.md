# Agawan Base 3D — Mobile Prototype

## Objective
Ship a top-down 3D tag prototype (1 Blue player + 2 Red AI, freshness-based tagging, prison capture, flag victory) from Unity primitives only, on Android API 24+, at 30 FPS, with an APK under 100 MB.

## Progress At A Glance — 7 / 7 steps built
Verified against the live project on disk this session, not against the old checkmarks.

| Item | State | Live evidence |
|---|---|---|
| Compile | Clean | `check_compile_errors` → "No compile errors" |
| Editor | Live, **in Play mode**, on Title screen | `get_unity_editor_state`: `playMode: true`, active scene `Assets/Scenes/Game.unity` |
| Scene | Complete | Full `Game/...` tree present (Environment, CameraRig, Characters, MatchManager, UI ×4 panels + joystick, EventSystem) |
| Scripts | 10 / 10 present | `Assets/Scripts/{Core,Gameplay,UI}` |
| Prefabs / Materials | 2 prefabs, 5 materials | `BluePlayer`, `RedEnemy`, `Mat_Blue/Red/Ground/Wall/Zone` |
| Build Settings | `Game.unity` only, enabled | `ProjectSettings/EditorBuildSettings.asset` line 8–10 |
| Android artifact | `Builds/AgawanBase.apk` exists | last measured 39,240,409 B (~37.4 MB) vs the 100 MB cap |
| Gaps | 4 open items below | on-device FPS, 60 s allocation profile, `PC_RPAsset` shadows, persisted build target |

## Changes (as shipped)
- `ProjectSettings/ProjectSettings.asset` – landscape-left only, portrait autorotate off, ARM64, min SDK 26 (floor was 24).
- `ProjectSettings/QualitySettings.asset` + `Assets/Settings/Mobile_RPAsset.asset` – real-time shadows off, `Mobile` level active.
- `Assets/Scripts/Core/{Team,BaseZone,CharacterStatus,CharacterMotor}.cs` – core data + locomotion.
- `Assets/Scripts/Gameplay/{PlayerController,EnemyAI,MatchManager}.cs` – input, AI, single rule authority.
- `Assets/Scripts/UI/{VirtualJoystick,GameManager,HudController}.cs` – joystick, state machine, HUD binding.
- `Assets/Scenes/Game.unity` – the whole game (the only enabled build scene; `SampleScene` untouched).
- `Assets/Prefabs/{BluePlayer,RedEnemy}.prefab` – CharacterController + `Body` child (collider stripped).
- `Assets/Materials/Mat_{Blue,Red,Ground,Wall,Zone}.mat` – **5** live materials, referenced by 22 renderer slots in `Game.unity`.

## Relevant Assets and Quirks (facts corrected against the live project)
- Editor **6000.6.0f1**, URP **17.6.0**, Input System **1.20.0**, UGUI **2.6.0**, `com.unity.pipeline 0.7.0-exp.1`.
- `activeInputHandler: 1` → Input System only; legacy `Input.GetKey` throws, so on-screen controls are mandatory. `EventSystem` must carry `InputSystemUIInputModule` bound to `Assets/InputSystem_Actions.inputactions`.
- **`requiresBothOutsideBases` ships `false`** — deliberate: standing in your own base forces `fieldTime = 0`, and 0 always wins a touch. `tieIsNoCapture = true`: exactly equal `fieldTime` is a draw.
- No physics bodies. All detection is distance-based in `MatchManager` (`tagDistance 1.2`, `flagDistance 1.5`, `tieEpsilon 0.01`). `CharacterController` is movement + wall sliding only; colliders are stripped from bases, flags and prisons.
- `GameManager.Awake()` owns `Application.targetFrameRate = 30`, `vSyncCount = 0`, `QualitySettings.shadows = Disable`.
- Blue player spawns at `(-11, 0, -2.5)` — inside the Blue base (radius 4) but off-centre, so it does not sit on the flag pole.
- `DontDestroyOnLoad/AppUIUpdater` in the live tree comes from `com.unity.dt.app-ui` (registered in `EditorBuildSettings.m_configObjects`); the string does not exist anywhere under `Assets/`. Not project code.
- **Corrections to the v1 text:** line 82 said min SDK 24 (actual **26**); line 23 said 4 materials (actual **5**); line 86 named an `interceptRadius` field that does not exist — `EnemyAI` exposes `detectRadius 12`, `giveUpMultiplier 1.4`, `homeStopDistance 0.5`, `guardOffset`.
- **Known deprecation:** `Object.FindFirstObjectByType<T>()` in `EnemyAI.Start()` emits CS0618 on 6000.6.

## Build Order
- [x] 1. Mobile/platform baseline — landscape-left (`defaultScreenOrientation: 3`), autorotate all off except landscape-left, min SDK **26**, ARM64, `Mobile` level active, `shadows: 0` / `shadowDistance: 0` in both quality levels and in `Mobile_RPAsset`. `check_compile_errors` clean. [depends: none]
- [x] 2. Game scene + arena — `Assets/Scenes/Game.unity` with the full `Environment` / `CameraRig` branches; live top-down game-view capture confirms the 32×18 arena framed with **zero shadows**. [depends: 1]
- [x] 3. Core scripts + character prefabs — `BluePlayer.prefab`, `RedEnemy.prefab`; all three characters spawn at `FT=0.00 inBase=True`, `fieldTime` climbs outside base and snaps to `0.00` inside. [depends: 2]
- [x] 4. Input + on-screen joystick — `VirtualJoystick` wired through `InputSystemUIInputModule`; HUD capture shows the stick present. Drag path not yet driven by simulated touch. [depends: 3]
- [x] 5. Enemy AI — reds left base (x=11 → 7.8 / 8.4) and captured the staler Blue player. [depends: 3]
- [x] 6. Match rules + capture — verified end-to-end: blue loses (FT 1.68 vs 0.00) → teleported to `PrisonForBlue` `(13,0,-7.1)` + `GameOver`; red loses (5.00 vs 3.00) → `PrisonForRed` `(-13,0,5.9)`, counter `1/2`; **tie = no capture** (3.00 vs 3.00 for ~3 s, nobody taken); Victory on `Flag_Red` at `(11,0,0)` while free; Pause/Resume toggles `Time.timeScale` 0 ↔ 1. [depends: 4,5]
- [x] 7. Screens, HUD and Android build — Title / HUD (freshness bar + `Captured 0 / 2` + pause) / Pause / End all built with legacy `Text` and persistent `Button.onClick` listeners; Android build **Succeeded**, 0 errors, 630,704 ms; Gradle output `minSdk 26`, `targetSdk 36`, `android:screenOrientation="landscape"`; APK 39,240,409 B. [depends: 6]

## Verify
**Proven**
- Console is clean and the editor is running the game right now on the Title screen.
- Game-view capture shows the arena, both bases with one blue and two reds inside them, both prisons, and the Title overlay reading "AGAWAN BASE / Top-down tag — steal the red flag before they take yours".
- Desktop perf sample: 57 draw calls, 10 setPass, 7,190 triangles, `cpuFrameTimeMs ≈ 2.99`.
- Static allocation review: no LINQ, no `GetComponent`, no reference-type `new` inside `MatchManager.Update` or `EnemyAI.Update`; `GetComponent` is confined to `Awake`/`Start`.

**Measured this run — 60 s sample, `ProfilerRecorder` on `GC.Alloc`, 1,801 frames at 30.0 FPS each time**

| State sampled | project scripts running | avg B/frame | worst frame (B) |
|---|---|---|---|
| Match **Playing**, HUD visible | all | 827,390 | 1,186,600 |
| Title, `matchActive=false`, HUD hidden | `HudController` only | 833,162 | 1,184,700 |
| Title, **all 12 project scripts disabled** | none | 832,624 | 1,134,900 |

The three agree within 0.7 %. The ~830 KB/frame is therefore **Editor-side per-frame overhead that `GC.Alloc` includes when profiling inside the Editor** — it is not attributable to `MatchManager`/`EnemyAI`, and it does not transfer to an Android build.

**Not proven**
- **On-device 30 FPS** — still not measured; no device or emulator was ever attached. The 30.0 FPS above is the Editor honouring `Application.targetFrameRate = 30`, not a device.
- **Gameplay allocation in bytes/frame** — the Editor profile is swamped by the constant above, so it cannot resolve the game's own allocations. A device build with the Profiler attached is required.
- `PC_RPAsset` shadow flags unverified (irrelevant to Android, which selects `Mobile`, but the desktop Editor session ran on `PC_RPAsset`).
- Joystick drag verified by code read + HUD capture only, never by simulated touch.
- `ProjectSettings/EditorUserBuildSettings.asset` is genuinely absent from disk; the Android target is confirmed only through the live API (`EditorUserBuildSettings.activeBuildTarget`).

## Open Items
- [x] 1. APK size and build target re-measured. `Builds/AgawanBase.apk` = **39,240,409 B (37.4 MB)**, written 9/20/2026 04:05:54 — comfortably inside the 100 MB cap. `EditorUserBuildSettings.activeBuildTarget` reads **Android** (live API; the on-disk `.asset` is absent, see Verify). `minSdk = AndroidApiLevel26`, `targetSdk = AndroidApiLevelAuto`, quality level = `Mobile`, `shadows = Disable`, `Application.targetFrameRate = 30`. [depends: none]
- [x] 2. 60 s allocation profile taken — three times, see the Verify table. Result is **measured but not attributable to game code**: a constant ~830 KB/frame appears even with every project script disabled, so it is Editor overhead. Worst GC frame recorded: **1,134,900 B** (quiet control) / **1,186,600 B** (gameplay). [depends: 1]
- [x] 3. Min SDK — no change needed. **26** already satisfies the "API 24+" requirement and the shipped APK was built against it, so nothing is blocked. Dropping to **24** remains a free preference (requires one rebuild); it is not a defect. [depends: none]
- [ ] 4. **Blocked — needs hardware.** On-device 30 FPS cannot be measured without a physical Android device or emulator; none is attached. Not attempted. [depends: 3]
- [x] 5. CS0618 silenced. Reflection against this Editor confirmed `Object.FindFirstObjectByType` **is** `[Obsolete]`: *"has been deprecated because it relies on instance ID ordering. Use FindAnyObjectByType instead."* `EnemyAI.Start()` now calls `Object.FindAnyObjectByType<PlayerController>()` (behaviour-identical — only one player exists). Recompile `status: completed, failed: false, errors: []`; `check_compile_errors` → "No compile errors"; no CS0618 in the console. [depends: none]

## Execution Notes (v1 run)
- All 7 steps were already present on disk when the run started (a parallel run had built them). Nothing was rebuilt or overwritten, so no work was duplicated.
- Wiring confirmed in `Game.unity`: `MatchManager` on `Game/MatchManager` with all 8 references plus a 2-entry `enemies` array, `GameManager` on `Game`, `HudController` on `Game/UI` with all three widgets bound, `EventSystem` on `InputSystemUIInputModule`, 6 persistent button listeners.
- The `MissingReferenceException … 'CharacterStatus' has been destroyed` seen earlier came from a throwaway test harness (`PROBE7:`) that cached a character reference across its own scene reload; `PROBE7` exists nowhere under `Assets/`. The harness was stopped.
- Tooling: the MCP-for-Unity bridge stayed unusable all session (`"No Unity Editor instances found."` on every editor-op call, while `query_project_assets` / `check_compile_errors` still route). Worked around with Aura's `execute_script` plus the `unity` CLI and the `com.unity.pipeline` server on port 7800. Bridge is **still broken right now** — `manage_scene action=get_build_settings` fails the same way.
- Cleanup done: `AgentScripts/`, `AuraPipelineStart.cs` and the IL2CPP intermediates folder removed. All 5 materials were **kept** — they are live dependencies across 22 renderer slots.
- Do not quote the BuildReport's `totalSizeBytes = 767,225,249` as the APK size; it sums IL2CPP staging intermediates (~19.6× the artifact). The real APK is `Builds/AgawanBase.apk`.

## Execution Notes (v2 run)
- **Root cause of "the game looks frozen": the Editor was PAUSED.** `EditorApplication.isPaused` read `True` while `isPlaying` was also `True`, so `Time.frameCount` sat at 17989 and two 60 s samples recorded **zero** frames. Clearing it moved `frameCount` 17989 → 18197 and `Time.time` to 615.82. Nothing was wrong with the project — but any play-mode verification must check `isPaused` before concluding a feature is broken.
- **Tooling that actually works this run:** the `unity` CLI / `com.unity.pipeline` server (port 7800) with `unity command run_script --file <path> --entry <method> --args "[...]"`, plus `editor_play` / `editor_stop` / `recompile_status`. Still broken: every `mcpforunity` tool (`manage_*`, `execute_code`, `unity_reflect`) → `"No Unity Editor instances found."`, and Aura's own `execute_script`, which fails to compile with `Could not find any resources appropriate for the specified culture … Microsoft.CodeAnalysis.CSharp.…CSharpResources.resources … assembly "Aura-v1.3.0"`. `query_project_assets` and `check_compile_errors` do route.
- **`run_script` gotcha:** each invocation compiles the file into a *fresh* assembly, so static fields do **not** survive between calls. The 60 s sampler had to write its progress to a file on disk (`AgentScripts/planprobe-state.txt`) rather than expose it through a field. `--args` must be a JSON **array** (`"[false]"`), not a bare value.
- `Application.runInBackground = true` plus `EditorApplication.QueuePlayerLoopUpdate()` were used to keep frames advancing; `Time.frameCount` still only moves once the Editor is unpaused.
- Temporary harness `AgentScripts/` (probe + state file) has been **deleted**. The only project change made this run is the one-line `EnemyAI.Start()` fix above.
- Probe `SetGameplay()`, used for the quiet control, was toggled off and back on in the same run; the scene was re-read afterwards and `HudController.enabled=True` with `gameState=Title` / `matchActive=False`, i.e. no lasting mutation.

## Verification (v2 run)
Verified in-editor with a game-view capture and a code audit. **VERDICT: working.**
- `compiles_cleanly=pass` — "No compile errors"; the `FindAnyObjectByType` edit at `EnemyAI.cs:47` is present and warning-free.
- `play_mode_title_screen=pass` · `panel_hud_hidden=pass` · `arena_visible=pass` — capture shows the 32×18 arena, both circular bases with one blue in the left and two reds in the right, both prison squares, and the "AGAWAN BASE / Top-down tag — steal the red flag before they take yours" overlay with START. No HUD or joystick drawn, i.e. `Panel_Title` up and `Panel_Hud` down.
- `no_realtime_shadows=pass` — `shadows: 0` and `shadowDistance: 0` in **both** quality levels.
- `no_per_frame_allocations_in_update=pass` — no LINQ, no `GetComponent`, no reference-type `new` in `MatchManager.Update` (95–132) or `EnemyAI.Update` (52–104); only value-type `Vector2`/`Vector3`. Project-wide, every `GetComponent` sits in `Awake`/`Start`/init (`CharacterMotor:28`, `EnemyAI:38–48`, `PlayerController:23–24`, `VirtualJoystick:32`); `CharacterStatus:66,82` are in event-driven `Capture`/`Release`.
- `target_frame_rate_30=pass` (`GameManager.cs:46,65`) · `min_sdk_26=pass` (`ProjectSettings.asset:183`).
- `apk_under_100mb=untested` · `active_build_target_android=untested` **by the verifier** — it has no shell/file tool and `EditorUserBuildSettings.asset` is absent from disk. Both were measured directly this run instead: APK **39,240,409 B** and `activeBuildTarget = Android` (live API). No repository-wide `activeBuildTarget` value exists under `ProjectSettings/`.
- `on_device_30fps=untested` — no device or emulator.
