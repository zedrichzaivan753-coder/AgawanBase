# Abrera_Zedrich_PT7_Evidence_Index

**Defense evidence index - Agawan Base 1.0.0-rc1 - Week 17**
**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 - Advanced Mobile Application Development, Module 5

Every claim made in the Week 17 documents has a file behind it. This index is that mapping: claim to file to what the file actually shows, for the release candidate and for the four weeks of work it integrates. It is written so that a reader who does not trust a sentence can go and check it in one step.

All paths are relative to `Docs/Zedrich Abrera/` unless they start with `Assets/` or `ProjectSettings/`.

## Contents

1. How to use this index
2. Release identity
3. Week 17 - release candidate evidence
4. Week 16 - profiling and optimisation evidence
5. Week 15 - testing and defect evidence
6. Week 14 - accessibility and playtest evidence
7. Week 13 - profile and security evidence
8. The documents themselves
9. Reproducing the gate
10. What has no evidence, and why

---

## 1. How to use this index

Three rules make this index useful rather than decorative:

- **One claim, one file.** If a claim cannot name a file, it does not belong in the documents.
- **Raw before summary.** Every summary in `Week_17/Evidence/` points at a raw artefact - a log, a CSV, an archive listing, a PNG with a hash. The summary is there to save time, not to be the evidence.
- **Blocked is not passed.** Anything a handset would have been needed for is written as **BLOCKED** and indexed under §10, not folded into a success list.

## 2. Release identity

| Claim | File | What it shows |
|---|---|---|
| The release candidate is this exact APK | `Week_17/Abrera_Zedrich_PT7.apk` | 33,170,107 bytes; md5 `ef38de8dd65100109791002a67738da1` |
| The identity of the artefact and its build settings | `Week_17/Evidence/build_and_commit.txt` | size, md5, source commit `84c0f2e`, build date and duration, engine, Platform/IL2CPP/ARM64/API 26, `bundleVersion 1.0.0-rc1`, versionCode 2, product name, app id, scene list, build report 0 errors / 13 warnings, and the two superseded builds with their hashes |
| The version shown in-game cannot drift from the binary | `Assets/Scripts/Core/ReleaseInfo.cs` | `Version` reads `Application.version`; `BuildId` = version + build stamp; no version string is hard-coded |
| The repository tag and commit | `README.md` (repository root, release section) and `Week_17/Evidence/build_and_commit.txt` | branch `main`, annotated tag `v1.0.0-rc1`, and the commit the tag points at |
| The package is a real APK | `Week_17/Evidence/readiness_results.md` §5 | `classes.dex` and `AndroidManifest.xml` at the archive root of 419 entries, no `BundleConfig.pb`; Build App Bundle off (the DEF-15-04 guard) |
| The build is a release build, not a development build | `Week_17/Evidence/build_and_commit.txt` | Development Build **off**; and why 31.63 MB is not comparable with Week 16's 68.30 MB |

## 3. Week 17 - release candidate evidence

| Claim | File | What it shows |
|---|---|---|
| Nothing was added after the freeze except closures and fixes | `Week_17/Evidence/feature_freeze_checklist.md` | frozen tree `b4cff53` / tag `week16-final`, the three permitted categories of change, the accepted Week 13-16 change sets, the checklist itself, and the explicit list of what is *not* in the RC |
| Every issue found this week is fixed or carried with a reason | `Week_17/Evidence/issue_disposition.md` | DEF-17-01 to DEF-17-07, ENV-17-01, OBS-17-01/02, BLOCKED-17-01, with severity, how each was found, and its disposition |
| The release candidate passes the four executable gates | `Week_17/Evidence/readiness_results.md` | six numbered checks with results and raw-output pointers; §"What is not claimed" |
| The game really runs, screen by screen | `Week_17/Evidence/E1_title_release.png` … `E6_restart.png` | six captures: title with the version caption, how-to, credits, live match, ending, restart |
| The captures are not stale or duplicated by accident | `Week_17/Evidence/week17_shots.txt` | run log: the version string it read, the four captures that ended the match, the restart, and `timeSinceLevelLoad 3.4 s`; plus the harness's hashing/repaint note. All six files have distinct MD5s |
| The six screens are reached by driving the game, not by editing scenes | `Assets/Editor/Week17Shots.cs` | the harness that performs the walk and hashes each frame |
| The two reading screens exist in all three scenes | `Assets/Editor/ReleasePanelsBuild.cs` | the builder that writes `Panel_HowTo`, `Panel_Credits`, the version labels and the title row into every scene, and warns if a target object is missing |
| No credential is versioned | `Week_17/Evidence/secret_scan.md`, `secret_scan_raw.txt` | the scan command, twelve matches, all prose; the hygiene actions taken |
| The architecture as built | `Week_17/Evidence/architecture.svg` | three scenes, the runtime scripts, the data outside the scene, and the two flows that cross between them |
| The demonstration sequence and its timings | `Week_17/Abrera_Zedrich_PT7.md` §10 | the seven-minute script, each step tied to a screenshot in this index |

