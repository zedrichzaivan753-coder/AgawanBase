# Agawan Base — Three-Map Expansion and Map Selection

## Objective
Add two new 64 × 40 m playable maps (Palengke, School Campus) beside the preserved Classic Open Field, plus a map-selection screen, per-map configuration, and NavMesh path-following for the existing AI so obstacles do not break it.

## Changes
- `Assets/Scripts/Core/MapSettings.cs` - (create) selected map id, PlayerPrefs persistence, default Classic, mirrors MatchSettings.
- `Assets/Scripts/Core/MapDefinition.cs` - (create) ScriptableObject: id, name, description, sceneName, preview sprite, arena center/size.
- `Assets/Config/Map_Classic.asset`, `Map_Palengke.asset`, `Map_SchoolCampus.asset` - (create) the three MapDefinitions.
- `Assets/Scripts/Gameplay/MapNavigator.cs` - (create) NavMesh path-follower: `Vector2 DirectionTo(Vector3 target)`, falls back to straight line when no NavMesh is baked.
- `Assets/Scripts/Gameplay/EnemyAI.Actions.cs` - route every walk through one `WalkTo(Vector3)` helper instead of 9 inline `motor.Move(FlatDirection(...))` calls.
- `Assets/Scripts/UI/GameManager.cs` - add `GameState.MapSelect`, `panelMapSelect`, and `LoadMap(MapDefinition)`; Restart/Title use the selected map's scene name.
- `Assets/Scripts/UI/MapSelectUI.cs` - (create) three cards, selected highlight, START MATCH, BACK.
- `Assets/Editor/MapBuild.cs` - (create) scene factory (clone, strip, rebuild bases, rebind references, frame camera, NavMeshSurface) + shared primitive helpers.
- `Assets/Editor/PalengkeBuild.cs`, `Assets/Editor/SchoolCampusBuild.cs` - (create) the two map generators.
- `Assets/Scenes/Map_Palengke.unity`, `Assets/Scenes/Map_SchoolCampus.unity` - (create).
- `ProjectSettings/EditorBuildSettings.asset` - add both scenes (Game.unity stays index 0).

## Relevant Assets and Quirks
- Arena now: `Ground` 35.5 × 20 m at (0,−0.1,−1); `Base_Blue` (−11,0,0) r=3.2, `Base_Red` (+11,0,0); `PrisonForBlue` (13,0,−6.5); camera ortho, `FollowCamera.arenaSize` (34.5,19), `visibleWidthPercent` 0.65 → 22.4 m visible width.
- **AI has no pathfinding.** Every state does `motor.Move(FlatDirection(pos,target))`; `CharacterMotor` is the only mover (CharacterController). On an obstacle-dense map this walks AI into walls — hence MapNavigator.
- **Blue must stay west, Red east**: `MatchManager.RescueExit`/`PrisonSpot` pick the exit by x-sign toward home, and `Intercept` assumes the road home.
- `MatchManager` holds `blueBase`/`redBase`/`prisons`/`flags`/`player`; `CharacterSpawner` holds `baseBlue`/`baseRed`/`flagBlue`/`flagRed`/`aiPrefab`; `GameManager` holds 5 panels + both flags. Deleting `Game/Environment` nulls all of them — a rebind pass is mandatory.
- `CharacterSpawner` spawns AI on a 2 × 2 formation at `slotOffset` 1.1 m around the base centre: keep a ≥ 8 m clear disc at each base.
- `FollowCamera` frame is derived, so per map set `arenaCenter`/`arenaSize` (64,40) and `visibleWidthPercent` ≈ 0.35 to keep characters the same apparent size.
- `com.unity.ai.navigation` 2.0.14 is installed; no NavMesh exists yet. Bake is editor-side (`NavMeshSurface.BuildNavMesh`).
- Domain reload is off on play — any new static must reset in `Awake`.
- Runtime scripts live under `Assets/Scripts` (asmdef `AgawanBase.Runtime`); editor builders under `Assets/Editor` (asmdef `AgawanBase.Editor`).
- Shadows are disabled globally (`GameManager.Awake`); every environment object must be static and share the existing `Mat_*` palette.
- Never delete `Assets/shandyicon.jpg`, `Assets/InputSystem_Actions.inputactions`, `Assets/Settings/SampleSceneProfile.asset`.

