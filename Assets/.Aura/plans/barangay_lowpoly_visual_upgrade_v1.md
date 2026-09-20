# Agawan Base — Low-Poly Barangay Visual Upgrade

## Objective
Replace the plain green field and sphere characters with a flat low-poly Philippine barangay street scene at a 55° camera tilt, changing zero gameplay logic, colliders, or packages.

## Camera decision (approved: option A)
`Main Camera` is orthographic (size 9.5), local (0,20,0), rot (90,0,0) — a straight top-down view. This is why both flag poles render as thin "─" lines in-game and there is no backdrop.
- `Game/CameraRig` rot X = **−35** (was 0); `Main Camera` localPosition = **(0, 27.7, 3.3)** (was (0,20,0)); orthographic size **12.2** (was 9.5); far clip stays 60.
- Result, measured on the real geometry: world/view point (0,0,4.06), arena near edge (z=−9) lands 6% up the screen, far wall top lands **70%**, so **the top 30% is a backdrop band** covering z ≈ 9.5–18.9, and side x span is ±16.2 (4:3) to ±27 (20:9). Backdrop props 3–7 m tall at z 10–16 fill it.
- Joystick mapping is unaffected: camera **yaw stays 0**, and `VirtualJoystick.Value` → `PlayerController` → `CharacterMotor.Move` maps 1:1 to world X/Z, so stick-up still means up-screen. A pitch change only foreshortens Z movement.
- NavMesh: **no NavMesh exists anywhere in this project** (grep for NavMesh/NavMeshAgent/NavMeshSurface across all `.cs` = 0 matches; AI is pure steering through `CharacterMotor.Move`). So decor cannot affect AI paths *provided it has no collider*.

## Changes
- `Assets/Scenes/Game.unity` — camera rig + camera/clear-colour; lighting ambient; ~170 new decor objects; 9 new-material assignments; 8 prison posts retinted; SPRINT button nudged.
- `Assets/Prefabs/BluePlayer.prefab`, `Assets/Prefabs/RedEnemy.prefab` — delete `Body` capsule; add `Visual` rig + blob quad + rig script. All 3 scene characters are prefab instances, so they inherit automatically.
- `Assets/Materials/Mat_Asphalt|Mat_Chalk|Mat_HouseWall|Mat_TinRoof|Mat_Foliage|Mat_Skin|Mat_Dark|Mat_WarmAccent|Mat_Blob.mat` (create).
- Retint `Mat_Ground` (dull warm green), `Mat_Wall` (warm concrete), `Mat_Zone` (warm off-white). **`Mat_Blue` and `Mat_Red` untouched.**
- `Assets/Scripts/Visual/CharacterRigAnimator.cs` (create, ~70 lines). **No existing script is modified.**

## Relevant Assets and Quirks
- Arena x ±16, z ±9. `Base_Blue` (−11,0) r=4, `Base_Red` (11,0) r=4, both share `Mat_Zone` (so team identity must come from the ring + flag). Prisons 3×3 at (−13,6.5) and (13,−6.5). `Flag_Red`/`Flag_Blue` roots sit at the base centres; `Flag.cs` moves the root (recorded `HomePosition`, carryHeight 1, 70° drop tilt) — only child meshes may be resized.
- `Ground` has the only gameplay collider (BoxCollider, top at y=0); `Body` child of each character has **only** Transform + MeshFilter + MeshRenderer → safe to delete.
- Android routes to quality level `Mobile` → `Mobile_RPAsset`: main/additional light shadows off, shadow distance 0, MSAA off, `m_RendererFeatures: []`, and there is **no Volume / post-processing object** in the scene. `GameManager.Awake` already sets 30 FPS and `ShadowQuality.Disable`. Nothing here needs changing.
- HUD: reference 1920×1080, match 0.5. `PrisonForBlue` (13,−6.5) projects to the lower-right and is currently **under the SPRINT button** (confirmed in the game-view screenshot). Pause (top-right) is clear because z=−6.5 is the near/bottom edge.
- 4:3 is the worst case for that overlap (visible half-width ±16.2 puts x=13 at 80% to the right edge).

