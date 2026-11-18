# Week 14 - Usability, Accessibility and Playtesting

## Objective
Ship Week 14 of MOBILE DEV 002 (Module 5): measure the build's accessibility with a script-driven audit, fix what it finds (no audio/haptic feedback at all, contrast and touch-target gaps, the AI debug read-out that would bias testers), run a 3-5 tester playtest session against a dedicated APK, and deliver SA6, PT6, the reflection and the evidence set in `Docs/Zedrich Abrera/Week_14/`.

## Changes
- `Assets/Editor/A11yAudit.cs` - (create) measures every uGUI `Text` and `Button` under `Game/UI` in all three scenes: WCAG contrast of each label against the panel behind it, rect height vs `fontSize * 1.12`, touch-target size in canvas px vs the 144 px floor. Writes `Week_14/Evidence/a11y_audit.log` with `PASS/FAIL` lines and a `TOTAL:` line; menu item `Tools/Playtest/Run Accessibility Audit`.
- `Assets/Editor/AccessibilityBuild.cs` - (create) loops `Game.unity`, `Map_Palengke.unity`, `Map_SchoolCampus.unity` and applies the audit's fixes: min 144x144 per Button, sub-minimum labels raised together with their rects, failing label colours moved to a >= 4.5:1 token, the six debug flags switched off, `Game/UI/Panel_Pause/SoundButton` and `Game/Audio` (`GameFeedback`) created and wired. Re-runnable; deletes its own button and audio object first.
- `Assets/Scripts/Core/GameFeedback.cs` - (create) five one-shots (catch, rescue, flag pick-up, victory, defeat) through one `AudioSource`, `Handheld.Vibrate()` on a catch, `PlayerPrefs["audio.feedback"]` on/off behind the pause-screen SOUND button, `[Feedback]` logs.
- `Assets/Scripts/Gameplay/MatchManager.cs` - (edit) add `CharacterCaptured` / `CharacterRescued` events raised at the end of `Capture()` / `Rescue()`, and a pick-up event on `Flag.TryPickUp`. Needed because the only existing hook, `CaptureCountChanged`, fires once with the reds-captured count and cannot tell a catch from a rescue.
- `Assets/Scripts/UI/HudController.cs` - (edit) `showAiDebug` becomes `true` only under `#if UNITY_EDITOR`, per the proposed correction in `Docs/Part_D_Test_Results.md` D.5.
- `Docs/Zedrich Abrera/Week_14/Playtest/` - (create) consent form, five-task session script, observation sheet with a severity scale, results table (one row per tester), facilitator checklist.
- `Docs/Zedrich Abrera/Week_14/` - (create) `Abrera_Zedrich_SA6.md`, `Abrera_Zedrich_PT6.md`, `Abrera_Zedrich_Week14_Reflection.md` + `.html` twins, and `Evidence/` (audit before/after logs, screenshots, playtest notes, build facts).
- `Docs/tools/MdToHtml.cs` - (edit) render `![caption](path)` as a `<figure>` with the image inlined as a base64 data URI, so the HTML and its PDF carry the screenshots themselves. Week 13's HTML inlined the SVG but referenced no PNG, which is why the images had to be pasted in by hand.

## Relevant Assets and Quirks
- Canvas `Game/UI` has `CanvasScaler` = Scale With Screen Size, 1920x1080, match 0.5, so on a 1080p phone 1 canvas unit is ~1 physical px and Android's 48 dp minimum is ~144 px at xxhdpi. The audit states that assumption; the report must not pretend it is a dp measurement from a device.
- HUD, Pause, Title and End panels are **scene-authored**; only `Panel_MatchSetup` (`TeamMatchBuild`), `Panel_MapSelect` (`MapSelectBuild`) and `Panel_Profile` (`ProfileBuild`) are script-built. Any HUD/pause change therefore goes through one editor script looping the three binary scenes - never hand-edit a `.unity` file.
- Legacy uGUI `Text` whose line height exceeds its rect renders nothing: growing a font size without growing the rect blank the label (`TeamMatchBuild.cs:212-216` precedent).
- House palette (`MapSelectBuild.cs:33-37`): backdrop `(0,0,0,0.75)`, button `(0.20,0.26,0.36)`, warm `(0.95,0.72,0.25)`, soft text `(0.75,0.79,0.85)`. Contrast must be computed per label against the panel it actually sits on - soft text on the button colour is not the same ratio as on the backdrop.
- `SafeAreaFitter` already exists on the UI root; the audit reports safe-area violations but nothing removes the fitter.
- No `AudioClip` asset exists anywhere in the project, so audio is a from-scratch addition: `generate_audio_clip` writes MP3s into `Assets/Audio/Generated` and refreshes the AssetDatabase, and `GameFeedback` is the only thing that plays them.
- `HudController.showAiDebug` is serialized `true` and is not editor-gated, so the shipped APK leaks each AI's role and state. A tester who can read enemy intent is testing a different game, which is why it is fixed **before** the playtest build rather than in Week 15.
- Five further debug flags are on: `FollowCamera.logView`, `MatchManager.logRules`, `MatchManager.allowDebugDrop`, `CharacterSpawner.logRoster`, `Flag.logFlagEvents`. All log-only except `allowDebugDrop`, which is a live gameplay hook.
- Domain reload is off - any static added by `A11yAudit` or `GameFeedback` needs the `[RuntimeInitializeOnLoadMethod]` reset pattern already used by `GameManager.ResetPlaySessionFlags`.
- `*.apk` is gitignored, so the playtest APK lives under `Docs/Zedrich Abrera/Week_14/` and is described only in `Evidence/build_and_commit.txt`.
- Unity writes a ~500 MB `*_BackUpThisFolder_ButDontShipItWithYourGame` folder beside every APK; it must be deleted before submission (Week 13 lesson).
- Playtest results arrive **after** this plan runs: the audit, the fixes, the kit, the APK and the documents are all produced here, but the findings section of PT6 needs the tester rows the student fills in.

