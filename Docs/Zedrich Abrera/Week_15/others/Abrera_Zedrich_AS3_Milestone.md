# Abrera_Zedrich_AS3_Milestone

**AS3 Milestone — Week 15**
**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Game:** Agawan Base v0.2 — a 3D mobile capture-the-flag game, two teams of two to four, three maps
**Build under test:** `Abrera_Zedrich_AS3_Milestone.apk` (AB-0.2-QA2), 54,489,688 bytes, md5 `f4fa8b51d465f2e9120d9d23c7769245`

> **Status of this document.** Everything below describes work that is finished and files that
> exist. Every number names the file it came from. Where a case could not be executed — no
> handset, no second configuration, a test never written — it is recorded as **BLOCKED** with
> the reason, not as a pass. The section *What this milestone does not prove* lists those gaps
> in one place, because a milestone document that only lists successes is not evidence.

## Contents

1. What this milestone asks for
2. The build under test
3. Test environment and the two configuration profiles
4. The requirements that were tested
5. How the testing was done
6. Results
7. Defects found, fixed, and one withdrawn
8. Baseline against final
9. Acceptance: the four checkpoint conditions
10. What this milestone does not prove
11. Evidence index
12. References

---

## 1. What this milestone asks for

The module's completion checkpoint is four binary conditions, not a demonstration:

| # | Condition |
|---|---|
| CC-1 | No unresolved defect causes **data loss** |
| CC-2 | No unresolved defect **prevents completion of the core gameplay loop** |
| CC-3 | No unresolved defect **exposes another player's information** |
| CC-4 | No unresolved defect **repeatedly crashes** the application |

and it asks for the evidence that lets a reader check those four claims: an identified build and
commit, two approved device or configuration profiles, an initial test and a final test, a defect
log, a crash buffer and a rapid-input run, and a reflection.

The four conditions were written down as testable criteria **before** anything was executed
(`Evidence/acceptance_criteria.md`), so that "it works" is not a judgement made after seeing the
result. Twenty requirements were derived from the game's own rules and each one was tied to a
test and to the script that implements it (`Evidence/requirements.md`).

## 2. The build under test

| | |
|---|---|
| File | `Docs/Zedrich Abrera/Week_15/Abrera_Zedrich_AS3_Milestone.apk` |
| Bytes / md5 | 54,489,688 / `f4fa8b51d465f2e9120d9d23c7769245` |
| Package | `com.zedrich.agawanbase`, versionCode 1, versionName 0.2 |
| Label | AgawanBase v0.2 |
| Engine | Unity 6000.6.0f1 (f7f8ed4d1e24), Universal RP 17.6.0 |
| Scripting | IL2CPP, ARM64 only |
| Android | minSdk 26 (Android 8.0), targetSdk 36 |
| Orientation | landscape, locked |
| Build type | Development build, debug-signed, installable with `adb install -r` |
| Scenes | `Game.unity`, `Map_Palengke.unity`, `Map_SchoolCampus.unity` |

Every one of those rows was read off the artefact itself — `aapt2 dump badging`, the archive
listing, and the editor build log — not copied from the previous week's document.

### The artefact that said `.apk` and was not one

The first build of this file reported `Build Finished, Result: Success` and wrote a
50,097,152-byte file named `.apk`. It was an Android **App Bundle**. The editor's *Build App
Bundle* flag was on, so Unity ran Gradle's `bundleDebug` task and the tool copied
`launcher-debug.aab` to the requested `.apk` path. The archive said so plainly: `BundleConfig.pb`
and `base/manifest/...` at the root, no `classes.dex`.

That is **DEF-15-04**, severity S1 for the deliverable, because a file that no handset can install
blocks every device test in this document. It was fixed by turning the flag off and rebuilding,
and the rebuild verified three ways: a zip listing that now shows `classes.dex`, `classes2-4.dex`
and `lib/arm64-v8a/*.so` across 428 entries; `aapt2 dump badging` showing the package, both SDK
levels and `native-code: 'arm64-v8a'`; and a size of 54,489,688 bytes, which is the bundle plus
the dex and native payload an APK stores and an AAB does not.

The same defect is present in the Week 14 artefact (`Abrera_Zedrich_PT6_Playtest.apk`,
50,097,691 bytes, md5 `0eef3dcb32fc0b9852bb4627c18d50d4`). It is logged rather than quietly
replaced, because the Week 14 evidence refers to that exact file. Anyone who installed it must
install AB-0.2-QA2 instead.