## Palette (14 shared materials; budget ≤ 16)
| Material | Colour / settings | Used by |
|---|---|---|
| Mat_Blue, Mat_Red *(existing, unchanged)* | as-is | player/enemy jerseys, base chalk rings |
| Mat_Ground *(retint)* | 0.42, 0.46, 0.30 | verge + outside world floor |
| Mat_Wall *(retint)* | 0.62, 0.58, 0.52 | walls, prison posts, utility posts, captured jersey |
| Mat_Zone *(retint)* | 0.86, 0.84, 0.79 | base + prison plates |
| Mat_Asphalt | 0.32, 0.31, 0.30 | court slab |
| Mat_Chalk | 0.94, 0.92, 0.86 | all non-team lines / prison squares |
| Mat_HouseWall | 0.88, 0.78, 0.62 | house + store bodies |
| Mat_TinRoof | 0.58, 0.42, 0.33 | roofs, awning, tree trunks, stall |
| Mat_Foliage | 0.38, 0.50, 0.32 | tree fronds |
| Mat_Skin | 0.85, 0.66, 0.50 | heads, legs, arms |
| Mat_Dark | 0.20, 0.19, 0.20 | hair, shoes, tyres, windows, eyes, wires |
| Mat_WarmAccent | 0.86, 0.58, 0.24 | jeepney, tricycles, banderitas, awning |
| Mat_Blob | URP/Unlit, black a=0.35, no z-write, queue Transparent | 4 blob quads |

All URP/Lit materials: metallic 0, smoothness 0.05, no specular highlights, no environment reflections, MOTIONVECTORS pass disabled. **Rule C holds by construction: Mat_Blue / Mat_Red are never used by decor.** No textures anywhere.

## Object list per prop (Unity built-in mesh costs: Quad 2, Cube 12, Cylinder 80, Sphere 768, Capsule 1088 tris)
| Prop | Built from | ~Tris |
|---|---|---|
| Court slab | 1 Cube (30, 0.03, 16) at y 0.015, no collider | 12 |
| Outside world floor | 1 Cube (52, 0.05, 44) at y −0.03 (below Ground top) | 12 |
| Court chalk markings | 12 Cubes 0.1×0.02 (halfway, sidelines, baselines, 6-segment centre circle) at y 0.04 | 144 |
| Base chalk ring ×2 | 12 Cubes (0.9, 0.02, 0.14) placed on the r=4 circle; Mat_Blue / Mat_Red | 288 |
| Prison chalk square ×2 | 4 Cubes per prison outlining the 3×3 plate | 96 |
| Prison posts ×8 *(existing)* | retint Mat_Red → Mat_Wall only | 0 |
| Tin-roof house ×5 | body Cube + 2 roof slabs + door Quad + 2 windows + awning | ~60 each / 300 |
| Sari-sari store ×1 | house variant + counter Cube + 3 sachet-row Cubes + striped awning | ~120 |
| Coconut tree ×7 | trunk Cylinder + 6 frond Cubes + 2 coconut Cubes | ~164 each / 1148 |
| Utility pole ×4 | Cylinder pole + cross-arm Cube + 2 insulator Cubes + transformer Cube | ~128 each / 512 |
| Wires | 2 long thin Cubes at 6.2 m between the base posts; 2 in the backdrop row | 48 |
| Banderitas ×2 runs | 10 Cube pennants per run, far row only, 4.5 m high | 240 |
| Jeepney ×1, tricycle ×2 | box body + cabin/roof + 4 cube wheels + sidecar | ~120 / ~140 |
| Base utility post ×2 | pole + cross-arm + transformer, 7 m, cross-arm reaching over the ring, **no collider** | 256 |

Locations: **all houses, store, trees, vehicles and banderitas sit outside the arena** (far row z 10–16.5, house/store row z 10.2–11.8, vehicles z≈10.5). The **only in-arena decor is the 2 utility posts at (±11, +5.6)** — 1.6 m outside their rings, on the far side, off the base-to-base lane, and thin enough that they do not mask a character. Court/chalk objects are flat slabs inside the play area that no character can see over.

