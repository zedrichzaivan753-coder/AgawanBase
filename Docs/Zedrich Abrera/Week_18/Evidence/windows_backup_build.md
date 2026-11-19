# Windows Backup Package — PA2

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Week:** 18 — PA2 Final Demonstration Package
**Recorded:** 2026-11-19, from `Assets/Editor/Week18Build.cs`

---

## 1. What this file records

The module asks for a submitted build that can be installed and demonstrated, and permits a
**backup installation package** alongside it. This machine has no Android handset attached
(`adb devices` returns nothing), so the only artefact that can be launched and shown running here is
a Windows desktop player. This file records how it was produced, what it is, and — in full — what it
is **not**.

**This package is not the submission.** The submitted binary is
`Docs/Zedrich Abrera/Week_18/Abrera_Zedrich_PA2.apk`, which is a byte copy of the Week 17 release
candidate. It was not rebuilt, and its hash is checked in `Evidence/release_record.md`.

---

## 2. Identity of the artefact

| Field | Value |
|---|---|
| Package | `Docs/Zedrich Abrera/Week_18/Abrera_Zedrich_PA2_Windows.zip` |
| Zip size | **41,305,552 bytes** (39.39 MB) |
| Zip MD5 | **`fe17f2060c994218e4593b2010eb9a98`** |
| Executable | `Builds/PA2_Windows/AgawanBase.exe` |
| Executable MD5 | `6466092eafd62cf0159050e5ef5d62b7` |
| Uncompressed player | 110,142,056 bytes (105.04 MB) |
| Engine | Unity **6000.6.0f1** (build `f7f8ed4d1e24`), URP 17.6.0 |
| Scripting backend | **Mono** — *not* IL2CPP (see §6) |
| Build result | `Succeeded`, **0 errors**, 8 warnings, 42.7 s |
| Scenes in build | `Game`, `Map_Palengke`, `Map_SchoolCampus` |
| Version label in binary | `1.0.0-rc1  -  built 2026-11-19` (same string as the APK) |
| Haptics | **Absent** (see §6) |

Both hashes were re-read from disk with `certutil -hashfile` after the build, not copied from the
build log, and they match `Evidence/windows_build.txt`.

The eight warnings are the project's long-standing `CS0618` obsolete-API notices
(`CharacterSpawner.cs:215`, `QualityRun.cs:261`, `ReleasePanelsBuild.cs`) that predate Week 18. No
warning was introduced by the Week 18 scripts.

---

## 3. How it was produced

`Assets/Editor/Week18Build.cs` runs three stages rather than one. Switching the active build target
reimports the entire project for the new platform, and doing that twice plus a 105 MB build inside a
single editor call is long enough to time out — which is exactly what happened on the first attempt.

| Stage | Method | What it does | Timestamp (UTC) |
|---|---|---|---|
| 1 | `StagePrepare` | Logs the Android facts, saves the Standalone backend, sets it to Mono, switches active target to `StandaloneWindows64` | 07:54:20 |
| 2 | `StageBuild` | Builds `AgawanBase.exe`, zips the player folder into `Week_18/` | 07:55:33 |
| 3 | `StageRestore` | Switches back to `Android`, restores the saved backend, re-asserts every Android fact | 07:57:02 |

Stage 2 refuses to run unless the active target is already `StandaloneWindows64`, so an incomplete
stage 1 cannot silently produce an Android build under a `.exe` name.

Reproduce with the menu items, in order:

```
Tools > Week18 > Build Windows Backup - stage 1 prepare
Tools > Week18 > Build Windows Backup - stage 2 build
Tools > Week18 > Build Windows Backup - stage 3 restore Android
```

### Android configuration restored

Stage 3 verifies rather than assumes. All seven facts read back correct after the detour:

```
activeTarget        = Android
buildAppBundle      = False
development         = False
scriptingBackend    = IL2CPP
targetArchitectures = ARM64
minSdkVersion       = AndroidApiLevel26
versionCode         = 2
bundleVersion       = 1.0.0-rc1
applicationId       = com.zedrich.agawanbase
orientation         = LandscapeLeft
```

The platform switch did leave two incidental edits in the working tree — an explicit
`Standalone: 0` line in `ProjectSettings.asset` and three platform entries added to
`UniversalRenderPipelineGlobalSettings.asset`. Both files were clean at baseline (`repo_state.txt`
records the tree as clean apart from the Week 18 scripts), so both were reverted with
`git checkout`. Reverting is behaviour-preserving: `Mono2x` was already the effective Standalone
backend before the build, and the reverted URP asset is the one the shipped Android APK was built
against.

---

## 4. Launch test

The package was not merely built; it was started and inspected. It was launched, allowed to run for
roughly fifteen seconds, and terminated. `tasklist` confirmed the process
(`AgawanBase.exe`, PID 17624) was alive and holding 549 MB.

Its player log (`%USERPROFILE%\AppData\LocalLow\Zedrich Zaivan B. Abrera\AgawanBase\Player.log`)
shows it reached the title screen under Direct3D 11 on the GTX 1070:

```
Initialize engine version: 6000.6.0f1 (f7f8ed4d1e24)
Direct3D:  Version: Direct3D 11.0 [level 11.1]
[Settings] loaded 1v1 (0 Blue AI + 1 Red AI) at NORMAL skill, tag hints ON.
[Map] loaded selection: Classic Open Field (id=classic, scene=Game)
[Release] build id shown on the title: 1.0.0-rc1  -  built 2026-11-19
[Title] starting on the title screen.
[Team] Blue: 1 member(s) - BluePlayer [FLEX]
[Team] Red: 1 member(s) - Red_AI_1 [FLEX]
```

So the package starts, initialises the input system and PhysX, loads the saved map selection,
constructs both teams, and paints the title with the same version caption the APK shows. This is
stronger evidence than "the build succeeded".

**What the launch test does not show.** No input was injected and no match was played, so this is
not a claim that the game is comfortably playable with a mouse and keyboard. The game's controls are
on-screen touch controls; they are UGUI and the virtual stick is pointer-driven, so they should
respond to a mouse, but that was not exercised here and is not asserted.

---

## 5. An unexpected result worth reporting

The very first launch produced this:

```
[Profile] a new per-install signature salt was created.
[Profile] save rejected (the signature does not match the contents (the file was edited by hand))
         - starting fresh; the old file was kept as ...\AgawanBase\profile.rejected.json.
```

This is the Week 13 profile-integrity feature working, on a file it was not aimed at. A Windows
player shares its persistent data path with the editor when both use the same company and product
name, so the player found a profile written by editor play-mode sessions, could not verify its
signature, refused to load it, preserved it as `profile.rejected.json`, and started fresh.

The feature behaved exactly as designed under a condition no test had covered — a different
executable reading the same save folder. It is recorded here because it is a genuine, unplanned
observation, and because it is a caution: **playing the Windows backup will disturb the editor's
saved profile.** The `.rejected.json` file next to `profile.json` is the preserved original.

---

## 6. What differs from the submitted APK

A backup package that is quietly different from the submission is worse than none, so the
differences are listed rather than glossed:

1. **Scripting backend: Mono, not IL2CPP.** The Android APK ships IL2CPP/ARM64. The backup uses
   Mono because it keeps the download small. Any behaviour that only manifests under IL2CPP would
   not be visible here.
2. **No haptics.** `GameFeedback.cs` guards the catch buzz behind `#if UNITY_ANDROID || UNITY_IOS`.
   On desktop that branch does not compile in, so the catch has its sound but no vibration.
3. **Some post-processing shaders are stripped.** The player log reports:

   ```
   Shader 'Hidden/Universal Render Pipeline/GaussianDepthOfField' is not supported or has been
   stripped from the build ... PostProcessing render passes will not execute.
   Shader 'Hidden/Universal Render Pipeline/BokehDepthOfField' ... not execute.
   Shader 'Hidden/Universal Render Pipeline/PaniniProjection' ... not execute.
   ```

   The Standalone target was built with URP's default per-platform stripping rather than the
   Android target's tuned entries, so depth-of-field and Panini projection do not run. Colour
   grading, bloom and the rest of the look are unaffected. This was not fixed, because fixing it
   means editing the URP asset and re-crossing the platform boundary that §3 just restored — a real
   risk to the Android configuration for a cosmetic gain on an artefact that is not the submission.
4. **Resolution and window mode** follow the desktop defaults rather than the locked LandscapeLeft
   phone presentation.
5. **It has not been playtested on a desktop.** The Week 14 usability sessions ran on a handset
   APK. None of that data applies to this package.

None of these is hidden from the assessor: they are the reason this artefact is described as a
backup and the APK is described as the submission.

---

## 7. Why a Windows package at all

Stated plainly, because the choice is a compromise and should be defended rather than assumed:

- The module penalises demonstrating a build other than the submitted one. The submitted APK is
  therefore what gets installed on a handset if one is available at defence time.
- With no handset on this machine, the alternative to this package is demonstrating from the
  **editor**, which is materially worse: the editor is not the submitted binary at all, it has
  debug affordances, and it invites exactly the "is that really your build?" question PA2 warns
  about.
- A compiled Windows player is at least a **built artefact** with a hash, a log, and a title screen
  that prints the same version string as the APK. It is the strongest available fallback, and it is
  labelled as a fallback.

The honest summary for the defence: *the APK is the submission; the Windows zip is the fallback for
a machine that cannot run an APK, and it differs in backend, haptics and two post-processing
passes.*

---

## 8. Source

- Build script: `Assets/Editor/Week18Build.cs`
- Raw stage log: `Docs/Zedrich Abrera/Week_18/Evidence/windows_build.txt`
- Ignore rule: `.gitignore` carries `/Docs/Zedrich Abrera/Week_18/*.zip`, scoped so the tracked
  Week 14 reference zip under `Docs/REFERENCE/` stays tracked. The package is identified by hash
  here instead of being versioned.

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
