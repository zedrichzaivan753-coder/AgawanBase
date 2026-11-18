# Defect log - AgawanBase v0.2, AS3 Milestone (Week 15)

One record per defect. A defect exists because a **test** produced it, and the record names
that test; nothing here was written from memory or from reading code and guessing.

Status vocabulary: **FIXED and re-tested**, **WITHDRAWN** (never a defect), **OPEN**, and
**OPEN - artefact only** (the defect is in a file that has already been submitted).

Severity uses `acceptance_criteria.md`: S1 data loss / core loop blocked / information
exposure / repeated crash, S2 a core-loop step needs a workaround, S3 completes but visibly
wrong, S4 cosmetic.

Summary table
-------------

| ID | Title | Found by | Severity | Priority | Status |
|---|---|---|---|---|---|
| DEF-15-01 | Backgrounding the app does not pause the match | TC-LIFE-03 on AB-0.2-QA1 | S2 | P1 | FIXED, re-tested |
| DEF-15-02 | (Proposed) the flag carry penalty is missing | Inspection of PT6 wording vs `SprintStamina.cs` | - | - | WITHDRAWN - not a defect |
| DEF-15-03 | `SetDifficulty` silently accepts an out-of-range preset | TC-SET-02 (`MatchSettingsTests`), EditMode baseline | S3 | P2 | FIXED, re-tested |
| DEF-15-04 | The ".apk" build artefacts are Android App Bundles | Zip listing of AB-0.2-QA2 during the step 6 build | S1 (deliverable not installable) | P1 | FIXED for QA2, OPEN for QA1's file |

---

## DEF-15-01 - Backgrounding the app does not pause the match

**Severity** S2 **Priority** P1 **Status** FIXED and re-tested

**Requirement** REQ-19. **Test that found it** TC-LIFE-03.

**Steps** Start a match. Send the app to the background (HOME, or an incoming call) with the
match running. Wait several seconds. Return to the app.

**Expected** The match is paused when the app is not in front, and the clock has not advanced
while it was away.

**Actual (baseline AB-0.2-QA1)** The match kept running in the background: `MatchManager` and
`MatchClock` continued to tick, so a player who took a call came back to a match that had
played on without them - a captured player, a stolen flag, or a match already ended. There
was no `OnApplicationPause` or `OnApplicationFocus` handler anywhere in the project; a search
for both names returned nothing.

**Why S2** It does not crash and does not destroy saved data (the profile is written on match
end, not on pause), but it silently changes the match state under the player, which is the
definition of "state is wrong after an event".

**Fix** Two handlers appended to `Assets/Scripts/UI/GameManager.cs`:

```csharp
void OnApplicationPause(bool paused)    { if (!paused)  return; if (State != GameState.Playing) return; Pause(); }
void OnApplicationFocus(bool focused)   { if (focused)  return; if (State != GameState.Playing) return; Pause(); }
```

They route into the existing pause path (`SetState:418`, which also sets `timeScale`), so
backgrounding and pressing PAUSE are the same state, not a second mechanism. They guard on
`GameState.Playing` so a background event on the title screen or in the settings does nothing.
There is deliberately **no auto-resume**: coming back to a match that instantly restarts under
the player's thumbs is worse than one tap on RESUME.

**Re-test** TC-LIFE-03 on AB-0.2-QA2: `paused with the clock frozen for 0.8 s` - PASS, in
`test_logs_final/scenario.tsv`. The device half of the same behaviour, TC-BND-06, is
**BLOCKED**: no handset was attached, see `android_crash_buffer.txt`.

**Residual risk** The harness invokes the handler directly rather than pressing HOME on a
phone, because the editor has no HOME button; `OnApplicationFocus(false)` does fire in the
editor and the suite compensates for it with `EnsurePlaying()`. The real HOME press on real
hardware remains untested and is recorded as such.

---

## DEF-15-02 - (Proposed) the flag carry penalty is missing

**Severity** - **Priority** - **Status** WITHDRAWN, never a defect

**Where it came from** The Week 14 report (PT6) promised that carrying the flag costs
something. `SprintStamina.cs` sets `carrySpeedMultiplier = 1f` - no speed cost - while
`carryRegenMultiplier = 0.5f` halves stamina regeneration. Read quickly, that looks like a
promised penalty that was never implemented.