## Character rig (built once per prefab; 12 renderers, ~1,970 tris)
Delete `Body`. Under a new empty child `Visual` (local 0,0,0), root at the feet:
| Part | Primitive / local pos | Tris | Material |
|---|---|---|---|
| Torso | Capsule scale (0.62, 0.40, 0.50), y 0.84 | 1088 | Mat_Blue / Mat_Red |
| Jersey hem | Cube (0.64, 0.10, 0.52), y 0.56 | 12 | Mat_Blue / Mat_Red |
| Head | Sphere scale 0.34, y 1.38 | 768 | Mat_Skin |
| Hair | Cube (0.30, 0.10, 0.32), y 1.50 | 12 | Mat_Dark |
| Face band | Cube (0.16, 0.06, 0.03), y 1.38, z +0.17 | 12 | Mat_Dark |
| Leg L/R | Cube (0.13, 0.42, 0.13), y 0.21, x ±0.12 | 24 | Mat_Skin |
| Shoe L/R | Cube (0.16, 0.08, 0.22), y 0.04, x ±0.12 | 24 | Mat_Dark |
| Arm L/R | Cube (0.11, 0.40, 0.12), x ±0.35, y 0.85 | 24 | Mat_Skin |
| Blob | Quad scale 1.0, rot (90,0,0), y 0.06 | 2 | Mat_Blob |

Kid height ≈ 1.55 m (~40 px at 1080p 16:9) inside the unchanged 2 m CharacterController. Colliders and all gameplay scripts untouched — the rig is children only, and `CharacterMotor` writes the root rotation, so there is no conflict.
`CharacterRigAnimator` (tunable fields): reads `CharacterMotor.IsMoving` (already public) → leg swing ±25°, torso bob 0.04; `CharacterStatus.isCaptured` → legs still + torso pitch 18° + jersey swapped to **Mat_Wall** (grey = clearly "out", and grey ≠ blue/red/white-chalk); optional `Flag` reference → `flag.carrier == transform` raises the right arm −150°, reverting automatically. Writes only child localRotation/position; never calls `motor.Move`; allocates 0 B/frame.

## Build order
- [x] 1. Camera + foundation: `CameraRig` rot −35, camera localPosition (0,27.7,3.3), size 12.2, clear colour warm sky (was 0.1,0.12,0.15), retint Mat_Ground/Mat_Wall/Mat_Zone, add court slab + outside world floor, set ambient to warm Flat. **Done when** the arena is fully framed with no void and the top 30% reads as warm sky. **Perf:** Batches ≤ 80, Tris ≤ 15,000.
- [x] 2. Readability pass: court markings + base chalk rings (Mat_Blue/Mat_Red) + prison chalk squares + retint the 8 prison posts to Mat_Wall. **Done when** both bases and both prisons are identifiable at a glance and no red/grey object is mistaken for a player. **Perf:** +≤740 tris, batches flat.
- [x] 3. Flags: enlarge `Banner` to 1.0×0.85×0.12 and thicken `Pole` (children only). **Done when** the flag is readable at character scale and still stands, tips 70° when dropped, and rides the carrier correctly. **Perf:** +24 tris; `[Flag]` carry/drop/return logs byte-identical to before.
- [x] 4. Blue rig: delete `Body` from `BluePlayer.prefab`, build the 11-part `Visual` + blob, add `CharacterRigAnimator.cs`. **Done when** the blue kid walks with leg swing and bob and faces its movement direction, and stops swinging when idle. **Perf:** 12 renderers on that character, `CharacterRigAnimator` shows 0 B/frame in the Profiler.
- [x] 5. Red rig + states: replicate into `RedEnemy.prefab`; captured = grey slumped, carrying = arm raised. **Done when** a captured enemy in prison is visibly grey and motionless and the player carrying the flag has a raised arm, both reverting correctly. **Perf:** Batches ≤ 80, Tris ≤ 20,000.
- [x] 6. Backdrop + vehicles: houses, sari-sari store, 7 coconut trees, 4 utility poles, wires, 2 banderita runs, jeepney, 2 tricycles — all Static, all 0 colliders, all outside the arena. **Done when** the backdrop band is filled at 16:9, 20:9 and 4:3 with no empty sky gap above the far wall. **Perf:** Batches ≤ 80, SetPass ≤ 30, Tris ≤ 60,000.
- [ ] 7. HUD nudge + test record + build: move `Panel_Hud/SprintButton` (and its `Label`) up-left until `PrisonForBlue` is clear at 4:3 and 16:9, then run V1–V8 and build the APK. **Done when** 20/20 SPRINT taps land, all 8 test cases are logged, and the APK is ≤ 100 MB. **Perf:** 3-minute device soak, avg ≥ 29.5 FPS, no frame > 45 ms.

