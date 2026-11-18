# Acceptance summary - AS3 Milestone (Week 15)

This is the document the module's completion checkpoint asks for. It answers four binary
conditions, states whether the build is ready for Week 16 optimisation, and lists what is
still open with a week against each item. Every number is traceable to a file in `Evidence/`.

Tested artefact: **AB-0.2-QA2**, `Docs/Zedrich Abrera/Week_15/Abrera_Zedrich_AS3_Milestone.apk`,
54,489,688 bytes, md5 `f4fa8b51d465f2e9120d9d23c7769245`.

## The four checkpoint conditions

| # | Condition | Verdict | Evidence |
|---|---|---|---|
| CC-1 | No unresolved defect causes **data loss** | **HOLDS** | Corrupt, empty and missing `profile.json` each load a default profile and keep the broken file rather than discarding the player's data (TC-PROF-01/02/03); a recorded match survives a reload (TC-PROF-05); a folder that cannot be created fails without claiming a new name and without losing the old profile (TC-BND-05). 17/17 EditMode. |
| CC-2 | No unresolved defect **prevents completion of the core gameplay loop** | **HOLDS with one gap** | The loop ran unaided to an end state in all three ways: win (TC-CORE-08), loss (TC-CORE-09), draw (TC-CORE-10), with catch (CORE-04, BND-01), prison (CORE-04), rescue (CORE-05), flag pick-up (CORE-06), drop and return (CORE-07). 16/16 scenario cases, 0 exceptions. The gap is the HUD-agreement check (Gap 2): the loop completes, but nothing automated compares the on-screen count to the rules' count. |
| CC-3 | No unresolved defect **exposes another player's information** | **HOLDS** | There is no networking code at all (`grep` for `UnityWebRequest`, `System.Net`, `HttpClient`, `Socket`, `WebClient` under `Assets/Scripts`: no matches), the profile is a single local file, and the AI read-out is editor-only and off in the running game (TC-BUILD-01, `showAiDebug=False`). |
| CC-4 | No unresolved defect **repeatedly crashes** the application | **HOLDS within the tested surface, BLOCKED on hardware** | 0 exceptions across 16 play-mode cases including 100 rapid captures, 50 pause toggles and 50 rapid flag attempts (TC-BND-01/02/03); 0 errors in the accessibility audit. The on-device crash-buffer check is **BLOCKED**: no handset was attached, so `android_crash_buffer.txt` and `android_rapid_input_monkey.log` record the gap and the substitute evidence rather than a pass. |

## Baseline to final - what actually changed

Verified, not remembered. Baseline = the working tree that produced AB-0.2-QA1. Final = the
tree that produced AB-0.2-QA2.

| Suite | Baseline | Final | Move |
|---|---|---|---|
| EditMode (17 cases) | 16 passed / **1 failed** | **17 passed / 0 failed** | +1, and the one failure was a real product defect (DEF-15-03) |
| Play-mode scenarios | 8 passed / 8 failed / 1 blocked | **16 passed / 0 failed / 0 blocked** | +8 passed, +8 cases now written that did not exist, 1 blocked cleared |
| Accessibility audit | 423 / 0 / 24 | 423 / 0 / 24 | unchanged - no Week 15 change touched layout, and this proves it |
| Compile errors | 0 | 0 | unchanged |

The one EditMode change is exactly the fix and nothing else:

    baseline : MatchSettingsTests.SetDifficulty_ClampsAnOutOfRangeValue   FAIL
               Expected: less than or equal to 2 / But was: 99
    final    : the same case                                          PASS
               [Settings] difficulty 99 is not a preset - clamped to HARD.

### Per-case diff on the play-mode suite

| Case | Baseline | Final | Why it moved |
|---|---|---|---|
| TC-CORE-05 | FAIL - "still captured 1.2 s after the player stood at the prison" | PASS | harness defect H-6: the case paired a Blue rescuer with a Blue prisoner, and a prisoner is freed only by a team-mate |
| TC-LIFE-01 | FAIL - "clock did not advance in 0.7 s after Resume" | PASS | harness defect H-2 (a `Wait` that was thrown away) and the editor's own `OnApplicationFocus(false)`; the suite now resumes with `EnsurePlaying()` and measures against a settled state |
| TC-LIFE-02 | FAIL - "clock 0.3/180" | PASS | harness: the case measured a restart that had not finished |
| TC-LIFE-03 | FAIL - "GameManager has no OnApplicationPause handler" / "State was GameOver" | PASS | **product** fix DEF-15-01, plus harness isolation so the case is not run on a match that has already ended |
| TC-CORE-08 | FAIL - "outcome None, victory feedback seen = True" | PASS | harness H-4/PlayerToField: `MatchManager.Update` runs `CheckWinCondition` before `CheckFlagTouches`, so the flag must be carried in from midfield, not placed in front of the base |
| TC-CORE-09 | FAIL - "outcome None, defeat feedback seen = True" | PASS | same as CORE-08 |
| TC-CORE-10 | FAIL - "Outcome was Elimination after the clock expired" | PASS | harness H-3 (`MatchClock` counts **up**) and H-5 (capturing all four Reds eliminated the enemy team before the clock could expire) |
| TC-AUTO-20 | BLOCKED - stalled 30 s | gone | the stall guard fired because of the cases above; with them fixed no phase stalls |
| TC-BND-02 | not written | PASS | added: 50 rapid pause toggles |
| TC-BUILD-01 | PASS, but on a self-contradictory criterion | PASS on a corrected criterion | harness H-9: the baseline row expected `showAiDebug` true in the editor and recorded PASS against an actual of `False` |
| the other 6 | PASS | PASS | no regression |

