# Camera Zoom + Follow, Off-Screen Markers, and Character Facing

## Objective
Zoom the orthographic camera in to ~65% of the arena width with a smooth, edge-clamped follow, add three light off-screen edge arrows, and make only the character **Visual** child turn toward its movement direction — touching no rules, AI decisions or balance.

## Changes
- `Assets/Scripts/Core/CharacterMotor.cs` - deleted the root `LookRotation` block; added `public Vector2 MoveDirection { get; private set; }` set in `Move()` (zeroed while frozen).
- `Assets/Scripts/Visual/CharacterFacing.cs` - (new) turns `Visual` only, `LateUpdate`.
- `Assets/Scripts/Visual/FollowCamera.cs` - (new) on `CameraRig`: zoom from aspect, smooth follow, clamped.
- `Assets/Scripts/UI/OffScreenMarkers.cs` - (new) on `Game/UI/Panel_Hud`: 3 edge arrows built in `Awake`.
- `Assets/Scripts/UI/SafeAreaFitter.cs` - (new) on `Panel_Hud` (StretchAnchors) and `VirtualJoystick` (CornerOffset).
- `Assets/Prefabs/BluePlayer.prefab`, `Assets/Prefabs/RedEnemy.prefab` - `CharacterFacing` added to the root (no inspector fields; `Visual` found by name).
- `Assets/Scenes/Game.unity` - `Main Camera` localPosition set to (0, 30.01, 0); `FollowCamera` on `Game/CameraRig`; `OffScreenMarkers` + `SafeAreaFitter` on `Game/UI/Panel_Hud`; `SafeAreaFitter` (CornerOffset) on `Game/UI/VirtualJoystick`; scene force-saved.
- `Assets/Scripts/Visual/CharacterIndicator.cs` - stale comment corrected (comment only).

## Relevant Assets and Quirks
- Camera: **orthographic**, `orthographicSize 14`, rig `Game/CameraRig` rotated 325° + camera 90° = **55° tilt, yaw 0**. `allowMSAA false`, near 0.3, far 60. There was **no existing follow script**.
- The 55° view centre landed ~4.02 m north of the rig origin, so the arena rendered in the lower ~56% of the screen with backdrop above — the measured cause of "field looks too small" together with a visible ground width of 49.8 m against a 34.5 m arena (144%).
- Arena (`Court_Slab`): **34.5 × 19**, x ±17.25, z −10.5…8.5. Walls x ±17.5, z 8.75/−10.75. Outside floor (`Floor_Outside`): x ±31, z −19…18.4. North backdrop bodies at z ≥ 10.15. Bases/flags x = ±11, z = 0; prisons (±13, 0, 6.5).
- Rig root = CharacterController + motor/status/AI/sprint + `CharacterRigAnimator` + `CharacterIndicator`; only child is `Visual`. Front cue already existed: `Visual/UpperBody/Face` on +Z.
- Only `CharacterMotor.cs:66` wrote a character root rotation, and `CharacterIndicator` cancelled it in `LateUpdate` — nothing ever read that yaw, so the model never appeared to turn. That was the reported bug.
- `IndicatorController` captures the icon billboard from `Camera.main` **once**; `FollowCamera` therefore translates only and never rotates.
- `Mobile_RPAsset`: `m_MSAA: 1`, `m_RenderScale: 0.8`, no shadows; `QualitySettings.shadows = Disable`. No post-processing, no new packages, no imported assets.
- Legacy editor drivers `BarangayBuild.CameraAndLighting()/Arena()`, `BarangayUtils.Zoom/CentreOn` still hard-code `orthographicSize 14` and camera localPosition (0, 27.7, 3.3) — do not re-run them, or re-apply (0, 30.01, 0).

## Part 1 design - camera
**Projection: orthographic. The orthographic size is the only thing that changed.** FOV is ignored in ortho, and camera distance does not affect an ortho zoom.

