# Abrera_Zedrich_PT7

**PT7 - Final game refinement and release candidate - Week 17**
**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 - Advanced Mobile Application Development, Module 5
**Game:** Agawan Base - a 3D mobile capture-the-flag game built on the Filipino street game *Agawang Base*
**Release candidate:** **1.0.0-rc1**, versionCode 2
**Build under test:** `Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk` - **33,170,107 bytes = 31.63 MB**, md5 **`ef38de8dd65100109791002a67738da1`**
**Repository:** branch `main`, annotated tag `v1.0.0-rc1`; the commit the tag points at is recorded in `Evidence/build_and_commit.txt`

This document is the front page of the PT7 submission package. It states what was frozen, what the release candidate is, what was verified against it, what was found and fixed, and what remains unproven. Every number in it names the file it came from. Where the module asks for something that could not be executed - installing on a handset, on this build, with no device attached - it is recorded as **BLOCKED** with the reason and with whatever evidence stands in its place, not as a pass.

## Contents

1. What PT7 asks for, and where each answer is
2. The release candidate
3. The feature freeze
4. The four gaps closed after the freeze, and two release blockers
5. Readiness results
6. Issue disposition
7. Clean install on the target device: BLOCKED
8. Repository hygiene
9. The submission package
10. Timed mock demonstration
11. What this package does not prove
12. References

---

## 1. What PT7 asks for, and where each answer is

| # | Week 17 procedure step | Deliverable in this package |
|---|---|---|
| 1 | Review the theme-to-mechanics justification; verify the final mechanics still communicate the theme | `Abrera_Zedrich_PT7_Technical_Documentation.md` §1 - three rule-to-theme links, checked against the mechanisms that actually ship |
| 2 | Complete the loop, menus, instructions, feedback, progression, win/loss, settings, credits, and exit/restart | §4 below; instructions, credits and EXIT were the three that were missing and are now in all three scenes |
| 3 | Integrate and verify the Week 13-16 player-data, service, security, usability, accessibility, testing and optimization work | §5 below - 17 EditMode tests, 516/0 accessibility, 16/0/0 quality scenarios, all re-run on this build |
| 4 | Clean the repository of secrets, unused files, temporary builds, duplicate assets and generated files | §8 below, plus `Evidence/secret_scan.md` and its raw output |
| 5 | Prepare a release candidate with a unique version and build identifier; install and run it on the target device from a clean state | §2 and §7 - the version and the identifier are unique and recorded; the **device install is BLOCKED**, no handset was attached |
| 6 | Technical documentation: concept, theme, architecture, scripts, data and service flow, controls, setup, testing, performance, limitations, credits, licences | `Abrera_Zedrich_PT7_Technical_Documentation.md`, 17 sections |
| 7 | A concise user guide and a defense evidence index pointing to commits, diagrams, tests, profiler captures and findings | `Abrera_Zedrich_PT7_User_Guide.md`, `Abrera_Zedrich_PT7_Evidence_Index.md` |
| 8 | A timed mock demonstration and defensible answers on architecture, mechanics, services, security, testing, performance, maintainability | §10 below - a seven-minute script, each step tied to a screenshot that already exists |

## 2. The release candidate

| | |
|---|---|
| File | `Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk` |
| Size on disk | **33,170,107 bytes = 31.63 MB** |
| md5 | **`ef38de8dd65100109791002a67738da1`** |
| Build output | `Builds_RC2/Abrera_Zedrich_PT7.apk` (staging folder, deleted after the copy above) |
| Built | November 19, 2026, 15:20 local - build started 15:20:27, took 24.8 s |
| Engine | Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 |
| Platform | Android, **release build** - Development Build **off**, Build App Bundle **off** |
| Scripting / architecture | IL2CPP, ARM64 only |
| Minimum Android | API 26 (Android 8.0) |
| Orientation | Landscape left, locked |
| Managed stripping | Minimal |
| `productName` | `AgawanBase` |
| `bundleVersion` / `AndroidBundleVersionCode` | **1.0.0-rc1** / **2** |
| Application id | `com.zedrich.agawanbase` |
| Scenes | `Game` (index 0), `Map_Palengke`, `Map_SchoolCampus` |
| Build report | `Succeeded`, 0 errors, 13 warnings |

![E1 - the release-candidate title screen. The version caption in the top-left corner reads "1.0.0-rc1 - built 2026-11-19", which is the same string the credits panel prints, and all three new controls (HOW TO PLAY, CREDITS, EXIT) are present above PROFILE.](Evidence/E1_title_release.png)