## 3. Test environment and the two configuration profiles

| | |
|---|---|
| Engine | Unity 6000.6.0f1, URP 17.6.0, Linear colour space |
| Input | com.unity.inputsystem 1.20.0 |
| Test framework | com.unity.test-framework 1.8.0 |
| Canvas | 1920x1080 reference, Scale With Screen Size, match 0.5 |
| Commit | `d08fc9b2c9e28876f337cfcad009b88802d2b534`, 144 dirty paths |

**P1 — a physical Android handset.** The Week 14 sessions ran on a Samsung Galaxy S9 and an Oppo
A5S, both Android 13. For this lab no handset was attached: `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe devices`
printed `List of devices attached` with no serial. The device-dependent cases are therefore
**BLOCKED**, with the command output quoted in `Evidence/android_crash_buffer.txt`.

**P2 — a Windows 64-bit player of the same tree at 1920x1080 and again at 1280x720.** No Windows
player was built in this lab, so TC-COMPAT-01 and TC-COMPAT-02 are **BLOCKED** too. This is
recorded rather than substituted with a resized editor window, because a resized Game view is not
a standalone build and would prove the wrong thing.

**Interrupted network access — N/A, with a reason.** A search for `UnityWebRequest`, `System.Net`,
`HttpClient`, `Socket` and `WebClient` under `Assets/Scripts` returned no matches, and the game
holds exactly one local file (the player profile). There is no network path to interrupt.
Recording this as N/A is honest; recording it as a pass would not be.

### The commit caveat, stated in the document rather than the footnote

The code that produced this APK is **not in HEAD**. Weeks 13, 14 and 15 are all uncommitted —
144 dirty paths over `d08fc9b`. The module requires that the tested build and commit are
identified, and the honest form of that identification here is:

    tested source = working tree over d08fc9b, 144 paths uncommitted
    tested binary = md5 f4fa8b51d465f2e9120d9d23c7769245

Quoting `d08fc9b` alone would name a commit that does not contain the code under test.

## 4. The requirements that were tested

Twenty requirements, each in a form a test can fail, each naming the script that implements it.
The full table is `Evidence/requirements.md`; this is the shape of it.

| Area | Requirements | Implemented in |
|---|---|---|
| Flow and movement | REQ-01 title → match, REQ-02 joystick movement, REQ-03 sprint and stamina | `GameManager`, `VirtualJoystick`, `CharacterMotor`, `SprintStamina` |
| The tag loop | REQ-04 catch, REQ-05 prison, REQ-06 rescue, REQ-07 flag pick-up, REQ-08 drop and 10 s return | `MatchManager`, `CharacterStatus`, `Flag` |
| End states | REQ-09 win, REQ-10 loss, REQ-11 draw, REQ-12 HUD agrees with the rules | `MatchManager`, `HudController` |
| Player data | REQ-13 name, REQ-14 stats, REQ-15 leaderboard, REQ-16 reset | `PlayerProfile`, `ProfileUI` |
| Feel and lifecycle | REQ-17 audio and haptics, REQ-18 pause and resume, REQ-19 backgrounding | `GameFeedback`, `GameManager` |
| Accessibility | REQ-20 contrast ≥ 4.5:1 and buttons ≥ 144x144 canvas px | `A11yAudit`, `AccessibilityBuild` |

## 5. How the testing was done

Three layers, each answering a question the other two cannot.

**EditMode, 17 cases.** Fast, deterministic, and it can reach seams a player never touches:
`PlayerProfile.SetDataDirectory` and `WriteGarbageForTests` let a test hand the profile a corrupt
file, an empty file and a directory it cannot create. `MatchSettingsTests` pins the clamping of
team size and difficulty. `SprintConfigTests` pins the carry penalty. These are the tests that
catch a regression in five seconds instead of five minutes.

**A play-mode scenario suite, `Assets/Editor/QualityRun.cs`, 16 cases over 29 phases.** It enters
play mode, drives the *real* match — the same `MatchManager`, `Flag`, `CharacterStatus` and
`SprintStamina` the player runs — and writes one TSV row per case. Three properties mattered more
than the cases themselves:

- **A stall is never a pass.** Every phase has a 30 s guard that writes `TC-AUTO-<n>` **BLOCKED**
  if it goes silent, and there is a 300 s budget for the whole run. Silver scattered through a log
  is not a green test.
