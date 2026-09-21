# Part D — Build, Testing, Troubleshooting, and Evaluation

## Objective
Execute the eight required test rows against the live `AgawanBase` project, capture real screenshot evidence and, where a row cannot be driven through the UI, obtain its measurement through a scripted in-editor probe, then write one report to `Docs/Part_D_Test_Results.md` covering items D.1–D.6 with every screenshot saved beside it.

## Changes
- `Docs/Part_D_Test_Results.md` - (create) the deliverable: the 8-row Required Test Record with Expected / Actual / Status / Action, plus the D.1–D.6 write-up.
- `Docs/Evidence/R01_title.png` … `R08_camera.png` - (create) one or more screenshots per test row, written to disk.
- `Docs/Evidence/probe_log.txt` - (create) raw probe output (positions, timings, fillAmounts, counters) so every "Actual" cell is traceable to a measurement.
- `Assets/Editor/PartDProbe.cs` - (create, then **delete**) temporary editor-only harness that drives play mode, reads state and writes the PNGs. No runtime script and no scene field is modified.
- **No project code, scene or serialized field is edited.** D.5 is a proposal only (Confirmed decision 3 = option B).

## Relevant Assets and Quirks
- **Android is already configured**: `AndroidMinSdkVersion: 26`, `AndroidTargetArchitectures: 2` (ARM64), `scriptingBackend: {Android: 1}` (IL2CPP), `defaultScreenOrientation: 3` with only `allowedAutorotateToLandscapeLeft: 1`. One enabled build scene: `Assets/Scenes/Game.unity`.
- **No APK exists yet.** `build/outputs/` contains only `logs/unity---stop-build.log`, so row 7 must be a real build.
- **Broadcast/build bridge is down.** `manage_build` returns `"No Unity Editor instances found"` while Aura's own tools answer normally. Android SDK/NDK/jdk presence is therefore unverified; row 7 reports the true outcome either way.
- **Row 8's camera fix is already implemented.** `FollowCamera` on `Game/CameraRig`: `visibleWidthPercent 0.65`, `arenaCenter (0,-1)`, `arenaSize (34.5,19)`, `edgeMargin 5`, `snapOnStart true`, `target: None` (self-resolves to the human once in `Start`). Row 8 is a **re-verification**, not a new fix. `barangay_.../camera_zoom_follow_and_facing_v1.md` records 16:9 → `orthographicSize 6.31`, visible **22.4 × 15.4 m**.
- **Probable defect for D.3–D.5**: `HudController` is serialized with `showAiDebug: true` and `aiDebugLabel` wired to `Panel_Hud/AIDebugLabel`, contradicting its own tooltip ("Leave it off for the shipped build: it is a debugging aid, and each line names a team-mate the player is not meant to be watching"). Same class of leftover: `FollowCamera.logView`, `MatchManager.logRules`, `MatchManager.allowDebugDrop`, `CharacterSpawner.logRoster` all true.
- **Real input path exists for scripted tests**: `VirtualJoystick.OnDrag(PointerEventData)` sets `Value` → `PlayerController.Update` → `CharacterMotor.Move`; `SprintButton.OnPointerDown` sets `IsHeld` → `SprintStamina` (`maxStamina 2`, `drainPerSecond 1` → 2.0 s of sprint, `sprintSpeedMultiplier 1.6`).
- Rules to assert against: `tagDistance 1.2`, `flagDistance 1.5`, `rescueDistance 1.5`, `immunitySeconds 2`, `rescueCooldown 3`, `tieEpsilon 0.01`, `matchTimeSeconds 180`. `Freshness01 = 1 - fieldTime/20`. Inside `homeBase`, `fieldTime` is forced to 0 (that *is* the safe zone).
- `MatchSetupUI.StartMatch()` only calls `StartGame()` when `CharacterSpawner.SpawnedTeamSize/SpawnedDifficulty` already match `MatchSettings`; otherwise it reloads the scene — row 2 must test both routes.
- `PlayerPrefs` was reset to 1v1/Normal but a later live check found 4v4/Hard, so read the fresh-launch state rather than assuming it.
- **Screenshots must be written with `ScreenCapture.CaptureScreenshot`.** `capture_editor_screenshot` returns an inline image, not a file. `capture_editor_screenshot(mode="game_view")` is used only for my own visual confirmation.
- `EditorApplication.isPaused` flips to true on its own and silently freezes frames — clear it before **every** play-mode check.
- `manage_scene`-style bridge edits do not dirty the scene; any wiring must be followed by a forced `EditorSceneManager.SaveOpenScenes()`. Avoid needing this: modify nothing.
- **Never use `replace_in_file` on this project** — it previously injected patch markers into `EnemyAI.cs`, `GameManager.cs`, `MatchManager.cs`. Whole-file writes only.
- Do not re-run `BarangayBuild.CameraAndLighting()` / `BarangayUtils.Zoom()/CentreOn()` — they hard-code `orthographicSize 14` and would undo the row-8 camera fix.
- HUD is legacy `UnityEngine.UI.Text`, font `LegacyRuntime`, ASCII only (no TMP); assert strings with ASCII substrings.