## Budgets (Android mid-range)
Batches ≤ 80 (projected ~75) · SetPass ≤ 30 (projected ~10) · triangles on screen ≤ 60,000 (projected ~12,000) · unique materials ≤ 16 (projected 14) · **textures 0** · particles 0 · transparent materials 1 (blob quads ≈ 1.2 m² each, ≤ 8% of screen) · renderers/character ≤ 13 (12) · decor 100% Static, 0 colliders · APK ≤ 100 MB (projected ~38 MB), min API 26, ARM64, landscape, no shadows.

## Risks and edge cases
- **Touch buttons over props:** PrisonForBlue already sits under SPRINT; decor must add nothing else to the corners. 4:3 is the worst case — re-verify both corners there.
- **AI pathing:** AI is steering, not NavMesh, so decor is harmless *only if colliderless* — one accidental collider on a backdrop prop would block an AI walk. Verify collider count = 0 on all decor.
- **Occlusion/clutter:** a tall in-arena prop between camera and character hides a player. Hence exactly 2 thin posts inside, on the far side, off the lane.
- **Aspect extremes:** decor density is designed for x ±22 (16:9) but 4:3 shows only ±16.2 — keep the essential read (court, bases, prisons, backdrop) inside ±16.
- **Readability clash:** red prison posts and red decor would compete with red enemies; Mat_Red is now player-and-ring only.
- **Character scale:** 1.55 m inside a 2 m controller means a 0.45 m visual gap at the head; tap/tag distances (`tagDistance` 1.2) are unchanged and must not be "fixed" by resizing the collider.
- **Blob z-fighting:** blob at y 0.06 over chalk at 0.03–0.05; keep the 0.01+ gap.

## Verify — Test Record (V1–V8)
- **V1 Readability @1080p 16:9:** namer identifies blue player, both bases, both prisons, the flag and a captured (grey) enemy within 0.5 s each, cold, without hints. **Pass:** 5/5.
- **V2 Framing + controls:** arena fully in frame at 16:9, 20:9, 4:3; no clipped play object; backdrop band has no empty gap; push stick up/right/down/left and confirm the character moves up/right/down/left of screen at every aspect (±10° tolerance).
- **V3 HUD overlap:** SPRINT + Pause clear of Prisons and of decor at 4:3 and 16:9; 20 consecutive SPRINT taps with 20/20 registering.
- **V4 Performance:** Game-view Stats during a live match with both AI chasing: Batches ≤ 80, SetPass ≤ 30, Tris ≤ 60,000. Device soak: 3 min, avg ≥ 29.5 FPS, no frame > 45 ms, GC alloc 0 B/frame from `CharacterRigAnimator`.
- **V5 APK:** Android build report — ≤ 100 MB, min API 26, ARM64 only, landscape, shadow distance 0.
- **V6 Gameplay regression:** run one scripted match before and after; `[Match]`, `[AI]` and `[Flag]` console output must match the pre-change run for touch resolution, capture, flag pick-up/drop/return and win/lose.
- **V7 Colliders + AI:** every decor object reports 0 colliders; no NavMesh data added; 60 s of AI state transitions matches the pre-change distribution (Patrol/Chase/Retreat/Intercept/ReturnFlag).
- **V8 No-clutter sweep:** camera→character sightlines clear in 8 sampled positions; no decor in the play lane x ±16 / z ±9 above 0 m except the 2 base posts.
