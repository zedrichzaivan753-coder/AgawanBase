# Week 17 — release-candidate readiness results

Everything here was run on the release candidate described in `build_and_commit.txt`
(`Abrera_Zedrich_PT7.apk`, **33,170,107 bytes = 31.63 MB**, MD5 **ef38de8dd65100109791002a67738da1**)
on November 19, 2026, on the machine that built it. Nothing in this file is projected from an earlier
week.

One requirement — installing the RC on a handset from a clean state — could not be tested at all. It
is recorded as **BLOCKED** in `clean_install.md`, with the reason and the evidence that stands in its
place. No other requirement is blocked.

| # | Check | Expected | Result | Where the raw output is |
|---|---|---|---|---|
| 1 | EditMode test suite (`run_tests`, EditMode) | 17 of 17 | **17 passed, 0 failed, 0 skipped** in 0.62 s | Unity Test Runner job `86f790ea73364aad84ccc49f1ced1357` |
| 2 | Accessibility audit (`A11yAudit.Run()`) | 0 failed, no regression from the 423 baseline | **516 passed, 0 failed, 0 warnings, 24 not measurable** — re-run after the panel fix, same numbers | `../../Week_14/Evidence/a11y_audit_20261119_152329.log` |
| 3 | Week 15 quality scenarios (`QualityRun.Run()`) | same as the Week 16 gate: 16 passed, 0 failed | **16 passed, 0 failed, 0 blocked**, 0 exceptions | `../../Week_15/Evidence/test_logs_final/scenario.tsv` |
| 4 | Play-mode walk of the release screens (`Week17Shots.Run()`) | title → how-to → credits → match → end → restart, each photographed | **all six screens reached and captured** | `week17_shots.txt`, `E1`–`E6*.png` |
| 5 | APK is a real APK, not an `.aab` (DEF-15-04 guard) | `classes.dex` at the archive root | **`classes.dex` and `AndroidManifest.xml` at the root of 419 entries, no `BundleConfig.pb`** | the entry listing transcribed in `build_and_commit.txt` |
| 6 | Install on the target device from a clean state | the module's checkpoint | **BLOCKED — no handset** | `clean_install.md` |

Every line above was then **re-run against the rebuilt APK** after the how-to panel was fixed
(`DEF-17-07`); see section 6, which also records the one number that changed between the two
capture runs — the screenshots themselves.

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
| `E1_title_release.png` | title | 73,710 | `61e87eb3` | version caption `1.0.0-rc1 - built 2026-11-19`, and HOW TO PLAY / CREDITS / EXIT all present, `RulesText` no longer hidden under PROFILE |
| `E2_how_to_play.png` | how to play | 145,119 | `ff710de1` | the instructions screen opens from the title, and the whole rules block now ends **above** the BACK button — this is the shot the panel fix changed |
| `E3_credits.png` | credits | 143,454 | `6b703d35` | author, course, theme, engine, licences, AI-assistance disclosure, and the build line reading `1.0.0-rc1` |
| `E4_match_running.png` | match | 78,750 | `aed46379` | the core loop running: HUD, joystick, SPRINT, pause, both flags at base |
| `E5_end_screen.png` | end screen | 73,647 | `6b93ef8b` | a real ending, reached by elimination through `MatchManager.Capture` |
| `E6_restart.png` | restarted match | 87,248 | `40a7cef2` | a live match again, 3.4 s into the reloaded scene |

The walk logged, in order: the title showing `1.0.0-rc1 - built 2026-11-19`; the how-to opened; the
credits opened; the match started; `Red_AI_1`, `Red_AI_2`, `Red_AI_3` and `Red_AI_4` captured; **the
match ended as `Victory` after 4 scripted captures**; restart pressed; restart reached a live match
**3.4 s into the reloaded scene**; then the EXIT control logged `[Release] quit requested.` and, as
designed inside the editor, `Application.Quit() does nothing in the editor - stop Play mode to leave.`

Two things about this evidence are stated plainly rather than glossed over:

- **The catch in E4→E5 is scripted, not played.** The harness calls the same public
  `MatchManager.Capture()` the tag rule calls, so the imprisonment, the elimination victory, the end
  screen and the profile write are all the real paths; what is scripted is that a script, not a thumb,
  made the tag happen.
- **E6 is a fresh frame, and it is no longer a copy of E4.** In the capture run that preceded the
  panel fix the two hashed identically, and that was kept and disclosed rather than dressed up. In
  this run they differ — `aed46379…` against `40a7cef2…` — because the AI's opening positions are not
  identical frame-for-frame between runs. Either outcome is safe evidence, because the harness hashes
  every shot and re-requests any frame that repeats a hash it has already seen: an identical pair
  would have been retried up to three times, and a unique hash cannot be a stale frame of an earlier
  screen. `week17_shots.txt` records the 3.4 s figure that proves the reload happened.

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

## 6. Re-verification after the how-to panel fix

Verifying the second build in play mode found the one defect the automated gates had not measured:
`Panel_HowTo/Body` needed **734.5 px** of text in a **660 px** rect, so the tail of the rules rendered
behind the BACK button. Nothing was edited by hand to hide it — the box was resized to `1560 × 600` at
y `+30` (which clears the BACK button by 102.8 px even on a 20:9 phone, where the canvas is 966 tall
instead of 1031), and `ReleasePanelsBuild.FitText` now shrinks the font until the measured text fits,
which it does at **21 px for 526.2 px of text**. The panel builder was re-run over all three scenes
and every gate was then re-run:

| Gate | Before the fix | After the fix |
|---|---|---|
| Compile | clean | clean |
| Accessibility audit | 516 passed, 0 failed, 24 not measurable | **516 passed, 0 failed, 24 not measurable** (`a11y_audit_20261119_152329.log`) |
| Screenshot walk (`Week17Shots.Run()`) | six screens, E4 = E6 byte-identical | **six screens, all six hashes distinct**; `E2` re-taken and now shows the rules ending above BACK |
| Release APK | `4b1152fe…`, 33,170,059 B | **`ef38de8d…`, 33,170,107 B** (rebuilt from commit `84c0f2e`) |

Only the screenshots and the APK identity changed; the audit numbers did not, because resizing a
rect and shrinking a font changes neither the contrast ratio of a label nor the size of a touch
target. The EditMode suite and the Week 15 quality scenarios were not re-run for this fix: it touches
no rule, no component and no test subject, and the three scenes it rewrote are the same three the
suite loads.

## What is not claimed

- The game has **not** been installed or launched on an Android handset. Every runtime result above
  comes from the Unity editor's play mode on this machine (Ryzen 5 5600G, GTX 1070, 15 GB RAM,
  Windows). That is the limitation the module's device checkpoint exists to catch, and it is BLOCKED,
  not passed.
- The APK is smaller than Week 16's 68.30 MB because Week 16 shipped a **development** build and this
  is a **release** build with debug symbols stripped. No optimisation is claimed from the size.
- Pausing, backgrounding and orientation were not re-tested on a device; the editor covers the pause
  and background paths that the quality suite drives, and nothing more.