## Steps
- [x] 1. Create `MapDefinition.cs`, `MapSettings.cs` (PlayerPrefs key `map.selected`, default `classic`, `Debug.Log` on every change) and the three `Assets/Config/Map_*.asset` entries via `create_script`/`manage_scriptable_object`. [depends: none]
- [x] 2. Create `MapNavigator.cs` (NavMeshPath, recompute every 0.3 s or after 1 m target drift, corner look-ahead 0.6 m, `Debug.LogWarning` once if no NavMesh) and add a `WalkTo(Vector3)` helper to `EnemyAI.Actions.cs`, replacing all nine `motor.Move(FlatDirection(...))` call sites with it; verify compile via `check_compile_errors`. [depends: none]
- [x] 3. Create `Assets/Editor/MapBuild.cs`: clone `Game.unity` to a target path, delete `Game/Environment`, rebuild `Ground`/`Base_Blue`/`Base_Red`/`Flag_Blue`/`Flag_Red`/`PrisonForBlue`/`PrisonForRed` with the same names on layers Ground/Interactable, rebind every MatchManager/CharacterSpawner/GameManager/HUD/camera reference by name, set `FollowCamera` framing, add `NavMeshSurface` to the Environment root, save. Add `NavMeshSurface` + rebake to `Game.unity` the same way. [depends: 1,2]
- [x] 4. Create `Assets/Editor/PalengkeBuild.cs` and generate `Map_Palengke.unity`: two base plazas (Blue −26, Red +26), 4 stall rows with 2.5–3 m aisles, dry-goods/sari-sari blocks, crates, jeepney/tricycle delivery area, perimeter walls with two flank routes; every solid gets a BoxCollider, decorations do not. [depends: 3]
- [x] 5. Generate `Assets/Scenes/Map_SchoolCampus.unity` from `SchoolCampusBuild.cs`: classroom blocks with 3 m corridors and open doorways, covered basketball court, canteen with tables, garden, gate/guardhouse/perimeter fence, two routes into each half. [depends: 3]
- [x] 6. Generate hero props with `generate_model_from_text` (palengke stall, produce crate stack, tricycle, basketball hoop, school bench; `face_limit` ≤ 3000, `pbr` false), place with `place_asset_in_scene`, add BoxCollider + layer Environment, then bake both NavMeshes and add both scenes to the build settings. [depends: 4,5]
- [x] 7. Add `GameState.MapSelect` + `panelMapSelect` to `GameManager`, build the panel with `create_ui_element`/`set_ui_text`/`set_rect_transform` (3 cards with real in-engine preview captures, START MATCH, BACK) and create `MapSelectUI.cs` that writes `MapSettings` and calls `GameManager.LoadMap`; wire Title → map select → match setup. [depends: 1]
- [x] 8. Verify: `run_tests` EditMode must stay 3/3; then `unity_verification_agent` plays each of the three maps (spawn, AI movement around obstacles, flag steal, capture, victory) with zero console errors. [depends: 6,7]
  - note (2026-11-18): done. School campus first came back 5 of 6 routable - `Base_Blue -> PrisonForRed` was `PathPartial` because the red cage sat INSIDE the south classroom block (x -18..-2, z -16.5..-14.1); it was moved to (-22, 0, -14) and every map then proved 6 of 6. Hero props are 5 AI models placed by `Assets/Editor/HeroProps.cs` (2 palengke stalls, crate stack, tricycle, hoop, 2 benches). Map previews are real renders of each scene (`Assets/Editor/MapPreviewBuild.cs`) into `Assets/UI/Previews`. `Game.unity` was also given a NavMeshSurface and baked (8 triangles, 6 of 6 routes), so all three maps route the same way. EditMode tests 3/3; autopilot verification verdict: working on all three maps and on the map-select flow.

## Verify
- Compile clean (`check_compile_errors`) and `run_tests` EditMode 3/3 after every script step.
- For each map: AI reaches the enemy flag from its own base within one match, no AI is stuck at a wall for > 5 s (`[Nav]` warning must not repeat per character), Blue and Red spawns are clear, and both teams' bases/flags/prisons resolve (no "not assigned" `Debug.LogError` from MatchManager).
- Select Palengke → play → Title → select School Campus → play → Title → select Classic: the correct scene loads every time and GameManager.sceneName follows the selection.
- Android: `manage_build` AAB still succeeds; note draw-call growth from `manage_graphics stats_get` on each map.
