# Week 13 - Secure Player Profile, PT5, and Documentation Set

## Objective
Ship Week 13 of MOBILE DEV 002 (Module 5) for AgawanBase: an in-game local player profile with an integrity-checked save and a local best-times leaderboard (PT5), plus the SA5 analysis, data inventory, data-flow diagram, disclosure record, evidence and Week 13 reflection in `Docs/Zedrich Abrera/`.

## Changes
- `Assets/Scripts/Core/PlayerProfile.cs` - (create) static profile store: name, W/L/D, streaks, 20-match history, top-5 fastest victories; JSON at `Application.persistentDataPath/AgawanBase/profile.json` with atomic `.tmp` replace + SHA-256 signature salted from `PlayerPrefs["profile.salt"]`; corrupt/tampered save starts fresh under `[Profile]` warnings, never throws.
- `Assets/Scripts/UI/ProfileUI.cs` - (create) `Game/UI/Panel_Profile` controller: name `InputField` (16 chars), stats line, top-5 board, SAVE / RESET (two-press confirm) / BACK, `[ProfileUI]` logging.
- `Assets/Scripts/UI/GameManager.cs` - (edit) add `GameState.Profile`, `public GameObject panelProfile`, `ShowProfile()`/`CloseProfile()`, panel visibility in `SetState()`, and one `PlayerProfile.RecordMatch(...)` call from `HandlePlayerWon` / `HandlePlayerLost` / `HandleMatchDrawn` (guarded against double-record).
- `Assets/Editor/ProfileBuild.cs` - (create) builds `Game/UI/Panel_Profile` into all three scenes and adds a `PROFILE` button under `Panel_Title/StartButton`, wiring listeners and `gm.panelProfile`; re-runnable, deletes its own panel first.
- `Assets/Editor/Pt5Checks.cs` - (create) writes `Evidence/pt5_checks.log` (PASS/FAIL lines) and captures the profile screenshots with `ScreenCapture.CaptureScreenshot`.
- `Docs/Zedrich Abrera/README.md` - (create) student/game/theme/platform/engine table, install, controls, known limitations, repository + commit ID, Drive folder name `ABRERA_Zedrich_<StudentNumber>_AgawanBase`.
- `Docs/Zedrich Abrera/Week_13/Abrera_Zedrich_SA5.{md,html}` + `Evidence/SA5_data_flow.svg` - (create) Theme-to-Mechanics Justification, player analysis, data inventory, data-flow diagram, integration plan, risks.
- `Docs/Zedrich Abrera/Week_13/Abrera_Zedrich_PT5.{md,html}` - (create) component description, save file schema, integrity design, L1-L15 security test table with actual results, disclosure record, commit ID.
- `Docs/Zedrich Abrera/Week_13/Abrera_Zedrich_Week13_Reflection.{md,html}` - (create) 1-2 page first-person reflection answering the four Week 13 prompts, ending with the required authenticity declaration.
- `Docs/Zedrich Abrera/Week_13/Evidence/` - (create) `pt5_checks.log`, `HOW_TO_RERUN.txt`, `secrets_and_gitignore.txt`, `build_and_commit.txt`, `E1..E4` PNGs.

## Relevant Assets and Quirks
- Scenes are binary-serialized; **all UI is built by editor scripts** (`MapSelectBuild.cs` iterates `Game.unity` / `Map_Palengke.unity` / `Map_SchoolCampus.unity` and is the exact pattern to copy). Never hand-edit `.unity`.
- Existing UI names: `Game/UI/Panel_Title` (has only `StartButton`), `Panel_MapSelect`, `Panel_MatchSetup`, `Panel_Hud`, `Panel_Pause`, `Panel_End`. Panels are a full-screen `Image` backdrop, `SetActive(false)`, `SetAsLastSibling()`.
- House UI palette (`MapSelectBuild.cs:33-37`): backdrop `(0,0,0,0.75)`, button `(0.20,0.26,0.36)`, warm `(0.95,0.72,0.25)`, soft text `(0.75,0.79,0.85)`. Legacy uGUI `Text`/`Button`; copy the font from `Panel_Title/TitleText` (`TeamMatchBuild.HouseFont()` is private - reimplement).
- A legacy `Text` taller than its rect renders nothing - size boxes at ~1.12x fontSize (see `TeamMatchBuild.cs:212-216`).
- `MatchSettings` (team size, difficulty, tag hints) and `MapSettings` (map id/scene) are the only current `PlayerPrefs` owners; the profile deliberately goes to a JSON file instead, and SA5 documents that split.
- Result data: `MatchManager.MatchClock` (public getter), `MatchManager.outcome` (`MatchOutcome.None/FlagCarriedHome/Elimination/TimeLimit/Draw`), events `PlayerWon`/`PlayerLost`/`MatchDrawn` already forwarded by `GameManager.Start`.
- Domain reload is **off** - statics survive play sessions; reset cached profile state with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, precedent `GameManager.ResetPlaySessionFlags`.
- `*.apk` is gitignored, so the PT5 build lands in `Docs/` and is not versioned - state that in PT5.
- Android build facts from `Docs/Part_D_Test_Results.md`: ARM64/IL2CPP, minSdk 26, landscape, ~99 s, ~36.6 MB; bundle id is still the URP template default (record as a known limitation).
- Data inventory must stay honest: only a player-typed display name, match settings, map choice and match results are stored. No device identifiers, no accounts, no network permission, no secrets.
- `Docs/` is outside `Assets/`, so `write_file` is allowed there and no Unity reimport is triggered.

