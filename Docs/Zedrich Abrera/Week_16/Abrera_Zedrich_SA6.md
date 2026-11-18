# SA6 - Game Performance and Resource Analysis

**Abrera, Zedrich Zaivan B.** · MOBILE DEV 002 - Advanced Mobile Application Development, Module 5, Week 16
**Game:** Agawan Base v0.2 - 3D mobile capture-the-flag, two teams of two to four
**Build analysed:** Windows x64 development player, `Builds/Week16/AgawanBasePerf.exe`, 181.41 MB (pre-fix)
**Engine:** Unity 6000.6.0f1, URP 17.6.0
**Target:** 30 fps, i.e. a **33.3 ms** frame budget
**Source of every number below:** `Week_16/Evidence/raw_csv/` (per-frame CSVs), `Week_16/Evidence/player_logs/` (the same runs' log summaries), and `Week_16/Evidence/bottleneck_table.md`

---

## 1. The question this analysis had to answer

The Week 15 build passed its tests but had never been measured. Nobody knew whether Agawan Base ran
at 60 fps or 12, and "it feels fine on my laptop" is not an answer a report can use. So the question
was deliberately narrow:

**On one fixed scenario, on one fixed machine: what is actually costing time and memory in the frame,
and which of those costs is worth changing?**

Two constraints shaped the method. First, **the 30 fps cap hides spikes**: `GameManager.Awake` sets
`Application.targetFrameRate = 30` and `vSyncCount = 0` on every scene load, so a 60 ms frame and a
33 ms frame look identical in frame *interval*. Capture therefore reads Unity's `PlayerLoop` profiler
counter, which is cap-independent, and a `-perf-capped` switch exists to keep the cap when the cap
itself is the thing under test. Second, **the editor lies about performance and memory**, so every
quoted number comes from the player, not from play mode (section 4.2 proves the memory half of that
claim with a number).

## 2. The controlled scenario

The scenario is code, not a note in a document: `Assets/Scripts/Diagnostics/PerfScenario.cs` sets it
up and `Assets/Scripts/Diagnostics/PerfCapture.cs` records it into every CSV as header lines, so a
file can be checked against the conditions it claims.

| Condition | Value |
|---|---|
| Scene | `Game.unity`, every segment |
| Teams | 4v4 at HARD - 1 human + 3 Blue AI against 4 Red AI |
| AI | fully active; AI random seed **20261118** |
| Human input | a fixed 4-waypoint loop between the two flags, walking, no sprint; the virtual stick is driven through reflection so the input path is the real one |
| Match handling | a paused match is auto-resumed; a finished match restarts as a **new segment** |
| Duration | 60 s of live match per run, 7-8 segments per run |
| Quality | level 0 `PC` with the Mobile level's values applied by name, because the Mobile *level* excludes Standalone (see 4.1) |
| Render path | `Mobile_RPAsset, render scale 0.80, main-light shadows off` - declared in each CSV's own header |
| Frame cap | uncapped, re-applied and recorded per segment |
| Build | Windows x64, development, Mono |
| Machine | Ryzen 5 5600G (12 threads), GTX 1070, 15 GB RAM, 1920x1080, Windows |

A segment is one live match, and a segment boundary is a scene reload. That matters because the
reload re-applies the frame cap and the quality settings, so the per-segment line is evidence that
the whole file ran under the same settings rather than only its first second.

### 2.1 Why this scenario, and what it does not cover

It is repeatable (seeded, scripted input, fixed scene) and it is the game's actual core loop: AI
moving, flag states changing, the HUD updating, the human walking. What it does **not** cover is
anything that only happens on a map transition: `Map_Palengke` and `Map_SchoolCampus` never load in
this capture, and neither do the props that live in them. That limitation is not a footnote - it
produced a real error in this week's analysis, recorded in section 7.2.

## 3. Baseline captures

### 3.1 The three runs

`perf_baseline2_run{1,2,3}_20261118_2331/2332/2333.csv` - 94,297 / 93,501 / 94,829 per-frame rows,
7 / 8 / 7 segments declared and 7 / 8 / 7 per-segment lines present, all three declaring the shipping
render path.

| Metric | run 1 | run 2 | run 3 | Spread |
|---|---|---|---|---|
| Main thread, mean ms | 0.63 | 0.63 | 0.62 | 0.01 |
| Main thread, p95 ms | 0.98 | 0.99 | 0.97 | 0.02 |
| Main thread, worst ms | 10.67 | 9.61 | 9.80 | 1.06 |
| Frame interval, mean ms | 0.64 | 0.64 | 0.63 | 0.01 |
| Uncapped fps | 1,573 | 1,560 | 1,582 | 22 |
| GPU ms | 0.49 | 0.50 | 0.49 | 0.01 |
| GC bytes/frame | 324 | 330 | 324 | 6 |
| Total memory MB | 245.2 | 245.6 | 244.7 | 0.9 |
| Texture memory MB | 44.8 | 44.8 | 44.8 | 0.0 |
| Gfx memory MB | 62.3 | 62.3 | 62.3 | 0.0 |
| Textures loaded | 82 | 82 | 82 | 0 |

**Any change smaller than the spread in its row is noise.** That is the rule the rest of the week is
held to. Texture memory and gfx memory are deterministic to the kilobyte, so movement there is real
even when it is small.

### 3.2 A second baseline set, and why it exists

These three runs were captured while the host machine was carrying an unrelated 5.4 GB process. Their
numbers looked clean (0.62-0.63 ms, 0.01 ms spread), but "looks clean" is not the same as "measured
under a controlled host". Later in the week six further runs on the same build degraded to 2.07 ms
mean purely from host load, which proved the host can move this game's main-thread number by 3x.

So a second baseline was captured on an idle host, in the same sitting as the post-fix set, from an
**A/B build**: the four textures this analysis targets were returned to their original import
settings and the player rebuilt (181.41 MB, exactly the pre-fix artefact size). That gives a
before/after pair whose only difference is the change under test:

`perf_baselclean_run{1,2,3}_20261119_0006/0007/0009.csv`

| Metric | run 1 | run 2 | run 3 | Spread |
|---|---|---|---|---|
| Main thread, mean ms | 0.59 | 0.60 | 0.60 | 0.01 |
| Main thread, p95 ms | 0.86 | 0.87 | 0.86 | 0.02 |
| Frame interval, mean ms | 0.60 | 0.60 | 0.61 | 0.01 |
| Uncapped fps | 1,660 | 1,653 | 1,643 | 17 |
| GPU ms | 0.48 | 0.48 | 0.49 | 0.01 |
| GC bytes/frame | 316 | 318 | 318 | 2 |
| Total memory MB | 245.5 | 245.7 | 245.1 | 0.6 |
| Texture memory MB | 44.8 | 44.8 | 44.8 | 0.0 |

The two baselines differ by 0.03 ms of main thread while each is internally stable to 0.01. That
gap is the host, not the game, and it is the reason section 8 refuses to rank a 0.03 ms change as a
bottleneck.

### 3.3 What one CSV row contains

`segment, frame, elapsed_s, interval_ms, main_thread_ms, gpu_ms, gc_bytes, draw_calls, batches,
triangles, total_memory_mb, texture_mb, mesh_mb, gfx_mb, gc_heap_mb, reserved_mb, texture_count`

Per frame, for 60 s, from 14 `ProfilerRecorder`s (`Internal/PlayerLoop`, `Memory/GC Allocated In
Frame`, `Memory/Total Used Memory`, `Memory/Texture Memory`, `Memory/Mesh Memory`,
`Memory/Gfx Used Memory`, `Memory/GC Used Memory`, `Memory/Total Reserved Memory`,
`Memory/Texture Count`, `Render/Triangles Count`, the three draw-call counters and the three batch
counters). Two columns had to be retired or caveated before any of it could be trusted - section 6.

## 4. Evidence that is not a CSV: rendering and the render path

### 4.1 Which render pipeline was actually running

A Windows player was not necessarily profiling what ships to a phone. `QualitySettings.asset` has two
levels, `0 = Mobile` and `1 = PC`, and the Mobile level **excludes Standalone** - so a desktop player
has exactly one level, `PC`, pointing at `PC_RPAsset` (render scale 1.0, 50 m main-light shadows).
Measuring that would have measured a path heavier than the phone's.

The perf player is therefore built with the `PC` level temporarily re-pointed at `Mobile_RPAsset`
(menu `Tools > Week16 > Perf Build`, `Assets/Editor/PerfBuildPipeline.cs`), then the setting is
restored immediately and the restore is confirmed on disk (`ProjectSettings/QualitySettings.asset`
line 105 back to guid `4b83569d...`). Every CSV's header names the pipeline and the render scale, so
the swap can be checked from the evidence rather than believed.

### 4.2 Editor numbers are not evidence (DEF-16-05)

Profiling the same scene in the editor reported **1,355 textures and 222 MB of texture memory**. The
project ships **nine** texture assets; the player loads **82** for 44.8 MB. The editor's counters
include editor-only and package assets. Every memory number in this report therefore comes from the
player, and the editor was used only to explain a number, never to supply one.

### 4.3 Draw calls, from the Frame Debugger (DEF-16-04)

The harness's `draw_calls` column reported ~132 per frame. That number is wrong. The Frame Debugger
enumerates a live match frame as **17 render events** - 6 opaque, 4 transparent, 1 blit, 5 UI - and a
second capture of the same scenario gave 20. The 0.63 ms main-thread time independently rules out 132
submissions.

The mechanism: the column sums three split counters and `SRP Batcher Draw Calls Count` counts
**renderers submitted to the batcher**, not draw calls. A run's own split line reads
`SRP 116 + standard 6 + instanced 0`: 116 renderers submitted, which static and SRP batching then
collapse into a handful of GPU draws. Every CSV now carries a header line retiring the column so it
cannot be lifted out of the evidence by mistake, and the batches column (3-4 static) corroborates the
Frame Debugger's magnitude.

## 5. The bottleneck table

Full version with method and caveats: `Week_16/Evidence/bottleneck_table.md`. Ranked by measured
cost against the 33.3 ms budget:

| # | Candidate | Measured | % of 33.3 ms | Verdict |
|---|---|---|---|---|
| 1 | Main-thread CPU | 0.59-0.63 ms mean, 0.86-0.99 ms p95 | **1.8-1.9 % / 2.6-3.0 %** | Already meets target, ~50x headroom |
| 2 | GPU | 0.48-0.50 ms | **1.5 %** | Already meets target, ~68x headroom |
| 3 | Draw calls / batches | 17 render events; 3-4 static batches | negligible | Already meets target |
| 4 | Triangles | ~9.7 k average, 16.0 k peak | negligible | Already meets target |
| 5 | Managed allocation | 316-330 B/frame | ~10 KB/s at 30 fps | Already meets target |
| 6 | Total memory | 245 MB: gfx 62.3 + texture 44.8 + managed ~12 | n/a | Worth attention on a phone |
| 7 | Worst frames | 6.9-10.7 ms, rare, not periodic | 21-32 % when it happens | Investigated, dismissed |

### 5.1 The headline finding

**The scene the game actually plays on already meets the target by a wide margin.** 0.6 ms of
main-thread work against a 33.3 ms budget is under 2 %. That is a measured finding, not a failure to
find something, and the module explicitly accepts it ("present a technically valid finding that the
tested area already meets the defined target").

But a table of "everything is fine" would be a lazy answer, so the analysis went one level down: the
target is met on this desktop, and the thing that does not transfer to a phone is **memory**, not
CPU time.

<!--INLINE:frame_budget.svg-->

*Figure 1 - the 33.3 ms budget with the measured marks on it: at full scale, and magnified to 1 ms so the marks are visible at all. The scene uses 1.8 % of the budget for the main thread and 1.5 % for the GPU.*

### 5.2 Why the rare 10 ms frames were not the fix target

21 frames over 3 ms in 92,997, scattered, only 3 of them near a segment boundary, no periodic
spacing. A spike that fires twice a minute inside a 60 s capture cannot be distinguished from noise,
and no code change could be shown to have fixed it. Chasing it would have produced a claim the data
cannot support, so it was dismissed on evidence rather than ignored.

## 6. Instrumentation defects found before the numbers could be trusted

A measurement tool that lies is worse than no tool, because it produces confident wrong decisions.
Five defects were found and fixed or retired this week:

| ID | Defect | Consequence |
|---|---|---|
| DEF-16-01 | `GameFeedback.cs` called `Handheld.Vibrate()` unguarded; the project had never compiled for Windows | no player could be built at all until it was behind `#if UNITY_ANDROID \|\| UNITY_IOS` |
| DEF-16-02 | Unity 6 publishes no `Render/Draw Calls Count`; the columns read `-1` | draw calls and batches are now summed from the three split counters; marker names were read out with `Tools/Week16/Dump Render Markers` |
| DEF-16-03 | `FrameTiming.gpuFrameTime` occasionally returns an uninitialised value (one run averaged 64,049,859 ms of "GPU" time) | anything over 1000 ms is now written as `-1` |
| DEF-16-04 | `draw_calls` over-reports ~8x (section 4.3) | column retired in every CSV header |
| DEF-16-05 | editor memory counters are editor/package data (section 4.2) | editor memory figures are never quoted |

The editor-side profiler frame buffer was also abandoned: `ProfilerDriver.enabled` read `False`
after the tool reported success, `firstFrameIndex` / `lastFrameIndex` stayed `-1` with or without an
open Profiler window, and both call-stack queries returned "No valid frame data found in profiler."
The call-stack half of the planned method was replaced by the Frame Debugger plus six new memory
recorders, which produced better evidence than the editor would have. That is a deviation from the
plan and is recorded as one in AS3.

## 7. Where the real waste is

### 7.1 A texture budget spent where nobody can see it

The project ships **nine** texture assets. Five are 2048x2048 JPEG BaseColor maps belonging to the
generated props in `Assets/Generated_Models/`. Measured with `Profiler.GetRuntimeMemorySizeLong`,
four of them cost **21,849 KB (21.3 MB)** of runtime texture memory - about 5.4 MB each once mipmaps
are counted - for a school bench, a tricycle, a palengke stall and a crate stack. These are low-poly
objects a few metres across, seen from a third-person camera on a phone screen.

That is the only finding in this analysis that can be fixed with **no visual trade-off**, which is
what makes it priority 1 (section 8).

### 7.2 The error I made, and kept on record

The first version of the bottleneck table ranked "texture memory 44.8 MB" as the top item and implied
those four textures were part of it. The post-fix measurement disproved it: after removing 20 MB of
textures the player still reported 44.8 MB and 82 textures. Checked per scene with
`AssetDatabase.GetDependencies`, `CrateStack`, `PalengkeStall` and `Tricycle` are used by
`Map_Palengke`, `SchoolBench` by `Map_SchoolCampus`, and **none of the four by `Game.unity`** - because
the capture only ever loads `Game.unity`, it never loads them.

So the 44.8 MB belongs to `Game.unity`'s own resources - engine and URP defaults, font atlases, and
the handful of project textures that scene uses - and the props' 21.3 MB only exists when a map scene
loads. Attributing one scene's footprint to another scene's assets is exactly the mistake a
single-scene capture invites; it is in section 6 of the bottleneck table because a report that hides
its own correction teaches nothing.

## 8. Optimisation priorities, and why each one was ranked where it is

| Priority | Candidate | Decision | Justification |
|---|---|---|---|
| **1** | The four 2048 prop BaseColor maps to 512 | **implemented** | -20.0 MB of runtime texture memory and -10.0 MB of shipped artefact on textures nobody can resolve at gameplay distance; no visual trade-off at the scales the game presents them; no code change, so no new bug surface |
| 2 | Gfx memory, 62.3 MB | **not attempted** | reaching it means HDR off or render scale below 0.8 - both are visible quality losses taken to buy time the game does not need, since it already runs at 1.8 % of budget |
| 3 | Managed allocation, ~320 B/frame | **not attempted** | ~10 KB/s at 30 fps. A pooling change here would be a large refactor of code that Week 15's 16-case suite covers, to remove GC pressure that is already negligible |
| 4 | Draw calls, 17 events/frame | **not attempted** | already batched; the remaining events are opaque/transparent/UI passes that cannot be merged without changing how the game draws |
| 5 | The rare 10 ms frames | **not attempted** | not reproducible inside a 60 s window (5.2) |
| 6 | Loading time | **not measured** | out of this scenario's scope; listed as a gap, not as a pass |
| 7 | Standing risk on a phone | **documented** | nothing here can be measured on hardware this week (section 9) |

The ranking rule was: *a change is only worth making if the number that justifies it is bigger than
the noise band of the metric it improves, and the change costs nothing the player can see.* Exactly
one candidate passed that test, which is why there is one fix rather than five.

## 9. What this analysis does not prove

- **It is not a phone measurement.** Every number is a desktop number on a GTX 1070. A mid-range
  Android device will show a main-thread time several times larger and a GPU time an order of
  magnitude larger. The 50x headroom found here is what absorbs that difference; it is not evidence
  that a specific phone hits 30 fps.
- **It covers one scene.** Map transitions, the other two maps and their props are outside the
  capture - which is precisely what produced the section 7.2 error.
- **Host state moved the main thread by 3x** in this week's measurements. That is why the final
  before/after comparison was taken on an idle host with an A/B build (AS3, section 3).
- **GC bytes are only comparable between identically instrumented builds.** Adding six recorders this
  week raised the measured figure from ~240 B to ~324 B per frame; the instrumentation is part of the
  measurement.
- **Loading time and battery use are not measured at all.** The module lists them as relevant metrics.

## 10. Evidence index

| File | What it is |
|---|---|
| `Evidence/bottleneck_table.md` | the ranked table with method, caveats and the correction |
| `Evidence/raw_csv/perf_baseline2_run{1,2,3}_20261118_2331*.csv` | first baseline set, 60 s each |
| `Evidence/raw_csv/perf_baselclean_run{1,2,3}_20261119_0006*.csv` | baseline from the A/B build, idle host |
| `Evidence/raw_csv/perf_fix1clean_run{1,2,3}_20261119_0000*.csv` | the shipped build, idle host |
| `Evidence/raw_csv/perf_fix1*_/fix1b*/fix1c*` | the contaminated sets, kept as the record of a failed method |
| `Evidence/raw_csv/perf_shipcheck_20261119_001031.csv` | the shipped `.exe` launched clean and wrote a CSV |
| `Evidence/player_logs/*.log` | each run's own summary: main thread, GPU, draw split, memory breakdown |
| `Evidence/test_logs_regression/` | the Week 16 regression gate (see AS3) |
| `Assets/Scripts/Diagnostics/PerfCapture.cs`, `PerfScenario.cs` | the recorder and the scenario |
| `Assets/Editor/ProfilerRun.cs`, `PerfBuildPipeline.cs`, `PerfMarkerProbe.cs` | menus: run the scenario, build the perf player with the right pipeline, dump recorder names |

*SA6 · Agawan Base v0.2 · Zedrich Zaivan B. Abrera · Week 16*
