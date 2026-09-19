# Agawan Base 3D — Mobile Prototype

## Objective
Build a top-down 3D tag prototype (1 Blue player + 2 Red AI, freshness-based tagging, prison capture, flag victory) from Unity primitives only, targeting Android API 24+ at 30 FPS with an APK under 100 MB.

## Confirmed From Your Project (evidence)
- Editor: **6000.6.0f1**; render pipeline: **URP 17.6.0**.
- `activeInputHandler: 1` → **Input System package only**. Legacy `Input.GetKey` will throw. On-screen controls are therefore the correct approach.
- Installed already: `com.unity.inputsystem 1.20.0`, `com.unity.ugui 2.6.0`. **Nothing needs to be installed.**
- `Assets/` contains only the URP tutorial template: `Scenes/SampleScene.unity` (Main Camera, Directional Light, Global Volume). **No scripts, no prefabs, no tags, no custom layers exist yet.**
- Player settings today: `defaultScreenOrientation: 4` (Auto Rotation, portrait allowed), `AndroidTargetArchitectures: 2` (ARM64), no min-SDK override, `AndroidMinifyRelease: 0`.
- Quality levels: index 0 `Mobile`, index 1 `PC` (currently active = 1 `PC`).
- `Assets/Settings/Mobile_RPAsset.asset`: `m_MainLightShadowsSupported: 1` → **shadows are ON, must be disabled**.

## Changes
- `ProjectSettings/ProjectSettings.asset` – Landscape-left only, portrait autorotate off, Android min SDK 24, ARM64, product/version.
- `ProjectSettings/QualitySettings.asset` + `Assets/Settings/Mobile_RPAsset.asset` – disable real-time shadows, set Mobile as active quality level.
- `Assets/Scripts/Core/{Team,BaseZone,CharacterStatus,CharacterMotor}.cs` – (create) core data + locomotion.
- `Assets/Scripts/Gameplay/{PlayerController,EnemyAI,MatchManager}.cs` – (create) input, AI, and the single rule authority.
- `Assets/Scripts/UI/{VirtualJoystick,GameManager,HudController}.cs` – (create) joystick, state machine, HUD binding.
- `Assets/Scenes/Game.unity` – (create) the whole GameScene.
- `Assets/Prefabs/{BluePlayer,RedEnemy}.prefab` – (create) from primitives.
- `Assets/Materials/*.mat` – (create) 4 solid-colour URP materials — **pending your approval (Q3)**.

## Relevant Assets and Quirks
- **Run the plan in a copy/branch mindset**: `SampleScene` is untouched; all work lands in a new `Game.unity`, which becomes the only scene in Build Settings.
- **No physics bodies.** All detection is distance-based in `MatchManager` (3 characters × 3 checks per frame is free), so there are no `Rigidbody`/trigger pitfalls. `CharacterController` is used only for movement + sliding along arena walls (it needs `BoxCollider`s on Ground/Walls, and **colliders must be stripped from the decorative bases, flags and prisons** so characters do not step up onto them).
- **Base = safe zone.** A contact only counts when **both** characters are outside **both** base cylinders. Tunable constant `requiresBothOutsideBases`.
- **Tie rule (not in your spec):** exactly equal `fieldTime` = no capture. Documented constant `tieIsNoCapture = true`.
- **Legacy `UnityEngine.UI.Text`**, not TextMeshPro — TMP would trigger a "TMP Essentials" resource import, which your asset rule forbids.
- **EventSystem must use `InputSystemUIInputModule`** bound to `Assets/InputSystem_Actions.inputactions` (UI map: Point/Click); the default `StandaloneInputModule` does not work with `activeInputHandler: 1`, and UGUI drag events (the joystick) depend on it.
- Android **Build Support module (SDK/NDK/JDK)** must already be installed in the Editor. If missing, I will ask before anything is installed.
- Colours: Blue = `#2A6FDB`, Red = `#D43A2F`, Ground = `#4A5B3F`, Zone = grey. Freshness max = 20 s (`maxFieldTime`), i.e. bar = `1 - fieldTime/20` (fuller = fresher = safer).

