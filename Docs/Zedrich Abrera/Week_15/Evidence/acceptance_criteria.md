# Acceptance criteria - AS3 Milestone (Week 15)

Defined **before** any test is executed, so that "it works" is not a judgement made after
seeing the result.

## The four checkpoint conditions

The module's completion checkpoint, restated as four binary conditions. Each must be backed
by a specific evidence file, named here so it cannot be asserted without one.

| # | Condition | How it is proved | Evidence |
|---|---|---|---|
| CC-1 | No unresolved defect causes **data loss** | Corrupt, empty and missing `profile.json` all load a default profile without throwing, and a recorded match survives a restart | `test_logs_*/editmode.txt`, TC-PROF-01..03 |
| CC-2 | No unresolved defect **prevents completion of the core gameplay loop** | The full loop (title -> map select -> setup -> catch -> prison -> rescue -> flag -> win/loss/draw) runs unaided to an end state | `test_results_*.tsv`, TC-CORE-01..08 |
| CC-3 | No unresolved defect **exposes another player's information** | No networking code exists; no profile, salt or stats are transmitted; `HudController.showAiDebug` is editor-only | `environment.txt`, TC-BUILD-02 |
| CC-4 | No unresolved defect **repeatedly crashes** the application | The scenario suite plus 100x rapid input plus a 500-event monkey run produce no unhandled exception and an empty crash buffer | `android_crash_buffer.txt`, TC-BND-01..03 |

## Per-requirement pass criteria

A requirement passes only when its test's *actual* result matches the criterion below. Any
other outcome is FAIL, not "mostly working".

| REQ | Requirement | Pass criterion |
|---|---|---|
| REQ-01 | Title to match navigation | From launch, a match can be started without the editor and without console errors |
| REQ-02 | Movement | The stick moves the player in the input direction at the configured speed |
| REQ-03 | Sprint and stamina | Sprint raises speed while stamina > 0 and cannot be held at 0 stamina |
| REQ-04 | Catch | Touching an enemy captures them and raises `CharacterCaptured` exactly once |
| REQ-05 | Prison | A captured character is immobilised in the prison until released |
| REQ-06 | Rescue | Touching the prison frees the prisoner and raises `CharacterRescued` exactly once |
| REQ-07 | Flag pick-up | Walking onto the enemy flag sets `FlagState.Carried` and raises `PickedUp` |
| REQ-08 | Flag drop | A carried flag drops on capture and returns home after 10 s |
| REQ-09 | Win | Carrying the flag into the home base ends the match as a win for the carrier's team |
| REQ-10 | Loss | The enemy carrying our flag home ends the match as a loss |
| REQ-11 | Draw | The clock reaching zero with no flag taken ends the match as a draw |
| REQ-12 | HUD | The capture count, clock and team state shown match the rules' own state |
| REQ-13 | Profile name | A name entered on the profile screen persists across an app restart |
| REQ-14 | Profile stats | Matches, wins, losses and captures persist across an app restart |
| REQ-15 | Leaderboard | The leaderboard reflects recorded matches and orders them correctly |
| REQ-16 | Reset | Profile reset clears name, stats and leaderboard, and leaves a usable profile |
| REQ-17 | Audio and haptics | Each of the five events plays its clip; the SOUND toggle silences and remembers |
| REQ-18 | Pause and resume | Pause stops the match clock and resume continues from the same state |
| REQ-19 | Backgrounding | Sending the app to the background pauses the match; returning does not lose it |
| REQ-20 | Accessibility | Every label >= 4.5:1 against its panel and every button >= 144x144 canvas px |

## Status vocabulary

- **PASS** - the actual result matched the criterion above.
- **FAIL** - it did not. A FAIL must produce a defect record in `defect_log.md`.
- **BLOCKED** - it could not be executed (no device attached, case needs hardware). A
  BLOCKED case is *not* a pass and must say what was missing.
- **N/A** - the behaviour does not exist in this build (only used for the network case).

## Severity and priority

Severity describes the damage; priority describes the order of work. They are recorded
separately because a crash in a rarely used screen is severe but not urgent.

| Severity | Meaning |
|---|---|
| S1 | Data loss, core loop blocked, information exposure, or a repeated crash |
| S2 | A core-loop step needs help or a workaround, or state is wrong after an event |
| S3 | Completes, but with visible wrongness or a complaint |
| S4 | Cosmetic |

## Ready for optimisation?

The acceptance summary must answer this explicitly. The build is ready for Week 16
optimisation only if CC-1..CC-4 all hold **and** no S1 or S2 defect is open. Anything else
is a deferred item with a week number, not a silent pass.
