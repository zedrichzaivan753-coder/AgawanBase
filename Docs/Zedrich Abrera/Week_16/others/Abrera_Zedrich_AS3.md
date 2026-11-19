# AS3 - Testing, Optimisation and Evaluation Report

**Abrera, Zedrich Zaivan B.** · MOBILE DEV 002 - Advanced Mobile Application Development, Module 5, Week 16
**Game:** Agawan Base v0.2 - 3D mobile capture-the-flag, two teams of two to four
**Engine:** Unity 6000.6.0f1, URP 17.6.0
**Target:** 30 fps / 33.3 ms per frame
**Analysis behind this report:** `Abrera_Zedrich_SA6.md`
**Repository:** `d08fc9b` was HEAD at the start of the week

---

## 1. What was delivered

| Artefact | Path | Size | State |
|---|---|---|---|
| Optimised device build | `Docs/Zedrich Abrera/Week_16/Abrera_Zedrich_AS3.apk` | **68.30 MB** (71,617,384 bytes), `classes.dex` at APK root | built, 0 errors |
| Optimised profiling build | `Builds/Week16/AgawanBasePerf.exe` (+ `_Data`) | **171.41 MB** | launched clean, wrote a CSV |
| Pre-fix A/B build | `Builds/Week16_ab/AgawanBasePreFix.exe` | **181.41 MB** | built for the same-host comparison only |
| Raw measurements | `Week_16/Evidence/raw_csv/` and `player_logs/` | 22 CSVs, 20 logs | every quoted number traces here |
| Regression logs | `Week_16/Evidence/test_logs_regression/` | 5 files | 17/17, 16/0/0, 423/0/24 |

The APK is the build a phone installs (IL2CPP, ARM64 only, API 26+). The `.exe` is the build the
measurements came from, because Week 16's approved profiling environment is a Windows standalone
development build - no Android device was attached this week.

## 2. Method, and the one time the method failed

The module asks for one change at a time so its effect can be examined. One change was made, so this
was easy to honour:

1. Measure a baseline (`perf_baseline2_run{1,2,3}`).
2. Make **one** change, rebuild with byte-identical settings, re-measure (`perf_fix1_run{1,2,3}`).
3. **Fail the comparison.** Those three runs, and six more (`fix1b`, `fix1c`), degraded monotonically
   from 0.68 to 2.07 ms mean while the baseline sat at 0.62-0.63 ms with a 0.01 ms spread. GPU time
   drifted the same way (0.49 -> 0.60 -> 0.71 ms). The host was carrying an unrelated
   `Client-Win64-Shipping` process holding 5.4 GB. A texture downsample cannot triple main-thread time,
   and the GPU moving with it is what a busy GPU looks like.
4. Instead of publishing a number from a busy machine, **build the A/B pair and re-measure both on an
   idle host.** The four textures were returned to their original import settings, the player rebuilt
   (181.41 MB - exactly the pre-fix artefact size), and then both builds were measured minutes apart:
   `perf_baselclean_run{1,2,3}` and `perf_fix1clean_run{1,2,3}`.

Step 4 is the part worth defending. A contaminated measurement is not a measurement, and the only way
to know which of two numbers was contaminated is to take both again under the same conditions.

## 3. Before and after

### 3.1 Deterministic metrics - what the change actually did

| Metric | Before | After | Delta | How measured |
|---|---|---|---|---|
| Imported size of the four maps | 2048x2048 | **512x512** | - | `TextureImporter` / `Texture2D.width` |
| Their runtime memory (editor, same API both times) | 21,849 KB | **1,369 KB** | **-20.0 MB** | `Profiler.GetRuntimeMemorySizeLong` |
| Windows player artefact | 181.41 MB | **171.41 MB** | **-10.00 MB (-5.5 %)** | same build tool, same settings, both counted on disk |
| Files in the player | 334 | 334 | 0 | `Get-ChildItem -Recurse` count |
| Compression, mipmaps | Compressed, on | Compressed, on | unchanged | importer settings |

