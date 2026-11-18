# Agawan Base — Bush Hiding and Base Freshness Fix

## Objective
Add a real bush-hiding (full stealth) mechanic to all three maps and fix why the human player never refreshes freshness at his base on the new maps.

## Changes
- `Assets/Scripts/Gameplay/HideSpot.cs` - (create) bush hide volume: flat-radius check + static registry + `IsHidden(CharacterStatus)`, `Debug.Log` on enter/exit.
- `Assets/Scripts/Core/CharacterStatus.cs` - new `isHidden` flag updated each `Update()`; carriers are never hidden.
- `Assets/Scripts/Gameplay/MatchManager.cs` - `CheckCharacterTouches` and `WouldWinTag` skip a hidden character.
- `Assets/Scripts/Gameplay/EnemyAI.Perception.cs` - `PickTarget` skips hidden foes and drops the memory of one that hides.
- `Assets/Scripts/Gameplay/CharacterSpawner.cs` - place the human on a free slot inside his own base and re-bind every `homeBase` by team.
- `Assets/Editor/MapBuild.cs` - `CreateBush(parent, name, position, radius)` shared helper (low rim + foliage + `HideSpot`).
- `Assets/Editor/SchoolCampusBuild.cs` - `BuildGarden` planters become walk-in bushes (rim 0.2 m, foliage 1.5 m, `HideSpot`).
- `Assets/Editor/PalengkeBuild.cs` - `CrateStack` groups get a `HideSpot` (radius 1.4).
- `Assets/Editor/ClassicHideSpotBuild.cs` - (create) adds 4 bushes to `Game/Environment` in `Game.unity`, then bakes/saves.

## Relevant Assets and Quirks
- **No hiding code exists today** — `grep` for Bush/Hide/Conceal/Cover across `Assets/Scripts` returns only HUD/refresh matches. This is a new mechanic, not a broken one.
- **Why the bush is not enterable at School**: `BuildGarden` builds `Planter` with `MapBuild.Solid(...)` at `(-11, 0.25, -3)`, size `(2.2, 0.5, 2.2)`, and `Shrub` with `MapBuild.Decor(...)` at y 0.95. `Decor` calls `DestroyImmediate(collider)`, and `CharacterController.stepOffset` is **0.3**, so the 0.5 m planter cannot be stepped into and the shrub is pure paint.
- **Why freshness fails on the new maps**: the human is a hand-placed object serialized at `(-11, 0, -2.5)` in `Game.unity` (confirmed in the live hierarchy), every new scene is a clone of it, and no builder ever repositions a placed character. The new Blue base is at `(-26, 0, 0)` with radius 4 — 15 m away — and `(-11, -2.5)` is *inside* the School planter box (x −12.1…−9.9, z −4.1…−1.9), so he starts embedded in a solid.
- **The freshness rule itself is not map-specific**: `CharacterStatus.Update` → `IsInHomeBase` → `BaseZone.Contains` (flat circle, radius 4 on new maps, 3.2 on Classic); `HudController` shows code 1 = `"SAFE AT BASE"`. The fault is position/binding, not the rule.
- **Touch pairing is pure distance** (`MatchManager.CheckCharacterTouches`, `tagDistance`, no colliders or triggers) — hiding must be distance-based too. `CharacterController` has no Rigidbody, so trigger volumes are unreliable here; do not add them.
- `CharacterSpawner` spawns AI on a 2×2 formation at `slotOffset` 1.1 m around the base centre — keep the human out of those four slots.
- `.unity` files are not searchable by the project grep tool (verified: `m_Name: Base_Blue` returns no match inside `Game.unity`), so scene state is confirmed from builder source plus the live hierarchy only.
- The editor was observed left **in play mode** with `Game.unity` loaded — stop play mode before any rebuild.
- Domain reload is off on play (`m_EnterPlayModeOptions: 3`) — reset the `HideSpot` static list in `Awake`/`OnEnable`.
- Re-running `SchoolCampusBuild.Build()` destroys every `Game/Environment` child, including the hero props — `HeroProps.Apply()` must be re-run before the final bake.
- Classic has **no builder**: bases (±11, radius 3.2), flags, prisons, ground and boundary must stay untouched; only new bushes are added.
- A character carrying a flag cannot hide (an untaggable flag carrier would be an exploit).

