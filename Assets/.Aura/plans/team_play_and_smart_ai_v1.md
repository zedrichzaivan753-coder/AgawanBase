# Team Play (N vs N) + Rescue + Team-Agnostic Smart AI

## Objective
Turn the hard-coded 1v2 match into a selectable 1v1–4v4 in which the human is the only non-AI character, add prison rescue, and give both teams the same utility-scored AI — with the existing fieldTime / prison / capture-the-flag rules unchanged.

## Verified baseline (measured live this session)
- Unity **6000.6.0f1**, URP **17.6.0**, Input System **1.20.0**, uGUI **2.6.0**. `com.unity.ai.navigation 2.0.14` is installed but **unused** — `BarangayBuild.cs:604` says the project has no NavMesh and `grep NavMesh` over all `.cs` returns nothing. **Budget NavMeshAgents = 0.**
- One scene, `Assets/Scenes/Game.unity` (`GameManager.sceneName = "Game"`); `Restart`/`Title` reload it. So a **static** settings class survives any restart, exactly like the existing `GameManager.startPlayingOnLoad`.
- Characters are prefab instances: `BluePlayer` (−10.2, 0.08, 0.11), `Enemy_Red_1` (10.6, 0.08, −0.47), `Enemy_Red_2`. Prefabs: `Assets/Prefabs/BluePlayer.prefab` (has `PlayerController`), `Assets/Prefabs/RedEnemy.prefab` (has `EnemyAI`) — both also carry `CharacterController`, `CharacterMotor`, `CharacterStatus`, `SprintStamina`, `CharacterRigAnimator`.
- Arena: `Ground` 32×18 (x ±16, z ±9), court slab 31×17.4, walls at x ±15.75 (`BoxCollider`) and z ±9 → playable **x ±15.5, z ±8.7**.
- `Base_Blue` (−11,0,0) / `Base_Red` (11,0,0), `BaseZone.radius = 4` → two 8-across safe circles with **14 units of open field between them**.
- Prisons: `PrisonForRed` (−13,0,6.5), `PrisonForBlue` (13,0,−6.5), plates **3.15 × 3.15**; `Wall_N` inner face is z = 8.7.
- `MatchManager.PrisonSpot` spreads 4 slots 1.2 apart along Z → z 4.7…8.3, which **bursts the 3.15 plate**. This is the one real capacity bug.
- Camera is **static**: orthographic size 12.2 at (0, 27.7, 3.3), Euler (90,0,0) → at 16:9 it shows x ±21.7, z −8.9…15.5, i.e. the entire arena. **No camera work needed for 8 characters.**
- Rules as shipped: `tagDistance 1.2`, lower `fieldTime` wins, `tieIsNoCapture = true`, `fieldTime` snaps to 0 inside your own base, `MaxFieldTimeSeconds` is `const 20f`.
- `EnemyAI` is hard-wired to the human: `Start()` does `FindAnyObjectByType<PlayerController>()`. 5 states, `decisionInterval 0.25 ± 0.05`, `minStateTime 0.6`, `chaseFresherMargin 0.3`, `guardOffset` (0,−1.5) / (0,+2.5).
- `MatchManager` holds one `player` + `CharacterStatus[2] enemies`, one `PlayerWon` / `PlayerLost` event each, and counts only `redsCaptured`.
- `HudController` prints `"Captured  X / N"` from `GameManager.RedsCaptured/TotalReds`; `FlagLabel` shows only while the player carries.
- **No tags are registered** (`get_tags_and_layers` → `tags: []`) and no script calls `FindGameObjectsWithTag`. Layers: `Default, TransparentFX, Ignore Raycast, Water(4), UI(5)`.
- **There is no `UIManager`, no `FieldTimer`, and no spawner script** — their equivalents are `HudController` + `GameManager`, `CharacterStatus`, and the hand-placed instances.
- HUD canvas is 2035.55 × 1018.69 at `scaleFactor 0.5478` in the 1115×558 game view. Occupied: left column x 50…670 down to y −254; right column x 1425…1985, y −50…−270; joystick x 120…380 y 120…380; sprint x 1685…1885 y 150…350; `FlagLabel` x 468…1568 y −24…−84. At the worst plausible phone (1600×720 @480 dpi, scale ≈0.745) **200 canvas units ≈ 50 dp**, so any new button must be ≥200 units tall.
- No `Assets/Tests` and no test assembly → verification is a manual Play-mode test record.

