# Agawan Base — Re-Angle Hero Props and Stop the Ground Flicker

## Objective
Level and angle the five generated hero props so they read realistically, and remove the z-fighting that makes the court, paths and prop bases flash in the ground.

## Changes
- `Assets/Editor/MapBuild.cs` — `Flat()` gives every floor marking its own depth layer 6 mm above the ground (no two markings can share a plane) and stops the paint slabs from casting shadows; `RebuildEnvironment()` resets the layer counter.
- `Assets/Editor/HeroProps.cs` — `Prop` gains `pitch` and `roll`; a prop is rotated fully, then planted from its **world** bounds with a 2 cm ground clip instead of being dropped to `min.y = 0`; the fitted `BoxCollider` stays in local space so it follows the rotation.
- `Assets/Editor/HeroProps.cs` — new orientation/height table for all 7 prop instances, plus a `[Props]` log line per prop with its final world `min.y` and size.

## Relevant Assets and Quirks
Ground truth measured in the open School scene:
- `Game/Environment/Ground` is a cube at `y = -0.1`, height `0.2` → its top face is **exactly y = 0**.
- `MapBuild.Flat()` builds a `Decor` cube at `y = 0.012`, `0.024` thick → it spans **0.000 … 0.024**, i.e. its bottom face is coplanar with the ground top and **every marking uses the same y**.
- Proven coplanar overlaps at School: `Grounds/Path_Central` (x −30…30, z −1.5…3.5) vs `CoveredCourt/Court` (x 2…18, z −6.5…7.5) and vs `Garden/Path_B` (x −10.8…−9.2, z −4.5…2.5) — both pairs occupy y 0.000…0.024 exactly. Palengke's crossing roads do the same. That is the flashing surface.
- `HeroProps.Place()` drops each prop with `y = -local.min.y * scale`, so every prop's lowest faces also land exactly on y = 0 → the model bases flash against the ground.
- `HeroProps.Place()` only ever sets yaw — no pitch/roll, so a model generated leaning or lying stays leaning. Live proof: `HeroBench_North` measures 0.34 × 0.54 × **0.95** (a bench standing on its end), `HeroHoop` measures 3.63 × 3.6 × **7.21** (board leaning well outside its post) with scale 7.23.
- `BasketballHoop.fbx` metadata says the model should be "standing upright" with a straight post, flat backboard and a short net; the generated mesh came out warped, so rotation can level it but cannot re-sculpt it. If it still reads badly after levelling, that one FBX has to be regenerated — it will be called out, not silently replaced.
- The props are `Environment` layer + static, so they carve the NavMesh (`EnsureSurfaceAndBake`) and `VerifyRoutes` must stay 6/6 in every scene.
- Scene build order is load-bearing: School and Palengke clone `Game.unity`, and `RebuildEnvironment` deletes all of `Game/Environment`, so `ClassicHideSpotBuild.Build()` must run last, after `HeroProps.Apply()`.

## Steps
- [x] 1. `Assets/Editor/MapBuild.cs`: stagger `Flat()` to `y = 0.006 + n*0.0015` with a `flatLayer` counter reset in `RebuildEnvironment()`, and set `shadowCastingMode = Off` on the flat marking's `MeshRenderer` [depends: none]
- [x] 2. `Assets/Editor/HeroProps.cs`: add `pitch`/`roll` to `Prop`, set a full `Quaternion.Euler(pitch, yaw, roll)`, measure model-space bounds for the `BoxCollider`, then compute world bounds and plant the prop at `min.y = -0.02` (2 cm into the ground, x/z centred on the prop position) [depends: none]
- [x] 3. `Assets/Editor/HeroProps.cs`: retune the table — `HeroTricycle` yaw 90 upright on its wheels along the east lane, `HeroCrates` yaw 0 square to the aisle, `HeroStall_West/East` keep ±90 (counter to the aisle), `HeroHoop` yaw 180 pitch/roll 0 with the board square to the court, `HeroBench_North/South` rolled flat (long axis horizontal, height 0.45 m); log each prop's final `min.y` and world size [depends: 2]
  - Note: the bench model is generated lying on its back, so `pitch +90` stood it on its head (legs up, seat under the ground). `pitch -90` at 0.5 m stands it on its legs with the seat level.
- [x] 4. Rebuild in order via `execute_script`: `SchoolCampusBuild.Build()` → `PalengkeBuild.Build()` → `HeroProps.Apply()` → `ClassicHideSpotBuild.Build()`, and confirm `[Routes] … 6 of 6` plus a fresh NavMesh bake on each scene [depends: 1,2,3]
  - Note: done through the new `Assets/Editor/RebuildMapsRunner.cs` (method `Run`), which runs exactly that order. `[Routes]` came back 6 of 6 for `Map_SchoolCampus`, `Map_Palengke` and `Game`.
- [x] 5. `capture_scene_object` on `HeroHoop`, `HeroBench_North`, `HeroTricycle` and the covered court, and adjust any prop that still floats, leans or sinks more than 2 cm [depends: 4]
  - Note: `HeroHoop` was the one prop rotation could not save — it is a single mesh whose backboard is a 3.6 m curved sheet with a scrambled rim beside it and a 7.2 m footprint running into the building, so no angle makes it read as a backboard. It was rebuilt from the map's own blocks (`HeroHoop_Foot`, `HeroHoop_Post`, board, target square, 8-block rim at 3.05 m, 4-slat net) in `HeroProps.PlacePrimitiveHoop()`, and the school scene was re-baked and saved. The benches and the tricycle kept the generated meshes and were only re-angled.
- [x] 6. Verify with `unity_verification_agent` in play mode on School Campus and Palengke: no flashing floor or prop bases while the camera pans, props upright, EditMode `3 / 3` still green, then `save_scene` [depends: 4]
  - Note: the agent returned `INCONCLUSIVE` only because the play harness cannot read the console or run EditMode tests. Everything it could observe passed: 18 school floor slabs at 0.018-0.0435 in distinct 1.5 mm steps with clean, non-speckled joins at both crossings, every hero prop at `min.y = -0.02`, benches up at 0.5 m with their long side horizontal, the tricycle upright and parallel to `Cross_E`, the block hoop board facing the court with the ring at 3.05 m, NavMesh baked (School 256 / Palengke 333 / Game 8 triangles) and 6 of 6 routes on all three maps. The two unobservable items were checked in the editor instead: console shows every `[Props] … base -0.02 m` line plus `[Routes] … 6 of 6` for all three scenes with no NavMesh errors, and EditMode tests passed `3 / 3`.

## Verify
- School Campus: the court, central path and garden path no longer shimmer where they overlap (`Path_Central` vs `Court`, `Path_Central` vs `Path_B`), and no prop base flickers against the ground while the camera rotates.
- `HeroBench_North` sits on the walkway like a bench (long side horizontal, ~0.45 m tall) instead of standing on end; the tricycle stands on its three wheels parallel to the road; the hoop's board faces the court squarely.
- Console: `[Props]` logs every prop with `min.y ≈ -0.02`, `[Routes]` 6 of 6 in `Map_SchoolCampus`, `Map_Palengke` and `Game`, and no NavMesh bake errors.
- EditMode tests stay `3 / 3` and the match still plays from base spawn to flag capture in play mode.