These are the numbers the change is claimed on. They are deterministic: the same API measured them
both times, they do not depend on host load, and the deltas are 10x larger than any run-to-run
variation observed this week.

### 3.2 Frame time - before and after on the same idle host

| Metric, 3-run mean | A/B pre-fix | Optimised | Delta |
|---|---|---|---|
| Main thread, mean ms | 0.60 | 0.60 | **0.00** |
| Main thread, p95 ms | 0.86 | 0.88 | +0.02 (spread 0.02-0.05) |
| Frame interval, mean ms | 0.61 | 0.61 | 0.00 |
| Uncapped fps | 1,652 | 1,640 | -12 (fps spread alone is 23) |
| GPU ms | 0.48 | 0.48 | 0.00 |
| GC bytes/frame | 317 | 313 | -4 (recorder noise) |
| Total memory MB | 245.4 | 246.7 | +1.3 (see 3.3) |
| **% of the 33.3 ms budget, main thread** | **1.8 %** | **1.8 %** | - |

**The change does not move frame time, and this report does not claim that it does.** `Game.unity`
does not reference the four textures, so the measured scenario never samples them. The earlier
0.63 -> 0.60 comparison in the contaminated sets was host state, and the A/B pair proves it.

That is the honest result, and it still satisfies the module's completion checkpoint twice over:
there **is** a measurable improvement (-20.0 MB of runtime texture memory, -10.00 MB of shipped data),
and there **is** a technically valid finding that the tested area already meets the target (1.8 % of a
33.3 ms budget, ~50x headroom).

<!--INLINE:memory_fix.svg-->

*Figure 1 - the A/B pair: what the change reduced, and what it deliberately left alone. Frame time sits on the same chart as the memory numbers because that is the finding of the week - the fix is real, and it is not a frame-time fix.*

### 3.3 One result that runs the wrong way

In the same A/B pair the optimised build reports **89 textures / 45.3 MB** against the pre-fix build's
**82 / 44.8 MB**, consistently across all three runs of each set. The four maps cannot explain it -
`Game.unity` does not load them - and this harness does not enumerate textures, so the cause is
unknown. It is printed here rather than quietly dropped, because a before/after table that only moves
in the author's favour is not evidence. The fix's memory benefit is claimed where the textures live:
the import settings, the runtime size measured directly, and the build artefact.

## 4. Change log

One functional change, one defect fix found by the regression gate, plus the instrumentation that
made any of it measurable.

### 4.1 The optimisation

| File | Change |
|---|---|
| `Assets/Generated_Models/CrateStack/CrateStack_BaseColor.jpg` | `maxTextureSize` 2048 -> **512**, `maxTextureSizeSet` on the default platform; compression `Compressed` and mipmaps untouched |
| `Assets/Generated_Models/PalengkeStall/PalengkeStall_BaseColor.jpg` | same |
| `Assets/Generated_Models/SchoolBench/SchoolBench_BaseColor.jpg` | same |
| `Assets/Generated_Models/Tricycle/Tricycle_BaseColor.jpg` | same |

No script changed for the optimisation. The evidence named an import setting, so the fix is an import
setting; a code change would have had to be invented to look like work.

### 4.2 The defect the regression gate found, and the fix

`Assets/Scripts/Core/PlayerProfile.cs`, `Load()`: added `lastError = ""` plus a comment. `Load()`
reset the state and the loaded flag but never cleared the last error, and the missing-file branch
returns before anything sets one - so a load could inherit the previous load's rejection reason. It
surfaced as an order-dependent EditMode failure (section 6.2) and was reachable in the game through
any load-see-a-rejection-load-again path. **No test was edited.**

### 4.3 Instrumentation added this week