| Knob | Value |
|---|---|
| `visibleWidthPercent` | **0.65** (visible ground width 34.5 × 0.65 = **22.4 m**, was 49.8 m) |
| `arenaCenter` / `arenaSize` | (0, −1) / (34.5, 19) |
| `edgeMargin` | **5** |
| `followLookAhead` | (0, 0, **1.5**) |
| `followSmoothing` | **12** /s, exponential |
| `pinchEnabled` / limits | false / 0.45–0.90 |

`orthographicSize = (arenaSize.x × visibleWidthPercent) / (2 × aspect)`, recomputed per frame. Measured by the probe: **16:9 → size 6.31, visible 22.4 × 15.4 m; 20:9 → size 5.05, visible 22.4 × 12.3 m** — same width on every phone, less depth on a taller one. Character height goes from 5.5% of the screen to 12.3% (16:9) / 15.4% (20:9).

Clamp verified settled at every extreme (allowed x ±11.04, z −8.79…6.79): SW (−11.04, −8.79), SE (11.04, −8.79), NE (11.04, 6.79), NW (−11.04, 6.79), centre unclamped at (0.00, 1.50). Every resulting visible ground rectangle sat inside `Floor_Outside`, so no background colour can show through.

## Part 2 design - off-screen markers
`OffScreenMarkers` builds three tinted triangle Images in `Awake` from one generated 32×32 sprite: enemy flag (red), own flag while stolen (blue), nearest captured team-mate (grey). Refreshed at 10 Hz, hidden when on screen, never a raycast target, zero allocation.

## Part 3 design - facing
`CharacterFacing` rotates **only `Visual`** in `LateUpdate` with `Quaternion.RotateTowards` at 720°/s, early-returning below `minMoveMagnitude 0.1` (idle holds its facing) and while captured. Reads `CharacterMotor.MoveDirection`, so the player and the AI follow one rule. There is no `NavMeshAgent` in this project, so nothing competes for the rotation.

## Steps
- [x] 1. Removed the root turn from `CharacterMotor` and exposed `MoveDirection`; created `CharacterFacing.cs`; added it to both character prefabs; forced a scene save. **Done when:** the model turns to the stick, prisoners stay frozen, compile clean. **Verified:** human model yaw 90 / 0 / 270 / 180 for the four move directions, **0.0° error**, root yaw 0.0 throughout, idle hold PASS.
- [x] 2. Created `FollowCamera.cs` on `Game/CameraRig` (translate only) and set `Main Camera` localPosition to (0, 30.01, 0). **Verified:** camera world (0, 24.58, −17.21) and the ground view centre lands exactly on the rig origin, confirming the clamp can be applied to the rig directly; all four corners clamp exactly on their limits.
- [x] 3. Created `SafeAreaFitter.cs`; applied StretchAnchors to `Panel_Hud` and CornerOffset to `VirtualJoystick`. **Verified:** both components present with the right modes; on a screen with no cutout the inset is zero, so nothing moves.
- [x] 4. Created `OffScreenMarkers.cs` on `Panel_Hud`. **Verified:** component present with the three arrows enabled; needs a live-match check for the arrow behaviour itself.
- [x] 5. Framing and scenery pass. **Verified numerically:** all five probe positions stayed inside the clamp; every visible ground rectangle was inside `Floor_Outside` (±31, −19…18.4); nearest ground point 38.8 m (near clip 0.3) and furthest visible ground corner 45.6 m (far clip 60); at the clamped extremes the followed character sat 27.7% of the width from centre, clear of the joystick and buttons.
- [x] 6. Verification pass: `check_compile_errors` clean, no exceptions, all temporary editor helpers deleted, scene saved with the wiring intact on disk.

## Notes on measurement (learned during execution)
`EditorApplication.update` fires many times per **rendered** frame, so counting its calls measures nothing; and acting and reading in the same tick measures the state before the action took effect. The first two probe runs were invalid for exactly those reasons and were rewritten to wait on a settle test. Worth remembering for any future in-editor probe.

