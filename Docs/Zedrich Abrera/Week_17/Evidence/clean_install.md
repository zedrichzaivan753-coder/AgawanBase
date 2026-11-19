# Week 17 — clean install on the target device

## Status: BLOCKED

**The release candidate has not been installed or launched on an Android handset.** No device is
available to this build, so the module's install-and-run checkpoint is recorded as blocked, with the
reason below and with the stand-in evidence listed afterwards. It is not reported as passed, and no
part of this file is written as though a phone was involved.

## Why it is blocked

`adb` is installed — it ships with this Unity's Android player — but nothing is attached to it. Read
off the machine on November 19, 2026:

```
C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe devices
List of devices attached

```

An empty device list is the whole reason: there is no handset, no tablet and no emulator image to
install **33,170,107 bytes / MD5 `ef38de8dd65100109791002a67738da1`** onto, and `adb install` cannot be
invented. `adb` on its own is not evidence of anything; its empty device list is.

## What would have been tested, and what stands in its place

| Requirement | What a device run would show | What this build substitutes | Strength |
|---|---|---|---|
| The APK installs from a clean state | `adb install` succeeds on a handset with the game not previously present | `classes.dex`, `AndroidManifest.xml` and `resources.arsc` at the archive root, Build App Bundle **off** — the DEF-15-04 guard that separates a real APK from an `.aab` renamed `.apk` | **Structural only.** It proves the package is APK-shaped, not that a given handset accepts it |
| The app launches and reaches the title | launcher icon → title screen with the version caption | `E1_title_release.png`, captured in play mode, showing `1.0.0-rc1 - built 2026-11-19` and the three title buttons | Editor, not device |
| The game runs and a match plays | real touch input on a real screen, real frame timings | `E4_match_running.png` and the 16-case quality suite in play mode, 0 exceptions | Editor, not device |
| Instructions, credits and exit are reachable | tapping HOW TO PLAY / CREDITS / EXIT on the device | `E2_how_to_play.png`, `E3_credits.png`, and `[Release] quit requested.` in the run log | Editor, not device |
| Persistence survives a restart | profile record still present after force-quitting and relaunching the app | profile write logged by the ending in the quality suite (`[Profile] match recorded: WIN (ELIMINATION) … now 7W/0L/0D`) | Editor, not device |
| Landscape lock, pause on backgrounding | rotating a real device, pressing Home | `TC-LIFE-03` drives the pause path in the editor | Editor, not device |

## What this means for the submission

Every runtime claim in `readiness_results.md` comes from the Unity editor's play mode on:

- Windows, Ryzen 5 5600G, GeForce GTX 1070, 15 GB RAM;
- Unity 6000.6.0f1 with the Android build target active, but **no build ever launched**;
- the same scenes, the same scripts and the same `bundleVersion` that the APK was built from.

The honest summary is that the release candidate is **build-verified and editor-verified, and
device-unverified**. A reviewer with a handset can close the gap in about two minutes:

```
adb install -r "Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk"
adb shell am start -n com.zedrich.agawanbase/com.unity3d.player.UnityPlayerActivity
```

and should expect the title screen, the six screens in order, and a version caption reading
`1.0.0-rc1 - built 2026-11-19`. Until someone does that, this file stays BLOCKED.
