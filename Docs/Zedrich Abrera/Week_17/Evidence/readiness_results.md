# Week 17 — release-candidate readiness results

Everything here was run on the release candidate described in `build_and_commit.txt`
(`Abrera_Zedrich_PT7.apk`, **33,170,059 bytes = 31.63 MB**, MD5 **4b1152fe203ba6f679404d955fdaa59d**)
on November 19, 2026, on the machine that built it. Nothing in this file is projected from an earlier
week.

One requirement — installing the RC on a handset from a clean state — could not be tested at all. It
is recorded as **BLOCKED** in `clean_install.md`, with the reason and the evidence that stands in its
place. No other requirement is blocked.

| # | Check | Expected | Result | Where the raw output is |
|---|---|---|---|---|
| 1 | EditMode test suite (`run_tests`, EditMode) | 17 of 17 | **17 passed, 0 failed, 0 skipped** in 0.62 s | Unity Test Runner job `86f790ea73364aad84ccc49f1ced1357` |
| 2 | Accessibility audit (`A11yAudit.Run()`) | 0 failed, no regression from the 423 baseline | **516 passed, 0 failed, 0 warnings, 24 not measurable** | `../../Week_14/Evidence/a11y_audit.log` |
| 3 | Week 15 quality scenarios (`QualityRun.Run()`) | same as the Week 16 gate: 16 passed, 0 failed | **16 passed, 0 failed, 0 blocked**, 0 exceptions | `../../Week_15/Evidence/test_logs_final/scenario.tsv` |
| 4 | Play-mode walk of the release screens (`Week17Shots.Run()`) | title → how-to → credits → match → end → restart, each photographed | **all six screens reached and captured** | `week17_shots.txt`, `E1`–`E6*.png` |
| 5 | APK is a real APK, not an `.aab` (DEF-15-04 guard) | `classes.dex` at the archive root | **`classes.dex`, `AndroidManifest.xml`, `resources.arsc` at the root** | `tar -tf`, transcribed in `build_and_commit.txt` |
| 6 | Install on the target device from a clean state | the module's checkpoint | **BLOCKED — no handset** | `clean_install.md` |

## 1. EditMode tests — 17 of 17

The suite covers the rules that are pure logic: team membership and its inverse, capture and rescue
state, flag ownership, the win conditions and the difficulty presets. It does not touch Unity
play mode, so it is a first gate, not the whole check.

## 2. Accessibility audit — 516 passed, 0 failed

The Week 14 baseline on the Week 13–15 tree was **423 passed / 0 failed / 24 not measurable**. The
release candidate scores **516 passed / 0 failed / 0 warnings / 24 not measurable**: every label and
button on the two new screens (HOW TO PLAY heading and body, CREDITS heading, build line, author line,
theme line, licence block, both BACK buttons, the three title buttons and the version caption) was
scored, and all of them clear the two thresholds the audit applies:

- **touch targets** at least 144 × 144 canvas px — the three title buttons are 440 × 144, both BACK
  buttons 480 × 144;
- **label height** at least 1.12 × font size, and **contrast** at least 4.5:1 against the panel behind
  it. The lowest ratio on the new screens is the theme line at **8.77:1**; the CREDITS heading is
  21.00:1 and the amber build line 11.68:1.

The 24 "not measurable" entries are unchanged from earlier weeks and are texts with no opaque panel
behind them, which the audit reports rather than guessing at.

## 3. Week 15 quality scenarios — 16 passed, 0 failed, 0 blocked

The suite runs in play mode against the real game and exercises 16 cases across the core rules, the
life-cycle (pause, resume, backgrounding, restart), boundary conditions (50 rapid catches, 50 rapid
pause toggles, 50 rapid pick-up attempts) and the build guard that keeps the AI debug read-out out of
player builds. This run finished at **2026-11-19 06:47:38 UTC** with **0 exceptions and errors** and
59 `[Feedback]` lines captured.

**It had to be re-run, and the first two runs failed 3 cases. That is worth recording rather than
deleting, because the cause was not the game.** The suite assumes a 4v4 roster: TC-CORE-05 needs two
Reds alive at once (one to imprison, one to do the rescuing), and the life-cycle cases need the match
to still be running after a capture. The machine's saved team-size preference was unset, so the
suite started at the 1v1 default and:

| Run | Roster | Result |
|---|---|---|
| 1 (06:40 UTC) | 1v1 | 13 passed, 3 failed, 1 blocked — TC-CORE-05 blocked *and* failed ("the roster did not provide both"), TC-LIFE-01 failed ("clock did not advance"), TC-LIFE-03 failed ("State was Victory") |
| 2 (06:44 UTC) | 1v1 | identical 13 / 3 / 1 |
| 3 (06:47 UTC) | **4v4** | **16 passed, 0 failed, 0 blocked** |

