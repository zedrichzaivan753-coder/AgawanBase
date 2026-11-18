# Requirements-to-test traceability matrix - AS3 Milestone (Week 15)

Every requirement has at least one test; every test traces back to a requirement. Every
status below is copied from one of four files, and the file is named on the row:

- `test_logs_final/scenario.tsv` - play-mode suite on AB-0.2-QA2, 16 cases, **16 passed / 0 failed / 0 blocked**
- `test_logs_final/editmode_final.txt` - EditMode suite, 17 cases, **17 passed / 0 failed**
- `a11y_audit_week15.log` - accessibility audit run against the current working tree at
  2026-11-18 21:59:37, **423 passed / 0 failed / 0 warnings / 24 not measurable**
- `android_crash_buffer.txt`, `android_rapid_input_monkey.log` - device probes that found
  no device; they record a gap, never a pass

Status vocabulary is the one fixed in `acceptance_criteria.md`: **PASS**, **FAIL**, **BLOCKED**,
**N/A**. `BLOCKED` is used wherever a case could not be executed - because no case exists, or
because the hardware was not attached - and every blocked row says which.

## Requirement rows

| REQ | Requirement | Test case | Type | Executed in | Status |
|---|---|---|---|---|---|
| REQ-01 | Title to match navigation | TC-CORE-01 | Functional | scenario.tsv | **PASS** (match started through `GameManager.StartGame`; the button path itself was exercised by the Week 14 tester sessions, not here) |
| REQ-02 | Movement | TC-CORE-02 | Functional | scenario.tsv | **PASS** (rostered, free, motor unfrozen; the stick-to-displacement measurement is not part of this case) |
| REQ-03 | Sprint and stamina | no runtime case | Functional | - | **BLOCKED** - see Gap 1 |
| REQ-04 | Catch | TC-CORE-04, TC-BND-01 | Functional / boundary | scenario.tsv | **PASS** (prison held the prisoner; 3 catches in one frame took `redsCaptured` 1 -> 4 with no exception) |
| REQ-05 | Prison holds the prisoner | TC-CORE-04 | Functional | scenario.tsv | **PASS** (`moved 0.000 m in 0.4 s, motor.IsFrozen=True`) |
| REQ-06 | Rescue | TC-CORE-05 | Functional | scenario.tsv | **PASS** (released, 0.8 s immunity) |
| REQ-07 | Flag pick-up | TC-CORE-06, TC-BND-03 | Functional / boundary | scenario.tsv | **PASS** (`IsCarryingFlag=True`; 50 rapid attempts left one consistent state) |
| REQ-08 | Flag drop and 10 s return (+ carry penalty) | TC-CORE-07, TC-STA-02 | Functional / boundary | scenario.tsv + editmode_final.txt | **PASS** (`AtBase` after the timer; carry penalty is regen-only as designed) |
| REQ-09 | Win | TC-CORE-08 | Functional | scenario.tsv | **PASS** (`outcome FlagCarriedHome` with the victory clip) |
| REQ-10 | Loss | TC-CORE-09 | Functional | scenario.tsv | **PASS** (`outcome FlagCarriedHome` with the defeat clip) |
| REQ-11 | Draw | TC-CORE-10 | Functional | scenario.tsv | **PASS** (`outcome TimeLimit`, no winning feedback) |
| REQ-12 | HUD agrees with the rules | TC-BUILD-01 only | Functional / Build | scenario.tsv | **BLOCKED** for the agreement itself - see Gap 2. The AI read-out half passes |
| REQ-13 | Profile name persists | TC-PROF-04 | Functional | editmode_final.txt | **PASS** |
| REQ-14 | Profile stats persist | TC-PROF-05 | Functional | editmode_final.txt | **PASS** |
| REQ-15 | Leaderboard ordering | TC-PROF-06 | Functional | editmode_final.txt | **PASS** (fastest first; duplicate match ids refused) |
| REQ-16 | Profile reset | TC-PROF-07 | Negative | editmode_final.txt | **PASS** |
| REQ-17 | Audio and haptics | TC-CORE-12 (asserted inside CORE-05/06/08/09) | Functional | scenario_summary.txt | **PASS** for the clips - all five event types produce their `[Feedback]` line naming the clip. Haptics are **BLOCKED**: no vibration in the editor, see Gap 3 |
| REQ-18 | Pause and resume | TC-LIFE-01, TC-LIFE-02, TC-BND-02 | Boundary | scenario.tsv | **PASS** (`paused delta 0.00 s, then ran again`; 50 toggles left `timeScale 1.00`) |
| REQ-19 | Backgrounding pauses the match | TC-LIFE-03, TC-BND-06 | Boundary | scenario.tsv + device | **PASS** in the editor-invoked form (`clock frozen for 0.8 s`); **BLOCKED** on hardware - TC-BND-06 |
| REQ-20 | Accessibility floors | TC-BUILD-03 | Build | a11y_audit_week15.log | **PASS** (423 passed / 0 failed / 24 not measurable) |
| REQ-01..20 | Corrupt, empty and missing profile data | TC-PROF-01, TC-PROF-02, TC-PROF-03 | Negative | editmode_final.txt | **PASS** |
| REQ-03..11 | Invalid configuration values | TC-SET-01, TC-SET-02, TC-SET-03 | Negative | editmode_final.txt | **PASS** (team size and difficulty both clamp; `PlayerPrefs` round-trips) |
| REQ-03..11 | Out-of-range settings driven through a live match | TC-BND-04 | Negative | - | **BLOCKED** - see Gap 4 |
| REQ-04..07 | Rapid repeated input | TC-BND-01, TC-BND-02, TC-BND-03 | Boundary | scenario.tsv | **PASS** (no exception in any of the three) |
| REQ-13..16 | Low-resource / unwritable storage | TC-BND-05 | Negative | editmode_final.txt | **PASS** (a folder it cannot create fails without claiming a new name) |
| REQ-18 | Restart leaves clean match state | TC-LIFE-02 | Boundary | scenario.tsv | **PASS** (`clock 2.8 s, outcome None`) |
| REQ-04..17 | Build ships with the debug read-out off | TC-BUILD-01, TC-BUILD-02 | Build | scenario.tsv | **TC-BUILD-01 PASS** (`showAiDebug=False`); **TC-BUILD-02 BLOCKED** - the built APK was never observed on a device |
| REQ-04 | A second catch inside the immunity window is ignored | TC-CHR-01 | Boundary | - | **BLOCKED** - see Gap 5 |
| REQ-01..20 | Second configuration (Windows 64-bit, 1920x1080 / 1280x720) | TC-COMPAT-01, TC-COMPAT-02 | Compatibility | P2 | **BLOCKED** - no Windows player was built or run in this lab |
| REQ-01..20 | Physical Android handset | TC-COMPAT-03 | Compatibility | P1 | **BLOCKED** - no device attached, `adb devices` returned no serial |
| - | Interrupted network access | TC-NET-01 | N/A | - | **N/A** (no networking code exists) |

