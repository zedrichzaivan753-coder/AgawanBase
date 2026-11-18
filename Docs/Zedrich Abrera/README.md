# AgawanBase

| | |
|---|---|
| Student | Zedrich Zaivan B. Abrera |
| Course | MOBILE DEV 002 Advanced Mobile Application Development |
| Module | Module 5 - Individual Thematic Mobile Game Development Laboratory |
| Theme | Philippine Games and Sports |
| Platform | Android (primary), Windows (editor play and standalone) |
| Engine | Unity 6000.6.0f1, Universal Render Pipeline 17.6.0, C# |
| Repository | local Git repository at the project root, branch `main` |
| Current commit ID | `d08fc9b` - and note that weeks 13, 14 and 15 are all **uncommitted**: 144 dirty paths at the time the Week 15 build was made, so the commit id does not identify the tested code. `Week_15/Evidence/build_and_commit.txt` records the APK's md5 as the identifier of what was actually tested. |
| Last updated | November 18, 2026 (Week 15, AS3 milestone) |

## The game

AgawanBase is a 3D mobile adaptation of *Agawang Base*, the Filipino playground game of
stealing the other team's base while defending your own. Two teams of one to four characters
face each other across a walled arena. A character who has been out in the field longer than
the opponent they touch is caught and locked in that opponent's prison; a free team-mate can
walk up to the prison and release them. A team wins by carrying the enemy flag into its own
base, by locking the whole enemy team up, or by leading when the three-minute clock runs out.

Three maps can be chosen from the map select screen - a classic open field, a Palengke
(market) yard, and a school campus - and every map has its own bushes, hedges and stacked
crates that hide a character standing inside them. A hidden character's body and overhead
bars disappear completely; only the ground ring of the character the player controls stays
visible, so the player never loses track of themselves.

Difficulty (Easy / Normal / Hard) changes only how quickly and how boldly the AI thinks. Both
teams always use the same preset, so neither side gets a hidden advantage.

## Installation

**Android.** Use the newest build: `Week_15/Abrera_Zedrich_AS3_Milestone.apk`
(54,489,688 bytes, md5 `f4fa8b51d465f2e9120d9d23c7769245`). Copy it to the phone, allow
installing from this source when asked, and open the file, or install it with
`adb install -r Abrera_Zedrich_AS3_Milestone.apk`. It carries the Week 14 accessibility and
sound work plus the two Week 15 fixes: the match now pauses when the app goes to the background
(DEF-15-01) and an out-of-range difficulty is clamped instead of silently falling through
(DEF-15-03). The build is debug-signed and targets 64-bit ARM phones (ARM64 / IL2CPP, minimum
Android API 26). Uninstall any older copy first.

> **Do not install `Week_14/Abrera_Zedrich_PT6_Playtest.apk`.** That file is an Android App
> Bundle with an `.apk` filename (DEF-15-04 in `Week_15/Evidence/defect_log.md`): it was built
> with *Build App Bundle* switched on, so it has no `classes.dex` at its root and no handset can
> install it. The Week 15 build is the corrected one; *Build App Bundle* is now off in the
> project's Android settings.

**From source.** Open the project folder in Unity 6000.6.0f1 with Android build support
installed, open `Assets/Scenes/Game.unity`, and press Play. The three scenes in
`ProjectSettings/EditorBuildSettings` are `Game`, `Map_Palengke` and `Map_SchoolCampus`.

## Controls

| Action | Touch |
|---|---|
| Move | Virtual stick, bottom left of the screen |
| Sprint (drains the stamina bar) | Sprint button, bottom right |
| Catch, rescue, steal the flag | Automatic - walk into the character, the prison, or the flag |
| Pause, resume, restart, tag hints | Buttons on the HUD and pause screens |
| Profile (name, record, fastest victories) | PROFILE button on the title screen |
| Mute or restore the five feedback sounds | SOUND: ON / SOUND: OFF button, top right of the pause panel |

Touch and the keyboard/mouse both reach the game through Unity's Input System. The on-screen
stick and buttons are the mobile path; the same actions are available through the Input
System's UI pointer handling on desktop.

## Documentation and evidence

