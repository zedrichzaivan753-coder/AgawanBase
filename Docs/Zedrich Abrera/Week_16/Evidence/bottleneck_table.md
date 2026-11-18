# Week 16 - Bottleneck Table

Abrera, Zedrich Zaivan B. - MOBILE DEV 002, Module 5, Week 16 - AgawanBase v0.2
Unity 6000.6.0f1, URP 17.6.0. Target: 30 fps / 33.3 ms.

The ranked list of what is actually costing time and memory in the running game, each row with the
number that proves it and the evidence file that number came from. Written before anything was
changed, then corrected in section 6 where the post-fix measurement proved an earlier reading wrong.

---

## 1. What was measured, and under what conditions

Every number below comes from the **player**, never the editor. The editor inflates CPU time and its
memory counters include editor-only assets - the editor reported 1,355 textures and 222 MB of texture
memory while the project itself ships 9 textures and the player loads 82. The editor was used only to
explain a number, never to provide one.

| Condition | Value |
|---|---|
| Build | `Builds/Week16/AgawanBasePerf.exe`, Windows x64 development build, 181.41 MB, 0 errors |
| Render path | `Mobile_RPAsset, render scale 0.80, main-light shadows off` - from each CSV's own `# render path:` line |
| Quality | level 0 `PC` with the Mobile level's values applied by hand (the Mobile level excludes Standalone) |
| Frame cap | uncapped (`-1`), re-applied per segment and recorded per segment inside each file |
| Scenario | 4v4 HARD, 1 human + 3 Blue AI vs 4 Red AI, AI seed 20261118, human on a fixed 4-waypoint loop, walking, no sprint |
| Scene | `Game.unity` only, for every segment |
| Duration | 60 s of live match per run, split into 7-8 segments (one segment = one live match) |
| Machine | Ryzen 5 5600G x12, GTX 1070, 15 GB RAM, 1920x1080 |

Baseline evidence (`Evidence/raw_csv/`): `perf_baseline2_run1_20261118_233119.csv` (94,297 rows,
7 declared segments, 7 per-segment lines), `perf_baseline2_run2_20261118_233230.csv` (93,501 rows,
8/8), `perf_baseline2_run3_20261118_233342.csv` (94,829 rows, 7/7). Declared segment counts match the
per-segment `# segment N ran:` line counts, and all three declare the shipping render path, so all
three are usable as evidence.

---

## 2. The three baseline runs agree - the noise band

| Metric | run 1 | run 2 | run 3 | Spread |
|---|---|---|---|---|
| Main thread, mean ms | 0.63 | 0.63 | 0.62 | 0.01 |
| Main thread, p95 ms | 0.98 | 0.99 | 0.97 | 0.02 |
| Main thread, worst ms | 10.67 | 9.61 | 9.80 | 1.06 |
| Frame interval, mean ms | 0.64 | 0.64 | 0.63 | 0.01 |
| Uncapped fps | 1,573 | 1,560 | 1,582 | 22 |
| GPU ms | 0.49 | 0.50 | 0.49 | 0.01 |
| GC bytes/frame | 324 | 330 | 324 | 6 |
| Batches (static) | 4 | 3 | 3 | 1 |
| Total memory MB | 245.2 | 245.6 | 244.7 | 0.9 |
| Texture memory MB | 44.8 | 44.8 | 44.8 | 0.0 |
| Gfx memory MB | 62.3 | 62.3 | 62.3 | 0.0 |
| Managed heap MB | 12.1 | 11.8 | 11.5 | 0.6 |
| Textures loaded | 82 | 82 | 82 | 0 |

Any change smaller than the spread in its row is noise. Texture and Gfx memory are deterministic, so
any movement there is real.

---

## 3. The bottleneck table

| # | Candidate | Measured | % of 33.3 ms | Verdict |
|---|---|---|---|---|
| 1 | Main-thread CPU | 0.63 ms mean, 0.98 ms p95 | **1.9 % / 2.9 %** | **Already meets target - 52x headroom** |
| 2 | GPU | 0.49 ms mean | **1.5 %** | **Already meets target - 68x headroom** |
| 3 | Draw calls | 17 GPU draw events per frame (Frame Debugger); 3-4 static batches | negligible | Already meets target |
| 4 | Triangles | ~9.7 k average, 16.0 k peak | negligible | Already meets target |
| 5 | Managed allocation | 324 B/frame | negligible (9.7 KB/s at 30 fps) | Already meets target |
| 6 | Total memory | 245 MB, of which gfx 62.3 + texture 44.8 + managed 12.1 + mesh 0.6 | n/a | Worth attention on a phone, but see 3.2 |
| 7 | Worst frames | 9.6-10.7 ms, rare and not periodic | 32 % when it happens | Investigated and dismissed - see 3.3 |