- **Isolation per case.** `FreezeAi()` disables every AI except the human's before a case runs, so
  a live AI cannot eliminate a team mid-case; `PlayerToField()` teleports the human to midfield,
  because `MatchManager.Update` runs `CheckWinCondition` *before* `CheckFlagTouches` and a flag
  carried out of the enemy base is the only sequence that proves a win.
- **Read the API, do not guess it.** `MatchClock` (read-only), `MatchManager.MatchOutcome` (the
  *reason* the match ended, not the winner) and the nested `Flag.FlagState` were found by
  reflecting over the live types, after three of my first four guesses were wrong.

**Manual and audit-level checks.** `Tools/Playtest/Run Accessibility Audit` measures every label
and every button in the built scene and writes a line per assertion; the rules it enforces are
REQ-20. In this lab it was re-run against the Week 15 tree.

**What was deliberately not faked.** The device cases were not simulated by calling
`OnApplicationPause` from a script and declaring the handset path proved. The editor half is
tested (TC-LIFE-03) and the device half is *BLOCKED* (TC-BND-06), and the difference is written
down.

## 6. Results

| Suite | Result | File |
|---|---|---|
| EditMode | **17 run / 17 passed / 0 failed**, job `151a2cba798e4a43ad10291e206331d6` | `Evidence/test_logs_final/editmode_final.txt` |
| Play-mode scenarios | **16 passed / 0 failed / 0 blocked**, 0 exceptions and 0 errors during the run, 75 `[Feedback]` lines captured | `Evidence/test_logs_final/scenario.tsv`, `scenario_summary.txt` |
| Accessibility audit | **423 passed / 0 failed / 0 warnings / 24 not measurable**, run 2026-11-18 21:59:37 | `Evidence/a11y_audit_week15.log` |
| Compile | no compile errors on the final tree | editor console |
| Device (P1) | **BLOCKED** — no handset attached | `Evidence/android_crash_buffer.txt`, `android_rapid_input_monkey.log` |
| Windows (P2) | **BLOCKED** — no player built | `Evidence/traceability_matrix.md`, Gap 7 |

![E1 — the match running in play mode: the joystick bottom-left, SPRINT and the camera arrows bottom-right, the state read-out top-left (SAFE AT BASE, STAMINA 100%, BLUE 3/4 free | flag: CARRIED, RED 4/4 free | flag: AT BASE), and the "YOUR FLAG HAS BEEN TAKEN!" indicator. This is the editor Game view at 1139x584, captured from the same source that produced the APK.](Evidence/E1_editor_match_hud.png)

The five end-to-end statements the module cares most about, each as it appears in the TSV:

| Case | What it proves | Actual |
|---|---|---|
| TC-CORE-08 | carrying the enemy flag home wins | `outcome FlagCarriedHome` with the victory clip |
| TC-CORE-09 | the enemy carrying our flag home loses | `outcome FlagCarriedHome` with the defeat clip |
| TC-CORE-10 | the clock running out is a draw | ended by the clock, `outcome TimeLimit`, no winning line |
| TC-CORE-04 | the prison holds a prisoner | moved 0.000 m in 0.4 s, `motor.IsFrozen=True` |
| TC-LIFE-01 | pause stops the clock and resume continues it | paused delta 0.00 s, then ran again |

![E2 — the defeat end state, reached unaided by the AI: "GAME OVER — The enemy carried your flag home." with RESTART and TITLE. This is the screen REQ-10 and TC-CORE-09 are about.](Evidence/E2_editor_game_over_defeat.png)

## 7. Defects found, fixed, and one withdrawn

Full records with steps, severity and evidence are in `Evidence/defect_log.md`.

| ID | Defect | Found by | Severity | Status |
|---|---|---|---|---|
| DEF-15-01 | Sending the app to the background did not pause the match; there was no `OnApplicationPause` or `OnApplicationFocus` in the project | TC-LIFE-03 on the baseline | S2 | **FIXED**, re-tested |
| DEF-15-02 | *(Proposed)* the flag carry penalty was missing | reading PT6's promise against `SprintStamina.cs` | — | **WITHDRAWN** — the penalty is regen-only by design |
| DEF-15-03 | `SetDifficulty` stored any integer, including 99 | the EditMode baseline, `Expected: <= 2 / But was: 99` | S3 | **FIXED**, re-tested |
| DEF-15-04 | The `.apk` artefacts were Android App Bundles | listing the archive produced in step 6 | S1 for the deliverable | **FIXED** for QA2, logged for QA1 |