## Changes
**Modify**
- `Assets/Scripts/Core/Team.cs` — add `TeamUtil.Other()`, `All` and a `ToColor()` helper so nothing re-implements the Blue/Red switch.
- `Assets/Scripts/Core/CharacterStatus.cs` — `Capture(prisonPosition, slot)` picks its own prison slot; `Release(immunitySeconds)` keeps the character in the field (fieldTime reset to 0) and starts an immunity timer; register/unregister with `TeamManager`; expose `SpawnIndex`.
- `Assets/Scripts/Gameplay/MatchManager.cs` — per-team member lists from `TeamManager`; `Team baseOf(Team)`, `Transform prisonOf(Team)`; team-agnostic touch loop (O(A×B), max 16 pairs); 2×2 `PrisonSpot`; `CheckRescueTouches`; per-team capture counters; `event Action<Team> TeamWon/TeamLost`; all-imprisoned elimination; optional `matchTimeSeconds`.
- `Assets/Scripts/Gameplay/EnemyAI.cs` — becomes the shared, team-agnostic brain: target chosen from perception instead of the human, utility scoring, role bias, RESCUE option, gates on `TeamManager.MatchActive`.
- `Assets/Scripts/Gameplay/Flag.cs` — factor out `CanBeTakenBy(Team)`; no logic change.
- `Assets/Scripts/Visual/CharacterRigAnimator.cs` — take the enemy flag and `teamJersey` from the spawner instead of one `FindObjectsByType<Flag>` per character.
- `Assets/Scripts/UI/GameManager.cs` — new `MatchSetup` and `Ready` states; map `TeamWon(Team.Blue)`→Victory, `Team.Lost(Team.Blue)`→GameOver; the captured-but-not-eliminated overlay.
- `Assets/Scripts/UI/HudController.cs` — replace the `Captured X / N` line with two scaling per-team lines + both flag states + an alert line.
- `Assets/Scenes/Game.unity` — add `Game/UI/Panel_MatchSetup` and the 4 new HUD labels; delete the `Enemy_Red_1`/`Enemy_Red_2` instances; add `Game/Systems` (TeamManager + CharacterSpawner).

**Add**
- `Assets/Scripts/Core/MatchSettings.cs` (static settings + the `DifficultyProfile` preset table + PlayerPrefs).
- `Assets/Scripts/Core/TeamManager.cs` (per-team registry, `MatchActive`, free/imprisoned queries, role review).
- `Assets/Scripts/Gameplay/CharacterSpawner.cs` (formation spawning from the one shared AI prefab).
- `Assets/Scripts/UI/MatchSetupUI.cs` (drives the Match Setup panel).
- `Assets/Scripts/UI/AIDebugLabel.cs` (phase B: toggleable role/state label + sight gizmos).