**The headline finding for the scene the game actually plays on is that it already meets the target
by a wide margin.** 0.63 ms of main-thread work against a 33.3 ms budget is 1.9 %; the GPU finishes in
1.5 % of it. That is a measured finding, not a failure to find something.

### 3.1 The one piece of real waste in the project

The project ships **9 texture assets**, and five of them are 2048x2048 JPEG BaseColor maps for simple
generated props. Measured with `Profiler.GetRuntimeMemorySizeLong`, four of those cost **21,849 KB
(21.3 MB)** of runtime texture memory - about 5.4 MB each once mipmaps are included - for a school
bench, a tricycle, a palengke stall and a crate stack. These are low-poly objects a few metres across,
seen from a third-person camera on a phone screen.

That is a textbook case of a texture budget spent where nobody can see it, and it is the only finding
in this table that can be fixed with **no visual trade-off at all**.

### 3.2 Where that waste is *not*, and why it matters

The four textures are **not referenced by `Game.unity`**. Checked per scene with
`AssetDatabase.GetDependencies`: `CrateStack`, `PalengkeStall` and `Tricycle` are used by
`Map_Palengke`, `SchoolBench` by `Map_SchoolCampus`, and none of the four by `Game.unity`. A fifth
(`BasketballHoop`) is referenced by no scene at all, because the school hoop is built from primitives.

The consequence is the single most important thing this document has to say about its own method:
**the 44.8 MB of texture memory the capture reports belongs to `Game.unity`'s own resources -
engine and URP defaults, font atlases, and the handful of project textures that scene uses - and not
to the props.** The props' 21.3 MB is only loaded when a map scene loads, and this scenario never
loads one. A capture on one scene cannot measure assets that live in another.

### 3.3 Why the 10 ms worst frames are not the fix target

A 10 ms frame is 3.8x the p95 and appears a handful of times per 93,000 frames. On the earlier
equivalent baseline the shape was measured in detail: 21 frames over 3 ms in 92,997, scattered (only
3 of 21 near a segment boundary), with no periodic spacing. A one-off that does not repeat is not
something a code change can be shown to fix, and a spike that happens twice a minute cannot be
distinguished from noise inside a 60 s run. Chasing it would produce a claim the data cannot support.

---

## 4. Why the draw-call column is not quoted anywhere (DEF-16-04)

The harness's `draw_calls` column reports ~132 per frame. That number is wrong and has been retired.
The Frame Debugger enumerates a live match frame as **17 render events** (6 opaque + 4 transparent +
1 blit + 5 UI) over two independent captures, and the batches column agrees at 3-4 static batches.
The 0.63 ms main-thread time is itself proof that ~132 draw calls cannot be real.

The mechanism: the column sums three split render counters, and `SRP Batcher Draw Calls Count` counts
**renderers submitted to the batcher**, not draw calls - a run's own split line reads
`SRP 116 + standard 6 + instanced 0`, i.e. 116 renderers submitted, which static and SRP batching then
collapse into a handful of GPU draws. Every CSV now carries a header line saying so, so the column
cannot be lifted out of the evidence and quoted by mistake.

---

## 5. Fix #1, as implemented

**Change:** `maxTextureSize` 2048 -> 512 on the `CrateStack`, `PalengkeStall`, `SchoolBench` and
`Tricycle` BaseColor maps. Compression stays `Compressed`, mipmaps stay on. No code changed.

| Metric | Before | After | Delta | How measured |
|---|---|---|---|---|
| Runtime memory, the four textures | 21,849 KB | 1,369 KB | **-20.0 MB** | `Profiler.GetRuntimeMemorySizeLong`, same API both times |
| Build artefact | 181.41 MB | 171.41 MB | **-10.0 MB (-5.5 %)** | Build pipeline, identical settings |
| Texture dimensions | 2048x2048 | 512x512 | - | `Texture2D.width/height` |
| Main-thread ms (A/B, see 5.1) | 0.60 | 0.60 | 0.00 |