| Folder | Contents |
|---|---|
| `Week_13/` | SA5 analysis, PT5 profile component, Week 13 reflection, runnable APK (the `.pdf` exports are the shipped copies) |
| `Week_13/Evidence/` | PT5 check log, screenshots, build and commit record, secret scan |
| `Week_14/` | SA6 accessibility analysis, PT6 playtest report, Week 14 reflection, playtest APK, all as `.md` + `.html` |
| `Week_14/Playtest/` | Consent form, five-task session script, observation sheet, results table, facilitator checklist |
| `Week_14/Evidence/` | Accessibility audit before/after logs, E1-E3 screenshots, recorded feedback lines, build and commit record |
| `Week_15/` | AS3 milestone report, Week 15 reflection, the corrected APK, all as `.md` + `.html` + `.pdf` |
| `Week_15/Evidence/` | Environment record, 20 requirements, acceptance criteria, initial and final test logs, traceability matrix, defect log, acceptance summary, accessibility audit, three editor screenshots, and the two Android gap files |
| `Week_16/` | SA6 performance analysis, AS3 optimisation report, Week 16 reflection, the optimised APK, all as `.md` + `.html` + `.pdf` |
| `Week_16/Evidence/` | Bottleneck table, 22 per-frame CSVs (baseline, contaminated, A/B pair, ship check), 20 player logs, the two diagrams, and `test_logs_regression/` (EditMode 17/17, play-mode 16/0/0, accessibility 423/0/24) |

Every document from Week 14 on is written twice: `.md` is the source, `.html` is the same text
with a print stylesheet and every screenshot embedded in the file itself as a data URI. Open the
`.html` in a browser and use Print -> Save as PDF to produce the submission copy
(`Abrera_Zedrich_AS3_Milestone.pdf`, and so on); from Week 15 that step is automated by printing
the HTML with a headless browser, so the `.pdf` is always generated from the same HTML that
carries the images. The HTML is not maintained by hand: it is generated from the Markdown by
`Docs/tools/MdToHtml.cs`, which lives outside `Assets/` so Unity never compiles it. It splices
`<!--INLINE:file.svg-->` SVG diagrams and `![caption](path)` PNG screenshots into the page, so a
moved `Evidence/` folder cannot blank out a figure. Re-run it after editing any `.md` so the two
cannot drift apart. (Week 13's Markdown was exported to PDF and the intermediate `.html`
removed, so that week ships as PDF only.)

## Known limitations

- **Five feedback sounds, and no more.** Week 14 added a catch, rescue, flag pick-up, victory
  and defeat one-shot through `GameFeedback`, plus a vibration on a catch and a SOUND toggle
  that persists in `PlayerPrefs["audio.feedback"]`. There is still no music and no ambience.
  The victory stinger was never heard in the recorded AI match (that run ended in a defeat), so
  its wiring is code-verified but not observation-verified - stated as such in PT6.
- **The profile is local to one device.** There is no account, no cloud save and no online
  leaderboard; the game makes no network calls at all. The save is tamper-*evident*
  (it is signed) but not encrypted, so a determined local attacker can re-sign an edited
  file. This is documented as the PT5 known limit.
- **One profile per install.** Two players sharing a phone share a record.
- **Phone performance has not been measured on hardware.** Week 16 profiled a Windows standalone
  development build of the shipping render path (`Mobile_RPAsset`, render scale 0.80): main thread
  0.60 ms and GPU 0.48 ms against a 33.3 ms budget, from `Week_16/Evidence/raw_csv/`. The optimised
  APK is built and verified, but no device ran it under a profiler - a mid-range phone will be
  several times slower on the CPU and an order of magnitude slower on the GPU.
- **The APK is debug-signed** and ships ARM64 only.
- **The AI debug read-out is editor-only now.** `HudController.showAiDebug` is `true` only
  under `#if UNITY_EDITOR`, so the playtest build no longer reveals each AI's role and state.
  A tester who can read enemy intent is testing a different game.
- **Accessibility is measured, not yet player-tested.** `Assets/Editor/A11yAudit.cs` scores every
  uGUI label and button in all three scenes (WCAG contrast, rect height against font size, and
  a 144 canvas-px touch floor): 345 passed / 69 failed before the fixes, 423 passed / 0 failed
  after - re-run on the Week 15 tree and still 423 / 0 / 24 not measurable. The numbers are
  canvas-pixel arithmetic on the 1920x1080 reference resolution, not a dp measurement taken from
  a device, and the 3-5 tester sessions that would test them with real hands are still
  outstanding.
- **The device and Windows evidence is still missing.** At the end of Week 15 no Android handset
  was attached (`adb devices` returned no serial) and no Windows standalone player was built, so
  three device cases including the crash buffer, and both Windows resolution cases, are recorded
  **BLOCKED** rather than passed. They are the first work of Week 16.
