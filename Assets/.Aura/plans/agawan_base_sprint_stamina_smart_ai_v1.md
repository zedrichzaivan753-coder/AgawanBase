# Agawan Base 3D — Sprint/Stamina + Smarter Red AI

## Objective
Add a shared sprint+stamina system used by the player and both reds, and replace the reds' simple chase with a 3-state AI that only fights when it would win — without regressing anything that already works.

## Blocker — read first
The MCP-for-Unity bridge has **no Editor connected**. `set_active_instance "8080"` → `"No Unity instance found on port 8080. Available: none."`; `get_sha` and `find_gameobjects` (both `core` group) fail with `"No Unity Editor instances found."` So `manage_components` / `manage_gameobject` / `manage_prefabs` / `create_script` / `apply_text_edits` cannot execute. Also **not available to me at all this session**: the shell tool (`execute_command`) and the file writers (`write_to_file` / `replace_in_file`) I used previously, and the verification subagent. Aura's own tools that *do* work are read-only (`read_file`, `grep`, `query_project_assets`, `get_game_object_info`, `check_compile_errors`). **Steps 2–7 cannot run until the bridge reconnects.** Restore it first (Unity Editor open on `C:\Users\Zedrich\AgawanBase`, MCP for Unity window → start/restart the bridge, or reload the domain).

## Changes
**Modify**
- `Assets/Scripts/Core/CharacterMotor.cs` — add `public float speedMultiplier = 1f;` and fold it into `Move()`. At the default 1f behaviour is bit-identical, so nothing existing changes.
- `Assets/Scripts/Gameplay/PlayerController.cs` — add `SprintStamina sprint` (auto-`GetComponent`) and `SprintButton sprintButton`; drive `sprint.WantsSprint` from the held button (plus `Left Shift` when `allowKeyboardFallback`); force it `false` when captured and in `OnDisable()`.
- `Assets/Scripts/Gameplay/EnemyAI.cs` — replace the two-branch chase with the state machine below. **Keep all four existing public fields and their scene values** (`detectRadius 12`, `giveUpMultiplier 1.4`, `homeStopDistance 0.5`, `guardOffset`).
- `Assets/Scripts/UI/HudController.cs` — add `staminaFill` (+ optional `staminaLabel`) to `Update()`. Existing `freshnessFill` / `freshnessLabel` / `captureLabel` bindings untouched.
- `Assets/Prefabs/BluePlayer.prefab` and `Assets/Prefabs/RedEnemy.prefab` — add the `SprintStamina` component (covers the player and both reds in one edit each).
- `Assets/Scenes/Game.unity` — add `Game/UI/Panel_Hud/StaminaBarBg/{StaminaFill}` and `Game/UI/Panel_Hud/SprintButton/{Label}`; wire `HudController.staminaFill` and `PlayerController.sprintButton`.

**Add**
- `Assets/Scripts/Core/SprintStamina.cs` — the **one** stamina/sprint component shared by the player and both reds. Owns drain/regen/lock-out; writes `CharacterMotor.speedMultiplier`.
- `Assets/Scripts/UI/SprintButton.cs` — hold-to-sprint UGUI widget (`IPointerDownHandler` / `IPointerUpHandler`), modelled exactly on `VirtualJoystick`, including its `OnDisable` reset.

**Not touched:** `MatchManager.cs` (the AI can already read everything it needs — `CharacterStatus.fieldTime`, `IsInHomeBase`, `isCaptured` are all public), `CharacterStatus.cs`, `BaseZone.cs`, `Team.cs`, `GameManager.cs`, `VirtualJoystick.cs`, all materials, and `SampleScene`.

## AI State Diagram
Three states, decided in priority order on a timer (never per frame):

| Priority | New state | Condition |
|---|---|---|
| 1 | **Retreat** | I am **not** fresher than the player — `myFieldTime + chaseFresherMargin >= playerFieldTime` — and I am not already inside my own base |
| 2 | **Chase** | player is **not** in their own base **and** I **am** fresher **and** flat distance ≤ `chaseRange` **and** `stamina01 >= minStaminaToChase01` |
| 3 | **Patrol** | everything else — hold position at home |

