# PA2 — Evidence Portfolio

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Week:** 18 · **Assessment:** PA2
**Covers:** Weeks 13 – 18

---

## 1. How to read this portfolio

Two rules govern this document.

**Every artefact is named by path and says what it proves.** A screenshot with no claim attached is
decoration; an artefact listed against the claim it supports is evidence.

**The unproven list in §9 is part of the portfolio.** The module counts *"submitting unverifiable or
fabricated evidence"* as a common mistake. A portfolio that lists only successes invites the reader to
assume the gaps were hidden. They are written down instead.

All paths are relative to `Docs/Zedrich Abrera/`.

---

## 2. The submitted binary — the anchor of everything

| Field | Value |
|---|---|
| File | `Week_18/Abrera_Zedrich_PA2.apk` |
| Size | 33,170,107 bytes |
| MD5 | `ef38de8dd65100109791002a67738da1` |
| Provenance | Byte copy of `Week_17/Abrera_Zedrich_PT7.apk`, hashes verified identical |

Every test result, screenshot, audit and profiling number below was produced against a build with
that identity or its documented predecessors. `Week_18/Evidence/release_record.md` explains why the
binary was not rebuilt for PA2 and records the superseded hashes so an older one cannot be mistaken
for this one.

---

## 3. Week 13 — secure player data and responsible development

| Artefact | What it proves |
|---|---|
| `Week_13/Evidence/build_and_commit.txt` | The build under test and the commit it came from |
| `Week_13/Evidence/pt5_checks.log` | The profile checks passed as run |
| `Week_13/Evidence/E1_profile_empty.png` | The empty-profile state |
| `Week_13/Evidence/E2_profile_stats.png` | A profile with a record |
| `Week_13/Evidence/E3_leaderboard.png` | The stored record rendered |
| `Week_13/Evidence/E4_after_reset.png` | Behaviour after a reset |
| `Week_13/Evidence/SA5_data_flow.svg` | The data-flow diagram — what is collected, stored and transmitted |
| `Week_13/Evidence/secrets_and_gitignore.txt` | Secret-handling and ignore configuration at the time |
| `Week_13/Evidence/HOW_TO_RERUN.txt` | Reproduction steps, so the checks are not a one-off |

![Figure 1. Week 13 data-flow diagram. It is the reason the profile is a signed file rather than PlayerPrefs: a file can carry an integrity signature over its own contents, and this diagram is what made that boundary visible.](Evidence/fig_data_flow.png)

---

## 4. Week 14 — usability, accessibility and playtesting

| Artefact | What it proves |
|---|---|
| `Week_14/Evidence/a11y_baseline.log` | The accessibility audit before the Week 14 changes |
| `Week_14/Evidence/a11y_after.log` | The audit after them |
| `Week_14/Evidence/a11y_audit.log` | The audit run itself |
| `Week_14/Evidence/a11y_audit_2026*.log` (11 runs) | Every audit run kept, timestamped — including repeats, so a cherry-picked result is impossible |
| `Week_14/Evidence/E1_pause_sound.png` | The SOUND control on the pause screen |
| `Week_14/Evidence/E2_hud_contrast.png` | HUD contrast after the fix |
| `Week_14/Evidence/E3_catch_feedback.png` | The catch feedback path |
| `Week_14/Evidence/feedback_lines.txt` | The feedback log the catch path writes |
| `Week_14/Evidence/feedback_lines_run1.txt` | First capture of the same, kept rather than overwritten |
| `Week_14/Evidence/week14_shots.txt` | How the screenshots were produced |
| `Week_14/Evidence/build_and_commit.txt` | The build under test |

**Playtest result:** 5 testers, 25 task attempts, **0 failures**, 23 completed unaided, 2 facilitated,
and 5 of 5 testers noticed both new sounds. The observation sheets behind those numbers are in the
Week 14 document set.

**Honest caveat on the images:** the three evidence captures were produced by a script that starts a
match and calls the same public `MatchManager.Capture()` the tag rule calls. The catch in `E3` is
therefore **scripted, not played**. The sound path it exercised is the real one, and the Week 14
document said so at the time rather than implying a human played it.

---

## 5. Week 15 — requirements, acceptance and integration testing

