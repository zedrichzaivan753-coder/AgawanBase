# Capture the Flag

## Objective
Change the win from "touch the Red flag" to a real capture-the-flag loop: pick up the flag, carry it home, and drop/return it if the carrier is caught.

## Changes
- `Assets/Scripts/Gameplay/Flag.cs` - (create) flag state machine (AtBase/Carried/Dropped), carry follow, drop timer.
- `Assets/Scripts/Gameplay/MatchManager.cs` - pickup detection, drop-on-capture, new win check; remove the two old flag rules.
- `Assets/Scripts/Gameplay/EnemyAI.cs` - add `Intercept` + `ReturnFlag` states; reuse `Chase`/`Patrol` for chasing the carrier and defending.
- `Assets/Scripts/Core/SprintStamina.cs` - carry penalty (`carrySpeedMultiplier`, `carryRegenMultiplier`, `IsCarryingFlag`).
- `Assets/Scripts/UI/HudController.cs` - flag indicator text.
- `Assets/Scripts/UI/GameManager.cs` - result-text wording + reset flags on Title/Victory/GameOver.
- `Assets/Scenes/Game.unity` - add `Flag` to both flags, clear `IsStatic`, re-wire `redFlag`/`blueFlag`, add `FlagLabel`.

## Relevant Assets and Quirks
- **Flags sit at the exact base centres**: `Flag_Red` (11,0,0), `Flag_Blue` (-11,0,0). Bases have `radius 4`, so the flag is *deep* in enemy territory.
- **Flags have no colliders** — the current grab is a pure distance check (`FlatDistance(player, redFlag.position) <= flagDistance (1.5)`). Keep distance checks, no physics triggers needed.
- **`Flag_Red` is `IsStatic: true`** (as are `Pole`/`Banner`). A static-flagged object can be excluded from updates by static batching, so a "carried" flag may not visibly move. Must be cleared or the whole feature looks broken.
- **Capture is permanent and ends the match** (`MatchManager.ResolveTouch` → `matchActive = false` → `PlayerLost` → Game Over). Consequence: a captured *carrier* drops the flag at the moment the match already ends, so drop/return/timeout can only be observed via the debug hook or the stretch goal. This is why Step 4 needs its own test hook.
- **Drop must happen BEFORE `Capture()`** — `Capture` teleports the carrier to the prison, so dropping afterwards would strand the flag in the prison.
- **The race is already fair**: everyone shares `moveSpeed 6` / `sprintSpeedMultiplier 1.6`, and a fresh player entering the Red base *ties* with a defending Red (`fieldTime` 0 vs 0, `tieIsNoCapture = true`) — that draw is the window to grab the flag. A **speed** penalty breaks this (the pursuer closes a 0.9x gap and the carrier never escapes); a **regen** penalty keeps the chase an even race. So default to regen-only.
- **Escape is a 18-unit run** (flag x=11 → Blue base edge x=-7) and stamina is 2s of sprint, so one full sprint nearly covers it. Tight by design.
- `guardOffset` for Red_1 is `(0,-2.5)`, Red_2 `(0,+2.5)`, `patrolRadius 1.5` — patrol already *is* "defend the flag", so no new defend state is needed, only new numbers.
- Prisons are **outside** their bases: `PrisonForBlue` (13,0,-6.5), `PrisonForRed` (-13,0,6.5).
- `Panel_Hud` is `IsActive: false` in edit mode (title screen); GameManager enables it while Playing. A new label will look "missing" until you press START.
- `Restart`/`Title` reload the scene, so flag state self-resets; `Flag.Awake` must capture `HomePosition` from the *initial* transform.
- No new packages or assets needed; the Input System is already in the project.

## State diagrams

**Flag (red; blue is identical but inert in the simple option)**
```
AtBase ──(free Blue player within flagDistance 1.5 of the flag)──────────► Carried
AtBase ──(a Red touches it: nothing)────────────────────────────────────► AtBase
Carried ─(carrier captured: Drop(carrier.position) BEFORE teleport)─────► Dropped
Carried ─(carrier enters their OWN base)────────────────────────────────► AtBase  + VICTORY
Dropped ─(free Red within returnDistance 1.2)───────────────────────────► AtBase
Dropped ─(free Blue player touches it again)────────────────────────────► Carried
Dropped ─(dropTimer <= returnAfterSeconds, default 10s, deltaTime)──────► AtBase
any ─────(Title / Victory / GameOver reset)─────────────────────────────► AtBase
```