## Steps
- [x] 1. Create `Assets/Scripts/Core/PlayerProfile.cs`: `Name`/`SetName()` (trim, strip control chars, 16-char cap, blank -> `PLAYER`), `Stats`, capped `Recent` list, `BestWins` top-5, `RecordMatch(result, outcome, seconds, mapId, teamSize, difficulty)`, `ResetProfile()`, `Load()`, salted SHA-256 signature, atomic save, corrupt-save recovery, `[Profile]` debug logs [depends: none]
- [x] 2. Create `Assets/Scripts/UI/ProfileUI.cs` bound to `nameField`, `statsText`, `boardText`, `statusText`, `saveButton`, `resetButton`, `backButton`, `game`, with `Open()` / `Close()` / `Refresh()` and `[ProfileUI]` logs [depends: 1]
- [x] 3. Edit `Assets/Scripts/UI/GameManager.cs`: add `GameState.Profile`, `panelProfile`, `ShowProfile()`/`CloseProfile()`, panel visibility in `SetState()`, and the single guarded `PlayerProfile.RecordMatch(...)` call in the three end-of-match handlers [depends: 1]
- [x] 4. Create `Assets/Editor/ProfileBuild.cs` and run it via `execute_script`: build `Game/UI/Panel_Profile` in all three scenes, add `Panel_Title/ProfileButton`, wire `gm.ShowProfile` + `ProfileUI` handlers, save scenes, restore `Game.unity` [depends: 2,3]
- [x] 5. Create `Assets/Editor/Pt5Checks.cs`, run it in play mode, and keep its output: `Docs/Zedrich Abrera/Week_13/Evidence/pt5_checks.log` (L1 no save / L2 valid name / L3 survives reload / L4 blank name / L5 long name / L6 control chars stripped / L7 match recorded / L8 board sorted+capped / L9 duplicate refused / L10 corrupt JSON no crash / L11 tampered signature rejected / L12 wrong types coerced / L13 reset erases on disk / L14 unwritable path no crash / L15 known limit: local save is tamper-evident, not encrypted) plus `E1_profile_empty.png`, `E2_profile_stats.png`, `E3_leaderboard.png`, `E4_after_reset.png` [depends: 4]
- [x] 6. Build the PT5 APK with `manage_build` (Android, ARM64/IL2CPP) to `Docs/Zedrich Abrera/Week_13/Abrera_Zedrich_PT5.apk`, then write `Evidence/build_and_commit.txt` (Unity version, pipeline, backend, APK bytes, `check_compile_errors` result, `git rev-parse --short HEAD` + `git status --porcelain` via `execute_command`) and `Evidence/secrets_and_gitignore.txt` (grep for keys/tokens + `.gitignore` APK exclusion) [depends: 5]
- [x] 7. Write the documentation set in `Docs/Zedrich Abrera/`: `README.md`, `Week_13/Abrera_Zedrich_SA5.md` + `.html` + `Evidence/SA5_data_flow.svg`, `Week_13/Abrera_Zedrich_PT5.md` + `.html`, `Week_13/Abrera_Zedrich_Week13_Reflection.md` + `.html`, `Week_13/Evidence/HOW_TO_RERUN.txt` - each HTML print-styled (`@media print`, A4, SVG inline) so Ctrl+P saves the PDF [depends: 6]
  - NOTE: `Tools > PT5 > Run Data Checks` and `Tools > PT5 > Capture Profile Screenshots` menu items were added to `Assets/Editor/Pt5Checks.cs` so `Evidence/HOW_TO_RERUN.txt` can name a real command instead of an execute_script call. Extra files beyond the plan: `Docs/tools/MdToHtml.cs` (the generator behind the three `.html` files) and `Evidence/build_and_commit.txt` / `Evidence/secrets_and_gitignore.txt` / `Evidence/HOW_TO_RERUN.txt`. Unity's 501 MB IL2CPP backup folder that the APK build left beside the APK was deleted from the submission folder.

## Verify
- `check_compile_errors` returns "No compile errors"; `get_unity_logs` shows `[Profile]`/`[ProfileUI]`/`[PT5]` lines only for the new feature, no new exceptions.
- `Evidence/pt5_checks.log` ends with `TOTAL: <n> passed, 0 failed`.
- Regression: Title -> PROFILE -> type a name -> SAVE -> BACK -> START -> map -> match setup -> play to a victory -> end panel -> PROFILE shows the win, the streak and the fastest time; then RESET clears the file on disk and the panel reads `PLAYER`.
- Close and reopen the editor (or play twice in a row) and confirm the name and stats persist - proves the save survives an app restart.
- Four screenshots exist and each shows the named state; APK exists at the required path and is >20 MB.
- All six documents open, the HTML prints to a readable PDF, and the reflection ends with the authenticity declaration.

## Open items the user must do
- Commit the change (suggested message: `PT5: local player profile with signed save and best-times board`) and paste the commit ID into `README.md` and `PT5.md`.
- Fill the student number in the Drive folder name, and export the three `.html` files to PDF (`Abrera_Zedrich_SA5.pdf`, `Abrera_Zedrich_PT5.pdf`, `Abrera_Zedrich_Week13_Reflection.pdf`).