| Artefact | What it proves |
|---|---|
| `Week_15/Evidence/requirements.md` | The requirement set implemented this week |
| `Week_15/Evidence/traceability_matrix.md` | Requirements mapped to tests — the map that makes "tested" checkable |
| `Week_15/Evidence/acceptance_criteria.md` | The acceptance conditions per requirement |
| `Week_15/Evidence/acceptance_summary.md` | The result against each criterion |
| `Week_15/Evidence/defect_log.md` | Defects found, with ids, including `DEF-15-04` |
| `Week_15/Evidence/environment.txt` | The machine and editor the runs happened on |
| `Week_15/Evidence/android_crash_buffer.txt` | The device crash buffer — checked, not assumed empty |
| `Week_15/Evidence/android_rapid_input_monkey.log` | A rapid-input monkey run |
| `Week_15/Evidence/a11y_audit_week15.log` | Accessibility state in Week 15 |
| `Week_15/Evidence/E1_editor_match_hud.png` | Match HUD |
| `Week_15/Evidence/E2_editor_game_over_defeat.png` | A defeat end state |
| `Week_15/Evidence/E3_editor_pause_menu.jpg` | The pause menu |
| `Week_15/Evidence/test_logs_baseline/` | Scenario runs at baseline (TSV plus summary) |
| `Week_15/Evidence/test_logs_baseline_d08fc9b/` | Baseline log set tied to commit `d08fc9b`, with `build.txt` and `editmode_baseline.txt` |
| `Week_15/Evidence/test_logs_final/` | The final Week 15 runs, same format |
| `Week_15/Evidence/test_logs_week16_regression/` | The same scenarios re-run in Week 16 — the regression link between weeks |

The four log folders are the portfolio's spine: the same scenario set, captured at baseline, at
final, and again a week later. That is what turns "the tests pass" into a claim with a history.

---

## 6. Week 16 — profiling and optimisation

| Artefact | What it proves |
|---|---|
| `Week_16/Evidence/bottleneck_table.md` | The ranked bottleneck table the optimisation was chosen from |
| `Week_16/Evidence/frame_budget.svg` | Frame budget against measured cost |
| `Week_16/Evidence/memory_fix.svg` | The texture-memory reduction |
| `Week_16/Evidence/raw_csv/` (28 captures) | The raw profiler CSVs, per run, timestamps in the filenames |
| `Week_16/Evidence/player_logs/` (21 logs) | The player-side logs for each capture |
| `Week_16/Evidence/test_logs_regression/` | The regression gate: `editmode_regression.txt`, `a11y_regression.log`, `scenario_regression.tsv`, `regression_gate_summary.md` |

The naming convention in `raw_csv/` is the point: `perf_baseline_run1..3`, `perf_fix1_run1..3`,
`perf_fix1b_run1..3`, `perf_fix1clean_run1..3`, `perf_shipcheck`. Three runs per condition, before and
after, and a final ship check — not a single favourable capture.

![Figure 2. Frame budget against measured cost. Main thread 0.59–0.63 ms and GPU 0.48 ms against a 33.3 ms budget at render scale 0.80 — under 2% of the budget on the main thread.](Evidence/fig_frame_budget.png)

![Figure 3. The texture-memory reduction: 21,849 KB down to 1,369 KB, a 20.0 MB recovery from prop textures the low-poly art never sampled at their stored resolution.](Evidence/fig_memory_fix.png)

**Regression result from this week:** 16 scenarios, **0 failures** at 4v4, with the accessibility
audit re-run alongside.

**What is not claimed:** Weeks 16 → 17 shrank the download from 68.30 MB to 31.63 MB because the
build changed from development to release. That is not an optimisation and is never counted as one.

---

## 7. Week 17 — feature freeze, release candidate, readiness

| Artefact | What it proves |
|---|---|
| `Week_17/Evidence/build_and_commit.txt` | The release candidate: size, MD5, backend, architecture, scenes, source commit |
| `Week_17/Evidence/feature_freeze_checklist.md` | The freeze was declared and what it forbade |
| `Week_17/Evidence/issue_disposition.md` | Every defect, including `DEF-17-01`, `DEF-17-02`, `DEF-17-07`, and how each was closed |
| `Week_17/Evidence/readiness_results.md` | **516 passed, 0 failed, 24 not measurable**, with the pre-fix and post-fix runs both recorded |
| `Week_17/Evidence/clean_install.md` | The clean-install attempt and its outcome |
| `Week_17/Evidence/secret_scan.md` | Secret scan result |
| `Week_17/Evidence/secret_scan_raw.txt` | The raw scan output, unedited |
| `Week_17/Evidence/architecture.svg` | The architecture diagram used throughout |
| `Week_17/Evidence/E1_title_release.png` | Title, release build |
| `Week_17/Evidence/E2_how_to_play.png` | Instructions screen after the `DEF-17-07` fix |
| `Week_17/Evidence/E3_credits.png` | Credits with licences and disclosure |
| `Week_17/Evidence/E4_match_running.png` | Match running |
| `Week_17/Evidence/E5_end_screen.png` | End screen |
| `Week_17/Evidence/E6_restart.png` | Restart |
| `Week_17/Evidence/week17_shots.txt` | How the captures were produced |