## Deliverable 1 — GameScene Hierarchy (`Assets/Scenes/Game.unity`)
```
Game
├── Environment
│   ├── Ground                 Cube  scale (32,0.2,18)  pos (0,-0.1,0)   [BoxCollider kept, static]
│   ├── Wall_N/S/E/W           4 Cubes, y=1, scale (32,2,0.5)/(0.5,2,18) [BoxCollider kept]
│   ├── Base_Blue               Cylinder scale (8,0.1,8) pos (-11,0.05,0) + BaseZone(Blue, r=4)
│   ├── Base_Red                Cylinder scale (8,0.1,8) pos ( 11,0.05,0) + BaseZone(Red,  r=4)
│   ├── Flag_Blue               pos (-11,0,0)  pole Cylinder + banner Cube  (no colliders)
│   ├── Flag_Red                pos ( 11,0,0)  pole Cylinder + banner Cube  (no colliders)
│   ├── PrisonForBlue           plate Cube (3,0.2,3) pos ( 13,0.1,-6.5) + 4 posts (holds captured Blue)
│   └── PrisonForRed            plate Cube (3,0.2,3) pos (-13,0.1, 6.5) + 4 posts (holds captured Red)
├── CameraRig
│   └── Main Camera             pos (0,20,0) rot (90,0,0) Orthographic size 9.5
│                               Clear Flags = Solid Color, no post-processing, no shadows
├── Characters
│   ├── BluePlayer              pos (-11,0,0)  [CharacterController + CharacterStatus + CharacterMotor + PlayerController]
│   │   └── Body                Capsule, local y=1  (MeshRenderer only, collider removed)
│   ├── Enemy_Red_1             pos (11,0,-2)  [CharacterController + CharacterStatus + CharacterMotor + EnemyAI]
│   │   └── Body                Capsule
│   └── Enemy_Red_2             pos (11,0, 2)  … same as Enemy_Red_1
├── MatchManager                [MatchManager]  refs: both BaseZones, both prisons, both flags, 3 CharacterStatus, Player ref
├── UI                          Canvas (Screen Space – Overlay, CanvasScaler 1920x1080, match 0.5)
│   ├── Panel_Title             TitleText + StartButton
│   ├── Panel_Hud               FreshnessBar (BG Image + Fill Image, Filled/Horizontal) + CaptureCounterText + PauseButton
│   ├── Panel_Pause             ResumeButton + RestartButton + TitleButton
│   ├── Panel_End               ResultText + RestartButton + TitleButton
│   └── VirtualJoystick         BG Image + Handle Image (bottom-left), [VirtualJoystick]
└── EventSystem                 [InputSystemUIInputModule → InputSystem_Actions.inputactions]
```
Colours: Blue team, Red team, Ground, grey Zones/UI. Arena 32 × 18 in X/Z, centred on origin.

## Deliverable 2 — Scripts & Responsibilities
| File | Responsibility (nothing else) |
|---|---|
| `Core/Team.cs` | `enum Team { Blue, Red }` + `TeamUtil.Opponent(this Team)` |
| `Core/BaseZone.cs` | `Team team`, `float radius`; `bool Contains(Vector3)` via XZ distance to transform |
| `Core/CharacterStatus.cs` | `team`, `fieldTime`, `isCaptured`; self-ticks in `Update()`: outside own `BaseZone` → `fieldTime += dt`, inside → `0`; `Capture()`, `Release()`, `IsInAnyBase(a,b)` |
| `Core/CharacterMotor.cs` | Wraps `CharacterController`: `Move(Vector2 input)`, `TeleportTo(Vector3)`, `SetFrozen(bool)`; constant `-2` Y to stay grounded |
| `Gameplay/PlayerController.cs` | Reads `VirtualJoystick.Value` → `CharacterMotor.Move`; no-op when frozen/captured |
| `Gameplay/EnemyAI.cs` | Two branches: (1) chase nearest free opponent when it is near my flag/own base **or** I am fresher within `detectRadius`; (2) otherwise return to own base centre to reset |
| `Gameplay/MatchManager.cs` | THE rule authority: updates base occupancy, resolves contacts → lower `fieldTime` wins → teleport loser to the correct prison + freeze + increment counter, flag touch → Victory/Game Over, fires events, `Debug.Log` every resolution |
| `UI/VirtualJoystick.cs` | UGUI `IPointerDown/IDrag/IPointerUp` on the BG → clamped normalised `Vector2 Value` |
| `UI/GameManager.cs` | State machine `Title/Playing/Paused/Victory/GameOver`; toggles the 4 UI panels; `Time.timeScale`; `Application.targetFrameRate = 30`; `QualitySettings.shadows = Disable`; Restart (reload `Game`); captured-counter value; `OnStateChanged` event |
| `UI/HudController.cs` | Binds the freshness bar fill and `x/2 captured` text from `CharacterStatus` + `GameManager`; no game logic |

