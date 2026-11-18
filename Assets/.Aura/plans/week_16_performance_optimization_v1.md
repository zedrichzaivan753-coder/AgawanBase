# Week 16 - Mobile Game Profiling and Performance Optimisation

## Objective
Capture a controlled 60 s profiling baseline on a Windows standalone development player built from the
current tree, use the raw measurements to prove at least two bottlenecks, fix them one at a time inside a
30 fps / 33.3 ms budget, re-measure under identical conditions, and ship SA6 + AS3 + reflection (md/html/pdf)
with the optimised build, regression logs and a repository commit ID.

## Changes
- `Assets/Scripts/Diagnostics/PerfCapture.cs` - (create) runtime recorder: `ProfilerRecorder` for PlayerLoop
  main-thread ms, GC alloc bytes/frame, draw calls, batches, triangles and total managed+native memory;
  writes one CSV row per frame plus a summary line per run; active only with `-perf` on the command line.
- `Assets/Scripts/Diagnostics/PerfScenario.cs` - (create) the repeatable scenario: fixed map, team size,
  difficulty, AI on, and the human walking a fixed waypoint loop as the recorded input sequence. Writes the
  scenario definition as CSV comments so every run carries its own conditions.
- `Assets/Editor/ProfilerRun.cs` - (create) menu `Tools/Week16/Run Profiling Scenario` + a builder that
  produces the Windows player with `-perf` arguments; prints the CSV path to the console.
