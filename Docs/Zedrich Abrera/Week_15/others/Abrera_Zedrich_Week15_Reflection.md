# Week 15 Reflection - AS3 Milestone

**Zedrich Zaivan B. Abrera**
MOBILE DEV 002 - Advanced Mobile Application Development, Module 5, Week 15
*Agawan Base* - 3D mobile capture-the-flag

---

## The week where the tests were the thing I had to fix

I expected this week to be about finding bugs in the game. Most of what I found was in my own
testing.

The first full run of the play-mode suite produced 8 passed, 8 failed, 1 blocked, and I sat there
looking at it as if the game were broken in eight places. It was not. The harness had never
resolved `GameManager` in phase 0; one helper wrote `Wait(x); Next();` and threw the wait away
before it happened; I had assumed the match clock counted down when it counts up; the feedback
search looked at the whole log so a victory line from an earlier case satisfied a defeat case; the
capture case captured all four Reds, which is a deliberate elimination, which ended the match and
poisoned everything after it; and the rescue case paired a Blue player with a Blue prisoner,
forgetting the rule I wrote myself - a prisoner is freed only by a team-mate.

Nine faults, listed as Appendix A in the defect log, and not one of them a defect in the game.
That was the lesson of the week and it was not a comfortable one: **a failing test does not mean
the code is wrong. It means the code or the test is wrong, and I had been assuming it was always
the code.**

## Filing a defect from a code read

At the start of the week I wrote down four defects. Two were real. One - the carry penalty - I
filed by comparing PT6's promise against `carrySpeedMultiplier = 1f` and concluding the penalty
had never been implemented.

It had. `SprintStamina.cs:45-54` documents the opposite decision: the penalty is to stamina
**regeneration**, not to speed, and it is deliberate, because a carrier slower than the chasers
never reaches home and the only win condition in the game becomes a loss condition. I had read
half a file, recognised half a story, and filed it as a defect.

The two defects that were real were both produced by a test. DEF-15-03 came out of an EditMode
assertion reading `Expected: less than or equal to 2 / But was: 99`. DEF-15-01 came out of a
play-mode case that said, in its own words, `GameManager has no OnApplicationPause handler, so
backgrounding cannot pause anything`. That is what a defect should look like: a failing case, not
a suspicion with a file path attached.

## The build that said Success and was not a build

This is the one I keep thinking about. The Android build printed `Build Finished, Result:
Success`, the tool reported the file size, and I was two steps from writing "build produced" into
the evidence when the numbers stopped adding up. The report said 834 MB; the file on disk was 50
MB and named `.apk`.

It was an Android App Bundle. The *Build App Bundle* flag was on, so Unity had run Gradle's
`bundleDebug` task and the tool had copied `launcher-debug.aab` to the path I asked for. The
archive listing settled it in one line: `BundleConfig.pb` at the root and no `classes.dex`.
Nothing can install that. Every device case in this milestone would have rested on a file that
does not install.

What bothers me is not the flag. It is that **Success was true and still meant nothing.** The
build pipeline reported what it did, faithfully, and I had to check what it had actually done
against what I needed. From now on the build document ends with three commands that take ten
seconds:

    aapt2 dump badging <file>.apk | findstr package
    dir <file>.apk
    certutil -hashfile <file>.apk MD5

And one rule: a file is a build only once `classes.dex` is at its root.

## A green suite that is smaller than it looks

The final suites are 17/17 EditMode and 16/16 play mode. Both are true and both are narrower than
the run of numbers suggests. There is no runtime sprint test. Nothing compares the HUD's numbers
to the rules' numbers. The capture case has a `Fail` path and no `Pass` path at all, so it can
never be recorded as passing even when it does. Four cases I planned were never written, and two
of the sixteen that ran succeeded partly by luck before I tightened them.

I put all eight gaps in the traceability matrix with a week against each, because the alternative
is a document that says "16/16" and lets a reader assume coverage that does not exist. `16/16`
means sixteen cases passed. It does not mean the game works, and the honest version of the
sentence has to say so in the same paragraph as the number.

## What I would do differently, in order

1. **Write the harness's failure modes down before the cases.** The stall guard, the one-shot
   feedback mark, the freeze-the-AI rule and the teleport-to-midfield rule were all invented *after*
   a case lied to me. They belong in the design of the tool, not in its third revision.
2. **Verify the artefact, not the message.** The single most valuable thing I did this week took
   one command and happened only because two numbers disagreed.
3. **Never file a defect without a failing test.** If I cannot make it fail on demand, I do not yet
   understand it well enough to call it broken.
4. **State the gap in the same breath as the result.** "16 passed, and here is what the 16 do not
   cover" is a sentence I want to be able to write in every future week.

## The thing I am actually proud of

Backgrounding. A player takes a call, comes back, and the match is exactly where they left it -
paused, clock stopped, flag still on the ground. That is two small handlers, and they route into
the pause path that already existed rather than inventing a second one, and there is no auto-
resume because coming back to a match that restarts under your thumbs is worse than one tap.

E3 in the milestone document is that behaviour happening by accident: I was capturing a
screenshot, the editor lost focus, and the pause menu came up with the clock stopped underneath
it. I did not have to stage it. The handler I added for the test fired for the reason it was
written.

## What Week 16 starts with

Not optimisation. Week 16 starts by installing AB-0.2-QA2 on a handset with `adb install -r` and
running the four device cases that are still BLOCKED, and by building the Windows player at
1920x1080 and 1280x720. I have been treating "no device attached" as an accident of the week for
two weeks now, and it has quietly become the largest hole in the evidence set: three cases have no
data, and one of them - the crash buffer - is a checkpoint condition I am currently meeting by
argument rather than by measurement.

The build is ready to be optimised. The evidence for how it behaves on the hardware it is for is
not there yet, and that is the first thing I will fix.

---

AgawanBase - Zedrich Zaivan B. Abrera - MOBILE DEV 002, Module 5, Week 15 - source: `Abrera_Zedrich_Week15_Reflection.md`