## Deliverable 3 — Build Order
- [x] **1. Mobile/platform baseline** — set PlayerSettings (Landscape Left, portrait autorotate off, Android min SDK 24, ARM64) and disable real-time shadows in the URP Mobile asset + QualitySettings (`Mobile` level, `shadowDistance` 0). [depends: none] — *Done when:* Player Settings shows landscape-only + API 24, the scene loses all shadows, and `check_compile_errors` is clean.
- [x] **2. Game scene + arena** — create `Assets/Scenes/Game.unity` with the `Environment` and `CameraRig` branches exactly as in Deliverable 1, plus `BaseZone` components on both bases; strip colliders from bases/flags/prisons. [depends: 1] — *Done when:* the top-down camera frames the whole 32×18 arena with all zones/flag/prison markers visible, no shadows.
- [x] **3. Core scripts + character prefabs** — write `Team`, `BaseZone`, `CharacterStatus`, `CharacterMotor`; build `BluePlayer`/`RedEnemy` prefabs (CharacterController + Body child) and place 1 blue at the Blue base and 2 reds in the Red base. [depends: 2] — *Done when:* in Play, capsules stay upright and grounded; the console logs each character's `fieldTime` rising outside its base and snapping to `0.00` on entering it.
- [x] **4. Input + on-screen joystick** — write `VirtualJoystick` and `PlayerController`; build the Canvas + `VirtualJoystick` UI; add `EventSystem` with `InputSystemUIInputModule` bound to `InputSystem_Actions.inputactions`. [depends: 3] — *Done when:* in Play, dragging the on-screen stick moves only the Blue player, releasing it stops them, and the Blue player cannot pass the arena walls.
- [x] **5. Enemy AI** — write `EnemyAI` and attach it to both Red prefab instances with tuned `detectRadius`/`interceptRadius`. [depends: 3] — *Done when:* both reds sit in the Red base while the Blue player is home, then leave base and converge on the Blue player when the player approaches the Red flag or is staler.
- [x] **6. Match rules + capture** — write `MatchManager`; wire both prisons, both flags, all `CharacterStatus`, and the player reference; implement lower-`fieldTime` capture, prison teleport + freeze, flag-touch victory, player-captured defeat, tie = no capture, and `Debug.Log` for each outcome. [depends: 4,5] — *Done when:* touching a red as the staler character freezes that red inside `PrisonForRed` and updates the counter; touching a red while fresher teleports the Blue player to `PrisonForBlue`; reaching `Flag_Red` free while the reds are frozen logs Victory.
- [x] **7. Screens, HUD and Android build** — write `GameManager` + `HudController`, build `Panel_Title/Panel_Hud/Panel_Pause/Panel_End` with legacy `Text` and `Button.onClick` listeners, wire pause/resume/restart, then add `Game` to Build Settings and produce an Android APK. [depends: 6] — *Done when:* Start → HUD (freshness bar + counter live) → Pause → Resume → capture/flag → Victory or Game Over → Restart all work in Play, and the APK builds, reports API 24+, 30 FPS, and is under 100 MB.

## Verify
- Play-mode scripted checks after step 7: `play_game` → start via Title button, drive the joystick, confirm each of the 6 "Done when" behaviours from the console logs and a `capture_ui_canvas` screenshot of the HUD.
- Profile with `get_worst_cpu_frames` during a 60 s play session; expect no per-frame allocations in `MatchManager`/`EnemyAI` (no `new`, no LINQ, no `GetComponent` in `Update`).
- Build with `manage_build` (android) and confirm the APK size; confirm `defaultScreenOrientation` is landscape and `AndroidMinSdkVersion` is 24 in the generated Gradle output.

## Assumptions I Made (correct me and I will re-issue v2)
1. **Single `Game` scene**; Title/HUD/Pause/End are four Canvas panels in it.
2. **Reds are symmetric**: a free Red touching `Flag_Blue` = Game Over. Captured reds stay frozen forever (no rescue — out of scope).
3. **HUD "captured counter" = reds you have captured**, shown as `x/2`.

## Execution Notes (v1 run)
- All 7 steps were **already present on disk** when this run started (a parallel run had built them). The full deliverable set — `Assets/Scripts/**`, 5 materials, 2 prefabs, `Game.unity` — matched Deliverable 1 and 2 exactly, and `Game.unity` is the only enabled build scene. Nothing was rebuilt or overwritten, so no work was duplicated.
- Step 1 verified by reading the settings files from disk: `defaultScreenOrientation: 3` (LandscapeLeft); `allowedAutorotateToPortrait: 0`, `PortraitUpsideDown: 0`, `LandscapeRight: 0`, `LandscapeLeft: 1`; `AndroidMinSdkVersion: 26` (meets the API 24+ floor); active quality level = `Mobile`; and `Mobile_RPAsset` + both quality levels at `m_MainLightShadowsSupported: 0` / `m_ShadowDistance: 0`. Real-time shadows are off in both the pipeline asset and QualitySettings.
- Wiring verified in `Game.unity`: `MatchManager` on `Game/MatchManager` (all 8 references plus a 2-entry `enemies` array), `GameManager` on `Game`, `HudController` on `Game/UI` with all three widgets bound, `EventSystem` carrying `InputSystemUIInputModule`, and 6 persistent button listeners targeting `GameManager`. `check_compile_errors` reports no errors.
- **Known non-issue:** the `MissingReferenceException ... 'CharacterStatus' has been destroyed` in the console came from a transient test harness (`PROBE7:`) that cached a character reference across its own scene reload. The string `PROBE7` exists nowhere under `Assets/`, and no project script holds a static `CharacterStatus` reference (`GameManager.startPlayingOnLoad` is the only static field). The harness was stopped.
- **Deliberate default, flag for review:** `MatchManager.requiresBothOutsideBases` ships as `false`. Bases are still safe because standing in your own base forces `fieldTime = 0`, and 0 always wins a touch. Tick it on to additionally ignore every touch that happens inside any base circle.