## Part 1 — Match settings
**Recommendation: a small `static class MatchSettings`, not a ScriptableObject.** Reasons: the whole game is one scene that `Restart`/`Title` reload, and a static field already survives that (the project does it today with `GameManager.startPlayingOnLoad`); Setup and gameplay live in the same scene, so there is nothing to carry across; and a ScriptableObject would add an asset file, a load path (Resources or an Inspector reference) and an instance pass — three failure points buying nothing at this scope. `MatchSettings` also becomes the single owner of PlayerPrefs (`match.teamSize`, `match.difficulty`). Trade-off: static state is not data-driven and needs `Load()` called once at boot (domain-reload-off in the Editor would otherwise keep the last session's values).

Match Setup screen: reachable from Title's START, landscape, one row of four size buttons (**260×200**) `1v1 2v2 3v3 4v4`, one row of three difficulty buttons (**260×200**) `EASY NORMAL HARD`, then `BACK` and `START` (480×130) — all ≥200 units tall so they clear 48 dp on the worst phone. Selection is remembered in PlayerPrefs and re-shown on re-entry.

Spawning: `CharacterSpawner` instantiates `(N−1)` Blue AI and `N` Red AI from the one shared `RedEnemy.prefab`, placing them on a 2×2 formation (±1.2 on each axis) inside each base, and sets `status.team`, `status.homeBase`, `rig.teamJersey` (`Mat_Blue`/`Mat_Red`), `rig.flag` and `EnemyAI.ourFlag` per spawn. The human `BluePlayer` stays hand-placed (it needs the joystick/sprint wiring). The two hand-placed Red instances are deleted, so no unused characters remain.

`TeamManager` API: `Instance`, `Members(Team)` (cached list), `FreeCount(Team)`, `CapturedCount(Team)`, `NearestFreeEnemy(Team, Vector3, float)`, `NearestCapturedTeammate(Team, Vector3)`, `AllImprisoned(Team)`, `MatchActive`, plus role review. Registration is `OnEnable`/`OnDestroy` on `CharacterStatus` — never `FindObjectsOfType`, and nothing in `Update` searches.

Capacity: prisons get a **2×2 slot grid** at 0.9 spacing (span 1.8 inside the 3.15 plate) for 4 prisoners; bases already hold 4 characters comfortably inside a radius-4 circle. **Arena verdict: no change needed** — the plan assumes answer (a) to Q2; if you pick (b) or (c) I will add a follow-up step.

## Part 2 — Rescue
A free character within `rescueDistance` 1.5 of an imprisoned **teammate in the enemy prison** frees them. Judged in `MatchManager.CheckRescueTouches` (distance only, same style as the flags, no triggers). The freed prisoner is unfrozen, teleported to a spot just outside the prison, `fieldTime` reset to **0**, and given `immunitySeconds` **2** during which `ResolveTouch` skips them. **Why fieldTime 0:** it is what `Release()` does today, it is exactly what the rescuer risked the trip to buy, and it makes the freed character *fresher* than the enemy standing at the prison — a real comeback rather than a re-capture loop. Immunity covers the walk out; the rescuer gets **no** immunity (it is outside its base, so its fieldTime is climbing and a free enemy can tag it under the normal rule). Anti-abuse: `rescueCooldown` 3 s per prisoner. Both teams' AI can rescue through the RESCUE option.

## Part 3 — Win/lose, elimination, HUD
- Win: be free, carry the enemy flag, step into your own base (unchanged) → `TeamWon(myTeam)`. Lose: an opponent does the same with your flag.
- Elimination (**recommended, Q1a**): when every member of a team is imprisoned that team loses; in 1v1 this is byte-for-byte today's "I was captured". Order inside one frame: **Rescues → Touches → Win → Elimination**, so a rescue landing in the same frame as a capture is honoured and nothing ends the match while a rescue touch is in range.
- Human captured, teammates free: **no state change.** Frozen in prison with a `CAPTURED — waiting for a rescue` overlay (new `AlertLabel`, initially inactive). The camera is static and already shows the whole pitch, so there is nothing else to do.
- Optional tie-breaker: `matchTimeSeconds` (default **180**, `0` = off) `/ 60`, on expiry the team with more free members wins; tie → lower average `fieldTime`; still tie → `DRAW` on the end screen. Marked optional and off is a valid setting.
- HUD: delete `"Captured X / N"`. Add `BlueStatusLabel` anchor(0,1) pivot(0,1) **(50, −262) 620×36** and `RedStatusLabel` anchor(1,1) pivot(1,1) **(−50, −280) 620×36**, each reading e.g. `BLUE  2/3 free  |  our flag: CARRIED`. Add `AlertLabel` anchor(0.5,0.5) **(0, 150) 1100×70**, inactive, for `YOUR FLAG HAS BEEN TAKEN!` and `CAPTURED - waiting for a rescue`. Every position is in verified free space: the left column ends at y −254, `CaptureLabel` ends at −270, the joystick occupies x ≤380, the sprint button x ≥1685, and the centre-top band is empty. Keep ASCII glyphs only (legacy `Text`, no TMP).

## Part 4 — Smarter AI (one brain, both teams)
Approach: keep the enum state machine, choose the state by **utility scoring** each decision tick (score every legal option, take the best, add a little noise). No ML, no packages.

```
AIBrain — decides every ~0.25 s, phase-staggered by spawn index
  captured ──────────────────────────────────────────────► frozen, no decisions
  every roleReviewInterval (1.5 s, staggered +25 ms) TeamManager re-derives team roles
  Defender → DEFEND    hold own flag (guard post inside own base)
  Raider   → STEAL     run to the enemy flag, grab, CARRY it home
  Rescuer  → RESCUE    run to the enemy prison, touch an imprisoned teammate
  Hunter   → HUNT      chase a taggable enemy (lower fieldTime by the margin)
  Flex     → best of the above by score
  hard vetoes (a score can never override them)
    HUNT needs IAmFresher(margin) && in sight && Stamina01 >= chaseEnterStamina01
    INTERCEPT replaces HUNT when an enemy carries our flag but HUNT is vetoed
    RESET fires when fieldTime > 0.6 x 20 s and nothing is urgent
```
States: `Defend, Hunt, Intercept, ReturnFlag, Steal, Carry, Rescue, Bait, Reset`.

| Option | Positive terms | Negative terms | Hard gate |
|---|---|---|---|
| DEFEND | `1 - enemyDist/defendRadius`; +0.4 if own flag carried/dropped | −0.3 if own flag at base and no enemy inside radius | none |
| STEAL | `1 - myDist/mapDiagonal`; +0.5 if own flag at base and a teammate defends; +0.3×riskTolerance | −0.6 if I already carry the enemy flag; −0.4 if I am the only free teammate | must be free |
| CARRY | `1.0` (always top once I hold the enemy flag) | −0.2 × distToHome/mapDiagonal | none |
| HUNT | `fresherBy/10s` + `0.4 × (1 - dist/visionRadius)` | −0.5 if target is in its own base | `IAmFresher`, in sight, stamina ≥ `chaseEnterStamina01` |
| INTERCEPT | `0.7 + 0.3 × (1 - dist/visionRadius)` | −0.4 if my fieldTime ≥ target's | target carries my flag, HUNT vetoed |
| RETURNFLAG | `0.9` if my flag Dropped, scaled by `1 - dropTimer/10s` | −0.6 if an enemy is closer by `flagRaceAdvantage` | own flag dropped |
| RESCUE | `0.6 + 0.4 × (1 - dist/mapDiagonal)`; +0.5 if a teammate is imprisoned | −0.8 if I am the only free teammate and my flag is carried | teammate imprisoned, I am free |
| BAIT | `0.5 × patience`; +0.3 if a teammate is closer to the enemy flag than me | −1.0 if no free teammate could profit | ≥2 free teammates |
| RESET | `fieldTime/20s`; +0.5 if winded | −1.0 if a teammate is imprisoned and within reach | none |

`score += Random.Range(-noise, noise)` with `noise = personality.instability × difficulty.noise`; switch only when `best > current + switchMargin` and `stateTimer ≥ minStateTime` (keeps the existing anti-flap hysteresis).

| Match | Chars | AI per team (Blue / Red) | Default role mix per team |
|---|---|---|---|
| 1v1 | 2 | 0 / 1 | 1 × Flex (must switch offense/defense) |
| 2v2 | 4 | 1 / 2 | 1 × Defender + 1 × Raider (Blue's AI defends while you raid) |
| 3v3 | 6 | 2 / 3 | 1 × Defender + 1 × Raider + 1 × Flex |
| 4v4 | 8 | 3 / 4 | 1 × Defender + 1 × Raider + 1 × Rescuer + 1 × Flex |

Roles are re-derived every 1.5 s from the situation: an own flag stolen/dropped forces an extra Defender, a prisoner forces one Rescuer, and nobody raids while the last free teammate is needed at home. The human always fills the gap, so Blue's AI count is one fewer.

Freshness tactics come out of the scoring, not extra code: `RESET` scores with accumulated fieldTime, `HUNT` is vetoed unless `IAmFresher` by `chaseFresherMargin`, and `Bait` exists so one AI draws attention while another raids. Flag stealing is already team-agnostic (`Flag.ownerTeam` + `TryPickUp` refuses your own) — the Raider only adds the `STEAL`/`CARRY` walking, and `MatchManager` alone judges pickup/drop/return, so no logic is duplicated. A HUD warning fires when **my** flag is carried.

Perception: `visionRadius` 12 m (Easy 10 / Hard 14) plus `memorySeconds` 3 s of last-known-position per target. **Zero raycasts** — the court slab is flat and empty inside the walls (all backdrop geometry is outside the arena), so occlusion does not exist and distance alone is honest. Unpredictability: per-instance `aggression`, `patience`, `riskTolerance`, `instability` rolled once (replacing the current `aggression`/`reactionTime` roll), a reaction delay before acting on a new decision, and approach offsets chosen per personality on the decision tick — never per-frame jitter. Stamina reuses the shipped pattern: sprint only when `distance ≥ sprintDistance`, and `chaseEnterStamina01` blocks a winded chase.

| Preset | decisionInterval | reaction delay | chaseFresherMargin | aggression | riskTolerance | stealBias | visionRadius | memorySeconds |
|---|---|---|---|---|---|---|---|---|
| Easy | 0.35 s | 0.30–0.60 s | 0.60 s | 0.70–0.95 | 0.15 | 0.7 | 10 m | 2.0 s |
| Normal | 0.25 s | 0.10–0.35 s | 0.30 s | 0.85–1.15 | 0.30 | 1.0 | 12 m | 3.0 s |
| Hard | 0.20 s | 0.05–0.15 s | 0.15 s | 1.00–1.30 | 0.50 | 1.3 | 14 m | 4.0 s |

**Teammate skill: recommend NO separate setting.** One difficulty applies to all seven AI because (a) fairness is your stated rule, (b) the design is already symmetric — identical `moveSpeed 6`, `sprintSpeedMultiplier 1.6`, one shared `SprintStamina`, (c) a hidden second difficulty makes "the AI cheated" indistinguishable from "my teammate is bad", and (d) the honest lever already exists: the preset itself, which weakens or strengthens both teams equally. Label the Setup row `AI SKILL (BOTH TEAMS)`.

## Match flow
```
Title ─START─► Match Setup ─START─► Ready (1.5 s, everyone frozen) ─► Playing
                                                                     │
  Playing ─PAUSE─► Paused ─RESUME─► Playing                          │
  Playing ─I carry the enemy flag into my base──────► Victory  ─► (Restart | Title)
  Playing ─an opponent carries my flag into their base─► Game Over
  Playing ─every member of my team imprisoned───────► Game Over (elimination)
  Playing ─every enemy imprisoned───────────────────► Victory  (elimination)
  Playing ─I am imprisoned, teammates free──────────► Playing + overlay, no state change
  Playing ─matchTimeSeconds expires─────────────────► Victory / Game Over / Draw
```
Win/lose stays in the one authority: `MatchManager` raises `TeamWon(Team)` / `TeamLost(Team)`, `GameManager` maps Blue→Victory, Red→GameOver and owns every panel.

## Performance budgets
| Item | Budget | Basis |
|---|---|---|
| Characters | **8 max** (4v4) | ground 32×18, bodies 1 m across |
| AI brains | **7 max** (3 Blue + 4 Red) | 4v4 roster |
| NavMeshAgents | **0** | no NavMesh exists; direct steering already ships |
| Decision interval | **0.25 s** ± 0.05, phase-staggered `firstDelay = (index/count) × interval` | ≤1 AI decides per frame |
| Decisions / second | **28** at 4v4, **0** while `matchActive` is false | 7 brains × 4 Hz |
| Cost per decision | ≤ **64 float ops** (7 enemies + 4 prisoners + 2 flags), **0 allocations, 0 raycasts** | pre-allocated buffers, no LINQ, no `new` |
| Role review | 1.5 s per team, staggered +25 ms | ~1.3 Hz total |
| Frame budget | AI + movement < **2 ms** of 33.3 ms @30 FPS | `get_worst_cpu_frames` |
| Draw calls | characters 3 → 8, i.e. +65 renderers (+5 × 13); SRP Batcher covers them (few URP/Lit materials) | `stats_get` before/after |
| Update rate at 4v4 | **do not drop it.** 28 decisions/s of ~64 float ops is nothing beside rendering; slowing the brains would make the AI visibly dumber exactly when the match gets more interesting. Spend the budget on staggering instead. | measured |

## Steps
- [ ] 1. Team-agnostic core: add `TeamManager` + registration in `CharacterStatus`, switch `MatchManager` to per-team lists (`baseOf`/`prisonOf`), make `EnemyAI` pick its target by team + perception instead of `FindAnyObjectByType<PlayerController>`, gate `EnemyAI.Update` on `TeamManager.MatchActive`, and route `CharacterRigAnimator`'s flag lookup through the spawner. Add `Debug.Log` on every registration and every target change. *(done when: 1v2 still plays exactly as before — movement, tag, capture, prison, flag carry, victory, game over — and `TeamManager.Members` reports 1 Blue + 2 Red)* [depends: none]
- [ ] 2. Create `Assets/Scripts/Core/MatchSettings.cs` (static, `TeamSize` 1–4, `Difficulty` enum, the `DifficultyProfile` table, PlayerPrefs `Load`/`Save`) and `Game/UI/Panel_MatchSetup` with four size buttons (260×200), three difficulty buttons (260×200), `BACK`/`START` (480×130), driven by `Assets/Scripts/UI/MatchSetupUI.cs`; add `GameState.MatchSetup` to `GameManager` and point Title's START at it. *(done when: Title → SETUP → pick 3v3 + Hard → START works, and re-entering Setup still shows 3v3/Hard; every button is ≥200 units tall)* [depends: 1]
- [ ] 3. Create `Assets/Scripts/Gameplay/CharacterSpawner.cs`: instantiate `(N−1)` Blue AI + `N` Red AI from `Assets/Prefabs/RedEnemy.prefab` on a 2×2 formation (±1.2) inside each base, configuring team / jersey / homeBase / flags per spawn, and **delete `Game/Characters/Enemy_Red_1` and `Enemy_Red_2`**. Log the spawn roster. *(done when: 1v1 spawns exactly 1 AI and 4v4 spawns 3 Blue + 4 Red with no overlapping bodies and nothing unused left in the hierarchy)* [depends: 2]
- [ ] 4. Capacity: replace `MatchManager.PrisonSpot` with a 2×2 slot grid at 0.9 spacing (span 1.8 inside the 3.15 plate) and have `CharacterStatus.Capture` claim its own slot; keep `BaseZone.radius 4`. *(done when: four prisoners stand cleanly inside `PrisonForRed` without any poking through the plate or the north wall, and four characters in `Base_Blue` never overlap)* [depends: 3]
- [ ] 5. Rescue + team win/lose: `MatchManager.CheckRescueTouches`, `CharacterStatus.Release(immunitySeconds)` (fieldTime 0, immunity 2 s, `rescueCooldown` 3 s), per-team counters, `TeamWon`/`TeamLost`, all-imprisoned elimination, optional `matchTimeSeconds` 180 (0 = off), and the AI `RESCUE` option; frame order Rescues → Touches → Win → Elimination. Log every rescue, capture and match end with both team counts. *(done when: touching a teammate in the enemy prison frees them with 2 s immunity and fieldTime 0.00; an enemy can still tag the rescuer mid-run; 1v1 elimination still reads exactly like today's "I was captured")* [depends: 4]
- [ ] 6. HUD: delete `"Captured  X / N"`, add `BlueStatusLabel` (50,−262) 620×36, `RedStatusLabel` (−50,−280) 620×36, `AlertLabel` centre (0,150) 1100×70 (inactive) with the captured overlay and the `YOUR FLAG HAS BEEN TAKEN!` warning, plus both flags' at-base/carried/dropped state; only assign a string when it changed. *(done when: the counters scale with team size at 1v1–4v4, both flag states read correctly live, and nothing overlaps the joystick, sprint button or pause button)* [depends: 5]

## Build order (full — items 7–10 get their own plans)
| # | Task | Done when | Perf check |
|---|---|---|---|
| 1–6 | Steps above (team-agnostic → Setup → spawn → capacity → rescue → HUD) | as each step states | `stats_get` draw calls after step 3; `get_worst_cpu_frames` after step 6 |
| 7 | Utility scoring + team-level role assignment in `EnemyAI`/`TeamManager` | at 4v4 teams hold 1 Defender / 1 Raider / 1 Rescuer / 1 Flex and roles rotate when the flag or a prisoner changes | ≤1 AI deciding per frame; 0 allocations per decision |
| 8 | Freshness tactics, flag stealing for both teams, baiting, HUD flag warning | a Raider steals and carries home; a teammate baits an enemy out; you are never tagged without the fieldTime rule allowing it | 28 decisions/s; no per-frame raycast |
| 9 | Debug aids: `AIDebugLabel` role/state label + sight gizmos; perception memory tuning | toggling the label shows role + state on all 7 AI; gizmos match `visionRadius` | label off in the shipping build costs 0 draw calls |
| 10 | Personality, difficulty presets, balance pass at every size | Easy/Normal/Hard measurably change reaction, aggression and steal frequency on **both** teams | 30 FPS held at 4v4 on device |

## Edge cases and risks
- **I am captured with teammates free** — no state change; overlay only. Rescuable by AI teammates.
- **Last free teammate captured while rescuing** — rescues resolve first in the same frame, so a simultaneous free-and-capture is honoured; elimination is only evaluated after.
- **Two AI go for the same prisoner or flag** — `RoleAssigner` caps Raider/Rescuer at 1 and holds a reserve list; `MatchManager` is the safety net (a second rescuer finds no prisoner, a second Raider finds the flag already carried).
- **Carrier captured or freed in the same frame** — the drop already happens *before* `Capture()`; `Release()` touches no flag and teleports to the prison exit, so a dropped flag can never land in a prison.
- **Capture loop at the prison** — the freed prisoner's 2 s immunity plus `rescueCooldown` 3 s stop a same-frame re-capture.
- **Restart mid-match** — the scene reload rebuilds spawns, `TeamManager`, roles and flags; `MatchSettings` is static so the chosen size/difficulty survives. Test from Title, Paused, Victory and GameOver.
- **AI stuck** — there is no NavMesh, so this is direct steering into walls: clamp every AI waypoint into x ±15.0 / z ±8.2 and add a `stuckSeconds` watchdog (moved <0.05 over 1.0 s while commanded to move → sidestep 90° for 0.4 s).
- **Crowding** — bases are fine at radius 4; prisons are fixed by the 2×2 grid; spawns use ±1.2 formations.
- **Unfair instant loss** — elimination runs after rescues, never while a rescue is in range; the optional timer prevents stalls; you are never lost while a teammate is free.
- **Title/Ready with AI running** — `EnemyAI.Update` currently ignores `matchActive`, so all 7 AI would walk during Ready. Gating on `TeamManager.MatchActive` is required, not optional.
- **Regression risk** — the core rules (fieldTime, tie, base safety, flag states) are not touched; the changes are list-shaped (per-team) rather than rule-shaped.

## Fairness / balance per size (default tuning)
- **1v1** — unchanged plus the smarter brain; the single AI is Flex and must switch. Escape run is 18 units against 2 s of sprint, so it stays tight.
- **2v2** — Blue is you + 1 AI Defender, Red is 2 AI. The Red Raider vs your AI Defender is the whole game; watch for a 2v1 snowball when your Defender is imprisoned (`roleReviewInterval` 1.5 s pulls the Raider home).
- **3v3** — cap Raider at 1 so Blue is never outnumbered per role; if matching stays even, the third slot should be Flex, not a second Raider.
- **4v4** — prison pressure becomes the deciding lever; verify `immunitySeconds` 2 / `rescueCooldown` 3 do not make rescue strictly dominant (5 free-for-every-2-imprisoned).
- Global levers: `chaseFresherMargin`, `roleReviewInterval`, `stealBias`, `immunitySeconds`, `matchTimeSeconds`.

## Test cases (for the test record)
| # | Test | Expected |
|---|---|---|
| TC-01 | Play 1v1 to a win and to a loss | Behaviour identical to the shipped 1v2 build; no unused characters in the hierarchy |
| TC-02 | Pick 3v3 + Hard in Setup, then Restart | Match starts at 3v3/Hard without revisiting Setup; PlayerPrefs survive an editor restart |
| TC-03 | Spawn check at 1v1/2v2/3v3/4v4 | 0/1/2/3 Blue AI and 1/2/3/4 Red AI, no overlapping bodies, no leftovers |
| TC-04 | Watch 60 s at 4v4 with role labels on | Roles differ per AI and rotate when the flag is taken or a prisoner exists |
| TC-05 | Free a teammate in `PrisonForRed` | Prisoner unfreezes at the prison exit, `fieldTime` reads 0.00, 2 s immunity, no instant re-capture |
| TC-06 | Stand next to a prisoner as an enemy tags you | Rescuer is captured normally — no immunity for the rescuer |
| TC-07 | Two AI sent to the same prisoner | Only one goes; the second re-roles |
| TC-08 | Your flag is stolen by a Raider | `YOUR FLAG HAS BEEN TAKEN!` appears; the flag state reads CARRIED in the HUD |
| TC-09 | Blue AI carries the Red flag into `Base_Blue` | Victory with the correct result text |
| TC-10 | Red AI carries the Blue flag into `Base_Red` | Game Over with the correct result text |
| TC-11 | Get every member of a team imprisoned | Elimination fires once, with the right result |
| TC-12 | You are imprisoned while an AI teammate is free | Overlay shows; the match continues; the HUD keeps updating |
| TC-13 | Record `stats_get` and `get_worst_cpu_frames` at 1v1 and 4v4 | 30 FPS held at 4v4; draw calls rise only by the character renderers |
| TC-14 | Pause / Resume / Restart / Title mid-match at 4v4 | No stuck AI, no stuck sprint, flags reset, settings preserved |
| TC-15 | Leave the AI alone for 5 minutes at 3v3 | No AI pinned against a wall; the stuck watchdog recovers any that is |
| TC-16 | `check_compile_errors` after each step | Clean, with `EditorApplication.isPaused` false before trusting any Play-mode result |

## Verify
- `check_compile_errors` clean and `EditorApplication.isPaused` false after every step; `get_unity_logs` shows the registration/spawn/rescue logs.
- Step 1 must be proven with a full 1v2 regression pass before anything else changes.
- `stats_get` draw calls at 3 and 8 characters, and `get_worst_cpu_frames` at 4v4, to confirm the 30 FPS / <2 ms AI budget.
- `capture_ui_canvas` on `Game/UI` before and after, to prove the freshness bar, freshness label, stamina bar, pause button, joystick and sprint button never moved.

## Open decisions (defaults assumed above)
1. **Q1 elimination** — (a) immediate loss on all-imprisoned *(assumed)*, (b) last prisoner walks free once, (c) 10 s grace window.
2. **Q2 arena** — (a) no change, prison grid only *(assumed)*, (b) base circles to radius 3.2, (c) enlarge and re-frame.
3. **Q3 spawning** — (a) runtime instantiate of one shared AI prefab *(assumed)*, (b) pre-placed 4+4 roster, enable as needed.