Where `chaseRange = detectRadius` normally, widening to `detectRadius * giveUpMultiplier` while already chasing (this is the existing field, reused as the range hysteresis).

Per-state behaviour:
- **Patrol** — walk to the guard post (`homeBase` centre + `guardOffset`); once there, hover near a point re-picked every `patrolPauseSeconds` within `guardRadius`. Never sprints (saves stamina and stays fresh).
- **Chase** — walk straight at the player. Sprint **only** while `distance >= sprintDistance` **and** `SprintStamina.CanSprint`. The shared lock-out throttles this into bursts automatically, with no extra code.
- **Retreat** — run to `homeBase` centre (not the guard post — get inside the circle fastest). Sprint while `sprintWhileRetreating` and `CanSprint`.

Why this is safe: a player standing in their own base has `fieldTime == 0` (`CharacterStatus.Update` snaps it there), and `MatchManager.ResolveTouch` gives the win to the lower `fieldTime`. So priority 1 fires automatically for a player tucked in their base — the "player is safe" case and the "I would lose" case collapse into the same rule. Chasing a based player today gets the red captured; this fixes that.

Anti-flapping: `minStateTime` (no state change for at least this long), `chaseFresherMargin`, the `chaseRange` widening, and `reactionTime` (a short freeze on each new state, which also desyncs the two reds).

Note that while both characters are outside their bases, `fieldTime` grows at the same rate for each, so the *difference* is constant — a chase cannot oscillate on its own. The decision only flips when someone resets at a base. That gives strong hysteresis for free.

Randomness: `aggression` (0.85–1.15, random per instance, scales `detectRadius` and `sprintDistance`), `reactionTime` (0–`reactionTimeRange`), and a per-tick jitter on `decisionInterval`. Randomised in `Awake()` only when `randomizeOnStart` is on. `guardOffset` is left alone — it is already scene-set to `(0,-2.5)` and `(0,2.5)`.

## Relevant Assets and Quirks
- **`CharacterStatus.MaxFieldTimeSeconds` is `const 20f`** — not serialized. `chaseFresherMargin` is compared against a 0–20 s scale, so 0.3 s is a ~1.5 % margin.
- **Hysteresis and drift:** `fieldTime` is `float` and increments per frame, but since both characters advance together the comparison is stable. No epsilon drift risk in practice.
- **The patrol clamp is load-bearing:** `Base_Red` has `radius = 4` and `guardOffset` is already 2.5 from its centre. A `guardRadius` of 1.5 could place a patrol point at 4.0 — exactly on the boundary — so the chosen point **must** be clamped to `homeBase.radius - 0.5`. Otherwise a red parks outside its base, its `fieldTime` starts climbing, and it becomes taggable while idle.
- **HUD canvas:** `CanvasScaler` = Scale With Screen Size, reference **1920×1080**, `matchWidthOrHeight 0.5`, current `scaleFactor 1.0`. On a 1920×1080 phone at 3× density 48 dp = 144 canvas units, so the **180×180** sprint button is ~60 dp — comfortably over the minimum. The joystick is 260×260 and the pause button 170×120.
- **Existing HUD geometry:** `FreshnessBarBg` anchor (0,1), pos (50,−50), size 620×52, pivot (0,1), Image colour `{0.13, 0.14, 0.17, 0.92}`, `sprite: None`, `raycastTarget: false`. `FreshnessFill` is `type: Filled`, `fillMethod: Horizontal`, `fillOrigin 0`, colour `{0.36, 0.84, 0.42, 1}` (green), `raycastTarget: false`. Copy both as the templates for the stamina bar.
- **`Panel_Hud` has no Image component at all**, so it casts no raycasts and cannot swallow the new button's input.
- **`VirtualJoystick`'s Image is `raycastTarget: true`** and has no `Selectable` — a bare `Image` + `IPointerDown/Up` handlers is the established pattern here. The new sprint button Image **must** be `raycastTarget: true` or it will never receive a press.
- **`PauseButton` has `targetGraphic: ""`**, so its `ColorTint` transition does nothing. Do not copy it as a template; drive the sprint button's colour from `SprintButton` instead.
- Sprint colour `{0.95, 0.72, 0.25, 1}` (amber) is distinct from the green freshness fill and the red team colour. Idle button colour matches the joystick's `{1,1,1,0.3}`.
- **No new packages needed** — `com.unity.ugui 2.6.0` + `com.unity.inputsystem 1.20.0` are already installed and sufficient. Nothing to import.
- Legacy `UnityEngine.UI.Text` only, never TextMeshPro (TMP would demand an Essentials import).
- The Editor is currently **in Play mode** and was previously found **paused**; check `EditorApplication.isPaused` before trusting any play-mode observation.