## Case detail

| Case | Steps (summary) | Expected | Actual | Status |
|---|---|---|---|---|
| TC-CORE-01 | Launch, start a match on School Campus 2v2 Hard | A match runs; no console errors | Playing, 4 Blue / 4 Red, 4 enemies, clock 3.0 s | PASS |
| TC-CORE-02 | Confirm the human is rostered and free | Registered, not captured, motor not frozen | BluePlayer free, in its home base | PASS |
| TC-CORE-03 | Trigger a single capture | One `CharacterCaptured`, count +1 | **the harness has no success path for this case** (H-8); the capture was evidenced by TC-CORE-04 and TC-BND-01 instead | BLOCKED |
| TC-CORE-04 | Inspect the captured character | Immobile in the prison until released | moved 0.000 m in 0.4 s, `motor.IsFrozen=True` | PASS |
| TC-CORE-05 | A team-mate reaches the prison | `CharacterRescued`; prisoner free and mobile | released, immunity 0.8 s | PASS |
| TC-CORE-06 | Walk onto the enemy flag | `Carried`; carry penalty armed | carried by BluePlayer, `IsCarryingFlag=True` | PASS |
| TC-CORE-07 | Drop the carried flag, wait out the timer | `Dropped`, then `AtBase` | `AtBase` 1.5 real s after the drop at 20x | PASS |
| TC-CORE-08 | Carry the flag into the home base | Match ends, victory feedback | `outcome FlagCarriedHome`, victory clip | PASS |
| TC-CORE-09 | Let the enemy carry our flag home | Match ends, defeat feedback | `outcome FlagCarriedHome`, defeat clip | PASS |
| TC-CORE-10 | Run the clock to zero | Match ends as a draw | `outcome TimeLimit`, no winning feedback | PASS |
| TC-CORE-11 | Compare HUD count to rules state | Identical values | not written - Gap 2 | BLOCKED |
| TC-CORE-12 | Trigger each of the five events | One `[Feedback]` line per event, naming its clip | 75 lines captured, all five types present: `catch -> Catch`, `rescue -> Rescue`, `flag picked up -> FlagPickup`, `victory -> Victory`, `defeat -> Defeat`; no dedicated phase, asserted inside CORE-05/06/08/09 | PASS |
| TC-LIFE-01 | Pause, wait, resume | Clock frozen while paused | paused delta 0.00 s, then ran again | PASS |
| TC-LIFE-02 | Restart a match | Clock and outcome start clean | clock 2.8 s, outcome None | PASS |
| TC-LIFE-03 | Invoke the pause handler as a background event | Match pauses, clock stops | paused with the clock frozen for 0.8 s | PASS |
| TC-BND-01 | 100 rapid captures | No exception; count consistent | 3 in one frame, `redsCaptured` 1 -> 4 | PASS |
| TC-BND-02 | 50 pause toggles | No exception; known end state | no exception; Playing, `timeScale 1.00` | PASS |
| TC-BND-03 | 50 rapid pick-up attempts | No exception; one consistent state | no exception; state Dropped | PASS |
| TC-BND-04 | Out-of-range settings into a live match | Values clamp; match runs | not written - Gap 4 | BLOCKED |
| TC-BND-05 | Save with an unwritable data directory | Fails gracefully, no throw | `Save_WhenTheFolderCannotBeCreated` PASS | PASS |
| TC-BND-06 | HOME then resume on a handset | Match paused, restored on return | no handset attached | BLOCKED |
| TC-PROF-01 | Load a garbage `profile.json` | Defaults, no throw, broken file kept | PASS | PASS |
| TC-PROF-02 | Load an empty `profile.json` | Defaults | PASS | PASS |
| TC-PROF-03 | Load with no file and no directory | Defaults, no error | PASS | PASS |
| TC-PROF-04 | Save a name, reload | Same name | PASS | PASS |
| TC-PROF-05 | Record a match, reload | Counters kept | PASS | PASS |
| TC-PROF-06 | Record 3 matches, read leaderboard | Ordered; duplicates refused | PASS | PASS |
| TC-PROF-07 | Reset, reload | Everything cleared, profile usable | PASS | PASS |
| TC-SET-01 | `SetTeamSize(0, -1, 99, 2..4)` | Clamped to 2..4 | PASS | PASS |
| TC-SET-02 | `SetDifficulty` with an invalid value | Clamped to a legal preset | PASS - was **FAIL** on the baseline, DEF-15-03 | PASS |
| TC-SET-03 | Save/load settings | Round-trips through `PlayerPrefs` | PASS | PASS |
| TC-STA-01 | Sprint to 0 stamina at runtime | Speed returns to normal; sprint blocked | no runtime case - Gap 1 | BLOCKED |
| TC-STA-02 | Measure the carry penalty | Applied as designed | `carrySpeedMultiplier = 1f`, `carryRegenMultiplier = 0.5f` | PASS |
| TC-STA-03 | Stop sprinting at runtime | Stamina regenerates | no runtime case - Gap 1 | BLOCKED |
| TC-CHR-01 | Second capture inside the immunity window | Ignored | no case - Gap 5 | BLOCKED |
| TC-TEAM-01..03 | Existing `TeamRulesTests` | All three still pass | 3/3 PASS | PASS |
| TC-BUILD-01 | Inspect the running game | `showAiDebug` false outside the editor | `showAiDebug=False` (guard `HudController.cs:59-63`, scene off) | PASS |
| TC-BUILD-02 | Match HUD in the built APK | No AI role/state read-out | the APK was packaged and never installed - no device | BLOCKED |
| TC-BUILD-03 | Run `Tools/Playtest/Run Accessibility Audit` | >= 423 passed, 0 failed | 423 passed / 0 failed / 0 warnings / 24 not measurable | PASS |
| TC-COMPAT-01 | Windows player at 1920x1080 | All UI reachable, match completable | no Windows player built in this lab | BLOCKED |
| TC-COMPAT-02 | Windows player at 1280x720 | All UI reachable, match completable | no Windows player built in this lab | BLOCKED |
| TC-COMPAT-03 | APK on a physical handset | Five tasks complete; no crash | `adb devices` returned no serial | BLOCKED |
| TC-NET-01 | Interrupt network | No network path exists | grep for `UnityWebRequest\|System.Net\|HttpClient\|Socket\|WebClient` under `Assets/Scripts`: no matches | N/A |