**Why the md5 is the identifier of what was tested.** A commit id cannot identify this binary, because the APK has to exist before the commit that ships it does. The md5 above is therefore what `Evidence/readiness_results.md` reports on; the source commit it was built from is `84c0f2e`, recorded beside the md5 in `Evidence/build_and_commit.txt`.

**This is the third build of the day.** The first (33,170,243 bytes, md5 `bb36569a7a1387997c556a56aeaf0344`) shipped the two defects described in §4 before the credits line and `productName` were corrected. The second (33,170,059 bytes, md5 `4b1152fe203ba6f679404d955fdaa59d`) was correct and was the build verified in play mode, which found the how-to overflow also described in §4; this third build is that defect fixed. Neither earlier hash may be quoted as the release candidate.

## 3. The feature freeze

`Evidence/feature_freeze_checklist.md` is the checklist itself. Its substance:

- **Frozen tree:** commit `b4cff53`, annotated tag `week16-final`, frozen on November 19, 2026.
- **What may still change:** (1) completeness gaps the module itself requires - instructions, credits, exit; (2) defect corrections that keep an accepted behaviour working; (3) release mechanics - version number, build configuration, documentation. Nothing else. A reviewer comparing `week16-final` with `v1.0.0-rc1` can check every change against those three categories.
- **The accepted change sets of Weeks 13-16** are listed with the script or asset each one lives in and the evidence each was accepted on: the signed local profile (`PlayerProfile.cs`), the accessibility pass (`A11yAudit.cs` and the uGUI in all three scenes), the five feedback sounds (`GameFeedback.cs`), the Week 15 fixes (DEF-15-01 background pause, DEF-15-03 difficulty clamp, DEF-15-04 APK packaging), the Week 16 profiling harness (`PerfCapture.cs`, `PerfScenario.cs`) and the prop-texture cap that returned 20.0 MB of runtime texture memory.
- **Checked at the freeze:** the game runs in all three scenes; no half-finished mechanic is switched on; the AI debug read-out is editor-only and the other six debug flags are off; Android is the active target with IL2CPP, ARM64, min API 26 and Build App Bundle off; the secrets scan is clean; every Week 13-16 evidence file is present.

No new map, rule, AI behaviour or art was added after that commit.

## 4. The four gaps closed after the freeze, and three release blockers

The freeze checklist lists three completeness gaps plus one release requirement. All four are closures of things the module asks for, not new mechanics:

| Gap | Module requirement it answers | Fix |
|---|---|---|
| No instructions screen - the title's only guidance was the flavour line *"Galingan mo boss"* | "Title or start screen and clear instructions or tutorial" | `Game/UI/Panel_HowTo`, written into all three scenes by `Assets/Editor/ReleasePanelsBuild.cs` |
| No credits anywhere in code or scenes | "settings, credits, and exit or restart behavior" | `Game/UI/Panel_Credits` - title, version, author, course, theme, engine, licences, AI-assistance disclosure |
| No exit control; `Application.Quit` existed only in the profiling harness | "exit or restart behavior" | EXIT button on the title calling `GameManager.QuitGame()` |
| No version shown in the running game | "unique version number and build identifier" | `VersionLabel` on the title and the build line on the credits, both filled from `ReleaseInfo` at runtime |

Three release blockers were found after the freeze. All are fixed in the artefact described in §2:

| Id | What it was | Why it mattered | Fix |
|---|---|---|---|
| **DEF-17-01** | The credits screen printed version **`0.2`** while the title printed `1.0.0-rc1` | The build line was a string baked into the scene when the panel builder last ran, so it could never follow the binary it was inside. A reviewer reading the credits would have concluded the wrong build was submitted | `GameManager.creditsVersionLabel` is filled in `Awake()` from `ReleaseInfo`, exactly like the title caption; the builder wires it and warns if the object is missing. Visible in `E3_credits.png` |
| **DEF-17-02** | `productName` was `AgawanBase v1.0.0-rc1` | Unity derives the `PlayerPrefs` namespace and `Application.persistentDataPath` from the product name, so putting the version in it gave the release candidate an empty settings store and a **new** profile folder: anyone upgrading would have silently lost their record and their saved match settings | `productName` is `AgawanBase` again; the version lives only in `bundleVersion`, which is what `Application.version`, the title caption and the credits line read |
| **DEF-17-07** | The how-to text overflowed its box: `Panel_HowTo/Body` needed 734.5 px of text in a 660 px rect, so the last ~74 canvas units of the rules - the end of *PAUSING AND SETTINGS* - rendered **behind the BACK button** | The instructions screen is the one screen the module's checklist demands and the first thing a new player reads; the one screen that teaches the rules was the one screen hiding part of them | The box is now 1560 x 600 at y +30, which clears BACK by 102.8 px even on a 20:9 phone, and `ReleasePanelsBuild.FitText` measures the paragraph and shrinks the font until it fits (21 px for 526.2 px of text) instead of guessing. Three scenes rebuilt, `E2_how_to_play.png` re-taken |