## 4. Week 16 - profiling and optimisation evidence

| Claim | File | What it shows |
|---|---|---|
| Main thread 0.59-0.63 ms, GPU 0.48 ms, 17 render events, against a 33.3 ms budget | `Week_16/Evidence/raw_csv/perf_baselclean_run1_20261119_000633.csv` (and `_run2`, `_run3`) | per-frame counters from the shipping render path; the three-run spread is 0.01 ms |
| The texture change returned 20.0 MB without moving frame time | `Week_16/Evidence/raw_csv/perf_fix1clean_run1_20261119_000043.csv` (and `_run2`, `_run3`) | the A/B pair against `perf_baselclean_*`; both builds measured minutes apart on an idle host |
| The contaminated runs that were discarded, not hidden | `Week_16/Evidence/raw_csv/perf_fix1_run1_20261118_233711.csv` … `perf_fix1b_run3` | the 2.07 ms outlier, and `Week_16/Evidence/player_logs/` showing the unrelated process that caused it |
| The bottleneck analysis and its corrections | `Week_16/Evidence/bottleneck_table.md` | ranked costs, the single-scene capture limitation, and the correction left in §6 rather than quietly edited |
| The two diagrams | `Week_16/Evidence/frame_budget.svg`, `memory_fix.svg` | frame budget breakdown; the 21,849 KB to 1,369 KB texture reduction |
| The regression gate after the optimisation | `Week_16/Evidence/test_logs_regression/` | EditMode 17/17, play-mode 16/0/0, accessibility 423/0/24, and `regression_gate_summary.md` |

## 5. Week 15 - testing and defect evidence

| Claim | File | What it shows |
|---|---|---|
| Twenty testable requirements, each tied to a test and to a script | `Week_15/Evidence/requirements.md` | REQ-01 to REQ-20 with the implementing code named |
| The checkpoint conditions were written before testing | `Week_15/Evidence/acceptance_criteria.md` | CC-1 to CC-4 as binary criteria |
| Requirement to test to result | `Week_15/Evidence/traceability_matrix.md` | the full matrix |
| Baseline and final runs | `Week_15/Evidence/test_logs_baseline/`, `test_logs_final/` | `scenario.tsv`, `scenario_summary.txt`, `editmode_final.txt` |
| The 16-case suite that the release candidate re-ran | `Week_15/Evidence/test_logs_final/scenario.tsv` | one row per case; this file now holds the passing 16/0/0 run that `Week_17/Evidence/readiness_results.md` §3 cites |
| What was broken before testing, and how it ended | `Week_15/Evidence/defect_log.md` | DEF-15-01 background pause, DEF-15-02 withdrawn, DEF-15-03 difficulty clamp, DEF-15-04 app bundle named `.apk` |
| The acceptance summary | `Week_15/Evidence/acceptance_summary.md` | the four conditions and their verdicts |
| The environment the tests ran in | `Week_15/Evidence/environment.txt` | machine, engine, build target |
| The two Android gaps | `Week_15/Evidence/android_crash_buffer.txt`, `android_rapid_input_monkey.log` | recorded as blocked, not passed - the same honesty as this week's `clean_install.md` |

## 6. Week 14 - accessibility and playtest evidence

| Claim | File | What it shows |
|---|---|---|
| 516 passed, 0 failed, 24 not measurable on the release candidate | `Week_14/Evidence/a11y_audit.log` and the timestamped `a11y_audit_20261119_143943.log` | the audit's own output, run against the rebuilt scenes |
| The before/after of the accessibility work | `Week_14/Evidence/a11y_baseline.log` (345 passed / 69 failed), `a11y_after.log` (423 / 0) | the two ends of the fix, on the same audit |
| How accessibility is measured | `Assets/Editor/A11yAudit.cs` | 144 x 144 canvas-px touch floor, rect height vs font size, WCAG 4.5:1 contrast against the nearest opaque panel |
| Five testers, 25 task attempts, 0 failures | `Week_14/Playtest/04_results_table.md`, `Week_14/others/Abrera_Zedrich_PT6.md` §6 | per-tester and per-task counts, median times, sound-noticed counts |
| The two S2 findings | `Week_14/others/Abrera_Zedrich_PT6.md` §7 | rescue and catch discoverability, seen by 1/5 each |
| The playtest kit and protocol | `Week_14/Playtest/01_consent_form.md` … `05_facilitator_checklist.md` | consent, session script, observation sheet, results table, facilitator checklist |
| The build the testers used | `Week_14/Evidence/build_and_commit.txt` | the PT6 playtest build - and note that this file is the one recorded as an app bundle with an `.apk` name (DEF-15-04) |
| The sound work | `Week_14/Evidence/feedback_lines.txt`, `feedback_lines_run1.txt` | recorded `[Feedback]` lines naming the clip each event would have played |