At 1v1 the suite's own capture ends the match by elimination, which is why the life-cycle cases then
report a frozen clock and `State was Victory`. With the documented 4v4 precondition restored the suite
matches the Week 16 gate exactly. `issue_disposition.md` records this as an environment precondition,
not a defect in the release candidate.

## 4. Play-mode walk of the release screens

`Tools > Week17 > Capture Release Screenshots` drives one uninterrupted play session from the title
screen to a restarted match and photographs each step. It is the substitute for the parts of the
module's timed demonstration that can be done without a handset.

| Shot | Screen | Bytes | MD5 (first 8) | What it proves |
|---|---|---|---|---|
| `E1_title_release.png` | title | 70,644 | `30a553b6` | version caption `1.0.0-rc1 - built 2026-11-19`, and HOW TO PLAY / CREDITS / EXIT all present, `RulesText` no longer hidden under PROFILE |
| `E2_how_to_play.png` | how to play | 168,135 | `d1f285a3` | the instructions screen opens from the title and is readable |
| `E3_credits.png` | credits | 135,495 | `bff35daf` | author, course, theme, engine, licences, AI-assistance disclosure, and the build line reading `1.0.0-rc1` |
| `E4_match_running.png` | match | 72,213 | `1a24411b` | the core loop running: HUD, joystick, SPRINT, pause, both flags at base |
| `E5_end_screen.png` | end screen | 62,356 | `cf69ce58` | a real ending, reached by elimination through `MatchManager.Capture` |
| `E6_restart.png` | restarted match | 72,213 | `1a24411b` | a live match again, 3.5 s into the reloaded scene |

The walk logged, in order: the title showing `1.0.0-rc1 - built 2026-11-19`; the how-to opened; the
credits opened; the match started; `Red_AI_1` captured; **the match ended as `Victory` after 1 scripted
capture**; restart pressed; restart reached a live match **3.5 s into the reloaded scene**; then the
EXIT control logged `[Release] quit requested.` and, as designed inside the editor,
`Application.Quit() does nothing in the editor - stop Play mode to leave.`

Two things about this evidence are stated plainly rather than glossed over:

- **The catch in E4→E5 is scripted, not played.** The harness calls the same public
  `MatchManager.Capture()` the tag rule calls, so the imprisonment, the elimination victory, the end
  screen and the profile write are all the real paths; what is scripted is that a script, not a thumb,
  made the tag happen.
- **E6 is byte-identical to E4, on purpose.** After RESTART the reloaded match starts from the same
  deterministic state — player in the blue base, `Red_AI_1` in the red base, both flags home, stamina
  full — so the first seconds of both matches render identically. The harness hashes every shot and
  re-requests the frame (forcing a Game View repaint) when a shot repeats an earlier one; that retry
  happened three times and produced the same bytes, which is what establishes the frame is live rather
  than stale. `week17_shots.txt` records both the retries and the 3.5 s figure. The two files are kept
  as they are instead of cropping something into E6 to make it look different.

### Why the harness needed fixing first

The first run of this harness produced six files of which **E1, E2 and E3 were byte-identical** — three
pictures of the title screen, none of them the how-to or the credits — and E4 and E6 likewise. The
panels were fine: a play-mode probe showed `panelHowTo` active and the screen rendering correctly. The
fault was that `ScreenCapture.CaptureScreenshot` photographs whatever the Game View last presented, and
with the editor driven from outside it presents rarely even while the player loop runs at full rate
(~28 fps measured). The harness now forces a repaint before every capture, hashes each file, and
re-requests a shot that repeats an earlier one. A second defect surfaced in the same run: the restart
wait counted editor update ticks, and 900 of them elapsed in well under a second, so a scene reload
that was still in progress was declared a failure. It now waits on real time. Both are `DEF-17-03` and
`DEF-17-04` in `issue_disposition.md`.

## 5. The package is installable-shaped

`tar -tf Builds/Week17/AgawanBase_RC.apk` lists `classes.dex`, `AndroidManifest.xml` and
`resources.arsc` at the archive root. This is the guard against DEF-15-04, where Build App Bundle
produced a bundle named `.apk` that no handset would install: a bundle keeps `classes.dex` inside
`base/dex/`. Build App Bundle is **off** and the listing is the proof for this exact file.

## What is not claimed

- The game has **not** been installed or launched on an Android handset. Every runtime result above
  comes from the Unity editor's play mode on this machine (Ryzen 5 5600G, GTX 1070, 15 GB RAM,
  Windows). That is the limitation the module's device checkpoint exists to catch, and it is BLOCKED,
  not passed.
- The APK is smaller than Week 16's 68.30 MB because Week 16 shipped a **development** build and this
  is a **release** build with debug symbols stripped. No optimisation is claimed from the size.
- Pausing, backgrounding and orientation were not re-tested on a device; the editor covers the pause
  and background paths that the quality suite drives, and nothing more.