DEF-17-02 was not found by inspection. The quality suite, clean in Week 16, suddenly reported three failures, and the cause was the roster coming back as 1v1 instead of the saved 4v4 - the product name had moved the store. `Evidence/issue_disposition.md` records the whole chain.

## 5. Readiness results

`Evidence/readiness_results.md` holds the detail and the raw-output pointers. The summary:

| # | Check | Expected | Result |
|---|---|---|---|
| 1 | EditMode test suite | 17 of 17 | **17 passed, 0 failed, 0 skipped**, 0.62 s |
| 2 | Accessibility audit (`A11yAudit.Run()`) | 0 failed, no regression from the 423 baseline | **516 passed, 0 failed, 0 warnings, 24 not measurable** |
| 3 | Week 15 quality scenarios (`QualityRun.Run()`) | the Week 16 gate: 16 passed, 0 failed | **16 passed, 0 failed, 0 blocked**, 0 exceptions |
| 4 | Play-mode walk of the release screens | title, how-to, credits, match, end, restart - each photographed | **all six reached and captured** |
| 5 | The package is a real APK, not an `.aab` | `classes.dex` at the archive root | **`classes.dex` and `AndroidManifest.xml` at the root of 419 entries, no `BundleConfig.pb`** |
| 6 | Install and launch on the target device from a clean state | the module's checkpoint | **BLOCKED - no handset** (§7) |

Check 2 is the one that grew: the Week 14 baseline scored 423 labels and buttons; the release candidate scores 516, because the two new screens added 93 measurable items, and every one of them clears both thresholds the audit applies - a 144 x 144 canvas-px touch floor (the title buttons are 440 x 144, both BACK buttons 480 x 144) and 4.5:1 contrast (the lowest ratio on the new screens is the theme line at 8.77:1).

Check 3 needed three runs. The first two reported 13 passed / 3 failed / 1 blocked, and the cause was **not** the game: the suite's documented precondition is a 4v4 roster, the machine's saved team size had been reset, and at 1v1 the suite's own capture ends the match by eliminating the only Red - after which the life-cycle cases correctly report a frozen clock and an ended match. With `MatchSettings.SetTeamSize(4)` restored, the untouched code scored 16 / 0 / 0, identical to the Week 16 gate. It is recorded as `ENV-17-01`, an environment precondition, in `Evidence/issue_disposition.md` rather than deleted.

Check 2 and check 4 were re-run after DEF-17-07 was fixed: the audit returned the identical 516 / 0 / 0 / 24, because resizing a rect and shrinking a font moves neither a contrast ratio nor a touch target, and the six screens were captured again - all six hashes distinct, with `E2` now showing the rules ending clear of the BACK button. The EditMode suite and the quality scenarios were not re-run for that fix; it changes no rule, no component and no test subject.

## 6. Issue disposition

| Id | Sev | Issue | Disposition |
|---|---|---|---|
| DEF-17-01 | **A** | Credits printed `0.2` | **Fixed**, APK rebuilt |
| DEF-17-02 | **A** | Version in `productName` moved the PlayerPrefs namespace and `persistentDataPath` | **Fixed**, APK rebuilt |
| DEF-17-03 | B | Screenshot harness captured stale frames - E1, E2 and E3 were byte-identical | **Fixed** in the harness, all six re-captured |
| DEF-17-04 | B | The harness's restart wait counted editor update ticks, so a scene reload still in progress was declared failed | **Fixed** (waits on real time), re-run |
| DEF-17-05 | B | The elimination step gave up the first time no free Red was returned | **Fixed** (waits up to 20 s, caps at 12 captures), re-run |
| DEF-17-06 | B | The title's `RulesText` sat at centre -330, underneath the opaque PROFILE button, and had never been visible at any aspect | **Fixed** - the whole title stack is top-anchored; visible in `E1_title_release.png` |
| DEF-17-07 | **A** | The how-to body needed 734.5 px of text in a 660 px rect, so the tail of the rules rendered behind the BACK button | **Fixed** - box resized and the font measured rather than guessed; three scenes rebuilt, APK rebuilt |
| ENV-17-01 | C | Two quality runs reported 13 / 3 / 1 | **Not a game defect** - missing 4v4 precondition (§5) |
| OBS-17-01 | C | `E6_restart.png` was byte-identical to `E4_match_running.png` | **Dissolved by the re-capture** - the pair differs in the current run (`aed46379…` / `40a7cef2…`); in both runs the proof of freshness is the 3.4 s `timeSinceLevelLoad` in the run log, not the pixels |
| OBS-17-02 | C | Console fills with `Releasing render texture that is set as Camera.targetTexture!` | **Not from the game** - a capture-tool artefact, never in a player build |
| BLOCKED-17-01 | - | Install and launch on a handset from a clean state | **BLOCKED**, no device (§7) |

