# Team Play (N vs N) + Rescue + Team-Agnostic Smart AI

## Objective
Turn the hard-coded 1v2 match into a selectable 1v1–4v4 in which the human is the only non-AI character, add prison rescue, enlarge the arena with smaller bases, and give both teams one utility-scored AI — with the fieldTime / capture-the-flag rules unchanged.

## Locked decisions
1. **Elimination = immediate loss** when every member of a team is imprisoned. In 1v1 this reproduces today's "I was captured" exactly.
2. **Arena enlarged AND both base circles shrunk** (numbers below; camera zoom-out keeps 4:3 framing).
3. **Runtime `Instantiate`** of the single shared `RedEnemy.prefab`, configured per spawn. Nothing unused is left in the scene.

## Arena + camera (computed from the live scene, not estimated)
Camera is a **static orthographic** rig: `CameraRig` at origin rotated −35° X, camera child at (0, 27.7, 3.3) with its own 90° X → **world pos (0, 24.58, −13.19), world pitch 55°, orthoSize 12.2, yaw 0** (yaw 0 is what keeps the joystick 1:1 with world X/Z — do not change it). Screen centre hits the ground at z ≈ +4.04; ground-z coverage = `2 × orthoSize × 1.221`.

| Object | Now | New | Why |
|---|---|---|---|
| `Main Camera.orthographicSize` | 12.2 | **14.0** | 4:3 visible half-width 18.67 > wall outer 17.75 (0.92 margin; today's margin is only 0.52) |
| `Ground` (pos, scale) | (0,−0.1,0) 32×0.2×18 | (0,−0.1,**−1**) **35.5**×0.2×**20** | spans x ±17.75, z −11…9 = exactly the wall outer faces |
| `Wall_E` / `Wall_W` scale | (0.5,2,18) @ x ±15.75 | (0.5,2,**20**) @ x **±17.5**, z −1 | inner faces ±17.25 |
| `Wall_N` | (0,1,8.75) 32×2×0.5 | (0,1,8.75) **35.5**×2×0.5 | **unmoved** — this is what preserves the tuned backdrop |
| `Wall_S` scale/pos | (32,2,0.5) @ z −8.75 | (35.5,2,0.5) @ z **−10.75** | inner face −10.5 |
| `Court_Slab` | (0,0.015,0) 31×0.03×17.4 | (0,0.015,**−1**) **34.5**×0.03×**19** | asphalt meets the new inner faces |
| `Floor_Outside` | (0,−0.04,−1.25) 62×0.06×35.5 | (0,−0.04,**−0.3**) 62×0.06×**37.4** | far edge z 16.5 → **18.4**, restoring the horizon to ~92% at the new zoom |
| Court lines | sidelines ±15, baselines ±8.4, halfway @z 0 | sidelines x **±16.75**, baselines z **+8.4 / −10.4**, halfway @z **−1**, centre circle @z **−1** | rebuilt by `BarangayBuild.CourtMarkings` |
| `BaseZone.radius` + plate | 4.0, plate 8×8, ring 4.35 | **3.2**, plate **6.4×6.4**, ring **3.55** | the plate edge becomes exactly the safe circle |
| `BasePost_Blue` / `BasePost_Red` | (±11, 0, 5.6) | (±11, 0, **4.9**) | keeps their 1.6 m stand-off from the smaller circle |
| Bases, flags, prisons | (±11,0,0) / (±13,0,±6.5) | **unmoved** | `Flag.Awake` HomePosition and all scene refs stay valid; prisons clear the new north wall (plate z 5.0…8.0 < 8.5) |

**Playable area 34.5 × 19.0** (was 31 × 17.0: **+11% × +12%**), plus the base circles shrink 8.0 → 6.4 across, so the open lane between the two flags grows from 14.0 to **15.6 m** without moving a single gameplay reference.
**Consequences:** a character draws ~13% smaller (12.2/14.0); the tilt, yaw, backdrop, roofline and horizon band are unchanged because the rig is not moved; there is no NavMesh (0 agents) so nothing needs rebaking; the flag escape run becomes 18.8 m instead of 18.0, and the "both on fieldTime 0" tie window at the flag narrows with the smaller circle. `BarangayBuild.Report()` prints the camera/framing percentages, so this is verified, not eyeballed.

## Verified baseline (live this session)
- Unity **6000.6.0f1**, URP **17.6.0**, Input **1.20.0**, uGUI **2.6.0**. `com.unity.ai.navigation 2.0.14` is installed but **unused** — `BarangayBuild.cs:604` states the project has no NavMesh and `grep NavMesh` over all `.cs` is empty → **NavMeshAgents = 0**.
- One scene, `Assets/Scenes/Game.unity`; `Restart`/`Title` reload it, so a `static` settings class survives any restart (the project already does this with `GameManager.startPlayingOnLoad`).
- Prefabs: `BluePlayer.prefab` (has `PlayerController`) and `RedEnemy.prefab` (has `EnemyAI`); both also carry `CharacterController`, `CharacterMotor`, `CharacterStatus`, `SprintStamina`, `CharacterRigAnimator`. Instance `BluePlayer` is at (−10.2, 0.08, 0.11).
- Rules: `tagDistance 1.2`, lower `fieldTime` wins, `tieIsNoCapture true`, own base snaps `fieldTime` to 0, `MaxFieldTimeSeconds` is `const 20f`.
- **`Ground`, `Wall_N/S/E/W`, `Base_Blue`, `Base_Red`, the prison plates and `PrisonFor*/Post_*` are scene-only objects — no current script creates them.** Only `Court_Slab`, `Floor_Outside`, the court lines, base rings and chalk squares are rebuilt by `Assets/Editor/BarangayBuild.cs`, which is **re-runnable**: `Prim()` and `Dashes()` find/create by name and update in place.
- **Trap:** `BarangayBuild.Prim()` does `DestroyImmediate(collider)`. `Ground` and `Wall_*` depend on their colliders, so the new `Arena()` method must set their transforms **directly** and must never route them through `Prim()`.
- Prison plate is exactly **3.0 × 3.0** (half 1.5). `MatchManager.PrisonSpot` currently spreads 4 slots 1.2 apart along Z → z 4.7…8.3, bursting the plate. Real bug.
- `EnemyAI.Start()` does `FindAnyObjectByType<PlayerController>()` — hard-wired to the human. 5 states, `decisionInterval 0.25 ± 0.05`, `minStateTime 0.6`, `chaseFresherMargin 0.3`, `guardOffset` (0,−1.5)/(0,+2.5).
- `MatchManager` holds one `player` + `CharacterStatus[2] enemies`, one `PlayerWon`/`PlayerLost` each, counts only `redsCaptured`. `HudController` prints `"Captured X / N"`.
- **No tags are registered** (`tags: []`) and nothing calls `FindGameObjectsWithTag`. Layers: Default, TransparentFX, Ignore Raycast, Water(4), UI(5).
- **There is no `UIManager`, no `FieldTimer`, no spawner** — equivalents are `HudController`+`GameManager`, `CharacterStatus`, and hand-placed instances.
- HUD canvas 2035.55 × 1018.69 @ `scaleFactor 0.5478`. Free space verified: left column ends y −254, right column y −50…−270, joystick x 120…380 y 120…380, sprint x 1685…1885 y 150…350, `FlagLabel` x 468…1568 y −24…−84. Worst phone (1600×720 @480 dpi, scale ≈0.745): **200 canvas units ≈ 50 dp**.
- No `Assets/Tests` and no test assembly → verification is a manual Play-mode test record.

## Changes
**Modify**
- `Assets/Scripts/Core/Team.cs` — add `TeamUtil.Other()`, `All`, `ToColor()`.
- `Assets/Scripts/Core/CharacterStatus.cs` — register/unregister with `TeamManager`; `Capture(prisonPosition, slot)` claims its own slot; `Release(immunitySeconds)` (fieldTime 0, stays in the field, immunity timer); expose `SpawnIndex`.
- `Assets/Scripts/Gameplay/MatchManager.cs` — per-team lists from `TeamManager`; `baseOf`/`prisonOf`; team-agnostic touch loop (max 16 pairs); 2×2 `PrisonSpot`; `CheckRescueTouches`; per-team counters; `event Action<Team> TeamWon/TeamLost`; all-imprisoned elimination; optional `matchTimeSeconds`.
- `Assets/Scripts/Gameplay/EnemyAI.cs` — shared team-agnostic brain: perception-based target, utility scoring, role bias, RESCUE, gated on `TeamManager.MatchActive`.
- `Assets/Scripts/Gameplay/Flag.cs` — factor out `CanBeTakenBy(Team)`; no logic change.
- `Assets/Scripts/Visual/CharacterRigAnimator.cs` — enemy flag + `teamJersey` supplied by the spawner instead of one `FindObjectsByType<Flag>` per character.
- `Assets/Scripts/UI/GameManager.cs` — `MatchSetup` + `Ready` states; Blue→Victory, Red→GameOver; captured overlay.
- `Assets/Scripts/UI/HudController.cs` — two scaling per-team lines, both flag states, alert line.
- `Assets/Editor/BarangayBuild.cs` — new `Arena()` (Ground/Walls/base plates/`BaseZone.radius`/BasePosts, transforms set directly, **not** via `Prim`), new constants in `Foundation()`/`CourtMarkings()`, ring radius 3.55 in `Prisons()`, camera `orthographicSize 14.0`.
- `Assets/Scenes/Game.unity` — `Game/UI/Panel_MatchSetup` + 4 HUD labels; delete `Enemy_Red_1`/`Enemy_Red_2`; add `Game/Systems` (TeamManager + CharacterSpawner).

**Add**
- `Assets/Scripts/Core/MatchSettings.cs` (static settings, `TeamSize` 1–4, `Difficulty`, the preset table, PlayerPrefs).
- `Assets/Scripts/Core/TeamManager.cs` (per-team registry, `MatchActive`, free/imprisoned queries, role review).
- `Assets/Scripts/Gameplay/CharacterSpawner.cs` (formation spawning from the one shared prefab).
- `Assets/Scripts/UI/MatchSetupUI.cs`, `Assets/Scripts/UI/AIDebugLabel.cs`.

## Part 1 — Match settings, spawning, team-agnostic code, capacity
**Settings: a `static class MatchSettings`, not a ScriptableObject.** One scene that `Restart`/`Title` reload means a static field already survives the transition (proven by `GameManager.startPlayingOnLoad`); Setup and play are in the same scene so there is nothing to carry across; and a ScriptableObject would add an asset, a load path and an instance pass — three failure points for zero benefit. It also becomes the single owner of PlayerPrefs (`match.teamSize`, `match.difficulty`). Trade-off: needs one `Load()` at boot, because domain-reload-off would otherwise leak the last session's values.

**Match Setup screen:** Title's START → Setup. Four size buttons **260×200** (`1v1 2v2 3v3 4v4`), three difficulty buttons **260×200** (`EASY NORMAL HARD`), `BACK`/`START` **480×130** — all ≥200 units tall so they clear 48 dp. The stored choice is re-shown on re-entry.

**Spawning (`CharacterSpawner`, Q3-a):** instantiate `(N−1)` Blue AI + `N` Red AI from `RedEnemy.prefab`, on a 2×2 formation of **±1.1** inside each base (outermost body edge 2.06 < new radius 3.2), setting per spawn: `status.team`, `status.homeBase`, `status.SpawnIndex`, `rig.teamJersey` (`Mat_Blue`/`Mat_Red`), `rig.flag` and `EnemyAI.ourFlag` (the enemy flag), and `EnemyAI.guardOffset` = the formation slot so no AI idles outside its own safe circle. Phase-stagger the AI's first decision by `index/count × decisionInterval`. `BluePlayer` stays hand-placed. Delete `Enemy_Red_1`/`Enemy_Red_2` so nothing unused remains.

**Team-agnostic:** AI, tag resolution, flags and prisons all read a `Team` value. `TeamManager` (singleton in `Game/Systems`) caches `Members(Team)` on register/unregister — **no `FindObjectsOfType`, no `FindGameObjectsWithTag`, nothing searched in `Update`**. API: `Members`, `FreeCount`, `CapturedCount`, `NearestFreeEnemy(Team, pos, radius)`, `NearestCapturedTeammate(Team, pos)`, `AllImprisoned(Team)`, `IsHuman(char)`, `MatchActive`, `ReviewRoles()`.

**Capacity:** prisons use a **2×2 grid at ±0.7** (span 1.4, outer edge 1.2 < 1.5 plate half; adjacent centres 1.4 > 1.0 body width) = 4 prisoners cleanly. Bases hold 4 characters inside radius 3.2.

## Part 2 — Rescue
A free character within `rescueDistance` **1.5** of an imprisoned **teammate in the enemy prison** frees them. Judged in `MatchManager.CheckRescueTouches` by distance, like the flags — no triggers. The freed prisoner is unfrozen, placed **0.6 m outside the prison** (outside its collider), `fieldTime` reset to **0**, with `immunitySeconds` **2** during which `ResolveTouch` skips them. **fieldTime 0 is the right call:** it is what `Release()` does today, it is exactly what the rescuer's risk bought, and it makes the freed character fresher than the enemy camped at the prison — a comeback, not a re-capture loop. Immunity covers the walk-out; the **rescuer gets no immunity** (it is outside its base, so its fieldTime is climbing and a free enemy tags it under the normal rule). `rescueCooldown` **3 s** per prisoner stops chain-rescues. Both teams' AI rescue via the RESCUE option.

## Part 3 — Win/lose, elimination, HUD
- Win: free + carrying the enemy flag + inside your own base (unchanged) → `TeamWon(myTeam)`. Lose: an opponent does it with your flag.
- **Elimination (locked):** all members imprisoned → that team loses, immediately.
- **Frame order: Rescues → Touches → Win → Elimination.** A rescue landing in the same frame as a capture is honoured, and nothing ends the match while a rescue touch is in range.
- **Human captured, teammates free: no state change.** Frozen in prison with an overlay `CAPTURED - waiting for a rescue`. The camera already shows the whole pitch.
- **Optional tie-breaker:** `matchTimeSeconds` (default **180**, `0` = off) `/ 60`; on expiry more free members wins, then lower average `fieldTime`, then `DRAW`.
- **HUD:** delete `"Captured X / N"`. Add `BlueStatusLabel` anchor(0,1) pivot(0,1) **(50,−262) 620×36** and `RedStatusLabel` anchor(1,1) pivot(1,1) **(−50,−280) 620×36**, e.g. `BLUE  2/3 free  |  our flag: CARRIED`. Add `AlertLabel` anchor(0.5,0.5) **(0,150) 1100×70**, inactive, for `YOUR FLAG HAS BEEN TAKEN!` and the captured overlay. All positions sit in verified free space; strings are only assigned when changed; **ASCII glyphs only** (legacy `Text`, no TMP).

## Part 4 — Smarter AI (one brain, both teams)
Keep the enum state machine; choose the state by **utility scoring** each decision tick. No ML, no packages.

```
AIBrain — decides every ~0.25 s, phase-staggered by SpawnIndex
  captured ─────────────────────────────────────────────► frozen, no decisions
  every roleReviewInterval (1.5 s, staggered +25 ms) TeamManager re-derives team roles
  Defender → DEFEND   Raider → STEAL/CARRY   Rescuer → RESCUE   Hunter → HUNT   Flex → best score
  hard vetoes (a score can never override them)
    HUNT needs IAmFresher(margin) && in sight && Stamina01 >= chaseEnterStamina01
    INTERCEPT replaces HUNT when an enemy carries our flag but HUNT is vetoed
    RESET fires when fieldTime > 0.6 × 20 s and nothing is urgent
```
States: `Defend, Hunt, Intercept, ReturnFlag, Steal, Carry, Rescue, Bait, Reset`.

| Option | Positive | Negative | Hard gate |
|---|---|---|---|
| DEFEND | `1 − enemyDist/defendRadius`; +0.4 if own flag carried/dropped | −0.3 if own flag at base and no enemy inside radius | — |
| STEAL | `1 − myDist/mapDiagonal`; +0.5 if own flag at base and a teammate defends; +0.3×riskTolerance | −0.6 if I already carry the enemy flag; −0.4 if I am the only free teammate | free |
| CARRY | `1.0` (top once I hold the enemy flag) | −0.2 × distToHome/mapDiagonal | — |
| HUNT | `fresherBy/10 s` + `0.4 × (1 − dist/visionRadius)` | −0.5 if the target is in its own base | `IAmFresher`, in sight, stamina ≥ `chaseEnterStamina01` |
| INTERCEPT | `0.7 + 0.3 × (1 − dist/visionRadius)` | −0.4 if my fieldTime ≥ target's | target carries my flag, HUNT vetoed |
| RETURNFLAG | `0.9 × (1 − dropTimer/10 s)` | −0.6 if an enemy is closer by `flagRaceAdvantage` | own flag dropped |
| RESCUE | `0.6 + 0.4 × (1 − dist/mapDiagonal)`; +0.5 if a teammate is imprisoned | −0.8 if I am the only free teammate and my flag is carried | teammate imprisoned, free |
| BAIT | `0.5 × patience`; +0.3 if a teammate is closer to the enemy flag than me | −1.0 if no free teammate could profit | ≥2 free teammates |
| RESET | `fieldTime/20 s`; +0.5 if winded | −1.0 if a teammate is imprisoned and within reach | — |

`score += Random.Range(−noise, noise)`, `noise = personality.instability × difficulty.noise`; switch only when `best > current + switchMargin` **and** `stateTimer ≥ minStateTime` (keeps the shipped anti-flap hysteresis).

| Match | Chars | AI per team (Blue / Red) | Default role mix per team |
|---|---|---|---|
| 1v1 | 2 | 0 / 1 | 1 × Flex (must switch offense/defense) |
| 2v2 | 4 | 1 / 2 | 1 × Defender + 1 × Raider |
| 3v3 | 6 | 2 / 3 | 1 × Defender + 1 × Raider + 1 × Flex |
| 4v4 | 8 | 3 / 4 | 1 × Defender + 1 × Raider + 1 × Rescuer + 1 × Flex |

Roles are re-derived every 1.5 s: an own flag stolen/dropped forces an extra Defender, a prisoner forces one Rescuer, and nobody raids while the last free teammate is needed at home. The human fills the gap, so Blue has one fewer AI. **Flag stealing needs no new rule** — `Flag.ownerTeam` + `TryPickUp` already refuse your own flag and `MatchManager` alone judges pickup/drop/return; the Raider only adds the walking.

**Perception:** `visionRadius` 12 m (Easy 10 / Hard 14) + `memorySeconds` 3 s last-known position. **Zero raycasts** — the court slab inside the walls is flat and empty (all decor is outside the arena), so there is no occlusion to test and distance alone is honest. **Unpredictability:** per-instance `aggression`, `patience`, `riskTolerance`, `instability` rolled once in `Awake`; a reaction delay before acting; a per-personality approach offset chosen on the decision tick. Never per-frame jitter. **Stamina:** sprint only when `distance ≥ sprintDistance`; `chaseEnterStamina01` blocks a winded chase.

| Preset | decisionInterval | reaction delay | chaseFresherMargin | aggression | riskTolerance | stealBias | visionRadius | memorySeconds |
|---|---|---|---|---|---|---|---|---|
| Easy | 0.35 s | 0.30–0.60 s | 0.60 s | 0.70–0.95 | 0.15 | 0.7 | 10 m | 2.0 s |
| Normal | 0.25 s | 0.10–0.35 s | 0.30 s | 0.85–1.15 | 0.30 | 1.0 | 12 m | 3.0 s |
| Hard | 0.20 s | 0.05–0.15 s | 0.15 s | 1.00–1.30 | 0.50 | 1.3 | 14 m | 4.0 s |

**Teammate skill: recommend NO separate setting.** One difficulty governs all seven AI because fairness is the stated rule; the design is already symmetric (identical `moveSpeed 6`, `sprintSpeedMultiplier 1.6`, one shared `SprintStamina`); a hidden second difficulty makes "the AI cheated" indistinguishable from "my teammate is bad"; and the honest lever already exists — the preset, which moves both teams together. Label the row `AI SKILL (BOTH TEAMS)`.

## Match flow
```
Title ─START─► Match Setup ─START─► Ready (1.5 s, all frozen) ─► Playing
  Playing ─PAUSE─► Paused ─RESUME─► Playing
  Playing ─ I carry the enemy flag into my base ────────► Victory
  Playing ─ an opponent carries my flag into their base ─► Game Over
  Playing ─ every member of my team imprisoned ─────────► Game Over (elimination)
  Playing ─ every enemy imprisoned ──────────────────────► Victory  (elimination)
  Playing ─ I am imprisoned, teammates free ─────────────► Playing + overlay (no state change)
  Playing ─ matchTimeSeconds expires ────────────────────► Victory / Game Over / Draw
```
`MatchManager` raises `TeamWon(Team)`/`TeamLost(Team)`; `GameManager` maps Blue→Victory, Red→GameOver and owns every panel and the end-screen text.

## Performance budgets
| Item | Budget | Basis |
|---|---|---|
| Characters | **8 max** (4v4) | 34.5 × 19 playable, 1 m bodies |
| AI brains | **7 max** (3 Blue + 4 Red) | 4v4 roster |
| NavMeshAgents | **0** | no NavMesh exists; direct steering ships today |
| Decision interval | **0.25 s** ± 0.05, phase-staggered `firstDelay = index/count × interval` | ≤1 AI decides per frame |
| Decisions / second | **28** at 4v4; **0** while `MatchActive` is false | 7 brains × 4 Hz |
| Cost per decision | ≤**64 float ops** (7 enemies + 4 prisoners + 2 flags), **0 allocations, 0 raycasts** | pre-allocated buffers, no LINQ, no `new` |
| Role review | 1.5 s per team, staggered +25 ms | ~1.3 Hz total |
| Frame budget | AI + movement **< 2 ms** of 33.3 ms @30 FPS | `get_worst_cpu_frames` |
| Draw calls | +5 renderers × 5 added characters; SRP Batcher covers the few URP/Lit materials | `stats_get` before/after |
| Update rate at 4v4 | **do not drop it.** 28 decisions/s of ~64 float ops is nothing next to rendering, and slowing the brains would make the AI visibly dumber exactly when the match gets interesting. Spend the budget on staggering. | measured |

## Steps
- [x] 1. Team-agnostic core: add `Assets/Scripts/Core/TeamManager.cs` + register/unregister in `CharacterStatus`; switch `MatchManager` to per-team lists (`baseOf`/`prisonOf`); make `EnemyAI` choose its target by team + perception instead of `FindAnyObjectByType<PlayerController>`; gate `EnemyAI.Update` on `TeamManager.MatchActive`; route `CharacterRigAnimator` through the spawner. `Debug.Log` every registration and target change. *(done when: 1v2 plays exactly as before — move, tag, capture, prison, flag carry, victory, game over — and `Members` reports 1 Blue + 2 Red)* [depends: none]
- [x] 2. `MatchSettings.cs` + `Game/UI/Panel_MatchSetup` (four 260×200 size buttons, three 260×200 difficulty buttons, `BACK`/`START` 480×130) driven by `MatchSetupUI.cs`; add `GameState.MatchSetup` and point Title's START at it. *(done when: Title → SETUP → 3v3 + Hard → START works and survives re-entry and an editor restart; every button ≥200 units tall)* [depends: 1]
- [x] 3. Arena + capacity: add `BarangayBuild.Arena()` (Ground/Wall_*/base plates/`BaseZone.radius 3.2`/BasePosts to z 4.9, transforms set **directly, never via `Prim`**), update `Foundation()`/`CourtMarkings()` constants and the ring radius to 3.55, set `orthographicSize 14.0`, then re-run `BarangayBuild.All()`. Replace `MatchManager.PrisonSpot` with the 2×2 ±0.7 grid. *(done when: `BarangayBuild.Report()` shows the arena near edge and horizon near their current percentages, 4 prisoners stand cleanly on the 3.0 plate, and 4 characters in `Base_Blue` never overlap)* [depends: 1]
- [x] 4. `CharacterSpawner.cs`: instantiate `(N−1)` Blue AI + `N` Red AI from `RedEnemy.prefab` on a ±1.1 2×2 formation, configuring team / jersey / homeBase / flags / guardOffset / staggered first decision per spawn; delete `Enemy_Red_1` and `Enemy_Red_2`. Log the roster. *(done when: 1v1 spawns exactly 1 AI, 4v4 spawns 3 Blue + 4 Red with no overlapping bodies, and nothing unused is left in the hierarchy)* [depends: 2, 3]
- [x] 5. Rescue + team win/lose: `MatchManager.CheckRescueTouches`, `CharacterStatus.Release(immunitySeconds)`, per-team counters, `TeamWon`/`TeamLost`, all-imprisoned elimination, optional `matchTimeSeconds 180`, and the AI RESCUE option. Log every rescue, capture and match end with both team counts. *(done when: 1v1 elimination still reads like today's "I was captured"; a touch frees a teammate with fieldTime 0.00 and 2 s immunity; an enemy can still tag the rescuer mid-run)* [depends: 4]
- [x] 6. HUD: delete `"Captured  X / N"`; add `BlueStatusLabel` (50,−262) 620×36, `RedStatusLabel` (−50,−280) 620×36, `AlertLabel` (0,150) 1100×70 (inactive) with the captured overlay and `YOUR FLAG HAS BEEN TAKEN!`, plus both flags' AtBase/Carried/Dropped state. *(done when: counters scale at 1v1–4v4, both flag states read live, and nothing overlaps the joystick, sprint button or pause button)* [depends: 5]
- [x] 7. AI: utility scoring + team role assignment in `EnemyAI`/`TeamManager`, `Steal`/`Carry`/`Rescue`/`Bait`, difficulty presets, and `AIDebugLabel` (role + state label, sight gizmos). *(done when: at 4v4 each team holds 1 Defender / 1 Raider / 1 Rescuer / 1 Flex and roles rotate when a flag or prisoner changes; Easy/Normal/Hard visibly change reaction, aggression and steal rate on **both** teams)* [depends: 6]

## Build order (each with a Play-mode check and a performance check)
| # | Task | Done when | Perf check |
|---|---|---|---|
| 1 | Team-agnostic core | 1v2 full regression pass, unchanged | `get_worst_cpu_frames` matches the 1v2 baseline |
| 2 | Settings + Setup screen | Title → Setup → 3v3/Hard → START | — |
| 3 | Arena + bases + capacity | `Report()` framing %s hold; 4 prisoners fit; 4 in base | draw calls unchanged (0 new characters yet) |
| 4 | Spawner 1v1–4v4 | correct roster, no overlap, nothing unused | `stats_get` at 2 and 8 characters |
| 5 | Rescue + win/lose | rescue works; elimination matches 1v1 today | 30 FPS held |
| 6 | HUD | scaling counters, both flag states | `capture_ui_canvas` shows nothing moved |
| 7 | Roles + scoring + debug | roles differ and rotate; presets measurable | ≤1 decision/frame, 0 allocations, 30 FPS at 4v4 |

## Edge cases and risks
- **I am captured with teammates free** — no state change; overlay only; AI teammates can rescue me.
- **Last free teammate captured while rescuing** — rescues resolve first in the frame, so a same-frame free-and-capture is honoured; elimination is evaluated only afterwards.
- **Two AI target the same prisoner or flag** — the role assigner caps Raider/Rescuer at 1 with a reserve list; `MatchManager` is the safety net (a second rescuer finds no prisoner, a second Raider finds the flag already carried).
- **Carrier captured or freed in the same frame** — the drop already happens *before* `Capture()`; `Release()` touches no flag and places the character outside the prison, so a dropped flag can never land inside one.
- **Prison camp loop** — 2 s immunity + 3 s cooldown stop the same-frame re-capture.
- **Restart mid-match** — the reload rebuilds spawns, `TeamManager`, roles and flags; `MatchSettings` is static so size/difficulty survive. Test from Title, Paused, Victory and GameOver.
- **AI stuck** — no NavMesh means direct steering into walls: clamp all AI waypoints to x ±17.0 / z ±10.2 and add a `stuckSeconds` watchdog (moved <0.05 in 1.0 s while commanded to move → sidestep 90° for 0.4 s).
- **Arena change reaching the backdrop** — the north wall must stay at z 8.75; the nearest backdrop body is at z 10.15, so growth north is impossible without redoing the visuals. That is why the enlargement is west/east/south only.
- **`BarangayBuild.Prim` collider trap** — routing `Ground` or `Wall_*` through it would silently delete their colliders and drop every character through the floor.
- **Unfair instant loss** — elimination runs after rescues and never while a rescue is in range; the optional timer prevents stalls; you cannot lose while a teammate is free.
- **Title/Ready with AI running** — `EnemyAI` currently ignores `matchActive`, so all 7 AI would walk during Ready. Gating on `MatchActive` is required, not optional.
- **Regression risk** — the core rules (fieldTime, tie, base safety, flag states) are untouched; the changes are list-shaped (per-team), not rule-shaped.

## Fairness / balance per size (default tuning)
- **1v1** — unchanged plus the smarter brain; the single AI is Flex and must switch. Escape run is 18.8 m vs 2 s of sprint, so it stays tight; the smaller base slightly favours the chaser.
- **2v2** — you + 1 AI Defender vs 2 AI; the Red Raider against your Defender is the whole game. Watch for a 2v1 snowball while your Defender is imprisoned (the 1.5 s role review pulls the Raider home).
- **3v3** — cap Raider at 1 so Blue is never outnumbered per role; the third slot should be Flex, not a second Raider.
- **4v4** — prison pressure decides it; verify 2 s immunity / 3 s cooldown do not make rescue strictly dominant (a 5-free-for-2-imprisoned loop).
- Global levers: `chaseFresherMargin`, `roleReviewInterval`, `stealBias`, `immunitySeconds`, `rescueCooldown`, `matchTimeSeconds`, and the new base radius.

## Step 5 and the HUD - verified in Play mode (4v4, Hard)

A temporary harness (since deleted) drove the real `MatchManager` and the real characters through
every ending, one scenario at a time, and logged which rules events fired. **43 checks, 43 passed.**

- **Rescue** - a free Blue touching an imprisoned team-mate freed it: placed at the prison exit on
  the side facing its own base (10.90, the 3 x 3 plate's 1.5 m half-width + 0.6 m clear),
  `fieldTime` 15.00 before the capture -> **0.00** after, **2.00 s** immunity, **3.00 s** cooldown.
- **The rescuer gets nothing** - it was tagged mid-run by a fresher enemy (fieldTime 5.03 v 0.03).
- **No chain-rescue** - the 3 s cooldown refused an immediate second rescue of the same prisoner.
- **Human captured with team-mates free** - the match kept running, no outcome was recorded, and
  the overlay read `CAPTURED - waiting for a rescue`.
- **Elimination** - every Blue imprisoned -> `GameOver`, "GAME OVER / Every one of your team was
  locked in prison." Events: `TeamWon(Red)`, `TeamLost(Blue)`, `PlayerLost`.
- **Time limit** - 4 free v 3 free -> `TimeLimit`, Victory, "Time ran out - your team was ahead.";
  a genuinely level board (3 v 3, equal average fieldTime) -> `MatchDrawn`, `Draw`.
- **1v1 still reads like the old game** - Blue trimmed to the human alone -> `GameOver`,
  "GAME OVER / You were captured."
- **HUD** - `BLUE  4/4 free  |  flag: AT BASE`, `RED  4/4 free  |  flag: AT BASE`; a Red carrying
  the Blue flag turned the line to `CARRIED` and raised `YOUR FLAG HAS BEEN TAKEN!`, which cleared
  when the flag went home. The old "Captured X / N" label is gone, and `capture_ui_canvas` on
  `Game/UI/Panel_Hud` shows both lines clear of the pause and sprint buttons.
- Not separately exercised: the `DROPPED` string (one branch off the same `Flag` state machine the
  capture-the-flag work already drives) and the joystick, which the preview hid - it sits
  bottom-left, three bands below the Blue line.

Note for any future Play-mode check: the editor was **paused** for the first harness run, so no
frame advanced and every frame-driven check failed. `EditorApplication.isPaused` must be false
before trusting a Play-mode result.

## Test cases (for the test record)
| # | Test | Expected |
|---|---|---|
| TC-01 | Play 1v1 to a win and a loss | Identical behaviour to the shipped build; nothing unused in the hierarchy |
| TC-02 | Pick 3v3 + Hard, then Restart | Starts at 3v3/Hard without revisiting Setup; PlayerPrefs survive an editor restart |
| TC-03 | Arena screenshot + `Report()` | Same composition as before: near edge and horizon within a few % of their old values; no backdrop body inside the walls |
| TC-04 | Spawn check at 1v1/2v2/3v3/4v4 | 0/1/2/3 Blue AI, 1/2/3/4 Red AI, no overlaps, no leftovers |
| TC-05 | Four characters walk into `Base_Blue` | All four fit on the 6.4 plate; none overlaps; all read `fieldTime 0.00` |
| TC-06 | Imprison four characters in `PrisonForRed` | All four stand on the 3.0 plate, none through a post or the wall |
| TC-07 | Watch 60 s at 4v4 with role labels on | Roles differ per AI and rotate when a flag is taken or a prisoner exists |
| TC-08 | Free a teammate in `PrisonForRed` | Unfreezes at the prison exit, `fieldTime 0.00`, 2 s immunity, no instant re-capture |
| TC-09 | Stand beside a prisoner while an enemy tags you | You are captured normally — no immunity for the rescuer |
| TC-10 | Two AI sent to the same prisoner | Only one goes; the second re-roles |
| TC-11 | Your flag is stolen by a Raider | `YOUR FLAG HAS BEEN TAKEN!` shows; the HUD flag state reads CARRIED |
| TC-12 | Blue AI carries the Red flag into `Base_Blue` | Victory with the correct text |
| TC-13 | Red AI carries the Blue flag into `Base_Red` | Game Over with the correct text |
| TC-14 | Imprison every member of a team | Elimination fires once, with the right result |
| TC-15 | You are imprisoned while an AI teammate is free | Overlay shows; the match continues; the HUD keeps updating |
| TC-16 | Record `stats_get` + `get_worst_cpu_frames` at 1v1 and 4v4 | 30 FPS held at 4v4; draw calls rise only by the character renderers |
| TC-17 | Pause / Resume / Restart / Title mid-match at 4v4 | No stuck AI, no stuck sprint, flags reset, settings preserved |
| TC-18 | Leave the AI alone for 5 minutes at 3v3 | No AI pinned to a wall; the stuck watchdog recovers any that is |
| TC-19 | `check_compile_errors` after every step | Clean, with `EditorApplication.isPaused` false before trusting any Play-mode result |

## Verify
- `check_compile_errors` clean and `EditorApplication.isPaused` false after each step; `get_unity_logs` shows the registration / spawn / rescue logs.
- Step 1 must pass a full 1v2 regression before anything else changes.
- Step 3 is verified by `BarangayBuild.Report()` (it prints camera position, pitch, orthoSize, aspect and the near-edge / far-wall / horizon percentages) plus a `capture_editor_screenshot` before and after, so the composition is proven unchanged.
- `stats_get` draw calls at 2 and 8 characters, and `get_worst_cpu_frames` at 4v4, to confirm the 30 FPS / <2 ms AI budget.
- `capture_ui_canvas` on `Game/UI` before and after, to prove the freshness bar, freshness label, stamina bar, pause button, joystick and sprint button never moved.
