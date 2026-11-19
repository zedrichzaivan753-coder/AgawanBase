# Week 17 - Feature Freeze, Release Candidate, and PT7 Readiness

## Objective
Freeze AgawanBase at a tagged release candidate `1.0.0-rc1` that actually meets the module's gameplay-completeness bar (instructions, credits, exit), then produce the PT7 release package: APK, technical documentation, user guide, evidence index, readiness checklist, and Week 17 reflection.

## Changes
- `Assets/Scripts/Core/ReleaseInfo.cs` - (create) static `Version`, `BuildId` (version + short commit baked as a const), `ProductName`, used by the new title/credits labels.
- `Assets/Scripts/UI/GameManager.cs` - add `HowTo` and `Credits` to `GameState`; add `panelHowTo`, `panelCredits`, `versionLabel` fields; `ShowHowTo()`, `ShowCredits()`, `CloseOverlay()`, `QuitGame()` (Android `Application.Quit`, editor stops play mode and logs).
- `Assets/Editor/ReleasePanelsBuild.cs` - (create) rerunnable builder modelled on `Assets/Editor/MapSelectBuild.cs`: writes `Game/UI/Panel_HowTo`, `Game/UI/Panel_Credits`, `Game/UI/Panel_Title/VersionLabel` and the HOW TO PLAY / CREDITS / EXIT buttons into **all three scenes**, binds `GameManager`, saves.
- `Assets/Editor/Week17Shots.cs` - (create) editor screenshot harness modelled on `Assets/Editor/Week14Shots.cs`, PNGs into `Docs/Zedrich Abrera/Week_17/Evidence/`.
- `Assets/Scenes/{Game,Map_Palengke,Map_SchoolCampus}.unity` - new panels and title buttons (via the builder only, not by hand).
- `ProjectSettings/ProjectSettings.asset` - `bundleVersion` `0.2` -> `1.0.0-rc1`, `AndroidBundleVersionCode` `1` -> `2`.
- `Docs/tools/MdToHtml.cs` - add `"Week_17"` to `Weeks` and its five documents to `Documents`.
- `Docs/Zedrich Abrera/Week_17/` - (create) five `.md`/`.html`/`.pdf` documents, `Abrera_Zedrich_PT7.apk`, `Evidence/`, `others/`.
- `Docs/Zedrich Abrera/README.md` - Week_17 rows, release/version table, updated limitations.

## Relevant Assets and Quirks
- **The module's completeness bar is genuinely unmet today.** `Game/UI/Panel_Title/RulesText` reads *"Galingan mo boss"* (flavour, not instructions); nothing in `Assets/Scripts` contains a credits screen; `Application.Quit` exists only in `PerfCapture.cs`. Week 17 step 2 of the procedure requires instructions, credits and exit.
- Panels must exist in **all three scenes** - they were cloned from `Game.unity`, so a panel only in `Game.unity` vanishes after a map load. `MapSelectBuild.Build()` is the precedent for writing UI into three scenes at once.
- Every new label and button is scored by `Assets/Editor/A11yAudit.cs` (144 canvas-px touch floor, 4.5:1 contrast). Baseline on the Week 16 tree is **423 passed / 0 failed / 24 not measurable**; the new screens must not regress it.
- Build config: Android is the active target, **Build App Bundle must stay OFF** (DEF-15-04 produced an uninstallable `.aab` named `.apk`), IL2CPP, ARM64, min API 26. `BarangayUtils.BuildApk()` writes `Builds/AgawanBase.apk`; appending `-rc1` to `bundleVersion` is safe for `Application.version` and the Android `versionName`.
- `Builds/`, `*.apk` and `*.log` are gitignored, so the APK reaches the repo only by being copied into `Docs/Zedrich Abrera/Week_17/`.
- No handset is attached: the clean-install case is recorded **BLOCKED**, exactly as Week 15's device cases were, with play-mode evidence standing in.
- The two Week 16 commits stay local (user decision); the `v1.0.0-rc1` tag is created locally and PT7 states it is not yet on `origin`.
- House style to mirror: `Docs/REFERENCE/CASTILLO_Janelle_Ann_23-10260_SiliTubigClash/Week_17/` ships `PT7.pdf`, `PT7_Technical_Documentation.pdf`, `PT7_User_Guide.pdf`, `PT7_Evidence_Index.pdf`, `Week17_Reflection.pdf`, `PT7.apk`. Author is Zedrich Zaivan B. Abrera, never Castillo.
- `execute_script` is broken in this project - invoke builders through `execute_code` reflection. `MdToHtml` must be copied to `Assets/Editor/` then invoked as `Type.GetType("MdToHtml, AgawanBase.Editor")`, and its internal `Convert` shadows `System.Convert` (always write `System.Convert.ToBase64String`).
- Images must be embedded in the documents (Week 13 complaint), so every screenshot goes through `![caption](Evidence/x.png)` before the PDF is printed.

