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
| Current commit ID | the Week 18 PA2 submission commit, tagged `v1.0.0-pa2` on `main`. The binary it ships is the Week 17 release candidate, tagged `v1.0.0-rc1`; the Week 17 build record is `Week_17/Evidence/build_and_commit.txt` and the Week 18 repository state is `Week_18/Evidence/repo_state.txt`. The frozen Week 16 tree is `b4cff53`, tagged `week16-final`. |
| Last updated | November 19, 2026 (Week 18, PA2 final demonstration package) |

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

**Android.** Use the PA2 submission build: `Week_18/Abrera_Zedrich_PA2.apk`. It is the Week 17 release
candidate copied byte for byte - 33,170,107 bytes, md5 `ef38de8dd65100109791002a67738da1`,
version `1.0.0-rc1` - so `Week_17/Abrera_Zedrich_PT7.apk` and `Week_18/Abrera_Zedrich_PA2.apk` are
the same file under two names. Copy either to the phone, allow installing from this source when
asked, and open the file, or install it with `adb install -r Abrera_Zedrich_PA2.apk`. It is a **release** build (Development Build off) carrying the
Week 13-16 work plus the Week 17 instructions screen, credits screen, EXIT control and version caption.
It is debug-signed and targets 64-bit ARM phones (ARM64 / IL2CPP, minimum Android API 26). Uninstall
any older copy first.

> **The release candidate has not been installed on a handset.** No Android device was attached when
> it was frozen, so `adb devices` printed an empty list and the module's clean-install checkpoint is
> recorded **BLOCKED** in `Week_17/Evidence/clean_install.md`. Every runtime result in `Week_17/`
> comes from the Unity editor's play mode on a desktop machine, not from hardware.

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
| `Week_17/` | PT7 report, technical documentation, user guide, evidence index, Week 17 reflection, and the release candidate APK - PDFs at the week root, sources and HTML in `others/` |
| `Week_17/Evidence/` | Feature-freeze checklist, release build record, readiness results, issue disposition, clean-install record (BLOCKED), secrets scan and its raw output, six release screenshots, the capture run log, and the architecture diagram |
| `Week_18/` | PA2 report, technical report, user guide, evidence portfolio, originality declaration, presentation deck, Week 18 reflection, the submitted APK (`Abrera_Zedrich_PA2.apk`) and the Windows backup package - PDFs and binaries at the week root, sources and HTML in `others/` |
| `Week_18/Evidence/` | Release record, final pre-submission checklist, defence pointer sheet (no scripted answers), E3 record (fields blank until sat), repository state, backup-package build log and write-up, the rehearsal log, and the nine demonstration captures D1-D9 |

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
- **The device evidence is still missing.** Week 16 built and profiled the Windows standalone player, so the
  Windows resolution cases are closed; but at the end of Week 17 no Android handset was attached
  (`adb devices` returned no serial), so the clean-install checkpoint and the crash buffer remain
  recorded **BLOCKED** rather than passed (`Week_17/Evidence/clean_install.md`).
- **Week 18 did not close that gap, and does not claim to.** PA2 ships the same APK and still records
  handset installation as **BLOCKED** (`Week_18/Evidence/final_pre_submission_checklist.md` section 5.1).
  The disclosed fallback is `Week_18/Abrera_Zedrich_PA2_Windows.zip`, a compiled Windows player that
  was launched and reached the title screen; it is not the submission, and it differs in scripting
  backend, haptics and two post-processing passes (`Week_18/Evidence/windows_backup_build.md`).
- **The repository is local only.** Nothing has been pushed, so `origin/main` is `d08fc9b` and the
  instructor cannot verify either tag remotely; `Week_18/Evidence/repo_state.txt` is the substitute record.