**The honest reading of the baseline TSV.** `test_logs_baseline/scenario.tsv` (8/8/1) is a
record of a **broken harness**, not a broken game. Seven of its eight failures were faults in
the test tool (`defect_log.md`, Appendix A). Reading it as "eight product failures fixed" would
be wrong, and this document does not do that. The only baseline number that is a product
statement is the EditMode one, and it is the one that produced DEF-15-03.

## Open items

| # | Item | Severity | State | Week |
|---|---|---|---|---|
| 1 | DEF-15-04: the Week 14 artefact on disk is an `.aab` named `.apk` (AB-0.2-QA1). AB-0.2-QA2 is a verified APK | S1 for that file only | **open - artefact only**; the submitted Week 14 evidence refers to that file, so it is logged, not silently replaced | 15 (logged), 16 (anyone who installed it must reinstall QA2) |
| 2 | TC-COMPAT-01/02: a Windows 64-bit player at 1920x1080 and 1280x720 was never built or run | none (not run) | **BLOCKED** | 16 |
| 3 | TC-COMPAT-03, TC-BND-06, TC-BUILD-02: no handset attached, `adb devices` empty | none (not run) | **BLOCKED**, gap documented with the commands that close it | 16 |
| 4 | Gap 1: no runtime sprint case (TC-STA-01/03) | none (not run) | **BLOCKED** - only the sprint *numbers* are pinned | 16 |
| 5 | Gap 2: no HUD-vs-rules case (TC-CORE-11) | S3 exposure | **BLOCKED** | 16 |
| 6 | Gap 4 (TC-BND-04), Gap 5 (TC-CHR-01), Gap 6 (H-8) | none (not run) | **BLOCKED** | 16 |
| 7 | The tested source is not in a commit: 144 dirty paths over `d08fc9b` | traceability | **open**, and the reason `build_and_commit.txt` quotes a file hash as well as a commit | 15 (commit), carried to 16 |

No **S1 or S2 defect is open in AB-0.2-QA2.** The one S1 in this log (DEF-15-04) is a property
of an earlier artefact that has already been delivered, not of the build under test.

## Ready for Week 16 optimisation?

**Yes, with the blocked cases carried forward as work, not as passes.**

- CC-1, CC-3 hold outright; CC-2 holds with the HUD gap named; CC-4 holds for everything that
  could be executed and is unproven on hardware.
- Nothing in the build blocks optimisation: the two product fixes are small, local and pinned
  by tests (`MatchSettingsTests`, the backgrounding handlers exercised by TC-LIFE-03), and the
  accessibility floors that Week 16 must not regress are held by a 423-assertion audit that
  still reads 423 / 0 / 24 after this week's changes.
- What Week 16 must not do: optimise against a build nobody has installed. Items 2 and 3 above
  are the first work of the week, because performance work on a handset is exactly what the
  device tests are for.

## Where each number came from

| Number | File |
|---|---|
| 16 passed / 0 failed / 0 blocked, 0 errors, 75 `[Feedback]` lines | `test_logs_final/scenario.tsv`, `test_logs_final/scenario_summary.txt` |
| 17 passed / 0 failed, job `151a2cba798e4a43ad10291e206331d6` | `test_logs_final/editmode_final.txt` |
| 423 passed / 0 failed / 24 not measurable, run 2026-11-18 21:59:37 | `a11y_audit_week15.log` |
| 54,489,688 bytes, md5 `f4fa8b51d465f2e9120d9d23c7769245`, `com.zedrich.agawanbase` v0.2 (1) | `build_and_commit.txt` |
| no device attached | `android_crash_buffer.txt`, `android_rapid_input_monkey.log` |
| 4 defects, 1 withdrawn, 9 harness faults | `defect_log.md` |
