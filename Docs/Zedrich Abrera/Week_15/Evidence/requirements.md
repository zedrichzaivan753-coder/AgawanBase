# Testable requirements - AgawanBase v0.2 (AS3 Milestone)

Every requirement below is stated so that a test can pass or fail it. "Implemented by" names
the script that holds the behaviour, so the matrix in `traceability_matrix.md` links a
requirement to both a test **and** a place in the code.

| REQ | Requirement (testable form) | Implemented by |
|---|---|---|
| REQ-01 | From launch, the player can reach a running match through title -> map select -> setup without the editor | `Assets/Scripts/UI/GameManager.cs` (`StartGame:249`, `StartMatchAndReload:300`, `GoToTitle:314`), `Assets/Scripts/UI/MatchSetupUI.cs:94` |
| REQ-02 | The joystick moves the player in the input direction at the configured speed | `Assets/Scripts/UI/VirtualJoystick.cs`, `Assets/Scripts/Gameplay/PlayerController.cs`, `Assets/Scripts/Core/CharacterMotor.cs` |
| REQ-03 | Sprint raises movement speed while stamina > 0, and cannot be held at 0 stamina | `Assets/Scripts/Core/SprintStamina.cs`, `Assets/Scripts/UI/SprintButton.cs` |
| REQ-04 | Touching an enemy captures them and raises `CharacterCaptured` exactly once | `MatchManager.cs:354` (`Capture`), `CharacterStatus.cs:164`, `IsImmune:60` |
| REQ-05 | A captured character is held in the prison and cannot act until released | `Assets/Scripts/Core/CharacterStatus.cs`, `MatchManager.cs` |
| REQ-06 | Touching the prison frees the prisoner and raises `CharacterRescued` exactly once | `MatchManager.cs:435` (`Rescue`, private), `CharacterStatus.cs:193` (`Release`) |
| REQ-07 | Walking onto the enemy flag sets `FlagState.Carried` and raises `PickedUp` | `Assets/Scripts/Gameplay/Flag.cs` (`TryPickUp:139`, `PickedUp:165`, states `:20`) |
| REQ-08 | A carried flag drops on capture and returns home after 10 s | `Flag.cs` (`Drop:171`, `ReturnHome:191`, `returnAfterSeconds:44`) |
| REQ-09 | Carrying the flag into the home base ends the match as a win for that team | `MatchManager.cs` (`WouldWinTag:300`, `EndMatch:693`, `PlayerWon:154`) |
| REQ-10 | The enemy carrying our flag home ends the match as a loss | `MatchManager.cs` (`EndMatch:693`, `PlayerLost:157`) |
| REQ-11 | The clock reaching zero with no flag taken ends the match as a draw | `MatchManager.cs` (`MatchClock:185`, `EndDraw:719`, `MatchDrawn:181`) |
| REQ-12 | The HUD's capture count, clock and team state match the rules' own state | `Assets/Scripts/UI/HudController.cs` |
| REQ-13 | A profile name entered on the profile screen survives an app restart | `Assets/Scripts/Core/PlayerProfile.cs` (`profile.json:133`), `Assets/Scripts/UI/ProfileUI.cs` |
| REQ-14 | Matches, wins, losses and captures accumulate and survive a restart | `PlayerProfile.cs:288` (`RecordMatch`), `GameManager.cs:351` (`RecordResult`) |
| REQ-15 | The leaderboard reflects recorded matches and orders them correctly | `PlayerProfile.cs` (`NextMatchId:148`, ranking), `ProfileUI.cs` |
| REQ-16 | Profile reset clears name, stats and leaderboard and leaves a usable profile | `PlayerProfile.cs:352` (`ResetProfile`) |
| REQ-17 | Each of the five events plays its clip; the SOUND toggle silences it and remembers | `Assets/Scripts/Core/GameFeedback.cs` (`Play:203`, `ToggleSound:144`, subscriptions `:99-113`), `Assets/Audio/Generated/*.mp3` |
| REQ-18 | Pause stops the match clock; resume continues from the same state | `GameManager.cs` (`SetState:418`, `timeScale` at `:426`) |
| REQ-19 | Sending the app to the background pauses the match; returning does not lose it | `GameManager.cs` (`OnApplicationPause`, `OnApplicationFocus`, appended at the end of the class) calling `Pause()`/`SetState:418`; fixed in step 5, DEF-15-01 |
| REQ-20 | Every label is >= 4.5:1 against its panel and every button >= 144x144 canvas px | `Assets/Editor/A11yAudit.cs` (measures), `Assets/Editor/AccessibilityBuild.cs` (fixes) |

## Test types used

- **Functional** - normal gameplay, driven the way a player drives it.
- **Negative** - invalid input, corrupt or missing data, spam input.
- **Boundary** - the edges: 0 stamina, clock end, 100 rapid events, backgrounding.
- **Build** - properties of the shipped player, not the editor (debug read-out, a11y totals).
- **Compatibility** - the same build on a second configuration.
- **N/A** - interrupted network access: no networking code exists in this project.

## Requirements that were known-broken before testing started

| REQ | State at the start | Defect | How it ended |
|---|---|---|---|
| REQ-19 | Unhandled: nothing paused on background | DEF-15-01 | Reproduced by TC-LIFE-03 against the baseline, fixed in `GameManager`, re-run green |
| REQ-08 | The carry penalty named in PT6 appeared absent: `SprintStamina.carrySpeedMultiplier = 1f` (`SprintStamina.cs:49`) while only regen halved (`:54`) | DEF-15-02 (proposed) | **Withdrawn.** `SprintStamina.cs:45-54` documents the penalty as regen-only by design; a speed penalty would make the run home unwinnable. `SprintConfigTests` now pins the two multipliers so the design cannot drift silently |
| REQ-03..11 | `MatchSettings.SetDifficulty` accepted any int and fell through to HARD's numbers without saying so | DEF-15-03 (found by the EditMode baseline, not by inspection) | Fixed with a clamp to 0..2 and a log line; `MatchSettingsTests` green |

DEF-15-01 and DEF-15-03 are tested **first**, because a test that only confirms what is already
known broken is worthless, while a test that reproduces it is the evidence the fix needs. Full
records, severities and evidence are in `defect_log.md`.