**Red AI (Patrol and Chase are reused; 2 new states)**
```
Decide() runs every ~0.25s, all changes gated by minStateTime 0.6s hysteresis
  captured ────────────────────────────────────────────────────────────► frozen
  flag is Dropped  && ShouldReturnFlag ─────────────────────────────────► ReturnFlag
  player carries our flag && IAmFresher && CanStartChase ───────────────► Chase
  player carries our flag && !IAmFresher && player not in base ─────────► Intercept
  player carries our flag && player in their base ──────────────────────► Retreat
  (otherwise the existing rules are unchanged)
  player in base or !IAmFresher ────────────────────────────────────────► Retreat
  fresh + close + has stamina ──────────────────────────────────────────► Chase
  nothing worth doing ──────────────────────────────────────────────────► Patrol (guard the flag)
ReturnFlag → touch the flag → flag goes AtBase
```

## Steps
- [x] 1. Create `Assets/Scripts/Gameplay/Flag.cs` (enum `FlagState`, `HomePosition` in `Awake`, `TryPickUp`/`Drop`/`ReturnHome`/`ResetToHome`, follow in `LateUpdate`, `logFlagEvents`); add `Flag` to `Flag_Red` (`Red`) and `Flag_Blue` (`Blue`); clear `IsStatic` on both flags and their `Pole`/`Banner`; change `MatchManager.blueFlag`/`redFlag` to type `Flag` and re-assign both in the scene [depends: none]
- [x] 2. In `MatchManager.CheckFlagTouches`, pick up the Red flag when the free player is within `flagDistance` (`TryPickUp` refuses a friendly team); attach it above the carrier (`carryHeight 2.3`, `followSpeed 20`) [depends: 1]
- [x] 3. Move the win to a new `MatchManager.CheckWinCondition` (carrier + `player.IsInHomeBase`, run *before* `CheckCharacterTouches`); delete the "touch flag = Victory" rule and the "Red touches Blue flag = Game Over" rule; update `GameManager` result text [depends: 2]
- [x] 4. In `ResolveTouch`, `redFlag.Drop(blue.transform.position)` **before** `Capture()`; implement the three return paths (+ `returnDistance`, `returnAfterSeconds = 10f`, `Dropped` pole tilt, debug drop key) [depends: 3]
- [x] 5. Add `FlagLabel` to `Game/UI/Panel_Hud` (copy `StaminaLabel` for font/colour) and wire `HudController.flagLabel` + `redFlag`; add `IsCarryingFlag`, `carrySpeedMultiplier = 1f`, `carryRegenMultiplier = 0.5f` to `SprintStamina` [depends: 3]
- [x] 6. Add the `Intercept` state to `EnemyAI` + `interceptLeadDistance = 4f`; reuse `Chase` for the carrier; set `Enemy_Red_1.guardOffset (0,-1.5)` / `patrolRadius 2` and `Enemy_Red_2.guardOffset (0,2.5)` / `patrolRadius 3` [depends: 3]
- [x] 7. Add the `ReturnFlag` state + `ShouldReturnFlag` (nearest Red, and not further from the flag than the player × `flagRaceAdvantage`) and `MatchManager` returns the flag on a free Red touch; then run the tuning table and the test record [depends: 4,6]

**Optional / STRETCH (separate plan, do not build now):** one Red runs the Blue flag home — `Flag` already supports it via `ownerTeam`, so it needs only a `Carry` AI state plus `MatchManager` handling a Red carrier reaching `Base_Red`.

## Deviations from the plan as written
Three places where the shipped code deliberately differs from the step text above. All three were forced by
behaviour found during testing, and all three are the *simpler* option:

1. **`carryHeight 1.0`, no `followSpeed` lerp.** Step 2 asked for `carryHeight 2.3` and a `followSpeed 20`
   follow. A lerp lets the flag trail behind a sprinting carrier (9.6 m/s), which reads as the flag being
   left behind. `Flag.LateUpdate` now *snaps* to `carrier.position + up * carryHeight`, and the height was
   lowered to 1.0 so the banner sits above the carrier's head with the pole looking held in his hand.
2. **`ReturnHome()`, not `ResetToHome()`.** Step 1 named a second reset method. One method is enough:
   the drop-timeout, the Red-touch return, the win, and the `GameManager` screen reset all call
   `ReturnHome()`. `TryPickUp` also zeroes `DropTimer`, so "carried" never carries a stale countdown.