- `Assets/Settings/Mobile_RPAsset.asset`, `Assets/Editor/MapBuild.cs` / `SchoolCampusBuild.cs` /
  `PalengkeBuild.cs` / `HeroProps.cs`, `Assets/Scripts/UI/HudController.cs` - (edit, **only those that
  step 3's evidence names**; nothing is touched on suspicion).
- `Docs/Zedrich Abrera/Week_16/` - (create) `Abrera_Zedrich_SA6.md`, `Abrera_Zedrich_AS3.md`,
  `Abrera_Zedrich_Week16_Reflection.md`, `Evidence/` (raw CSVs, profiler + frame-debugger captures,
  build records, regression logs, before/after screenshots).
- `Docs/tools/MdToHtml.cs` - (edit) add `Week_16` + its three documents to `Weeks` / `Documents`.

## Relevant Assets and Quirks
- **The 30 fps cap hides spikes.** `GameManager.Awake` sets `Application.targetFrameRate = 30`,
  `vSyncCount = 0`, `shadows = Disable`. With a cap on, a 60 ms frame still reads as 33.3 ms delta. So
  `PerfCapture` times the **PlayerLoop recorder** (cap-independent) and a `-perf-uncapped` switch lifts the
  cap for capture runs only; the cap is restored for normal play and never silently removed.
- **The scene is already lean on GPU settings**: `Mobile_RPAsset` has render scale 0.8, MSAA off
  (`m_MSAA: 1`), no shadows (`m_MainLightShadowsSupported: 0`), SRP Batcher on, dynamic batching off,
  HDR on, Linear colour space. `QualitySettings` has one "Mobile" level, shadows 0, AA 0, vSync 0.
  Remaining GPU levers are HDR and render scale, and both are visual trade-offs that need justifying.
- **Decor is already flagged static**: `MapBuild.cs:680`, `HeroProps.cs:296` and `BarangayBuild.cs:684` set
  `StaticEditorFlags.BatchingStatic`, so draw calls may already be batched. `manage_graphics stats_get` and
  the Frame Debugger decide this; the plan does not assume the batching story either way.
- **Rendering weight is real**: `Game.unity` alone reports 200+ `MeshRenderer`s (court lines, 16-segment
  rings, houses, coconut fronds are all separate primitives), plus 8 characters with procedural animation
  (`CharacterRigAnimator`), per-character world bars and NavMesh-driven AI.
- **Per-frame script work is already throttled** (`IndicatorController` 0.15 s, `OffScreenMarkers` timer,
  `HudController` cached strings, `MapNavigator` repath timer, `TeamManager` avoids `FindObjectsOfType`).
  A CPU-side finding must beat that evidence before anything is "optimised".
- Editor play mode inflates CPU time, so **all quoted numbers come from the player**, not the editor.
- Domain reload is off: statics survive play sessions, so `PerfCapture` resets its own counters per run.
- Week 15 lesson (DEF-15-04): verify the artefact, not the success message - the .exe + `_Data` folder for
  Windows, and `classes.dex` at the root for the APK.

## Steps
- [x] 1. Add `PerfCapture` + `PerfScenario` + `ProfilerRun`, and prove the pipeline with a 10 s smoke run that
      writes a CSV with plausible rows and a summary `Debug.Log`. [depends: none]
      Progress: done, smoke run wrote 3,735 rows. Three instrumentation defects were found later and fixed:
      DEF-16-02 - Unity 6 publishes no `Render/Draw Calls Count`; draw calls and batches are now summed from
      `SRP Batcher Draw Calls Count`, `Standard Draw Calls Count`, `Standard Instanced Draw Calls Count` and the
      three batch counters (names read out with the new editor tool `Tools/Week16/Dump Render Markers`), so
      `draw_calls`/`batches` finally hold numbers instead of `-1`. DEF-16-03 - `FrameTiming.gpuFrameTime`
      occasionally returns an uninitialised value (one run averaged 64,049,859 ms of "GPU" time); anything over
      1000 ms is now written as `-1`. Every CSV also carries a per-segment line recording the quality settings and
      frame cap that segment actually ran under, because the header alone could not prove it.
- [x] 2. Build the Windows 64-bit development player (`manage_build`, windows64, development, 1920x1080
      windowed, `-perf`) from the untouched tree; record editor version, player bytes/md5, resolution, quality
      level and launch arguments in `Evidence/environment.txt`. [depends: 1]
      Progress: done, 181.41 MB, 15-28 s incremental rebuilds. DEF-16-01 - the project had never compiled for
      Windows: `GameFeedback.cs` called `Handheld.Vibrate()` unguarded, now behind `#if UNITY_ANDROID || UNITY_IOS`.
      Also settled the open quality-level question: `Mobile` excludes Standalone in `QualitySettings.asset`, so a
      Windows player has exactly ONE level, "PC", pointing at `PC_RPAsset` (render scale 1.0, 50 m main-light
      shadows). Profiling that on the desktop would have measured a path heavier than the phone. The perf player
      is therefore built with the "PC" level temporarily re-pointed at `Mobile_RPAsset` and restored immediately
      afterwards - see `Docs/tools/PerfMobileBuild.cs`; the CSV header and a `[Perf] quality:` log line both name
      the pipeline and render scale so the swap can be checked from the evidence.
- [x] 3. Baseline: three 60 s runs on that player, one CSV each, then the same scenario profiled in the editor
      to read the call stacks behind the worst frames (`get_worst_cpu_frames`, `get_worst_gc_frames`,
      `manage_profiler` stats + Frame Debugger). Publish the bottleneck table: each row with the number that
      proves it and the candidate ranks. [depends: 2]
      Progress: the three baselines are captured and verified (`perf_baseline_run{1,2,3}_20261118_2316/2317/2318`,
      headers declare `Mobile_RPAsset, render scale 0.80, main-light shadows off`, declared segment counts match
      the per-segment lines): 0.67-0.69 ms mean, 1.09-1.13 p95, 9.4-11.7 worst, 1,451-1,491 fps, 0.66-0.68 ms
      main thread, 239.2-240.0 B GC per frame, 128.0-135.5 draw calls (216-226 max), ~15.9k triangles max, 241 MB,
      0.49-0.51 ms GPU across 7-8 segments per run. Noise band is tight: mean spread 0.02 ms, p95 spread 0.04 ms,
      GC spread 0.83 B, draw calls spread 7.5. Spike shape over 3 ms: 21 frames in 92,997, scattered (only 3 of 21
      near a segment boundary), so there is no periodic hitch to chase. Remaining for this step: the published
      bottleneck table from the editor-side call stacks and the Frame Debugger.
      Correction: the pipeline swap tool is `Assets/Editor/PerfBuildPipeline.cs` (menu Tools > Week16 > Perf Build);
      the earlier `Docs/tools/PerfMobileBuild.cs` note above is superseded - a file outside `Assets` cannot register
      a menu item, and the script runner could not compile it.
      Progress: bottleneck table published - `Docs/Zedrich Abrera/Week_16/Evidence/bottleneck_table.md`.
      Method note: the editor's profiler frame buffer proved unusable in Unity 6 (`ProfilerDriver.enabled`
      read False even after the tool reported success, and first/last frame index stayed -1), so the
      call-stack half of this step was replaced by the Frame Debugger plus six new memory-counter recorders
      added to PerfCapture and measured in the PLAYER. Deviation from the step as written, but it produced
      better evidence than the editor could. Findings: main thread 0.63 ms (1.9 % of the 33.3 ms budget), GPU
      0.49 ms (1.5 %), 17 draw events/frame, ~9.7 k triangles, 324 B GC/frame - the tested area already meets
      the target by a wide margin. Two defects found: DEF-16-04, the draw_calls column sums `SRP Batcher Draw
      Calls Count`, which counts renderers SUBMITTED to the batcher rather than draw calls, and over-reports
      ~8x (CSVs now carry a header line retiring the column); DEF-16-05, the editor reported 1,355 textures /
      222 MB while the project ships 9 textures and the player loads 82 - editor and package texture data,
      never to be quoted. The one piece of real waste found: four 2048x2048 BaseColor maps on generated props
      cost 21.3 MB of runtime texture memory.