## Steps
- [x] 1. Create `Assets/Editor/A11yAudit.cs` and run it in edit mode: contrast ratio per label against its backing panel, rect-height vs font-size, Button size in canvas px, over all three scenes; write `Week_14/Evidence/a11y_baseline.log` as the "before" record the report cites [depends: none]
- [x] 2. Create `Assets/Editor/AccessibilityBuild.cs` and run it: `HudController.showAiDebug` under `#if UNITY_EDITOR`, the five other debug flags off, every Button lifted to >= 144x144, sub-minimum labels raised with their rects, failing colours moved to a >= 4.5:1 token - applied identically to all three scenes [depends: 1]
- [x] 3. Add the feedback layer: `MatchManager.CharacterCaptured`/`CharacterRescued`, `Flag` pick-up event, `Assets/Scripts/Core/GameFeedback.cs`, five clips via `generate_audio_clip` (catch, rescue, flag picked up, victory stinger, defeat stinger), `Handheld.Vibrate()` on catch, SOUND button on the pause panel wired through `AccessibilityBuild` [depends: 2]
- [x] 4. Re-run `A11yAudit` into `Week_14/Evidence/a11y_after.log` and prove the fix: `check_compile_errors` clean, `[A11y]`/`[Feedback]` lines in the console, and play-mode screenshots `E1_pause_sound.png`, `E2_hud_contrast.png`, `E3_catch_feedback.png` captured through the existing `Pt5Shots`-style `EditorApplication.update` harness [depends: 3]
- [x] 5. Build the playtest APK with `manage_build` to `Docs/Zedrich Abrera/Week_14/Abrera_Zedrich_PT6_Playtest.apk`, delete the backup folder Unity leaves beside it, and write `Evidence/build_and_commit.txt` (Unity version, backend, APK bytes, scene list, `git rev-parse --short HEAD`, `git status --porcelain` count) [depends: 4]
- [x] 6. Write `Week_14/Playtest/`: consent form, five-task script (set a profile name, choose School Campus at 2v2 Hard, catch an enemy, rescue a team-mate, win a match), observation sheet with a severity scale, results table with one row per tester, facilitator checklist [depends: 5]
- [x] 7. Write `Abrera_Zedrich_SA6.md` (usability/accessibility analysis, WCAG 2.2 mapping, audit numbers, fix list), `Abrera_Zedrich_PT6.md` (the playtest build, protocol, findings table, severity, what was fixed vs deferred) and `Abrera_Zedrich_Week14_Reflection.md` (ending in the authenticity declaration); extend `Docs/tools/MdToHtml.cs` to inline PNGs as data URIs and generate the three `.html` [depends: 6]

## Verify
- `check_compile_errors` returns "No compile errors"; `get_unity_logs` shows `[A11y]` and `[Feedback]` lines only for the new code, with no new exceptions.
- `a11y_baseline.log` and `a11y_after.log` exist, both end in a `TOTAL:` line, and the after log has 0 failures against a non-zero baseline - the delta is the evidence, not the assertion.
- In play mode a catch, a rescue, a flag pick-up and a match end each produce one `[Feedback]` line naming the clip, and the AI debug label is inactive during a match with `showAiDebug` false outside the editor.
- The playtest APK exists under `Week_14/`, is over 20 MB, and the `*_BackUpThisFolder_ButDontShipItWithYourGame` folder beside it has been removed.
- All three documents exist as `.md` and `.html`; opening the `.html` in a browser with the `Evidence/` folder moved away still shows every screenshot, and printing it to PDF keeps them - this is the Week 13 defect being closed.
- Every finding in PT6 has a severity and either a fix in this build or a scheduled week; the reflection ends with "I confirm that this reflection describes my own development experience and that all assistance, including AI assistance, has been disclosed."

## Open items the user must do
- Run the 3-5 tester sessions with `Abrera_Zedrich_PT6_Playtest.apk` and the kit in `Week_14/Playtest/`, then paste the filled results table back so PT6's findings section and any follow-up fixes can be completed.
- Commit the work and paste the commit id into `Week_14/Evidence/build_and_commit.txt`, `PT6.md` and `README.md`.
- Export the three `.html` files to PDF and drop them into the Drive folder `ABRERA_Zedrich_<StudentNumber>_AgawanBase`.