Severity is about the release, not the effort: **A** means the APK would be wrong for a reviewer or a player, **B** means the evidence would be wrong or unrepeatable, **C** is cosmetic or process.

## 7. Clean install on the target device: BLOCKED

`adb` ships with this Unity's Android player and it was run on November 19, 2026. It printed an empty device list:

```
List of devices attached
```

There is no handset, tablet or emulator image to install 33,170,107 bytes onto, so the module's install-and-run checkpoint is **BLOCKED**, not passed. `Evidence/clean_install.md` records the command, its output, and a requirement-by-requirement table of what stands in for each device check - in every row, editor play-mode evidence, which is weaker than a device run and is labelled as such.

A reviewer with a handset can close the gap in about two minutes:

```
adb install -r "Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk"
adb shell am start -n com.zedrich.agawanbase/com.unity3d.player.UnityPlayerActivity
```

and should expect the title screen with `1.0.0-rc1 - built 2026-11-19`, then HOW TO PLAY, CREDITS, a match, an ending, and RESTART.

## 8. Repository hygiene

`Evidence/secret_scan.md` is the scan and `secret_scan_raw.txt` its raw output.

- **No credential of any kind is versioned.** `findstr` over every text-bearing file type a secret could hide in returned twelve matches, and all twelve are prose: plan text describing the scan itself. The game makes no network call, stores no token, and the APK is signed by Unity's own debug keystore rather than by a checked-in keystore.
- **Duplicate browser mirrors removed from version control.** `Week_14/Abrera_Zedrich_PT6_files/` and `Week_14/others/Abrera_Zedrich_SA6_files/` were "Save page as" copies of images already base64-inlined in the `.html` documents plus Word junk. They were untracked (the files stay on disk), and `*_files/` was added to `.gitignore`.
- **Build and profiler artefacts ignored.** `*.raw`, `/Builds/`, `*.apk`, `*.aab`, `*.log` are ignored. **No APK is versioned** - `git ls-files` lists none, in this week or any earlier one - so the release candidate ships as the file in `Docs/Zedrich Abrera/Week_17/` and in the Drive submission, identified by its md5. Week 16's 1.38 GB profiler capture and two 170 MB players never enter the repository.
- **One file force-added back in.** `Week_16/Evidence/test_logs_regression/a11y_regression.log` matched the global `*.log` rule, so it was shipping in the folder but missing from a clone; it is force-added so the Week 16 regression gate is reproducible.
- **Week 16 sources moved into `Week_16/others/`**, matching the Week 14 layout where the PDFs are the deliverables and their sources sit beside them.

## 9. The submission package

| File | What it is |
|---|---|
| `Abrera_Zedrich_PT7.apk` | the release candidate (33,170,107 bytes, md5 `ef38de8d…`) |
| `Abrera_Zedrich_PT7.pdf` | this document |
| `Abrera_Zedrich_PT7_Technical_Documentation.pdf` | concept, theme, architecture, scripts, data and service flow, controls, setup, testing, performance, limitations, credits, licences |
| `Abrera_Zedrich_PT7_User_Guide.pdf` | install, first launch, every screen, controls, rules, troubleshooting |
| `Abrera_Zedrich_PT7_Evidence_Index.pdf` | claim to file to what it shows, for Weeks 13-17 |
| `Abrera_Zedrich_Week17_Reflection.pdf` | the four required reflection prompts |
| `Evidence/` | the raw material: feature-freeze checklist, build record, readiness results, issue disposition, clean-install record, secrets scan, six screenshots, harness log, architecture diagram |