The AI half of the facing rule cannot be exercised on the Title screen, because `EnemyAI` calls `motor.Move(Vector2.zero)` every frame while the match is not live. It needs a live match to confirm.

## Edge cases
- **Arena corners/edges:** the clamp stops that axis while the player keeps walking; the worst offset measured was 27.7% of the width, so the player never reaches the corner controls.
- **Aspect ratios:** size derived from the live aspect, so the visible width is constant and only the depth changes (15.4 → 12.3 m).
- **Notch/cutout:** only the HUD corners move, via `SafeAreaFitter`.
- **Player captured while followed:** the camera slides to the prison; no target switching.
- **Restart:** scene reload, `snapOnStart` re-frames on the player.
- **Pause:** `deltaTime = 0` freezes both the follow and the rotation.
- **Flag carried:** the flag keeps its own rotation and rides above the root, so it stays broadside while the model turns.
- **Idle facing:** held, verified over 15 frames with no snap-back.
- **180° turn:** shortest path at a constant 720°/s, arriving exactly.
- **AI stuck:** facing only reads `MoveDirection`, so an AI pushing at a wall still points where it pushes; the camera follows the human only.
- **Character indicators:** ring/icon/arrow are siblings of `Visual` on the root, and the camera never rotates, so the billboard captured once stays correct.
- **AI sight range:** unchanged (distance based) and now wider than the player can see (Hard ≈ 14–17.8 m vs 11.2 m sideways / 7.7 m vertically). This asymmetry is handled by the off-screen markers, not by touching the AI.

## Test cases (for the test record)
| # | Test | Expected |
|---|---|---|
| 1 | Start a 1v1 match | Field fills ~65% of the width; camera sits over the player |
| 2 | Walk west to the wall | Camera stops, wall + outside ground visible, player still on screen |
| 3 | Walk into each corner | Both axes clamp; no cream background anywhere |
| 4 | Switch phone aspect 16:9 → 20:9 | Visible width unchanged, less depth, no HUD element in the notch |
| 5 | Push the stick in 8 directions | Model turns to face each direction, smoothly, front cue leading |
| 6 | Release the stick | Model holds its last facing, no jitter |
| 7 | Push the stick 180° the other way | Shortest-path turn at 720°/s, no spin |
| 8 | Watch an AI patrol / chase / retreat | Every AI turns to its own walk direction; states unchanged |
| 9 | Get captured | Prisoner stops turning; camera slides to the prison and follows |
| 10 | Pause, resume, restart | Nothing rotates or drifts while paused; restart re-frames on the player |
| 11 | Steal the flag and carry it home | Carry pose and flag stay readable while turning; HUD banner clear of the player |
| 12 | Let an enemy take your flag | Blue arrow appears at the screen edge, points at the carrier, disappears when it is on screen |
| 13 | Imprison a Blue team-mate | Grey arrow points at the prison; clears when they are freed |
| 14 | 4v4 Hard, 3 minutes | 30 FPS held, roles/outcomes identical to today, no new console warnings |

## Verify
- `check_compile_errors` clean with `EditorApplication.isPaused == false`, then play: confirm visible width, clamp behaviour, one `logView` line per edge (not per frame), all three markers, and every test row above.
- Compare `get_worst_cpu_frames` before/after with 8 characters, and confirm no GC spike from the new scripts.
- Confirm nothing changed in the rules: results, roles, states, fieldTime and flag behaviour must match the pre-change build exactly.

## Assumptions
1. The camera keeps following the human player while imprisoned (no switch to a team-mate).
2. Pinch-to-zoom stays off by default (code present, `pinchEnabled false`), with 0.45–0.90 limits.
3. The view may reach 5 m past the arena edge (`edgeMargin 5`) so the player never hugs the screen edge or slides under the joystick.