3. **`ShouldReturnFlag()` does not look for the "nearest Red".** Step 7's wording implies comparing the two
   enemies, which means reading the other enemy's transform — something neither enemy could actually see.
   It compares only *its own* distance to the dropped flag against the *player's*, and yields when
   `myDistance > playerDistance * flagRaceAdvantage (1.5)`. Both enemies therefore make the same honest,
   independently-derivable decision and the player can predict the race by looking at the field.

## Tuning
All values are serialized fields on the scene components; every number was chosen so the escape is a real
race rather than a formality.

**`Flag` (identical on `Flag_Red` and `Flag_Blue`)**

| Field | Value | Why |
|---|---|---|
| `ownerTeam` | Red / Blue | Only the other team may take it. |
| `carryHeight` | 1.0 | Banner clears the carrier's head; pole reads as held. |
| `returnAfterSeconds` | 10 | The safety net that stops a match stalling. |
| `droppedTiltDegrees` | 70 | Dropped reads at a glance from the top-down camera. |
| `tiltSpeed` | 240 | ~0.3 s to fall over: visible but not sluggish. |
| `logFlagEvents` | true | Every state change is logged; turn off for the final build. |

**`MatchManager`**

| Field | Value | Why |
|---|---|---|
| `tagDistance` | 1.2 | Existing rule, unchanged. |
| `flagDistance` | 1.5 | Larger than `tagDistance`: you can be tagged just *before* you reach the flag. |
| `returnDistance` | 1.2 | A Red must genuinely walk onto its own dropped flag. |
| `tieEpsilon` | 0.01 | Existing draw window. |
| `requiresBothOutsideBases` | false | Bases are protected by `fieldTime == 0`, not by a special case. |
| `tieIsNoCapture` | true | A fresh raider and a fresh defender draw — that draw *is* the steal window. |
| `allowDebugDrop` | true | SPACE drops the flag in the editor (`#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`). |

**`SprintStamina` (shared by the player and both Reds)**

| Field | Value | Why |
|---|---|---|
| `maxStamina` | 2 | 2 s of sprint. |
| `drainPerSecond` | 1 | |
| `regenPerSecond` | 0.7 | ~2.9 s to refill from empty. |
| `regenDelay` | 0.6 | Stops tap-sprinting. |
| `restartThreshold` | 0.4 | The lock-out that makes sprint a resource, not a toggle. |
| `sprintSpeedMultiplier` | 1.6 | 6 → 9.6 m/s. Shared by everyone, so the chase is even. |
| `carrySpeedMultiplier` | **1** | **Deliberate.** Any speed penalty lets the pursuer close the gap and the run home can never be won. |
| `carryRegenMultiplier` | 0.5 | The carry penalty: regen is halved, so a long run out leaves you too winded to sprint all the way back. |

**`EnemyAI` (both Reds), plus the two per-instance differences**

| Field | Value | Field | Value |
|---|---|---|---|
| `detectRadius` | 12 | `minStateTime` | 0.6 |
| `giveUpMultiplier` | 1.4 | `chaseFresherMargin` | 0.3 |
| `homeStopDistance` | 0.5 | `chaseEnterStamina01` | 0.5 |
| `decisionInterval` | 0.25 | `chaseExitStamina01` | 0.1 |
| `decisionJitter` | 0.05 | `sprintDistance` | 7 |
| `patrolPauseSeconds` | 2.5 | `interceptLeadDistance` | 4 |
| `aggressionMin/Max` | 0.85 / 1.15 | `flagRaceAdvantage` | 1.5 |
| `reactionTimeRange` | 0.5 | `returnStopDistance` | 0.6 |

| Instance | `guardOffset` | `patrolRadius` |
|---|---|---|
| `Enemy_Red_1` | (0, −1.5) | 2 |
| `Enemy_Red_2` | (0, +2.5) | 3 |

The two Reds are given different guard posts and different patrol radii so they do not stand on the same
spot and do not cover identical ground; `NextPatrolPoint()` clamps every patrol point inside
`radius − 0.5`, so an idle guard can never drift outside its own base and start collecting `fieldTime`.

`flagRaceAdvantage 1.5` is the one number that decides whether the AI contests a dropped flag: at 1.0 both
sides sprint for it and the outcome is a coin flip; at 1.5 a Red only commits when it would clearly arrive
first, which keeps the "fetch it back" state from being a free teleport home.