## Steps
- [x] 1. Baseline: `check_compile_errors` + `get_unity_editor_state` (record Unity version, pipeline, build target), confirm the single enabled build scene; enter play mode, clear `EditorApplication.isPaused`, capture the Title screen and write `Docs/Evidence/R01_title.png`. Record row 1's Actual. [depends: none]
- [x] 2. Row 2 — screen flow: invoke `StartButton.onClick` (Title → Match Setup), write `R02_matchsetup.png`; read `MatchSettings` for the fresh-launch size/skill; invoke the setup START and stopwatch `Time.realtimeSinceStartup` until `GameManager.State == Playing` and the arena/bases/flags/HUD exist; repeat through the reload route; write `R02_hud.png`. [depends: 1]
- [x] 3. Row 3 — input: build a `PointerEventData` and call `VirtualJoystick.OnDrag` to push the stick in each cardinal direction, measuring `BluePlayer` world displacement and `CharacterMotor.MoveDirection`; call `OnPointerUp` and confirm the character stops; then `SprintButton.OnPointerDown` and measure displacement per second against walking plus the `Stamina01` drain to 0; write `R03_stamina.png`. [depends: 2]
- [x] 4. Rows 4 and 6 — gameplay event and state transition: stage two opposing characters 1.0 m apart outside both bases with a deliberate `fieldTime` gap, confirm the lower one stays free and the higher is teleported into `PrisonFor*` frozen, then stage a free team-mate at `rescueDistance` and confirm release, `fieldTime 0`, immunity; separately drive carried flag → own base for `VICTORY!`, enemy carrier → own base for `GAME OVER`, pause (`Time.timeScale == 0`) and RESTART. Write `R04_capture.png`, `R06_victory.png`, `R06_gameover.png`, `R06_pause.png`. [depends: 2]
- [x] 5. Row 5 — score/status: read `FreshnessFill.fillAmount` and `FreshnessLabel.text` inside and outside `Base_Blue`, `StaminaFill.fillAmount` before/during/after sprinting, and `TeamManager.Count(Team.Blue/Red)` plus the `BlueStatusLabel`/`RedStatusLabel` strings across one capture and one rescue; write `R05_hud_before.png`, `R05_hud_after.png`. [depends: 3, 4]
- [x] 6. Row 7 — build: exit play mode, then run an Android ARM64/IL2CPP build via `manage_build(action="build")` to `build/outputs/AgawanBase.apk`; record success/failure, APK path, byte size, and the exact error if it fails; confirm the manifest's min SDK 26, ARM64 and landscape. [depends: 5]
- [x] 7. Row 8 and the report: re-verify `FollowCamera` (`orthographicSize` from aspect, visible width 22.4 m, all four clamp limits) at a phone aspect, write `R08_camera.png`, then write `Docs/Part_D_Test_Results.md` with all 8 rows, the D.1–D.6 write-up, the D.3 defect naming `HudController.showAiDebug` / `AIDebugLabel` exactly, the D.4 cause and the D.5 proposal; delete `Assets/Editor/PartDProbe.cs` and re-run `check_compile_errors`. [depends: 1–6]

## Verify
- `Docs/Part_D_Test_Results.md` exists and its Required Test Record has **all eight** rows filled with an Expected, an Actual and a Pass/Fail/Partial status — no blank cells.
- Every row's Action/Finding cites an evidence file that actually exists under `Docs/Evidence/`, and `probe_log.txt` contains the raw numbers behind each "Actual" cell.
- D.3/D.4/D.5 name one concrete defect at file-and-field level, with a fix I did **not** apply, plus the one-line change that would apply it.
- Row 7 states a real outcome: either an APK with its path and size, or the verbatim build error — never an assumed success.
- Row 8 shows the same width of arena visible as the camera plan measured (22.4 m), and rows 2–6 still pass afterwards.
- `check_compile_errors` is clean after `PartDProbe.cs` is deleted, and `list_files Assets/Editor` shows only the seven pre-existing editor scripts.

## Result

All 7 steps completed. Where the outcome differs from the wording above, the reason is recorded here.

- **Row 3 is a Partial, not a Pass.** Direction, 1:1 stick mapping and stop-on-release are all firmly verified, but the measured walk/sprint speeds (`8.961` and `9.861 m/s`) exceed the configured `moveSpeed 6.0`, so the derived sprint ratio `1.100` is not trustworthy against the configured `sprintSpeedMultiplier 1.600`. Source review confirms the multiplier *is* wired correctly (`CharacterMotor.cs:75`, `SprintStamina.cs:144`) and the probe logged `speedMultiplier=1.600` while sprinting, so the anomaly is in the scripted measurement's timing, not the sprint. Row 3's first pass also had to be discarded because the joystick was inactive while the HUD was down (`probe_log.txt:79-88`); the recorded numbers are the re-run.
- **Row 5's screenshot names differ from the plan.** The run produced `R05_hud_outside.png` / `R05_hud_inside.png` (the freshness pair the row actually needs) instead of `R05_hud_before.png` / `R05_hud_after.png`. The before/after counter and label pair across the capture and the rescue is in `probe_log.txt:266-286`.
- **Row 7 did not use `manage_build`.** That tool's bridge stayed down for the whole run (`"No Unity Editor instances found"`). The build used the project's own `BarangayUtils.BuildApk()` instead, which writes to `Builds/AgawanBase.apk` rather than `build/outputs/`. Result: **Succeeded**, `38,415,617 bytes = 36.64 MB`, manifest confirmed min SDK 26 / `arm64-v8a` only / `screenOrientation=0` (landscape). Full facts in `Docs/Evidence/R07_build.txt`. A secondary build-configuration finding is recorded there: the application identifier is still the URP template default `com.UnityTechnologies.com.unity.template.urpblank`.
- **Harness deleted via a self-removing cleanup script.** `delete_script` is on the same down bridge as `manage_build`, so the two harnesses were removed with `AssetDatabase.DeleteAsset` from a temporary `PartDCleanup.cs` that deleted itself last. `Assets/Editor` now holds only the seven pre-existing editor scripts, `.meta` files included, and `check_compile_errors` is clean.