## Steps
- [x] 1. Freeze and clean: annotated tag `week16-final` on HEAD, write `Week_17/Evidence/feature_freeze_checklist.md` (frozen scope = the four accepted Week 13-16 change sets, nothing new after this point), delete the stray generated folders `Week_14/Abrera_Zedrich_PT6_files/` and `Week_14/others/Abrera_Zedrich_SA6_files/`, force-add `Week_16/Evidence/test_logs_regression/a11y_regression.log`, run a secrets scan (`ApiKey|token|secret|password|ghp_|AIza` over `Assets/ Docs/ ProjectSettings/`) and extend `.gitignore` for those generated folders. [depends: none]
- [x] 2. Add `ReleaseInfo.cs` and extend `GameManager.cs` with the `HowTo`/`Credits` states, three serialized fields, four methods, and `Debug.Log` lines (`[Release] how-to opened`, `[Release] credits opened`, `[Release] quit requested`). Compile-check. [depends: none]
- [x] 3. Write `Assets/Editor/ReleasePanelsBuild.cs` and run `Build()`: a How-to-Play panel (goal, freshness catch rule, prison rescue, flag steal, three win conditions, stick/sprint controls), a Credits panel (product name, `ReleaseInfo.BuildId`, Zedrich Zaivan B. Abrera, theme *Philippine Games and Sports*, Unity 6000.6.0f1 + URP 17.6.0 licences, TMP/font notice, AI-assistance disclosure), a `VersionLabel` on the title, and the three title buttons - in **all three scenes**. [depends: 2]
- [x] 4. Unfreeze the version: `bundleVersion = 1.0.0-rc1`, `AndroidBundleVersionCode = 2`, Development Build **off**, Build App Bundle **off**; build via `BarangayUtils.BuildApk()`, verify `classes.dex` at the APK root and record the real byte size, then copy to `Week_17/Abrera_Zedrich_PT7.apk`. [depends: 3]
- [x] 5. Readiness verification: `run_tests` EditMode (expect 17/17), `Tools/Week15/Run Quality Scenarios` (expect 16 passed / 0 failed), `Tools/Playtest/Run Accessibility Audit` (expect 0 failed including the new screens), a play-mode walk title -> how-to -> credits -> match -> end screen -> restart, plus `Week17Shots.Capture()`. Write `Evidence/readiness_results.md`, `Evidence/issue_disposition.md` and `Evidence/clean_install.md` (BLOCKED, with the reason and the substitute evidence). [depends: 4]
- [x] 6. Write the five documents in `Week_17/others/` with embedded screenshots: `Abrera_Zedrich_PT7`, `_PT7_Technical_Documentation`, `_PT7_User_Guide`, `_PT7_Evidence_Index`, `_Week17_Reflection` (four required prompts, ends with the exact declaration). Register `Week_17` in `MdToHtml.cs`, generate the HTML, headless-print the PDFs and confirm each PDF carries its images. [depends: 5]
- [x] 7. Commit the release tree locally, create annotated tag `v1.0.0-rc1` on that commit, and update `Docs/Zedrich Abrera/README.md` (version 1.0.0-rc1, Week_17 rows, install instructions for the RC, known limitations including the BLOCKED device test and the local-only tag). No push. [depends: 6]
  - *Note after verification (2026-11-19):* play-mode verification of the tagged RC found `Panel_HowTo/Body` needing **734.5 px** of text in a **660 px** rect, so the tail of the rules rendered behind the BACK button (`DEF-17-07`). Fixed in `ReleasePanelsBuild` - box `1560 x 600` at y `+30` and a new `FitText` that measures the paragraph instead of guessing (settles at font 21 / 526.2 px) - all three scenes rebuilt, `E1`-`E6` re-captured, `A11yAudit` re-run (516 / 0 / 24, unchanged), APK rebuilt to **33,170,107 B / md5 `ef38de8d…`** from commit `84c0f2e`, all five documents re-converted and re-printed, and the tag moved to the shipping commit `07057bd`. Also corrected the hygiene claim that the APK was force-added: `git ls-files` lists no `.apk` in the repository, and no earlier week's either. Re-verified afterwards: whole rules block clears BACK by 135.4 canvas units and all four screen behaviours pass.

## Verify
- `check_compile_errors` reports no errors after steps 2-3; `A11yAudit` returns **0 failed** with a higher passed count than 423 (the new screens add labels).
- Open the built APK's zip listing and confirm `classes.dex` and `AndroidManifest.xml` sit at the root, and that the on-disk size is what `readiness_results.md` and PT7 quote.
- Play mode on all three scenes: HOW TO PLAY, CREDITS and EXIT buttons exist and the panels open and close; the version label reads `1.0.0-rc1`; `[Release]` lines appear in the console.
- Grep each `.html` for `<img` and the document name, and check the PDFs open with their figures present - a document without its screenshots fails the Week 13 lesson.
- `git tag -l` shows both `week16-final` and `v1.0.0-rc1`, and PT7 names the commit id they point at.