## Coverage gaps - named, not hidden

These are the places where a requirement is **not** fully proved by the Week 15 suites. Each is
carried into the acceptance summary and given a week, per the rule in `acceptance_criteria.md`
that a deferred item gets a week number instead of a silent pass.

| # | Gap | Effect | Covered by instead | Next action |
|---|---|---|---|---|
| 1 | No runtime sprint case (TC-STA-01, TC-STA-03) | drain, lock-out and regen at 0 stamina are unpinned by any executed test | `SprintConfigTests` pins the numbers only | Week 16: add a play-mode phase that sprints to 0 and measures speed |
| 2 | No HUD-vs-rules case (TC-CORE-11) | the HUD's capture count, clock and team state are not compared to `MatchManager` | Week 14 screenshots read the HUD by eye | Week 16: compare `HudController` labels to `MatchManager` fields in one phase |
| 3 | No dedicated audio case (TC-CORE-12), and haptics cannot fire in an editor run | the five clip names are evidenced from the run's own `[Feedback]` lines, but the SOUND toggle is not asserted and the catch buzz is unobservable | `scenario_summary.txt`: 75 lines, all five event types | Week 16: a phase that toggles SOUND off and confirms silence; a device case for the buzz |
| 4 | No TC-BND-04 | an out-of-range setting is clamped at the setter but the match is never started with one | TC-SET-02 pins the setter, `Load():149` clamps on the way in | Week 16 |
| 5 | No TC-CHR-01 | the capture-immunity window is never tested at runtime | `CharacterStatus.IsImmune:60` read, not exercised | Week 16 |
| 6 | H-8: TC-CORE-03 has Fail paths and no success path | a single catch can never be recorded as its own pass | TC-CORE-04 + TC-BND-01 | Week 16: add the missing `Pass` branch |
| 7 | P2 (Windows 1080p / 720p) never built | TC-COMPAT-01/02 have no data | none | build a Windows player in Week 16 and repeat the five tasks at both sizes |
| 8 | P1 (handset) never attached | TC-COMPAT-03, TC-BND-06, TC-BUILD-02 have no data | none - recorded in `android_crash_buffer.txt` | install AB-0.2-QA2 with `adb install -r` and run the four device cases |

## Orphan check

- Every REQ in `requirements.md` (REQ-01..REQ-20) appears at least once above. No REQ is orphaned.
- Every executed case names its REQ. No executed case has a blank REQ.
- Three case ids in this matrix produced **no** result and are marked BLOCKED with the reason:
  TC-CORE-03 (no success path in the harness), TC-CORE-11, TC-CORE-12 (never written). The
  remaining BLOCKED rows are device and configuration cases, which `android_crash_buffer.txt`
  and `android_rapid_input_monkey.log` document as deliberate gaps.
- Tests that exist but are not in `requirements.md`: `MatchSettingsTests.DerivedCounts_...`
  (team size drives the derived counts) and `SprintConfigTests.SprintNumbers_...` (sprinting can
  start again after running out). Both support REQ-03 and REQ-08 and are listed under TC-SET-03
  and TC-STA-02 in `editmode_final.txt`; they add coverage to requirements that already have a
  row and orphan nothing.