**Why it is withdrawn** `SprintStamina.cs:45-54` documents the choice: the penalty is a
**stamina** penalty, not a **speed** penalty, and deliberately so. A carrier slowed below the
speed of the chasers never reaches home, which turns the one win condition in the game into a
loss condition. Halving regen is the version that bites without making the run home
impossible.

**What was done instead** `Assets/Tests/EditMode/SprintConfigTests.cs` pins both numbers
(`CarryPenalty_IsAStaminaPenalty_NotASpeedPenalty`), so a future edit that makes carrying
slower turns the build red instead of quietly changing the balance.

**Lesson recorded** A defect filed from a code read, without a failing test, was wrong. The
two defects that were real (DEF-15-01, DEF-15-03) both came out of a test.

---

## DEF-15-03 - `SetDifficulty` silently accepts an out-of-range preset

**Severity** S3 **Priority** P2 **Status** FIXED and re-tested

**Requirement** REQ-03..11 (invalid configuration). **Test that found it** TC-SET-02,
`MatchSettingsTests.SetDifficulty_ClampsAnOutOfRangeValue`.

**Steps** `MatchSettings.SetDifficulty(99)`.

**Expected** The value is clamped to a legal preset (0..2) and the caller can tell that it was.

**Actual (baseline)** `difficulty` was stored as 99. The EditMode baseline failed with:

    Expected: less than or equal to 2
    But was: 99

Downstream code indexes a preset array with that value, so an out-of-range difficulty either
throws or silently falls through to whichever preset the array yields for that index - the
match then runs on numbers the player never chose, with nothing in the log to say so.

**Why S3** The match still runs and nothing is saved corrupt, but the stated difficulty and
the real difficulty disagree.

**Fix** `Assets/Scripts/Core/MatchSettings.cs` `SetDifficulty:181` clamps to 0..2 and says so:

    [Settings] difficulty 99 is not a preset - clamped to HARD.
    [Settings] difficulty = HARD

`SetTeamSize:171` already clamped and `Load():149` clamps the same field on the way in, so the
setter was the only door left open; a search of the file confirmed it.

**Re-test** 17/17 EditMode green in `test_logs_final/editmode_final.txt` (job
`151a2cba798e4a43ad10291e206331d6`); the clamp case moved from FAIL to PASS and no other case
changed, which is what "the fix, and nothing else" looks like.

---

## DEF-15-04 - The ".apk" build artefacts are Android App Bundles

**Severity** S1 for the deliverable (a file that cannot be installed blocks every device test)
**Priority** P1 **Status** FIXED for AB-0.2-QA2; **OPEN - artefact only** for AB-0.2-QA1

**Requirement** The module's submission rule: the folder must contain a **runnable build**.
**Found by** Listing the zip contents of the artefact produced in step 6, because the reported
build size (834 MB) did not match the 50 MB file on disk.

**Expected** A file named `.apk` that installs with `adb install` or sideloads on a handset.

**Actual** The file was an `.aab`. `EditorUserBuildSettings.buildAppBundle` was **on**, so the
build ran Gradle's `bundleDebug` task and the tool copied `launcher-debug.aab` to the requested
`.apk` path. Evidence, from the archive itself:

    BundleConfig.pb
    base/assets.pb
    base/assets/bin/Data/Managed/Metadata/global-metadata.dat
    BUNDLE-METADATA/com.android.tools.build.gradle/app-metadata.properties

No `classes.dex` at the root; `base/` in front of every path. The build log agrees:

    Copy '.../build/outputs/bundle/debug/launcher-debug.aab'
      -> 'C:/Users/Zedrich/AgawanBase/Docs/Zedrich Abrera/Week_15/Abrera_Zedrich_AS3_Milestone.apk'

An AAB is a publishing container, not an installable application: it has no signature over a
single APK and no `classes.dex` at the root, so `adb install` rejects it.

**Why S1** The artefact is the whole point of the milestone. A perfect test log behind a file
nobody can install is not a runnable build.

**Which files are affected**

| Artefact | Bytes | md5 | Real format |
|---|---|---|---|
| Week 14 `Abrera_Zedrich_PT6_Playtest.apk` (AB-0.2-QA1) | 50,097,691 | `0eef3dcb32fc0b9852bb4627c18d50d4` | `.aab` - **still on disk as submitted** |
| Week 15 `Abrera_Zedrich_AS3_Milestone.apk` (AB-0.2-QA2) | 54,489,688 | `f4fa8b51d465f2e9120d9d23c7769245` | `.apk` - rebuilt and verified |