**DEF-15-01** is the one this milestone exists for. On the baseline, a player who took a call came
back to a match that had played on without them. The fix is two handlers at the end of
`GameManager` that route into the *existing* pause path, so backgrounding and pressing PAUSE are
one state and not two mechanisms; they guard on `GameState.Playing`, and there is deliberately no
auto-resume.

**DEF-15-02** is the record of a mistake worth keeping. Reading PT6's promise next to
`carrySpeedMultiplier = 1f`, I filed a missing penalty. `SprintStamina.cs:45-54` documents the
opposite: the penalty is to stamina **regeneration**, because a carrier slower than the chasers
never reaches home and the only win condition in the game becomes a loss condition. The fix was
not to the game — it was to the test suite, which now pins both multipliers so the design cannot
drift silently.

![E3 — the pause screen, captured because the editor lost focus while the match was running: the focus handler added for DEF-15-01 paused the match, exactly as a HOME press on a handset would. RESUME, RESTART, TITLE and the SOUND toggle remember their positions after every button grew to 144 px.](Evidence/E3_editor_pause_menu.jpg)

## 8. Baseline against final

| Suite | Baseline (AB-0.2-QA1) | Final (AB-0.2-QA2) |
|---|---|---|
| EditMode | 16 passed / **1 failed** (job `51c56bba…`) | **17 passed / 0 failed** |
| Play-mode scenarios | 8 passed / 8 failed / 1 blocked | **16 passed / 0 failed / 0 blocked** |
| Accessibility audit | 423 / 0 / 24 | 423 / 0 / 24 — unchanged, which is the point |
| Compile errors | 0 | 0 |

The one EditMode change is the DEF-15-03 fix and nothing else: the clamp case moved from
`Expected: less than or equal to 2 / But was: 99` to `[Settings] difficulty 99 is not a preset -
clamped to HARD.`, and the other sixteen cases did not move.

**The baseline scenario TSV has to be read carefully.** Eight of its eight failures were faults in
my own harness, not in the game: a phase that never resolved `GameManager`, a `Wait` that was
thrown away, an assumed clock direction, a cumulative feedback search, a capture case that
eliminated a whole team, a rescue case that paired a Blue rescuer with a Blue prisoner, and one
case that passed only because the flag was re-picked up in the base. Nine such harness faults are
listed in `Evidence/defect_log.md`, Appendix A, next to the *one* defect a test actually found in
the game before the fixes. Reading that baseline as "eight product failures" would be wrong, and
this document does not.

## 9. Acceptance: the four checkpoint conditions

| # | Verdict | Basis |
|---|---|---|
| CC-1 | **HOLDS** | corrupt, empty and missing `profile.json` each load a default and keep the broken file; a recorded match survives a reload; an unwritable directory fails without losing the old profile — 17/17 EditMode |
| CC-2 | **HOLDS with one gap** | the loop ran unaided to all three end states, with catch, prison, rescue, flag and return each evidenced; the gap is that no automated case compares the on-screen count to the rules' count (Gap 2) |
| CC-3 | **HOLDS** | no networking code exists at all; the profile is one local file; the AI read-out is editor-only and off in the running game |
| CC-4 | **HOLDS where it could be executed** | 0 exceptions across 16 play-mode cases including 100 rapid captures, 50 pause toggles and 50 rapid flag attempts; the on-device crash buffer is **BLOCKED** |

**Ready for Week 16 optimisation: yes, with the blocked cases carried forward as work.** No S1 or
S2 defect is open in AB-0.2-QA2 — DEF-15-04 is a property of an earlier file, already delivered.
The two fixes are small and pinned by tests, and the 423-assertion accessibility floor that
optimisation must not regress still reads 423 / 0 / 24 after this week's changes.

The first work of Week 16 is not optimisation, though: it is installing AB-0.2-QA2 on a handset
and building the Windows player at both sizes. Optimising a build nobody has installed is how a
performance week produces a worse game.

## 10. What this milestone does not prove

Eight gaps, each with a week against it in `Evidence/traceability_matrix.md`:

| # | Gap |
|---|---|
| 1 | No runtime sprint case — TC-STA-01 and TC-STA-03 are unwritten, so drain, lock-out and regeneration at 0 stamina are only pinned as *numbers*, not behaviour |
| 2 | No HUD-vs-rules case — TC-CORE-11 is unwritten; the HUD was read by eye in Week 14 |
| 3 | No dedicated audio phase, and no haptics: all five clips appear in the run's 75 `[Feedback]` lines, but the SOUND toggle is never asserted and the catch buzz cannot fire in an editor |
| 4 | No TC-BND-04 — an out-of-range setting is clamped at the setter but never driven through a live match |
| 5 | No TC-CHR-01 — the capture-immunity window is unexercised |
| 6 | TC-CORE-03 has failure paths and no success path (harness fault H-8); the catch is proved by TC-CORE-04 and TC-BND-01 instead |
| 7 | P2 — no Windows player at 1920x1080 or 1280x720 |
| 8 | P1 — no handset: TC-COMPAT-03, TC-BND-06 and TC-BUILD-02 have no data |

Two structural caveats belong here as well. `MatchClock` is reached by **reflection** from the
harness, because its setter is private and the test needed to age the clock; that is a test-only
make-believe and it is stated wherever the clock is mentioned. And the tested source is not
committed, so the binary hash is the only identifier of what was actually run.

## 11. Evidence index

| File | What it holds |
|---|---|
| `Evidence/build_and_commit.txt` | Build facts: file, bytes, md5, manifest, engine, scenes, commit, the DEF-15-04 finding |
| `Evidence/environment.txt` | Build ids, platform range, profiles P1/P2, the N/A network case, the commit caveat |
| `Evidence/requirements.md` | REQ-01..REQ-20 with the script that implements each |
| `Evidence/acceptance_criteria.md` | CC-1..CC-4, the per-requirement pass criteria, severity and priority definitions |
| `Evidence/test_logs_baseline_d08fc9b/editmode_baseline.txt` | The initial EditMode run: 16/17, the failure that became DEF-15-03 |
| `Evidence/test_logs_baseline/scenario.tsv`, `scenario_summary.txt` | The baseline scenario run, 8/8/1 — a record of harness faults, explained in §8 |
| `Evidence/test_logs_final/editmode_final.txt` | 17/17 and the baseline-to-final diff |
| `Evidence/test_logs_final/scenario.tsv`, `scenario_summary.txt` | 16/0/0 and every `[Feedback]` line |
| `Evidence/a11y_audit_week15.log` | The 423-assertion accessibility audit run on this tree |
| `Evidence/traceability_matrix.md` | REQ → test → type → executor → status, plus the eight gaps |
| `Evidence/defect_log.md` | DEF-15-01..04, the withdrawn DEF-15-02, and the nine harness faults |
| `Evidence/acceptance_summary.md` | The CC verdicts, the regression diff and what Week 16 must do first |
| `Evidence/android_crash_buffer.txt` | The device probe, its real output, and what substitutes for it |
| `Evidence/android_rapid_input_monkey.log` | The rapid-input gap on a device, and the editor substitute |
| `Evidence/E1_editor_match_hud.png` | The match running, HUD and controls |
| `Evidence/E2_editor_game_over_defeat.png` | The defeat end state |
| `Evidence/E3_editor_pause_menu.jpg` | The pause screen, produced by the focus handler DEF-15-01 added |
| `Abrera_Zedrich_AS3_Milestone.apk` | The build under test, AB-0.2-QA2 |

## 12. References

Module 5, *Individual Thematic Mobile Game Development Laboratory*, MOBILE DEV 002 Advanced Mobile
Application Development, prepared by Jayson S. Nacorda, MIT, University of La Salette, Inc., 2026
— the completion checkpoint, the required evidence set and the submission rules.

Unity Technologies. (2024). *Unity Test Framework documentation.*
https://docs.unity3d.com/Packages/com.unity.test-framework@1.8/manual/index.html

Unity Technologies. (2025). *Unity User Manual 6000.6.*
https://docs.unity3d.com/6000.6/Documentation/Manual/index.html

OWASP Foundation. (2024). *OWASP Mobile Application Security.* https://mas.owasp.org/

World Wide Web Consortium. (2024). *Web Content Accessibility Guidelines 2.2.*
https://www.w3.org/TR/WCAG22/

---

AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 15 · source: `Abrera_Zedrich_AS3_Milestone.md`