- [x] 4. Implement **only** bottleneck #1 in the file the evidence names, with `Debug.Log` markers around the
      change, then rebuild the player with byte-identical settings (resolution, quality, arguments).
      [depends: 3]
      Progress: done without a code change, because the evidence named a texture import setting rather than a
      script. `maxTextureSize` 2048 -> 512 on the CrateStack, PalengkeStall, SchoolBench and Tricycle
      BaseColor maps, compression and mipmaps untouched. No `Debug.Log` markers were needed for the same
      reason; the CSV header and the memory columns identify the build. Rebuilt with byte-identical settings
      (windows64, development, same swap/restore cycle): 181.41 MB -> 171.41 MB, 0 errors, and the project's
      PC level was restored to PC_RPAsset and confirmed on disk after both builds.
- [x] 5. Re-measure three 60 s runs, compare mean / p95 / worst-frame ms, GC bytes/frame, draw calls, memory
      and build size against the baseline, and keep or revert on that comparison; then repeat once for
      bottleneck #2 - or record the honest "already meets the target" finding if the numbers refuse to move.
      [depends: 4]
      Progress: kept. Measured deterministically: the four textures 21,849 KB -> 1,369 KB (-20.0 MB, same
      `Profiler.GetRuntimeMemorySizeLong` call both times) and the build artefact -10.0 MB (-5.5 %).
      NOT claimed: any frame-time change. Three post-fix sets were taken (fix1, fix1b, fix1c, 9 runs) and they
      degrade monotonically, 0.68 -> 0.70 -> 2.07 ms mean with worst frames to 61 ms, while the baseline sat at
      0.62-0.63 ms with a 0.01 ms spread. The cause is host contention, not the change: `Get-Process` shows a
      `Client-Win64-Shipping` process holding 5.4 GB alongside several Unity, Chrome and Python processes. A
      texture downsample cannot triple main-thread time, and GPU ms drifted the same way (0.49 -> 0.60 ->
      0.71). p95 stayed inside 33.3 ms even under that load (worst p95 11.23 ms), so the budget conclusion is
      unaffected. One clean post-fix set on an idle machine is still outstanding and must be taken before AS3
      quotes any after-frame-time number.
      Correction to an earlier reading: the four textures are NOT in Game.unity (CrateStack/PalengkeStall/
      Tricycle are in Map_Palengke, SchoolBench in Map_SchoolCampus), so the capture never loads them and the
      player still reports 44.8 MB of texture memory after the fix - that 44.8 MB belongs to Game.unity's own
      engine/URP/font resources. The first draft of the bottleneck table attributed one scene's footprint to
      another scene's assets; corrected in section 6 of the evidence document, and the error is kept on record
      because a single-scene capture invites exactly that mistake.
      Second bottleneck: recorded as the honest "already meets the target" finding this step allows. The only
      remaining candidates are gfx memory (62.3 MB) and ~125 MB of native/unattributed, neither reachable
      without a visual trade-off (HDR, render scale) or an engine-level change.