| File | Purpose |
|---|---|
| `Assets/Scripts/Diagnostics/PerfCapture.cs` | `ProfilerRecorder`-based per-frame CSV, 14 counters, scenario conditions written into the header, per-segment quality/frame-cap lines, `-perf` command-line switches |
| `Assets/Scripts/Diagnostics/PerfScenario.cs` | the repeatable scenario: 4v4 HARD, seed 20261118, 4-waypoint loop driven through the real input path |
| `Assets/Editor/ProfilerRun.cs` | menus to run a 10 s smoke or 60 s capture and open the evidence folder |
| `Assets/Editor/PerfBuildPipeline.cs` | points the `PC` quality level at `Mobile_RPAsset` before a perf build and restores it after, with a status menu |
| `Assets/Editor/PerfMarkerProbe.cs` | dumps the real `ProfilerRecorder` marker names, which is how the draw-call counters were found |
| `Assets/Scripts/Core/GameFeedback.cs` | `Handheld.Vibrate()` behind `#if UNITY_ANDROID \|\| UNITY_IOS` - without it the project had never compiled for Windows (DEF-16-01) |

## 5. Trade-offs

### 5.1 Accepted

**Visual resolution of four prop textures, from 2048 to 512.** This is a real quality reduction and it
is justified three ways: the objects are a bench, a tricycle, a stall and a crate stack, seen from a
third-person camera a few metres away on a phone screen; both the Windows and Android builds use
`Mobile_RPAsset` at render scale 0.8, so the final on-screen resolution is already below 1080p; and
the alternative - leaving 21.3 MB of texture memory on four minor props - costs a mid-range phone
something real (resident memory, load time, eviction pressure) to protect detail nobody can resolve at
that distance. Mipmaps stay on and compression stays `Compressed`, so no new aliasing was introduced.

### 5.2 Rejected, with reasons

| Candidate | Why it was rejected |
|---|---|
| HDR off / render scale below 0.8 | Both are visible quality losses across the whole screen, taken to buy frame time the game already has (1.8 % of budget). The module requires a trade-off to be justified; this one buys nothing. |
| Disabling shadows | Already disabled on the Mobile path. |
| Object pooling / allocation work | ~320 B/frame is ~10 KB/s at 30 fps. A refactor of gameplay code covered by 16 regression cases, to remove GC pressure that does not exist. |
| Draw-call reduction | Already 17 render events with static and SRP batching; what remains are the passes that must happen. |
| Chasing the rare 10 ms frames | Not reproducible inside a 60 s window, so a change could not be shown to have fixed it. |
| A second optimisation for the report's sake | Would have meant inventing a bottleneck. The analysis found one, and one is what it fixed. |

## 6. Regression results

Run on the optimised tree, logs in `Week_16/Evidence/test_logs_regression/`.

| Suite | Required | Measured | Verdict |
|---|---|---|---|
| EditMode unit tests (17) | 17/17 | **17 passed / 0 failed** | PASS |
| Play-mode scenario suite (16) | 16/0/0 | **16 passed / 0 failed / 0 blocked**, 0 exceptions, 71 `[Feedback]` lines | PASS |
| Accessibility audit | 423/0/24 | **423 passed / 0 failed / 0 warnings / 24 not measurable** | PASS |

### 6.1 Where the suites live

The play-mode suite is `Assets/Editor/QualityRun.cs` (menu `Tools > Week15 > Run Quality Scenarios`),
not an NUnit assembly - it enters play mode and drives a real 4v4 match through the same public
methods the rules use, recording one TSV row per case. It was run with the label `week16_regression`;
the harness writes to `Week_15/Evidence/test_logs_week16_regression/` and the files in this week's
evidence folder are unedited copies. The 24 accessibility items are checks the audit cannot perform
from a scene; they are recorded as not-measurable, never as passes.

### 6.2 The gate caught something, and that is the point of a gate

The first EditMode run was **16/17**: `PlayerProfileTests.MissingSave_LoadsDefaultProfile_WithoutAnError`
failed, reporting `LastError` = "the file is empty" when a save file did not exist at all. It was
proved order-dependent before anything was touched - the same test alone passes - and traced to
`Load()` not clearing `lastError` while NUnit runs the fixture in name order, so
`EmptySave_LoadsDefaultProfile` always handed it a stale message. Fixed in the product (4.2), re-run
**17/17**, with every file under `Assets/Tests/EditMode/` still carrying its Week 15 timestamp as proof
that no test was modified.