### 5.1 What the fix does to frame time - measured, and the answer is nothing

The first attempt at a post-fix set was unusable: three sets (`fix1`, `fix1b`, `fix1c`) were taken
while a `Client-Win64-Shipping` process held 5.4 GB, and they degraded monotonically (0.68 -> 0.70 ->
2.07 ms). Rather than publish a number from a busy machine, the measurement was repeated on an idle
host, twice over, in one sitting:

- `perf_fix1clean_run{1,2,3}_20261119_0000*.csv` - the shipped 171.41 MB build
- `perf_baselclean_run{1,2,3}_20261119_0006/0007/0009*.csv` - an A/B build made by returning the four
  maps to `maxTextureSize` 2048 and rebuilding (181.41 MB, exactly the pre-fix artefact size), so the
  only difference between the two sets is the fix itself

| Metric, 3-run mean | A/B pre-fix | Fixed | Delta |
|---|---|---|---|
| Main thread, mean ms | 0.60 | 0.60 | **0.00** |
| Main thread, p95 ms | 0.86 | 0.88 | +0.02 (inside the 0.02-0.05 spread) |
| Frame interval, mean ms | 0.61 | 0.61 | 0.00 |
| Uncapped fps | 1,652 | 1,640 | -12 (the fps spread alone is 23) |
| GPU ms | 0.48 | 0.48 | 0.00 |
| GC bytes/frame | 317 | 313 | -4 (see 5.3) |
| Total memory MB | 245.4 | 246.7 | +1.3 (see 5.2) |
| Build artefact | 181.41 MB | 171.41 MB | **-10.00 MB (-5.5 %)** |

**The fix changes no measurable frame time**, and it was never going to: `Game.unity` does not
reference the four textures, so the scenario never samples them. What the earlier 0.63 -> 0.60
comparison showed was the host machine, not the code. That is now proved rather than suspected:
pre-fix and fixed builds measured minutes apart on the same idle machine are indistinguishable.

The honest claim for fix #1 is therefore: **-10.00 MB of shipped data and -20.0 MB of import-level
texture memory, on four textures that are seen a few metres wide from a third-person camera, with no
measurable frame-time effect in either direction.** It is a memory and storage fix, not a frame-time
fix, and it is kept because it costs nothing visually.

### 5.2 One observation that runs the wrong way, stated rather than smoothed over

In this capture the FIXED build reports **89 textures / 45.3 MB** where the A/B build reports
**82 / 44.8 MB** - the opposite direction from the fix. It is consistent across all three runs of each
set, so it is not noise, and it cannot be caused by the four maps, which `Game.unity` does not load.
Something about what the engine and URP bring in under the two builds differs, and this harness does
not enumerate textures, so the cause is unknown. It is recorded because a table that only ever moves
in the direction the author wants is a table nobody should trust. The fix's memory benefit is
evidenced where the textures actually live - the imported sizes and the build artefact - not from
this counter.

### 5.3 What is still not measured

- Frame time under host contention was never usable, so the contaminated sets (`fix1`, `fix1b`,
  `fix1c`) stay in `raw_csv/` as the record of a method that failed, not as evidence.
- GC bytes/frame differs by 4 B between the two sets. At this scale that is measurement noise from
the recorders themselves; no allocation change was made and none is claimed.
- Loading time and battery use are not measured at all. The module lists them as relevant metrics;
  this week's scenario measures steady-state play only.
- Every number here is a desktop number on a GTX 1070. It proves the delta and the headroom on this
  machine, not phone performance.

---

## 6. Correction to the first version of this table

The first version of this document ranked "texture memory 44.8 MB" as bottleneck #1 and implied the
four prop textures were part of that figure. The post-fix measurement disproved it: after removing
20 MB of textures the player still reported 44.8 MB and 82 textures, because those textures are in
the map scenes and the capture only ever loads `Game.unity`.

The corrected ranking is the one in section 3. The original error was worth keeping a record of,
because the failure mode - attributing one scene's footprint to another scene's assets - is exactly
what a single-scene capture invites.

---

## 7. Standing caveat for SA6 and AS3

Every number here is a **desktop** number from a GTX 1070. It proves the delta and the headroom on
this machine. It is not a measurement of any phone, and a mid-range Android device will show a
main-thread time several times larger and a GPU time an order of magnitude larger. The 30 fps budget
is met with enormous margin here; on a phone that margin is what absorbs the difference.