## 7. Week 13 - profile and security evidence

| Claim | File | What it shows |
|---|---|---|
| The profile works: name, record, leaderboard, reset | `Week_13/Evidence/pt5_checks.log` | the scripted checks and their output |
| The four profile screens | `Week_13/Evidence/E1_profile_empty.png` … `E4_after_reset.png` | empty profile, statistics, leaderboard, post-reset state |
| How the profile data flows and is signed | `Week_13/Evidence/SA5_data_flow.svg` | the diagram used in the Week 13 analysis |
| No secrets, and what is ignored | `Week_13/Evidence/secrets_and_gitignore.txt` | the Week 13 scan and the ignore rules |
| The build that PT5 tested | `Week_13/Evidence/build_and_commit.txt` | build identity and size |
| The save format and its guard | `Assets/Scripts/Core/PlayerProfile.cs` | `profile.json`, signature check on load, `RecordMatch`, `ResetProfile`, `lastError` cleared before loading |

## 8. The documents themselves

| Document | Source | Purpose |
|---|---|---|
| `Week_17/Abrera_Zedrich_PT7.pdf` | `Week_17/others/Abrera_Zedrich_PT7.md` | the PT7 report: freeze, release candidate, readiness, issues, package |
| `Week_17/Abrera_Zedrich_PT7_Technical_Documentation.pdf` | `Week_17/others/Abrera_Zedrich_PT7_Technical_Documentation.md` | concept, theme, architecture, scripts, data and service flow, controls, setup, testing, performance, limitations, credits, licences |
| `Week_17/Abrera_Zedrich_PT7_User_Guide.pdf` | `Week_17/others/Abrera_Zedrich_PT7_User_Guide.md` | install, screens, controls, rules, troubleshooting |
| `Week_17/Abrera_Zedrich_PT7_Evidence_Index.pdf` | `Week_17/others/Abrera_Zedrich_PT7_Evidence_Index.md` | this file |
| `Week_17/Abrera_Zedrich_Week17_Reflection.pdf` | `Week_17/others/Abrera_Zedrich_Week17_Reflection.md` | the four required reflection prompts |

Each `.html` in `others/` is generated from its `.md` by `Docs/tools/MdToHtml.cs`, with every figure inlined as a data URI so the printed PDF carries its own pictures. The PDFs above are printed from those HTML files.

## 9. Reproducing the gate

The four checks that decide whether this release candidate is ready, in order of cost:

```
# 1. Rules, with no play mode involved  (expect 17 passed)
run_tests(mode: EditMode)

# 2. Accessibility over all three scenes  (expect 516 passed / 0 failed / 24 not measurable)
A11yAudit.Run()

# 3. Sixteen play-mode scenarios  (expect 16 passed / 0 failed, at a 4v4 roster)
MatchSettings.SetTeamSize(4)
QualityRun.Run()

# 4. The package is APK-shaped  (expect classes.dex at the root)
tar -tf Builds/Week17/AgawanBase_RC.apk
```

`Week_17/Evidence/readiness_results.md` records the output of each of these against the md5 in §2. `Week_17/Evidence/issue_disposition.md` records what happened when the third one was run without its 4v4 precondition, because a gate that only ever shows green teaches nothing.

## 10. What has no evidence, and why

| Requirement | Status | Why there is no file |
|---|---|---|
| Install and launch on an Android handset from a clean state | **BLOCKED** | No device was attached; `adb devices` printed an empty list. `Week_17/Evidence/clean_install.md` records the command and its output, and lists what stands in for each device check |
| Device frame rate, thermal behaviour, long-session stability | **Not measured** | Same missing device. The desktop numbers in `Week_16/Evidence/` are the substitute, and they are labelled as desktop numbers everywhere they are quoted |
| Rotation lock and background pause on real hardware | **Editor only** | `TC-LIFE-03` drives the pause path in the editor; a real Home press and a real rotation were never available |
| Re-testing the two playtest findings after the instructions screen was added | **Open** | The screen exists and is captured in `E2_how_to_play.png`, but no second round of testers has used it |
| Any online service, account or leaderboard | **Does not exist** | The game makes no network call; there is nothing to evidence |

A defense that can say which of its own claims are unwitnessed is in a stronger position than one that cannot. That is why this section exists and is not empty.
