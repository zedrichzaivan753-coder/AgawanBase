# Week 16 Reflection - Profiling and Performance Optimisation

**Abrera, Zedrich Zaivan B.** · MOBILE DEV 002, Module 5
**Game:** Agawan Base v0.2 - 3D mobile capture-the-flag, theme: traditional Filipino street games
**Build version:** 0.2, branch `main`, HEAD at the start of the week `d08fc9b`; the end-of-week commit
is recorded in `Abrera_Zedrich_AS3.md`, section 8
**Evidence this reflection leans on:** `Week_16/Evidence/raw_csv/perf_baselclean_run1_20261119_000633.csv`,
`perf_fix1clean_run1_20261119_000043.csv`, `Assets/Scripts/Diagnostics/PerfCapture.cs`,
`Assets/Scripts/Core/PlayerProfile.cs`, `Week_16/Evidence/bottleneck_table.md`

---

## Which measured bottleneck was most significant?

The one that was most significant turned out to be the one I did not expect: **there wasn't one.** I
went into this week expecting the frame to be dominated by something - the eight characters with
procedural animation, the AI, the per-character world bars, the 200-odd mesh renderers in `Game.unity`,
the court lines, the 16-segment rings. I built a harness that measures the `PlayerLoop` counter rather
than frame interval, because `GameManager` pins the game to 30 fps and a 60 ms frame would have looked
exactly like a 33 ms one. Then I ran 60 seconds of 4v4 on HARD, three times, and the main thread came
back at **0.59-0.63 ms** with a **0.01 ms** spread, GPU at 0.48 ms, 17 render events per frame.

That is 1.8 % of a 33.3 ms budget. The most significant measured bottleneck was that the scene the
game actually plays on already meets the target by roughly fifty times, and the honest thing to write
in the report was that, not a manufactured list of micro-optimisations.

The one cost that *is* real is not in the frame at all: memory. The project ships nine texture assets
and five of them are 2048x2048 BaseColor maps on generated props, four of which cost **21,849 KB** of
runtime texture memory for a bench, a tricycle, a stall and a crate stack - objects a few metres across
on a phone screen.

## What optimisation did I attempt, and what changed in the measurements?

I dropped those four maps from 2048 to 512, leaving compression and mipmaps alone. What changed:

- their runtime memory went **21,849 KB to 1,369 KB** (-20.0 MB), same `Profiler.GetRuntimeMemorySizeLong`
  call both times;
- the Windows player went **181.41 MB to 171.41 MB** (-10.00 MB);
- **frame time did not move at all**, and I only know that because the second attempt at measuring it
  was built properly.

The first post-fix measurement looked like a catastrophe: three runs at 0.68, 0.70, then 2.07 ms where
the baseline had been 0.62, with GPU time drifting the same way. I spent a while looking at my own
change before looking at the machine, which was the actual cause - an unrelated `Client-Win64-Shipping`
process holding 5.4 GB. A texture downsample cannot triple main-thread time, and I should have
suspected that sooner than I did.

So I did the thing I should have done first: I reverted the four imports, rebuilt the player to the
exact pre-fix size (181.41 MB), and measured **both builds minutes apart on an idle host**. Both came
back at 0.60 ms mean and 0.61 ms frame interval. The earlier 0.63 -> 0.60 difference was the host,
not my code. That A/B pair is the reason AS3 says the change has no frame-time effect - not because I
could not measure it, but because I measured it both ways and it is not there.

## What trade-off did I accept or reject?

**Accepted:** a real reduction in the resolution of four prop textures. I justified it rather than
hid it: the shipping render path is `Mobile_RPAsset` at render scale 0.8, so the screen is already
below 1080p; the props are seen at a distance where 512 and 2048 are hard to tell apart; and leaving
21.3 MB resident on four minor props costs a mid-range phone something real in exchange for detail
nobody resolves.

**Rejected:** turning HDR off and pushing render scale below 0.8. Both would have produced a cleaner
before/after table, and both would have been a visible quality loss across the whole screen bought
with frame time the game does not need. I also rejected object pooling and allocation work: the game
allocates about **320 B per frame**, roughly 10 KB/s at 30 fps, and refactoring gameplay code covered
by sixteen regression cases to remove GC pressure that is not there would have been activity in place
of improvement.

## What performance limitation still needs attention?

**Nothing in this week's numbers was measured on a phone.** Every figure comes from a Windows
standalone development build on a GTX 1070, because that was the approved profiling environment and no
Android device was attached. The APK is built and verified - IL2CPP, ARM64, `classes.dex` at the root -
but it has never run under a profiler, and a mid-range Android device will show a main-thread time
several times larger and a GPU time an order of magnitude larger. The 50x headroom I measured is
exactly what is supposed to absorb that difference; it is not proof that a specific phone hits 30 fps.

The second limitation is coverage. The scenario loads `Game.unity` and nothing else, so the other two
maps never appear in any capture. That is not a footnote - it is how I made my worst mistake this
week. My first bottleneck table ranked the 44.8 MB of texture memory as the top item and implied my
four props were part of it. After the fix the player still reported 44.8 MB, because those props live
in `Map_Palengke` and `Map_SchoolCampus` and my scenario never loads either. I left the correction in
section 6 of the bottleneck table instead of quietly fixing the number, because a single-scene capture
invites exactly that error and the next person to read it should know.

One more thing worth recording: the regression gate caught a defect that had nothing to do with
performance. `PlayerProfile.Load()` was not clearing `lastError`, so a missing save file could report
the previous load's rejection reason - an order-dependent failure that passes when you run the test
alone and fails in the suite. I fixed the product, not the test. A week spent making the game faster
would have been worth much less if it had shipped a profile that lies about its own state.

---

I confirm that this reflection describes my own development experience and that all assistance,
including AI assistance, has been disclosed.

*Zedrich Zaivan B. Abrera · Week 16*