- [x] 6. Regression gate: EditMode 17/17, the 16-case play-mode suite 16/0/0, accessibility audit still
      423/0/24; save the logs under `Evidence/test_logs_regression/`. [depends: 5]
      Progress: PASS on all three, but only after one real failure was found and fixed. The 16-case
      play-mode suite is `Assets/Editor/QualityRun.cs` (menu Tools > Week15 > Run Quality Scenarios), NOT
      an NUnit assembly - that is why only 3 asmdefs exist and no PlayMode test assembly was found. Run
      with label `week16_regression`: 16 passed / 0 failed / 0 blocked, 0 exceptions, 71 [Feedback]
      lines. Accessibility re-run: 423 passed / 0 failed / 0 warnings / 24 not measurable, identical to
      Week 15. EditMode first run was 16/17 - `PlayerProfileTests.MissingSave_LoadsDefaultProfile_-
      WithoutAnError` failed with a stale "the file is empty". Proved order-dependent (the same test
      alone passes), traced to `PlayerProfile.Load()` never clearing `lastError`, and fixed in the
      PRODUCT (`lastError = "";` at the top of Load, one line + comment); re-run 17/17. No test file was
      edited - every file under Assets/Tests/EditMode still carries its Week 15 timestamp, and the
      `dir /T:W` output is pasted into the log. Evidence: `Evidence/test_logs_regression/`
      (`editmode_regression.txt`, `scenario_regression.tsv` + `_summary.txt`, `a11y_regression.log`,
      `regression_gate_summary.md`). The suite ran under the same host contention as the perf runs (a
      Client-Win64-Shipping process at 4.56 GB) and still passed, so the pass is not timing-fragile -
      but nothing in the gate is quoted as a performance number.
- [x] 7. Write SA6, AS3 (before/after metrics, change log, trade-offs, regression, commit ID) and the
      reflection against the four module prompts, extend `MdToHtml.cs`, regenerate HTML + PDF, build the final
      APK/EXE, then make the single end-of-week commit and paste its id into AS3. [depends: 6]
      Progress: done, in four parts.
      (a) Documents. `Abrera_Zedrich_SA6.md` (10 sections), `Abrera_Zedrich_AS3.md` (9 sections) and
      `Abrera_Zedrich_Week16_Reflection.md` (the four module prompts, first person, ending with the exact
      required declaration) written; two hand-authored SVGs (`frame_budget.svg`, `memory_fix.svg`) inlined
      into SA6 section 5 and AS3 section 3.2 with `<!--INLINE:...-->`; HTML regenerated with `MdToHtml` and
      PDF printed from that HTML with headless Edge. One rendering defect (a `>` blockquote printing
      literally in SA6 section 1) was caught by screenshotting the HTML and fixed.
      (b) Builds. The optimised APK is `Docs/Zedrich Abrera/Week_16/Abrera_Zedrich_AS3.apk`, 68.30 MB on
      disk, `classes.dex` at the root plus `classes2/3/4.dex` and `lib/arm64-v8a/`, 0 build errors. The
      shipped Windows perf player is 171.41 MB and was launched once from a clean start, writing a CSV with
      the correct `# render path:` header. The build tool's own `total_size_mb 846.81` is not the artefact
      size and is not quoted in any document.
      (c) Commit. **`55b043a`** - "Week 16: profile the shipping render path, cut prop texture memory, fix
      profile error leak", 421 files, +1,648,822 / -2,421, made as the single end-of-week commit.
      (d) Closing the loop, because a commit cannot contain its own id. Follow-up commit **`a9c26c3`** puts
      `55b043a` into AS3 section 8 and reprints its HTML and PDF. It also repairs a real mistake made while
      collecting evidence: `set "DST=..." & mkdir "%DST%"` on one command line does not expand, so
      `scenario_regression.tsv`, `scenario_regression_summary.txt` and `a11y_regression.log` were copied into
      a repo-root folder literally named `%DST%` and the first two were committed there. All three were
      verified against the figures `regression_gate_summary.md` quotes, moved into
      `Evidence/test_logs_regression/` (now 5 files, matching AS3), and the stray folder removed; git
      recorded the two moves as 100 % renames. `a11y_regression.log` stays untracked because `.gitignore`
      line 24 ignores `*.log` - the same as every earlier week's audit log - so it ships in the submitted
      folder but not in the repository.

## Verify
- `Evidence/raw_csv/` holds 3 baseline + 3 post-fix runs per configuration, each with the scenario definition
  in its header, and the CSVs reproduce the numbers quoted in SA6/AS3.
- At least one metric improves beyond run-to-run noise (state the noise band from the three baseline runs)
  and p95 frame time stays inside 33.3 ms; if nothing moves, the report carries a measured "target already
  met" finding with the same raw data behind it.
- Regression logs show 17/17 EditMode, 16/0/0 scenarios, 423/0/24 accessibility; no test was edited to make
  the build pass.
- The Windows artefact opens from a clean launch with `-perf` and writes a CSV; the APK starts with
  `classes.dex` at its root (DEF-15-04 check).
- Every image in the three HTML documents is an inline `data:` URI and the PDFs are generated from those
  HTML files, so nothing has to be pasted in by hand.