## Steps
- [ ] 1. Reconnect the MCP-for-Unity bridge and take a baseline: confirm `manage_components`/`manage_gameobject` respond and `check_compile_errors` is clean. *(done when: a `manage_scene get_active` call returns the `Game` scene instead of "No Unity Editor instances found".)* [depends: none]
- [ ] 2. Create `Assets/Scripts/Core/SprintStamina.cs` with `maxStamina 2`, `drainPerSecond 1`, `regenPerSecond 0.7`, `regenDelay 0.6`, `restartThreshold 0.4`, `sprintSpeedMultiplier 1.6` — all public; add `speedMultiplier` to `CharacterMotor` via `apply_text_edits`. *(done when: `check_compile_errors` is clean and `CharacterMotor` still reads `moveSpeed 6` in the Inspector.)* [depends: 1]
- [ ] 3. Create `Assets/Scripts/UI/SprintButton.cs` exposing `IsHeld`, with `normalColor {1,1,1,0.30}` / `heldColor {0.95,0.72,0.25,0.85}` and an `OnDisable` reset. *(done when: `check_compile_errors` is clean.)* [depends: 1]
- [ ] 4. Add `SprintStamina` to `BluePlayer.prefab` and `RedEnemy.prefab` with `manage_prefabs`, then confirm all three scene instances show the component. *(done when: `get_game_object_info` on `Game/Characters/BluePlayer` and both reds lists `SprintStamina`.)* [depends: 2]
- [ ] 5. Build the HUD: `StaminaBarBg` (anchor (0,1), pos (50,−112), size 620×34, colour `{0.13,0.14,0.17,0.92}`, `raycastTarget false`) with child `StaminaFill` (Filled/Horizontal, amber, `raycastTarget false`); plus `SprintButton` (anchor (1,0), pos (−250,250), 180×180, `raycastTarget true`) with a `SPRINT` `Text` child. Wire `HudController.staminaFill` and `PlayerController.sprintButton`. *(done when: `capture_ui_canvas` shows the amber bar under the freshness bar and a round SPRINT button bottom-right, none of the existing HUD moved.)* [depends: 4]
- [ ] 6. Extend `PlayerController` with the sprint input (button + `Left Shift`) and wire `HudController` to `player.GetComponent<SprintStamina>()`. *(done when: holding the button or Shift in Play makes the player visibly faster, the amber bar drains, and releasing refills it after the delay.)* [depends: 5]
- [ ] 7. Rewrite `EnemyAI` as the 3-state machine plus `aggression`/`reactionTime`/`decisionInterval` fields and a `logStateChanges` toggle. *(done when: the console logs each red's state transitions with both `fieldTime` values, and a red that is staler than the player runs home instead of chasing.)* [depends: 6]

## Risks and Edge Cases
- **Player ducks into base mid-chase** — handled by priority 1: the player's `fieldTime` snaps to 0, so `iAmFresher` goes false and the red retreats. It stops *before* contact, so it is never captured. Must be tested explicitly, since this is the exact bug being fixed.
- **Both sprinting** — no shared state, no interaction: each character has its own `SprintStamina`. Both drain independently. Purely a speed-multiplier race, and the faster party still has to win on `fieldTime`.
- **Stamina hits 0 during a tag** — `MatchManager` resolves touches on distance and `fieldTime` only; stamina is never consulted. A winded character is slower but tags and is tagged exactly as before. Zero risk to the core rule.
- **Pause / Victory / GameOver during sprint** — `Time.timeScale = 0` freezes `Time.deltaTime`, so drain and regen stop on their own. The residual risk is `SprintStamina.WantsSprint` staying `true` because `OnPointerUp` never arrives — that would make the player sprint instantly on resume. Mitigated three ways: `SprintButton.OnDisable` releases, `PlayerController.OnDisable` clears `WantsSprint`, and `SprintStamina` refuses to sprint while `motor.IsFrozen`.
- **Captured while sprinting** — `isCaptured` freezes the motor and `SprintStamina` gates on `IsFrozen`, so stamina cannot silently drain in prison.
- **Red captured mid-retreat** — `EnemyAI` early-returns on `status.isCaptured` and clears `WantsSprint`, so a prisoned red is idle, not vibrating.
- **Patrol point inside the base** — clamped to `homeBase.radius - 0.5`; without this an idle red drifts out and starts accumulating `fieldTime`, becoming taggable for free.
- **Both reds flip-flopping in sync** — `reactionTime`, `aggression` and the `decisionInterval` jitter are randomised per instance in `Awake()`, so they desync.
- **AI runs on the Title screen** — `matchActive` is false but `EnemyAI.Update` still runs. Harmless: the player spawns 22 m away (beyond `detectRadius 12`) and inside `Base_Blue`, so the reds sit at their guard posts. Deliberately *not* wiring a `MatchManager` reference into `EnemyAI` — it would add scene wiring for no gameplay gain.
- **Log spam** — `logStateChanges` only fires on a state transition (~1 per 0.25 s worst case), matching `MatchManager`'s existing style. Toggle it off for the 30 FPS pass.
- **Regression risk to the working core** — `MatchManager`, `CharacterStatus`, `BaseZone` and `GameManager` are not edited at all. `HudController` only gains a binding. `CharacterMotor`'s change is inert at `speedMultiplier = 1`.

## Test Cases (for the test record)
| # | Test | Expected |
|---|---|---|
| TC-01 | Hold SPRINT with the stick pushed, out of base | Player visibly faster; amber bar drains smoothly |
| TC-02 | Release SPRINT | Bar refills only after `regenDelay` 0.6 s, then at 0.7/s |
| TC-03 | Drain to empty, keep holding | Sprint cuts out at 0 ; bar will not re-sprint until it passes `restartThreshold` 0.4 (no spamming) |
| TC-04 | Tap SPRINT repeatedly from empty | No sprint at all until the threshold is passed |
| TC-05 | Sprint while standing still (stick centred) | No drain, no movement (sprint requires input) |
| TC-06 | Sprint, then pause mid-sprint, then resume | Sprint is off on resume; no instant lurch; bar unchanged while paused |
| TC-07 | Player sprints into a red while stale | Player still loses the tag and is teleported to `PrisonForBlue` — sprint did not change the rule |
| TC-08 | Stand in `Base_Blue` with a red chasing | Red abandons the chase and returns to `Base_Red`; red is never captured |
| TC-09 | Walk out of base with `fieldTime` higher than both reds | Both reds chase and the player is captured |
| TC-10 | Bait a red out, run home, let it go stale, then re-engage | Red retreats home, resets `fieldTime` to 0.00, then comes back and can win |
| TC-11 | Watch both reds for 60 s from the base | They do not chase into the base, do not leave `Base_Red` while patrolling, and do not sit outside accumulating `fieldTime` |
| TC-12 | Watch `logStateChanges` output | Transitions are ≥ `minStateTime` apart — no per-frame or per-tick flip-flopping |
| TC-13 | Compare the two reds over 60 s | Different `aggression`/`reactionTime` — they do not move as one unit |
| TC-14 | Reach `Flag_Red` while free (reds may be mid-chase) | Victory still triggers; pause/restart/title still work |
| TC-15 | Profiler / frame timing over 60 s Play | Still 30 FPS, no per-frame allocations added in `EnemyAI.Update` |

## Verify
- `check_compile_errors` clean after every step.
- `capture_ui_canvas` on `Game/UI` before and after — existing HUD (freshness bar, pause, captured counter, joystick) unchanged, stamina bar and sprint button added.
- `capture_editor_screenshot` (game view) during Play with `logStateChanges` on, to watch a red's transition to Retreat.
- Re-run the five original behaviours (capture, tie = no capture, red loses, Victory, pause) to prove no regression.
- Confirm no new packages were installed (`list_packages` unchanged) and `SampleScene.unity` is untouched.