## Steps
- [x] 1. Create `Assets/Scripts/Gameplay/HideSpot.cs` (flat radius, static registry, `IsHidden`, enter/exit `Debug.Log`) and add `MapBuild.CreateBush(...)` next to `CreateBase` [depends: none]
- [x] 2. Add `isHidden` to `CharacterStatus.cs`, refreshed in `Update()` via `HideSpot.IsHidden(this)` and forced false while carrying, with a one-line log per state change [depends: 1]
- [x] 3. Make stealth real: skip hidden characters in `MatchManager.CheckCharacterTouches` + `WouldWinTag`, and skip/forget them in `EnemyAI.PickTarget` [depends: 2]
- [x] 4. In `CharacterSpawner`, after the AI spawns, find the tag `Player` body, re-bind every `homeBase` by team, and teleport the human to a free slot inside his own base (`CharacterMotor.TeleportTo`), logging the placement [depends: none]
- [x] 5. Rebuild the School garden as walk-in bushes and give Palengke `CrateStack` a hide volume (edits in `SchoolCampusBuild.BuildGarden`, `PalengkeBuild.CrateStack`, `PalengkeBuild.Build`) [depends: 1]
- [x] 6. Add `Assets/Editor/ClassicHideSpotBuild.cs` for 4 bushes at `(±5, ±4.5)` in `Game.unity`, then re-run `SchoolCampusBuild.Build()`, `PalengkeBuild.Build()`, `HeroProps.Apply()`, `EnsureSurfaceAndBake()` and `SaveScene` per scene [depends: 5, 4]
- [x] 7. Verify: `check_compile_errors`, `run_tests` EditMode 3/3, then one `unity_verification_agent` pass over the three scenes [depends: 6]

## Verify
- School: the human starts inside the Blue base, the HUD reads `SAFE AT BASE` and `fieldTime` stays 0 while he stands there.
- Walk into a School garden bush → `[Hide] BluePlayer entered bush ...` and a nearby Red AI stops chasing (`targetVisible` false), and touching it does not capture.
- Step out → `[Hide] ... left bush ...` and the AI picks him up again once inside `noticeRange`.
- Palengke: a `CrateStack` hide volume hides the same way; `[Routes]` still reports 6 of 6 routable and the NavMesh triangle counts are non-zero on all three scenes.
- Classic: 4 new bushes exist, bases/flags/prisons unmoved, and a match still plays normally.

## Notes (2026-11-18)
- Build order used: `SchoolCampusBuild.Build()` → `PalengkeBuild.Build()` → `HeroProps.Apply()` → `ClassicHideSpotBuild.Build()`. The Classic bushes are planted last on purpose: both map builders clone `Game.unity`, so anything added to Classic first would be copied into the clones and then deleted by `RebuildEnvironment`.
- Confirmed in the scenes: 8 `Game/Environment/Garden/Bush_Garden_*` with `HideSpot.radius = 0.935`; 4 `Game/Environment/Props/Crates_*` with `HideSpot.radius = 1.15`; 4 `Game/Environment/Bushes/Bush_Classic_*` with `HideSpot.radius = 0.935`.
- `[Routes]` after the rebuilds: Game 6 of 6 (bake 8 tris), Map_Palengke 6 of 6, Map_SchoolCampus 6 of 6 (bake 246 tris). EditMode tests 3 / 3.
- Re-cloning replaced both map scene assets, so their GUIDs changed; `HeroProps.Apply()` re-added them and the build settings hold all three scenes, enabled.
- The School garden bed is now a 0.2 m `Solid` kerb (was 0.5 m) with 1.5 m `Decor` foliage, so a character steps into it instead of being blocked at the edge.
- Hiding is switched off while a character carries a flag, and it is symmetric: a hidden AI is equally un-targetable by the player's side.