`readiness_results.md` is the strongest single artefact in the portfolio: it records the audit
**before and after** a defect fix, with identical totals, so the fix demonstrably did not regress
accessibility. The 24 items marked *not measurable* are left marked rather than silently passed.

---

## 8. Week 18 — demonstration package and defense

| Artefact | What it proves |
|---|---|
| `Week_18/Evidence/repo_state.txt` | HEAD `983d758`, both tag targets, `ahead 9`, `origin/main` at `d08fc9b`, tracked binaries |
| `Week_18/Evidence/release_record.md` | The submitted binary's identity and why it was not rebuilt |
| `Week_18/Evidence/final_pre_submission_checklist.md` | Every PA2/E3 requirement marked PASS / BLOCKED / PENDING with its proof |
| `Week_18/Evidence/demo_rehearsal.txt` | The demonstration sequence, with wall-clock offsets for all 18 steps |
| `Week_18/Evidence/D1_title.png` … `D9_restart.png` | Nine captures, nine distinct MD5s — see the table below |
| `Week_18/Evidence/windows_build.txt` | Raw three-stage build log for the backup package |
| `Week_18/Evidence/windows_backup_build.md` | What the backup is, what it is not, and its differences |
| `Week_18/Evidence/defense_prep.md` | Pointers to files, classes, commits and numbers — **no drafted answers** |
| `Week_18/Evidence/e3_record.md` | The E3 sitting record, fields blank until sat |

### The demonstration captures

| Capture | Screen | MD5 (first 8) |
|---|---|---|
| `D1_title.png` | Title with version caption | `61e87eb3` |
| `D2_how_to_play.png` | Instructions | `ff710de1` |
| `D3_credits.png` | Credits, licences, disclosure | `6b703d35` |
| `D4_map_select.png` | Map select | `57dfa273` |
| `D5_match_setup.png` | Team size and difficulty | `2e61e60a` |
| `D6_match_running.png` | Core loop running | `a77ac870` |
| `D7_pause_settings.png` | Pause, SOUND, TAG HINTS | `b8961c39` |
| `D8_end_screen.png` | End screen, Victory | `098e8af7` |
| `D9_restart.png` | Match live again after RESTART | `61effdb0` |

All nine hashes were re-read from disk with `certutil` after the run and matched the values the
harness logged, so the pictures on disk are the pictures that were taken.

![Figure 4. The core loop captured at 7.55 s into the rehearsal. Nine screens were captured in 14.2 seconds; every one has a distinct hash, because the harness re-takes any shot that repeats an earlier one.](Evidence/D6_match_running.png)

---

## 9. The unproven list

Claims this portfolio does **not** make, with the reason:

| # | Not proven | Why | Consequence |
|---|---|---|---|
| 1 | That the submitted APK installs and runs on an Android handset | No device attached; `adb devices` empty | Marked **BLOCKED**. The file is hashed and unmodified, but installation is not demonstrated |
| 2 | The E3 sitting | Sat under supervision, after this was written | `e3_record.md` is blank by design |
| 3 | That the two Week 14 discoverability findings are fixed | Never re-tested against the rebuilt instructions screen | Open. The rebuild was for `DEF-17-07`, which is a different defect |
| 4 | AI difficulty balance across a broad range of human players | Only 16 regression scenarios exist | 16/0 is a regression net, not a balance study |
| 5 | That the Windows backup plays comfortably with mouse and keyboard | It was launched and reached the title; no input was injected | Recorded as "runs", not "playable" |
| 6 | That the instructor can verify the final tag and commit remotely | **Resolved 2026-11-19**: published to `origin` | `v1.0.0-pa2` resolves to `5e63b1c`; `v1.0.0-rc1` and `week16-final` resolve too |
| 7 | That the Windows backup is equivalent to the APK | It is not: Mono, no haptics, two post-processing passes stripped | Disclosed in `windows_backup_build.md` §6 |

---

## 10. Reproducing the verification

Everything above can be re-checked without trusting this document:

```
git rev-parse HEAD                       # 983d758 at Week 18 start
git tag -l                               # v1.0.0-rc1, week16-final, v1.0.0-pa2
git rev-parse v1.0.0-rc1^{commit}        # 07057bd
certutil -hashfile "Week_18\Abrera_Zedrich_PA2.apk" MD5
                                         # ef38de8dd65100109791002a67738da1
certutil -hashfile "Week_18\Abrera_Zedrich_PA2_Windows.zip" MD5
                                         # fe17f2060c994218e4593b2010eb9a98
git ls-files "*.apk" "*.aab" "*.zip"      # no output: no APK, AAB or zip is versioned
```

The EditMode suite is `Assets/Tests/EditMode/` and runs in 0.45 s; the last result on this tree was
**17 passed, 0 failed, 0 skipped**.

---

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