It was **not** a Week 16 regression - the optimisation touched four texture import settings - but it
would have shipped, and the gate is the reason it did not.

### 6.3 What the gate does not cover

The suites test functional rules, not performance; nothing in them is quoted as a timing measurement.
They ran against the editor's scene tree, not against the shipped `.exe`. And they are the Week 15
suites unchanged, so they prove the optimisation broke nothing known - they cannot prove an optimised
path is correct if that path has no case.

## 7. Evaluation

**Does the build meet the target?** Yes, on the measured environment: 0.60 ms of main-thread work and
0.48 ms of GPU time per frame against a 33.3 ms budget, with p95 at 0.88 ms. The margin is roughly
50x.

**Was the optimisation worth making?** Yes, but for memory and storage, not frame time: -20.0 MB of
runtime texture memory and -10.00 MB of shipped data, at no visible cost. On a phone, where
`Mobile_RPAsset` at render scale 0.8 is already the shipping path and memory is the scarce resource,
that is the number that matters.

**What did the week actually teach?** That the honest outcome of a profiling week can be "the thing
you expected to be slow was already fast". The value is in knowing it with numbers, in having a
harness that writes its own conditions into every file so a result can be checked later, and in
having caught two instrument defects and one real product defect along the way.

**What still limits this report?** Section 9.

## 8. Repository and artefacts

| Item | Value |
|---|---|
| Repository | `AgawanBase`, Unity `6000.6.0f1` |
| Branch | `main` |
| HEAD at the start of the week | `d08fc9b` - "Update README" |
| **End-of-week commit** | **`55b043a`** - "Week 16: profile the shipping render path, cut prop texture memory, fix profile error leak", 421 files, +1,648,822 / -2,421 - contains the optimisation, the instrumentation, the evidence, the regression logs and these three documents |
| Follow-up commit | records `55b043a` inside AS3 itself and moves the three regression logs into `Evidence/test_logs_regression/` (a commit cannot contain its own id) |

How to reproduce the measurement: build the perf player through
`Tools > Week16 > Perf Build > 1` (points the PC level at `Mobile_RPAsset`), `manage_build` windows64
development to `Builds/Week16/AgawanBasePerf.exe`, then `2 - Restore`; run
`AgawanBasePerf.exe -perf -perf-label <name> -perf-duration 60 -perf-out <evidence folder>`; summarise
with `Docs/tools/PerfStats.ps1 -Pattern "<label>*.csv"`. The CSV header states the pipeline, render
scale, scenario, seed and per-segment quality settings, so a file that does not match its conditions
can be discarded rather than trusted.

## 9. Limitations, stated plainly

- **No phone was measured.** Every timing number is from a Windows standalone development build on a
  GTX 1070. The APK is built and verified (IL2CPP, ARM64, `classes.dex` at root) but not profiled; no
  Android device was available this week. Anything said about phone frame time would be an inference.
- **One scene.** `Game.unity` only. The other two maps and their props never load in this scenario,
  which is exactly how the section 7.2 error in SA6 happened.
- **The APK's size delta is not measured.** The -10.00 MB figure is for the Windows artefact, where
  both builds were made by the same tool with the same settings. The APK was not rebuilt in a
  pre-fix variant, so no APK delta is claimed.
- **The texture-memory delta was measured in the editor** (`Profiler.GetRuntimeMemorySizeLong`), on
  the same API both times, because the player's own capture cannot see textures its scene never loads.
- **Host state is a measured hazard**: it moved this game's main thread by 3x within one evening.
  Every comparison in this report is therefore same-host, same-sitting, A/B, and anything else in
  `raw_csv/` is labelled as contaminated.
- **Loading time, battery use and thermal behaviour are not measured.**

*AS3 · Agawan Base v0.2 · Zedrich Zaivan B. Abrera · Week 16*