## Test record
Runtime proof, driven through the real components on `Assets/Scenes/Game.unity` in Play mode (Unity
6000.6.0f1). Every reading below was taken from live scene objects, not from a simulation, and the state
transitions were confirmed against the `[Flag]` / `[Match]` console lines.

| # | Scenario | Expected | Observed |
|---|---|---|---|
| 1 | Player 2.0 m from `Flag_Red` (outside `flagDistance 1.5`) | No pickup | Flag stayed `AtBase`, `carrying=false`, match still `Playing` |
| 2 | Player 1.2 m from the flag | Picked up, **no win** | `[Flag] Flag_Red AtBase -> Carried`; HUD banner on; match still `Playing` |
| 3 | Pause while carrying (4 s) | Flag stays attached, timer frozen | Flag still `Carried` on the carrier; `fieldTime` frozen at 26.45 |
| 4 | Resume | Carry preserved | Still `Carried`, same carrier |
| 5 | Carry into `Base_Blue` | **VICTORY** | `GameManager.State = Victory`, flag → `AtBase`, banner hidden |
| 6 | Restart with the flag carried | Flag back on its pole | `AtBase` at (11,0,0) on a fresh `Playing` match |
| 7 | Debug drop | Lies tilted where dropped | `Dropped` at (9.80,0,0), `tiltZ 70`, `dropTimer 10.0` |
| 8 | Leave a dropped flag alone (10 s) | Returns itself | `[Flag] … left on the ground too long -> it goes home`; `AtBase` at (11,0,0), pole upright |
| 9 | Pause with a dropped flag (5 s) | Timer frozen | `dropTimer` frozen at exactly 4.1, `tiltZ 70` |
| 10 | Drop the flag, then stand a free Red on the spot | Flag sent home **by the touch**, not the timer | `[Flag] Flag_Red Carried -> Dropped at (9.8, 0.0, 0.0)` → the very next frame `[Match] Enemy_Red_1 touched the dropped RED flag -> it goes home` → `[Flag] Flag_Red Dropped -> AtBase at (11.0, 0.0, 0.0)`. The touch fired ~0.005 s into the 10 s `dropTimer`, so the timer cannot be the cause; the read-back 4.4 s later showed `AtBase`, `dropTimer 0.0`, pole upright |
| 11 | Capture a live carrier (player `fieldTime 20`, Red `fieldTime 0`) | Flag drops at the carrier, **before** the teleport; then Game Over | `[Match] the flag carrier was caught -> the RED flag drops where he stood` → `[Flag] Flag_Red Carried -> Dropped at (9.8, 0.0, 4.0)` → `[Match] the BLUE player was captured -> GAME OVER`. The drop lands at the carrier, **not** in the prison at (13,0,−6.5) |
| 12 | Drop the flag near the Red base, player back in his base, both AI awake | A Red **decides** for itself to fetch it, runs there under its own power, and the touch returns it | Recorded frame by frame — see *Frame-by-frame recording* below. Short version: `Patrol` → `ReturnFlag` at 0.20 s, sprint (stamina 1.00 → 0.92) at the flag, flag `Dropped → AtBase` at 0.50 s, back to `Patrol` at 0.80 s. An earlier run from 8 m away reproduced the same result in 0.876 s, which matches 8 m at the 9.6 m/s sprint speed |

Also verified: `check_compile_errors` → no compile errors, `unity command recompile` → `{"status":"completed","failed":false,"errors":[]}`.

### Re-run of the three checks that need no input

Re-driven at the end of the session, after the code was frozen, to close the checks a hands-off verifier cannot
reach (no keyboard or pointer injection). Each was driven through the real `GameManager` / `Flag` API with both
Reds switched off and parked ~4 m away, so nothing but the rule under test could produce the result:

| Check | Evidence |
|---|---|
| 10 s timeout | `[Flag] Flag_Red Carried -> Dropped at (14.0, 0.0, 0.0)` at 03:06:02.369 → `[Flag] Flag_Red was left on the ground too long -> it goes home` at 03:06:12.260 → `[Flag] Flag_Red Dropped -> AtBase at (11.0, 0.0, 0.0)`, both from `Flag.Update`. That is 9.89 s against `returnAfterSeconds 10`, and both Reds were disabled, so the timer is the only thing that could have done it. |
| Pause with a dropped flag | `dropTimer 10.00` at the drop → still **exactly** `10.00` after ~10 s of wall time paused (`timeScale 0`), flag still `Dropped` at (14,0,0), pole still down. |
| Pause while carrying | Flag still `Carried`, `carrier=BluePlayer`, `carrying=true`, `dropTimer 0.00` (no stale countdown). 8 s later still attached and snapped above him at (−9.00, 1.00, 0.00), i.e. `carryHeight 1.0`. `Resume()` → `Playing`, `timeScale 1`, same carrier, still carrying. |
| Restart while carrying | `Carried` by `BluePlayer` at (9.8,0,0) on a `Playing` match → `Restart()` → after the deferred reload a fresh `Playing` match with `Flag_Red AtBase` at (11.00, 0.00, 0.00), `dropTimer 0.00`, `carrier=none`, `carrying=false`, player back at his spawn (−11,0,−2.5). |

Two harness honesty fixes were needed to make #10–#12 observable at all, and both are now in the shipped code:

- A drop always lands inside grab range of whoever was carrying it, so the carrier would instantly re-take it
  and no return rule could ever be watched. `HandleDebugDrop` (SPACE) now steps the carrier +4 on Z *clear of
  the drop spot before* calling `Drop(...)`, which is exactly what a real capture does by teleporting him to
  prison. The harness `DropFlag` entry does the same.
- `TryPickUp` now zeroes `DropTimer`, so a carried flag never shows a stale countdown.

### Frame-by-frame recording of #12

The AI fetch is over in about half a second — faster than one round trip through the CLI used to poke the
editor — and `EnemyAI.logStateChanges` is off by default, so the transition is invisible in the console. A
small editor-side recorder (one sample per frame from `EditorApplication.update`, dumped to the console at the
end) captured the whole thing, which is what makes #12 a *decision* test rather than a "something moved" test:

```
t=0.00  flag=Dropped  Enemy_Red_1 state=Patrol      pos=(13.0, 0.0, 3.0)  stamina=1.00
t=0.20  flag=Dropped  Enemy_Red_1 state=ReturnFlag  pos=(12.6, 0.1, 2.1)  stamina=1.00  <- its own decision
t=0.36  flag=Dropped  Enemy_Red_1 state=ReturnFlag  pos=(12.9, 0.1, 1.7)  stamina=0.98  <- sprinting at the flag
t=0.46  flag=Dropped  Enemy_Red_1 state=ReturnFlag  pos=(13.4, 0.1, 0.9)  stamina=0.93
t=0.50  flag=AtBase   Enemy_Red_1 state=ReturnFlag  pos=(13.1, 0.1, 0.8)                <- MatchManager judged the touch
t=0.80  flag=AtBase   Enemy_Red_1 state=Patrol      pos=(11.3, 0.1, 0.1)                <- back to guarding
```

The player sat inside `Base_Blue` at (−9,0,0) the whole time, so the fetch was never a race — `ShouldReturnFlag()`
was true because the enemy's own distance to the dropped flag beat the player's, which is the only comparison
that function makes.

A third harness artifact was found and *measured* while recording #12. The first parking spot chosen for the
Reds, (14,0,9), turned out to be inside the north wall: a downward raycast there hits `Wall_N` at y=2.00, while
the real floor is `Ground` at y=0.00. `CharacterMotor.Move` pushes every character down at a constant
`groundStickSpeed = −2 m/s` to keep it glued to the floor, so a character parked inside the wall sinks 2 m per
second while it walks, and the first recording showed exactly that (−2 m/s of Y drift). Parking the Reds on real
ground inside the arena removed it: Y held steady at 0.0–0.1 for the whole run, as the trace above shows. This
is a property of the parking spot, not of the flag rules — every flag and touch test is a flat XZ distance check,
and all of the game's own positions (bases, prisons, spawns) sit on `Ground` at y=0.

Not built, by design: the stretch goal (a Red steals the Blue flag). `Flag.ownerTeam` already supports it;
it needs a `Carry` AI state and a `MatchManager` rule for a Red carrier reaching `Base_Red`.

## Verify
- Carry the flag into `Base_Blue` → VICTORY; touch the flag and walk away → no win.
- Debug-drop the flag: it lies tilted at the spot, a Red runs to it and returns it; leaving it untouched returns it at 10s.
- Pause while carrying → flag stays attached, drop timer frozen; Restart/Title → flag back at (11,0,0).
- Console shows `[Flag] AtBase -> Carried`, `[Flag] Carried -> Dropped`, `[Flag] Dropped -> AtBase (returned by Red)` and no compile errors.
