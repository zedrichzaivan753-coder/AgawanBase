# Agawan Base 3D — Sprint/Stamina + Smarter Red AI

## Objective
Add one shared sprint+stamina component used by the player and both reds, and replace the reds' two-branch chase with a 3-state AI that only fights when it would win — without regressing anything that already works.

## Baseline — verified live this session (not carried over from the earlier v1 file)
- `check_compile_errors` → **"No compile errors"**; `get_unity_editor_state` → active scene `Assets/Scenes/Game.unity`, **`playMode: false`** (the older v1 plan's "bridge has no Editor / Editor is in Play mode" blocker is **stale** — the MCP-for-Unity bridge responds normally now, so `manage_gameobject` / `manage_components` / `manage_prefabs` / `create_script` are usable).
- `grep Sprint|stamina|speedMultiplier` across `Assets/**/*.cs` → **no matches**. Nothing exists yet.
- `Game/Characters/{BluePlayer,Enemy_Red_1,Enemy_Red_2}` are real **prefab instances** (3 `PrefabInstance` blocks in `Game.unity`) of `Assets/Prefabs/{BluePlayer,RedEnemy}.prefab` → one component added per prefab reaches the player and both reds.
- Decisions taken (say so if you disagree): (a) sprint requires movement input — holding SPRINT with a centred stick does nothing; (a) a red with nothing to do patrols **inside** `Base_Red`; (a) both reds use identical rules plus per-instance randomised tuning.

## Changes
**Modify**
- `Assets/Scripts/Core/CharacterMotor.cs` — add `public float speedMultiplier = 1f;` folded into `Move()`, plus a read-only `public bool IsMoving` (set from the same `input.sqrMagnitude > 0.0001f` test already on line 46). Inert at the default `1f`, so nothing existing changes.
- `Assets/Scripts/Gameplay/PlayerController.cs` — add `SprintStamina sprint` + `SprintButton sprintButton` (both `GetComponent`), drive `sprint.WantsSprint` from the held button (plus `Left Shift` when `allowKeyboardFallback`), force it `false` when captured and in `OnDisable()`.
- `Assets/Scripts/Gameplay/EnemyAI.cs` — replace the two-branch chase with the state machine below. **Keep the four existing public fields and their scene values** (`detectRadius 12`, `giveUpMultiplier 1.4`, `homeStopDistance 0.5`, `guardOffset (0,-2.5)` / `(0,2.5)`).
- `Assets/Scripts/UI/HudController.cs` — add `staminaFill` and `staminaLabel` to `Update()`. Existing `freshnessFill` / `freshnessLabel` / `captureLabel` untouched.
- `Assets/Prefabs/BluePlayer.prefab` and `Assets/Prefabs/RedEnemy.prefab` — add the `SprintStamina` component (one edit per prefab, covers all three characters).
- `Assets/Scenes/Game.unity` — add `Game/UI/Panel_Hud/{StaminaBarBg/StaminaFill, StaminaLabel, SprintButton/Label}`; wire `HudController.staminaFill/staminaLabel` and `PlayerController.sprintButton`.

**Add**
- `Assets/Scripts/Core/SprintStamina.cs` — the **one** stamina/sprint component, shared by the player and both reds (reused, not copied). Owns drain / regen / lock-out and writes `CharacterMotor.speedMultiplier`. Public fields: `maxStamina 2`, `drainPerSecond 1`, `regenPerSecond 0.7`, `regenDelay 0.6`, `restartThreshold 0.4`, `sprintSpeedMultiplier 1.6`, `logSprint false`. `WantsSprint` is the only external input; `CanSprint` and `Stamina01` are the read-only outputs the AI and HUD use.
- `Assets/Scripts/UI/SprintButton.cs` — hold-to-sprint UGUI widget (`IPointerDownHandler` / `IPointerUpHandler`), modelled exactly on `VirtualJoystick`, including its `OnDisable` reset.

**Not touched:** `MatchManager.cs` (the AI can already read everything it needs — `CharacterStatus.fieldTime`, `IsInHomeBase`, `isCaptured`, `homeBase` are all public), `CharacterStatus.cs`, `BaseZone.cs`, `Team.cs`, `GameManager.cs`, `VirtualJoystick.cs`, all 5 materials, `SampleScene`.

## AI State Diagram
Three states, re-evaluated on a timer (`decisionInterval` 0.25 s ± jitter), never per frame. Movement still happens every frame; only the *decision* is throttled.

| Priority | New state | Condition |
|---|---|---|
| 1 | **Retreat** | ( player `IsInHomeBase` **or** I am not fresher — `myFieldTime + chaseFresherMargin >= playerFieldTime` ) **and** I am not already inside my own base |
| 2 | **Chase** | player not in their own base **and** I am fresher **and** flat distance ≤ `chaseRange` **and** stamina ≥ `chaseEnterStamina01` |
| 3 | **Patrol** | everything else — hold at the guard post inside `Base_Red` |

`chaseRange = detectRadius × aggression` normally, widening to `detectRadius × giveUpMultiplier × aggression` while already chasing — the existing `giveUpMultiplier` field is reused as the range hysteresis.

Transitions (what changes the state, i.e. what a beginner should read off the code):
- **Patrol → Retreat**: player ducks into `Base_Blue`, *or* the player's `fieldTime` drops below mine (they reset at home), *or* I walked out and went stale.
- **Patrol → Chase**: all three chase conditions become true on a decision tick.
- **Chase → Retreat**: player reaches `Base_Blue` or resets `fieldTime` — fires *before* contact, so the red is never the loser.
- **Chase → Patrol**: player further than `chaseRange`, **or** stamina drops below `chaseExitStamina01` (no chasing on empty stamina), **or** the player is captured.
- **Retreat → Patrol**: I am inside `Base_Red` (`homeBase.Contains`), which also snaps my `fieldTime` to 0.
- **Any → Patrol** immediately if `status.isCaptured` or the target is null; `WantsSprint` is cleared on that same line.

Per state:
- **Patrol** — walk to the guard post (`homeBase` centre + `guardOffset`); on arrival hold for `patrolPauseSeconds`, then pick a point within `patrolRadius` **clamped to `homeBase.radius - 0.5`**. Never sprints, so it always sits at `fieldTime 0.00` and is never taggable while idle.
- **Chase** — walk straight at the player; sprint only while `distance >= sprintDistance` and `SprintStamina.CanSprint`. The shared lock-out turns that into short bursts with no extra AI code.
- **Retreat** — run at `homeBase` **centre** (not the guard post — get inside the circle fastest); sprint while `sprintWhileRetreating` and `CanSprint`.

Why this is safe: a character inside its own base has `fieldTime == 0` (`CharacterStatus.Update` snaps it there) and `MatchManager.ResolveTouch` gives the win to the lower `fieldTime`. So the "player is safe" case and the "I would lose" case collapse into the same rule — and priority 1 fires the instant the player reaches `Base_Blue`. Chasing a based player today gets the red captured; this fixes it.

Anti-flapping: `minStateTime` (nothing changes for at least this long), `chaseFresherMargin`, the `chaseRange` widening, the `chaseEnterStamina01` / `chaseExitStamina01` hysteresis pair, and `reactionTime` (a short freeze on entering a state, which also desyncs the two reds). Note that while both characters are outside their bases `fieldTime` grows at the same rate for each, so the *difference* is constant — a chase cannot oscillate on its own. The decision only flips when someone resets at a base.

Randomness, all randomised per instance in `Awake()` when `randomizeOnStart` is on: `aggression` (drawn from `aggressionMin 0.85`…`aggressionMax 1.15`, scales `chaseRange` and `sprintDistance`), `reactionTime` (0…`reactionTimeRange 0.5`), and a per-tick `decisionJitter` on `decisionInterval`. `guardOffset` is left alone — it is already scene-set per instance.

## Relevant Assets and Quirks
- **`Game/UI/Panel_Hud` is currently INACTIVE** (`IsActive: false` — the editor sits on the Title screen). `capture_ui_canvas` on an inactive panel proves nothing. Activate `Panel_Hud` for the build/capture and leave it inactive afterwards; `GameManager.SetState` owns its real visibility.
- **HUD placement conflict:** `FreshnessLabel` already sits at `anchoredPosition (50, -112)`, size 700×52 — v1's stamina-bar position. `FreshnessBarBg` is `(50,-50)` 620×52 (bottom edge −102) and `FreshnessLabel` runs −112…−164, so the stamina bar goes at **`(50,-174)`, 620×34** and `StaminaLabel` at **`(50,-218)`, 320×36**. Existing HUD does not move.
- **Sprint button geometry:** anchor `(1,0)`, pivot `(0.5,0.5)`, `anchoredPosition (-250,250)`, size **200×200**, `raycastTarget: true`, sprite = the built-in `UISprite` (`{fileID: 10913, guid: 0000000000000000f000000000000000}`) that `VirtualJoystick`'s Image uses, so it renders as a circle. Idle colour `{1,1,1,0.30}` (matches the joystick), held colour `{0.95,0.72,0.25,0.85}` (amber — distinct from the green freshness fill and the red team colour). `SPRINT` `Text` child, stretch anchors, `raycastTarget: false`.
- **48 dp check:** `CanvasScaler` = Scale With Screen Size, reference **1920×1080**, `matchWidthOrHeight 0.5`. At the worst plausible case (1600×720 device at 480 dpi) `scaleFactor ≈ 0.745`, so 200 units = 149 px = **≈ 50 dp**. At 1920×1080 @ 480 dpi it is 66 dp. Comfortably over 48 dp. (The editor's current 1115×558 game view has `scaleFactor 0.5477`, so the button looks smaller on screen than on a phone.)
- **`Panel_Hud` has no Image component at all**, so it casts no raycasts and cannot swallow the button's input. `VirtualJoystick`'s Image *does* have `raycastTarget: true` and there is a bare-`Image` + `IPointerDown/Up` precedent — the new button must match it (`raycastTarget: true`) or it will never receive a press. Do **not** put a `Button` on it: `PauseButton` has `targetGraphic: ""`, so its `ColorTint` does nothing.
- **Pause already blocks the HUD:** `Panel_Pause` is a full-screen `Image` (black, α 0.75, `raycastTarget: true`) drawn after `Panel_Hud`, and `GameManager.SetState` sets `playerController.enabled = false` outside `Playing`. `Panel_Hud` stays *visible* while paused, so the sprint button is visible-but-dead — correct, and no `GameManager` edit is needed.
- **`EventSystem` lives at `Game/EventSystem`, not under `Game/UI`** — it exists and carries `InputSystemUIInputModule`; touches on the new button route normally.
- **`Base_Red` `radius = 4`, `guardOffset` is 2.5 from its centre** → the patrol clamp to `radius - 0.5` is load-bearing. Without it a red parks outside the circle, its `fieldTime` starts climbing, and it becomes taggable for free while idle.
- `CharacterStatus.MaxFieldTimeSeconds` is `const 20f` (not serialized), so `chaseFresherMargin 0.3` is a ~1.5 % margin on that scale. `fieldTime` is a `float` but both characters advance together, so there is no drift risk on the comparison itself.
- **The scene's `EnemyAI` instances carry per-instance `guardOffset` overrides**, so new fields inherit prefab defaults — no per-instance rewiring of the AI is needed.
- **No packages needed:** `com.unity.ugui 2.6.0` + `com.unity.inputsystem 1.20.0` are installed and sufficient. Nothing to import.
- Legacy `UnityEngine.UI.Text` only, never TextMeshPro (TMP would demand an Essentials import).
- **`EditorApplication.isPaused` must be checked before believing any Play-mode observation** — it has silently frozen this project before (`frameCount` stuck at 17989). Also: if `restartThreshold >= maxStamina`, warn loudly in `SprintStamina.Awake` with `Debug.LogWarning` — that value would permanently lock out sprinting.

## Steps
- [x] 1. Create `Assets/Scripts/Core/SprintStamina.cs` (create_script) and add `speedMultiplier` + `IsMoving` to `CharacterMotor` (apply_text_edits). *(done when: `check_compile_errors` is clean and the Inspector on `Game/Characters/BluePlayer` still shows `moveSpeed 6`)* [depends: none]
- [x] 2. Create `Assets/Scripts/UI/SprintButton.cs` exposing `IsHeld`, with `normalColor {1,1,1,0.30}` / `heldColor {0.95,0.72,0.25,0.85}` and an `OnDisable` reset. *(done when: `check_compile_errors` is clean and the console shows no error on domain reload)* [depends: 1]
- [x] 3. Add `SprintStamina` to `BluePlayer.prefab` and `RedEnemy.prefab` with `manage_prefabs` `modify_contents` (`components_to_add`). *(done when: `get_game_object_info` on `Game/Characters/BluePlayer`, `Enemy_Red_1` and `Enemy_Red_2` each lists `SprintStamina` with `maxStamina 2`, `sprintSpeedMultiplier 1.6`)* [depends: 2]
- [x] 4. Build the HUD inside `Panel_Hud`: `StaminaBarBg` `(50,-174)` 620×34 colour `{0.13,0.14,0.17,0.92}` `raycastTarget false` with child `StaminaFill` (type `Filled`, `fillMethod Horizontal`, amber `{0.95,0.72,0.25,1}`); `StaminaLabel` `(50,-218)` 320×36; `SprintButton` 200×200 bottom-right with the circle sprite and a `SPRINT` label; then wire `HudController.staminaFill/staminaLabel`. *(done when: `capture_ui_canvas` on `Game/UI` shows the amber bar below the freshness label and a round SPRINT button bottom-right, with the freshness bar, counter, pause button and joystick in unchanged positions)* [depends: 3]
- [x] 5. Extend `PlayerController` with the sprint input (button + `Left Shift` fallback) and `sprint.WantsSprint` handling with `Debug.Log` on each sprint start/stop. *(done when: in Play, holding SPRINT with the stick pushed makes the player visibly faster and the amber bar drains; releasing refills it only after `regenDelay 0.6 s`; the console logs each sprint on/off with the stamina value)* [depends: 4]
- [x] 6. Rewrite `EnemyAI` as the 3-state machine with `aggression` / `reactionTime` / `decisionInterval` / `chaseEnterStamina01` / `chaseExitStamina01` / `sprintDistance` / `patrolRadius` / `minStateTime` / `logStateChanges`, driving the shared `SprintStamina.WantsSprint`. *(done when: the console logs each red's transitions with both `fieldTime` values and a red that is staler than the player runs home instead of chasing)* [depends: 5]
- [x] 7. Tune and verify: `logStateChanges`/`logSprint` off, 60 s Play FPS + behaviour pass, and a screenshot of a red entering Retreat. *(done when: `get_worst_gc_frames`/frame timing still holds 30 FPS and a red visibly abandons a chase the moment the player steps into `Base_Blue`)* [depends: 6]

## Risks and Edge Cases
- **Player ducks into `Base_Blue` mid-chase** — priority 1: the player's `fieldTime` snaps to 0, so the retreat condition fires *before* contact and the red is never captured. This is the exact bug being fixed, so test it explicitly.
- **Both characters sprinting** — no shared state at all: each has its own `SprintStamina`, so they drain independently. It is a speed-multiplier race only, and the tag is still decided by `fieldTime`.
- **Stamina at 0 during a tag** — `MatchManager` resolves touches on distance + `fieldTime` and never reads stamina. A winded character is slower but tags and is tagged exactly as before. Zero impact on the core rule.
- **Both reds flip-flopping in sync** — `aggression`, `reactionTime` and the `decisionInterval` jitter are randomised per instance in `Awake()`, so they desync.
- **Pause / Victory / GameOver during sprint** — `Time.timeScale = 0` freezes `Time.deltaTime`, so drain and regen stop on their own. The residual risk is `WantsSprint` staying `true` because `OnPointerUp` never arrives, which would lurch the player on resume. Mitigated three ways: `SprintButton.OnDisable` releases, `PlayerController.OnDisable` clears it, and `SprintStamina` refuses to sprint while `motor.IsFrozen`.
- **Stamina visibly changes while paused** — `HudController.Update` runs under `timeScale 0`, so it must read `Stamina01` (a pure value) rather than ticking anything, or the bar will keep moving while paused.
- **Captured while sprinting** — `isCaptured` freezes the motor and `SprintStamina` gates on `IsFrozen`, so stamina cannot silently drain in prison.
- **Red captured mid-retreat** — `EnemyAI` early-returns on `status.isCaptured` and clears `WantsSprint` on the same line, so a prisoned red is idle rather than vibrating on the spot.
- **Chase on empty stamina** — blocked by `chaseEnterStamina01`, and once chasing it only drops out below `chaseExitStamina01`, so the red recovers at home instead of stuttering in and out of the chase. Without that pair, regen at 0.7/s would re-trigger a chase roughly twice a second.
- **Patrol point outside the base** — clamped to `homeBase.radius - 0.5`; without this an idle red accumulates `fieldTime` and becomes taggable for free.
- **AI runs on the Title screen** — `matchActive` is false but `EnemyAI.Update` still runs. Harmless: the player spawns 22 m away (beyond `detectRadius 12`) inside `Base_Blue`, so both reds sit at their guard posts in Patrol. Deliberately *not* wiring a `MatchManager` reference into `EnemyAI` — it would add scene wiring for no gameplay gain.
- **Log spam** — `logStateChanges`/`logSprint` fire only on change (≈1 per 0.25 s worst case, matching `MatchManager`'s existing style). Toggle both off for the 30 FPS pass.
- **Restart / Title reset** — `GameManager.Restart()` reloads the scene, so stamina and AI state reset with it; nothing needs clearing manually.
- **Regression risk to the working core** — `MatchManager`, `CharacterStatus`, `BaseZone` and `GameManager` are not edited at all. `HudController` only gains two bindings, and `CharacterMotor`'s change is inert at `speedMultiplier = 1`.

## Test Cases (for the test record)
| # | Test | Expected |
|---|---|---|
| TC-01 | Hold SPRINT with the stick pushed, outside base | Player visibly faster; amber bar drains smoothly |
| TC-02 | Release SPRINT | Bar refills only after `regenDelay` 0.6 s, then at 0.7/s |
| TC-03 | Drain to empty, keep holding | Sprint cuts out at 0; no re-sprint until stamina passes `restartThreshold` 0.4 (no spamming) |
| TC-04 | Tap SPRINT repeatedly from empty | No sprint at all until the threshold is passed |
| TC-05 | Hold SPRINT with the stick centred | No drain, no movement, no speed change (sprint requires input) |
| TC-06 | Sprint, pause mid-sprint, resume | Sprint is off on resume; no instant lurch; bar unchanged while paused |
| TC-07 | Player sprints into a red while stale | Player still loses the tag and is teleported to `PrisonForBlue` — sprint did not change the rule |
| TC-08 | Stand in `Base_Blue` with a red chasing | Red abandons the chase *before* contact and returns to `Base_Red`; it is never captured |
| TC-09 | Leave base with `fieldTime` higher than both reds | Both reds chase and the player is captured |
| TC-10 | Bait a red out, run home, let it go stale, re-engage | Red retreats home, `fieldTime` reads 0.00, then returns and can win |
| TC-11 | Watch both reds for 60 s from `Base_Blue` | They never chase into the base, never leave `Base_Red` while patrolling, and never sit outside accumulating `fieldTime` |
| TC-12 | Watch the `logStateChanges` output | Transitions are ≥ `minStateTime` apart — no per-frame or per-tick flip-flopping |
| TC-13 | Compare the two reds over 60 s | Different `aggression`/`reactionTime` — they do not move as one unit |
| TC-14 | Bait a red out, stand still, let its stamina empty | It stops chasing and walks home to recover instead of chasing winded |
| TC-15 | Retreat a red while watching stamina | It sprints home, drains, arrives and refills at `Base_Red` |
| TC-16 | Reach `Flag_Red` while free (reds may be mid-chase) | Victory still triggers; pause/restart/title still work |
| TC-17 | Profiler / frame timing over 60 s Play | Still 30 FPS, no per-frame allocations added in `EnemyAI.Update` or `SprintStamina.Update` |

## Verify
- `check_compile_errors` clean after every step; confirm `EditorApplication.isPaused` is false before trusting any Play-mode result.
- `capture_ui_canvas` on `Game/UI` before and after — freshness bar, freshness label, captured counter, pause button and joystick unchanged; stamina bar and SPRINT button added.
- Play-mode `capture_editor_screenshot` (game view) with `logStateChanges` on, to watch a red enter Retreat the moment the player steps into `Base_Blue`.
- Re-run the five original behaviours — player loses a tag, tie = no capture, red loses a tag, Victory at `Flag_Red`, pause/resume — to prove no regression.
- Confirm `list_packages` is unchanged (nothing installed) and `SampleScene.unity` is untouched.

## Execution Notes (this run)
- **All 7 steps built.** `check_compile_errors` clean after every step and at the end.
- **Two tools were dead this run, both worked around without touching the design:** `mcpforunity` (`execute_code`, `manage_*`, `find_gameobjects`, `manage_prefabs`) returns `"No Unity Editor instances found"`, and Aura's own `execute_script` fails with `Could not find any resources appropriate for the specified culture … Microsoft.CodeAnalysis.CSharp.…CSharpResources.resources … assembly "Aura-v1.3.0"` (same failure as the v2 prototype run). Everything was therefore built with Aura's native scene tools — `write_to_file`/`replace_in_file` for the scripts, `add_component` for the prefabs, `create_ui_element`/`set_rect_transform`/`set_property`/`duplicate_game_object`/`parent_game_object` for the HUD.
- **Prefabs:** `SprintStamina` added to `Assets/Prefabs/BluePlayer.prefab` and `Assets/Prefabs/RedEnemy.prefab`; all three scene instances (`Game/Characters/BluePlayer`, `Enemy_Red_1`, `Enemy_Red_2`) show it with `maxStamina 2` / `sprintSpeedMultiplier 1.6`.
- **HUD built as planned**, with one deviation: `FreshnessLabel` already occupied `-112`, so the stamina bar sits at `(50,-174)` 620×34 and `StaminaLabel` at `(50,-218)` 420×52. The `SPRINT` button is anchored `(1,0)` at `(-250,250)`, **200×200**, `raycastTarget true`, and reuses the joystick's built-in circle sprite. The two new Texts were made by duplicating `FreshnessLabel` and `PauseButton/Label`, so they inherit the working font (no TextMeshPro, no import).
- **Deviation on the sprint wiring:** the `sprintButton` field on the *prefab instance's* `PlayerController` would not persist to `Game.unity` through any available tool (the override is applied in memory but never written), so `PlayerController.Awake` now resolves it itself with `Object.FindAnyObjectByType<SprintButton>(FindObjectsInactive.Include)` — `Include` is required because `Panel_Hud` is inactive on the title screen. A manual Inspector assignment still takes priority, and a warning is logged if no button is found. `HudController.staminaFill`/`staminaLabel` are plain scene references and **did** persist.
- **Play-mode evidence (self-check, since no input can be simulated in this session):** no exceptions across the whole session. With the Blue player moved out of `Base_Blue` at runtime, both reds left `Base_Red` and reported `state: "Chase"` (`aggression` 1.053 / 1.107, `reactionTime` 0.036 / 0.314 — randomised per instance as designed) while the player's `fieldTime` climbed to 16.25. Moving the player back into `Base_Blue` made both reds return inside `Base_Red` with `fieldTime 0.00` and `state: "Patrol"`. Stopping play restored the player to `(-11, 0, -2.5)`.
- **Not verified this run:** sprint/stamina in motion (needs touch or Shift input, which cannot be simulated with the tools available), the `Retreat` state label itself (the reds were back at `Patrol` by the time they were read), on-device FPS, and the five original match behaviours end-to-end.
- **Leftover file:** `AuraScripts/AgawanHudBuilder.cs` sits at the project root (outside `Assets/`, so Unity never compiles it and it is not in the build). It is the one-shot HUD builder that could not run because `execute_script` is broken. Safe to delete.