**Fix** `EditorUserBuildSettings.buildAppBundle = false`, then rebuild. The 22.0 s rebuild
reused the IL2CPP output of the same working tree, so nothing but the packaging changed. The
new file verified as an APK three ways:

- archive listing: `classes.dex`, `classes2.dex`, `classes3.dex`, `classes4.dex`,
  `lib/arm64-v8a/libgame.so`, `libil2cpp.so`, `libmain.so`, `lib_burst_generated.so`, 428 entries;
- `aapt2 dump badging`: `package: name='com.zedrich.agawanbase' versionCode='1' versionName='0.2'`,
  `minSdkVersion:'26'`, `targetSdkVersion:'36'`, `native-code: 'arm64-v8a'`,
  `launchable-activity: com.unity3d.player.UnityPlayerGameActivity`, `application-debuggable`;
- size: 54,489,688 bytes, which is the bundle plus the dex and native payload stored the way
  an APK stores it.

Full facts in `build_and_commit.txt`.

**What is still open** The Week 14 file is unchanged. Its Week 14 evidence (sizes, md5, the
PT6 document) refers to that file, so replacing it now would invalidate a submitted record;
it is logged here instead, with the correct package id, so the mistake is traceable rather
than quietly corrected. Two consequences to carry into Week 16:

1. Any tester who installed the Week 14 artefact must reinstall AB-0.2-QA2.
2. Nothing in the Week 14 report about device behaviour can be assumed to describe this file,
   because an AAB is not what a handset installs.

**Prevention** `build_and_commit.txt` now ends with the three commands that distinguish an APK
from a bundle, to be run before any build is recorded as delivered:

    aapt2 dump badging <file>.apk | findstr package
    dir <file>.apk
    certutil -hashfile <file>.apk MD5

A file is only recorded as a build once `classes.dex` is at its root.

---

## Appendix A - test-tool defects, deliberately NOT product defects

These were real failures in the Week 15 run, and they are recorded so the baseline numbers
are not mistaken for product failures. None of them is a defect in the game; each was fixed in
`Assets/Editor/QualityRun.cs` and the case re-run.

| # | Harness defect | What it looked like on the baseline | Fix |
|---|---|---|---|
| H-1 | Phase 0 never resolved `GameManager`, so every later phase ran against a null | every case BLOCKED behind 30 s stall guards | resolve once in phase 0 and fail the run loudly if it is null |
| H-2 | `Wait(x); Next();` discarded the wait | cases "passed" against a state that had not been given time to happen | `WaitThenNext(x)` waits, then advances |
| H-3 | The clock was assumed to count down | TC-CORE-10 could never end | `MatchClock` counts **up** to `matchTimeSeconds`; `SetClock(float)` set to the top of the range |
| H-4 | `SawFeedback` was cumulative across cases | a victory line from an earlier case satisfied a defeat case | each case takes a mark first and the search starts there |
| H-5 | `TC-BND-01` captured all four Reds | a deliberate elimination ended the match and contaminated later cases | capture at most `TotalEnemies - 1` |
| H-6 | The rescue case paired a Blue player with a Blue prisoner | "rescue" never ran | a prisoner is freed only by a **team-mate**: Blue rescues Blue |
| H-7 | `TC-CORE-07` passed for the wrong reason | a re-pick-up in the Blue base delivered the flag instead of the 10 s timer | the case drops the flag away from both bases and watches the timer |
| H-8 | `TC-CORE-03` has `Fail` paths and no `Pass` path | a single catch could never be recorded as its own pass; the case could only ever be red | recorded as a coverage gap (Gap 6 in `traceability_matrix.md`) - the catch itself is evidenced by TC-CORE-04 and TC-BND-01 |
| H-9 | `TC-BUILD-01`'s expectation contradicted its own result | the baseline row expected `showAiDebug` **true** in the editor and recorded **PASS** against an actual of `False` - a case that passed while stating it had not | the criterion was corrected to what the build must show (read-out off in a running game), and the case re-run on that criterion |

**Consequence, stated plainly**: `test_logs_baseline/scenario.tsv` (8 passed / 8 failed /
1 blocked) is a record of a broken harness, **not** of a broken game. The only baseline number
that is a product statement is the EditMode one: 16/17, with
`SetDifficulty_ClampsAnOutOfRangeValue` red - which is DEF-15-03.