Every `.md` source in `others/` and every `.html` beside it is generated into the PDF above it by `Docs/tools/MdToHtml.cs` and printed with a headless browser, so the figures in the PDF are the same images that are inlined in the HTML.

## 10. Timed mock demonstration

Seven minutes, run in the Unity editor on this machine until a handset is available. Each step is tied to a screenshot that already exists, so the demonstration can also be given from the evidence set alone.

| Time | Show | Backing evidence |
|---|---|---|
| 0:00 - 0:40 | Launch: title, version caption `1.0.0-rc1 - built 2026-11-19` | `E1_title_release.png` |
| 0:40 - 1:30 | HOW TO PLAY, then CREDITS: author, course, theme, engine, licences, AI disclosure | `E2_how_to_play.png`, `E3_credits.png` |
| 1:30 - 2:00 | MAP SELECT and match setup: three maps, 1v1-4v4, three skill presets stored in PlayerPrefs | `MatchSettings.cs`, `MapSelectUI.cs` |
| 2:00 - 2:40 | A live match: HUD bars, SPRINT, the freshness race, tag rings, edge markers | `E4_match_running.png` |
| 2:40 - 3:20 | The rules in one breath: fresher character wins the tag, prison and rescue, flag and win conditions | `MatchManager.cs` |
| 3:20 - 4:00 | An ending and a restart | `E5_end_screen.png`, `E6_restart.png` |
| 4:00 - 5:00 | Architecture: three scenes, one panel tree, the state machine, the single rules authority | `Evidence/architecture.svg` |
| 5:00 - 6:00 | Evidence: 17/17 EditMode, 516/0 accessibility, 16/0/0 scenarios against this APK | `readiness_results.md` |
| 6:00 - 6:40 | Performance: 0.60 ms main thread and 0.48 ms GPU against a 33.3 ms budget, render scale 0.80 | `Week_16/Evidence/raw_csv/` |
| 6:40 - 7:00 | What is not proven: no device run, no cloud, no music, tamper-evident not encrypted | `clean_install.md` |

## 11. What this package does not prove

- **It is not device-verified.** Every runtime result comes from Unity's play mode on a Windows machine (Ryzen 5 5600G, GTX 1070, 15 GB RAM). The APK is build-verified and structurally checked - `classes.dex` at the archive root, Build App Bundle off - but no handset has run it. A mid-range phone will show a main-thread time several times larger and a GPU time an order of magnitude larger than the desktop figures in §10.
- **The catch in `E4` to `E5` is scripted, not played.** The harness calls the same public `MatchManager.Capture()` the tag rule calls, so the imprisonment, the elimination victory, the end screen and the profile write are all the real paths; what is scripted is that a script, not a thumb, made the tag happen.
- **`E6` and `E4` are no longer the same bytes.** In the first capture run they hashed identically, because both matches begin from the same deterministic state; that pair was kept and disclosed rather than dressed up. The re-capture after DEF-17-07 gave two distinct frames (`aed46379…` and `40a7cef2…`), and in both runs the evidence that the restart really happened is the run log's 3.4 s `timeSinceLevelLoad`, not the picture.
- **No optimisation is claimed from the APK size.** 31.63 MB against Week 16's 68.30 MB is a release build against a development build; the difference is stripped debug symbols, not removed content.
- **Playtesting was five testers on two handsets** (Week 14), not a field study: 25 task attempts, 0 failures, 23 unaided, 2 facilitated, two S2 findings about rule discoverability.
- **The profile is tamper-evident, not encrypted**, and it is one profile per install.

## 12. References

The following were used while building and verifying this release; the reference set for this course is the SiliTubig Clash submission in `Docs/REFERENCE/CASTILLO_Janelle_Ann_23-10260_SiliTubigClash`, whose Week 17 package (release candidate, technical documentation, user guide, evidence index, reflection) is the model for this one's structure.

- Google. (2024). *Application bundling and APK structure*. Android Developers. https://developer.android.com/guide/app-bundle
- OWASP Foundation. (2024). *OWASP Mobile Application Security*. https://mas.owasp.org/
- Unity Technologies. (2024). *Unity Profiler documentation*. https://docs.unity3d.com/Manual/Profiler.html
- Unity Technologies. (2024). *Building and distributing for Android*. https://docs.unity3d.com/Manual/android-building.html
- Unity Technologies. (2023). *Optimize your mobile game performance*. https://unity.com/resources/optimize-mobile-game-performance-unity-2022lts
- World Wide Web Consortium. (2024). *Web Content Accessibility Guidelines 2.2*. https://www.w3.org/TR/WCAG22/
