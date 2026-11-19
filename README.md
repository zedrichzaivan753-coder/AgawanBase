# Agawan Base 3D

A 3D top-down mobile capture-the-flag game built on the Filipino street game **Agawang Base**, for Android. Release candidate **1.0.0-rc1**.

| | |
|---|---|
| **Course** | MOBILE DEV 002 - Advanced Mobile Application Development, Module 5 (Individual Thematic Mobile Game Development Laboratory) |
| **Theme** | Philippine Games and Sports |
| **Author** | Zedrich Zaivan B. Abrera |
| **Version** | **1.0.0-rc1** (versionCode 2) - release build |
| **Release tag** | annotated tag **`v1.0.0-rc1`** on branch `main`, published to `origin` on 2026-11-19 |
| **PA2 final tag** | annotated tag **`v1.0.0-pa2`** on the Week 18 submission commit, published to `origin` on 2026-11-19 |
| **Release APK** | `Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk` - 33,170,107 bytes (31.63 MB), md5 `ef38de8dd65100109791002a67738da1` |
| **PA2 submission build** | `Docs/Zedrich Abrera/Week_18/Abrera_Zedrich_PA2.apk` - **the same bytes, copied not rebuilt**: 33,170,107 bytes, md5 `ef38de8dd65100109791002a67738da1` |
| **Engine** | Unity 6000.6.0f1, Universal Render Pipeline 17.6.0, C# |
| **Platform** | Android (primary) - ARM64, IL2CPP, minimum API 26, landscape |

![The release-candidate title screen, showing the version caption "1.0.0-rc1 - built 2026-11-19"](Docs/Zedrich%20Abrera/Week_17/Evidence/E1_title_release.png)

## 1. Title and concept

In the traditional street game, two teams start from home bases (usually a pole or a tree) and run out to tag and capture each other. The rule that makes it work is **freshness**: *the player who left their base most recently can tag anyone who left earlier.*

**Agawan Base 3D** turns that rule into a small mobile action game. You control one Blue character in a 3D Filipino setting - a barangay street court, a public market yard or a school campus. Steal the Red team's flag and carry it back into your own base while still free, before the Red team does the same to your flag. Everyone else on both teams is AI.

Core rules in one place:

- Every character has a **freshness** timer (`CharacterStatus`). It counts up while the character is outside its home base and resets to 0 inside it. Characters inside their own base are **safe**.
- When two opponents touch, the one who has been out **less** time wins the tag. The other is teleported to the enemy **prison** and frozen. If both timers are equal, nobody is captured.
- A free teammate who touches a prisoner **rescues** them; the freed character gets 2 seconds of immunity, and rescue has a 3-second cooldown.
- Touch the enemy flag to pick it up. Carry it into your own base to win. If the carrier is captured, the flag drops and returns to its base after 10 seconds.
- A team also wins if every enemy is imprisoned. The match has a 180-second limit (more free members wins, then the fresher team, otherwise a draw).
- Bushes, hedges and crate stacks **hide** a character: body and overhead bars disappear completely, while the controlled character keeps a ground ring so the player never loses themselves.

## 2. Intended users

Filipino students and casual mobile players, about 10 years old and up, who know the street game or want a quick, easy-to-learn action game that keeps a Philippine traditional game alive on a phone.

## 3. Development environment

| Item | Value |
|---|---|
| Game engine | Unity **6000.6.0f1** (Unity 6), C# |
| Render pipeline | Universal Render Pipeline (URP) 17.6.0, mobile renderer settings, Linear colour space |
| Packages used | Input System 1.20.0, Unity UI (uGUI) 2.6.0, URP 17.6.0 |
| Target platform | Android, ARM64, IL2CPP, minimum Android 8.0 (API 26), landscape left, locked |
| Build type | **Release** - Development Build off, Build App Bundle off |
| Managed stripping | Minimal |
| Development / test machine | Windows, Ryzen 5 5600G, NVIDIA GeForce GTX 1070, 15 GB RAM |
| Scenes | `Game.unity` (build index 0, Classic Open Field), `Map_Palengke.unity`, `Map_SchoolCampus.unity` |
| Quality level | Mobile: no real-time shadows, vSync off, target frame rate 30, render scale 0.80 |

## 4. Project structure

```
AgawanBase/
├── Assets/
│   ├── Scenes/                       Game.unity, Map_Palengke.unity, Map_SchoolCampus.unity
│   ├── Config/                       Map_Classic / Map_Palengke / Map_SchoolCampus (MapDefinition assets)
│   ├── Scripts/                      assembly AgawanBase.Runtime
│   │   ├── Core/                     CharacterStatus, CharacterMotor, SprintStamina, BaseZone, Team,
│   │   │                             TeamManager, MatchSettings, MapDefinition, MapSettings,
│   │   │                             PlayerProfile, ReleaseInfo, GameFeedback
│   │   ├── Gameplay/                 MatchManager (rules), EnemyAI (+ .Scoring, .Actions, .Perception),
│   │   │                             Flag, PlayerController, CharacterSpawner, MapNavigator, HideSpot
│   │   ├── UI/                       GameManager (screens/states), HudController, MapSelectUI,
│   │   │                             MatchSetupUI, ProfileUI, VirtualJoystick, SprintButton,
│   │   │                             OffScreenMarkers, SafeAreaFitter
│   │   ├── Visual/                   FollowCamera, CharacterFacing, CharacterRigAnimator,
│   │   │                             CharacterBars, CharacterBushHide, CharacterIndicator,
│   │   │                             IndicatorController, IndicatorMeshes
│   │   └── Diagnostics/              PerfCapture, PerfScenario (Week 16 measurement harness)
│   ├── Editor/                       assembly AgawanBase.Editor - scene builders, A11yAudit,
│   │                                 QualityRun, ReleasePanelsBuild, Week17Shots (27 scripts)
│   ├── Tests/EditMode/               assembly AgawanBase.Tests.EditMode - 17 unit tests
│   ├── Materials/                    shared colour materials
│   ├── Shaders/IndicatorUnlit.shader Unlit shader for the tag rings and icons
│   ├── Prefabs/                      BluePlayer, RedEnemy
│   └── Settings/                     URP assets (mobile and PC), build profiles
├── Packages/                         manifest.json, packages-lock.json
├── ProjectSettings/
└── Docs/
    ├── Zedrich Abrera/Week_13 … Week_17/   weekly submissions and their Evidence/
    ├── REFERENCE/                    the course's reference submission
    └── tools/MdToHtml.cs             Markdown -> print-ready HTML converter (outside Assets/)
```

## 5. How the main scripts fit together

- **MatchManager** is the single place where game rules are decided: who wins a tag, what a rescue does, flag pickup, drop and return, elimination, the time limit and every ending.
- **GameManager** runs the screen states: `Title, Profile, HowTo, Credits, MapSelect, MatchSetup, Playing, Paused, Victory, GameOver, Draw`. It also fills the version labels in `Awake()` and records each finished match into the profile.
- **TeamManager** keeps each team's roster and assigns AI roles (Defender, Raider, Rescuer, Flex) by team size; **EnemyAI** drives *every* AI character, on both teams, with the same logic and the same difficulty preset.
- **CharacterMotor** moves any character (player or AI) with the same code; **CharacterStatus** holds any character's freshness, prison and immunity state.
- **MatchSettings** is the only class that talks to `PlayerPrefs` (team size, difficulty, tag hints); **PlayerProfile** owns `profile.json` and its signature; **ReleaseInfo** is the only place the release identity is assembled.

## 6. Setup and run

### Open the project

1. Install **Unity Hub** and Unity **6000.6.0f1**. Add the **Android Build Support** module (with OpenJDK and Android SDK & NDK Tools).
2. In Unity Hub choose **Add > Add project from disk** and select the project folder (the one that contains `Assets`, `Packages` and `ProjectSettings`).
3. Open the project. The first open imports assets and resolves packages, so it can take a few minutes.
4. Open `Assets/Scenes/Game.unity`.

> **Note:** `Packages/manifest.json` also lists two AI development-tool packages downloaded from GitHub. They are not used by the game code. If the first open shows package errors (for example no internet or Git not installed), remove those two lines from `manifest.json` and reopen; the game does not depend on them.

### Play in the Editor

Press **Play**. Use the mouse on the on-screen joystick and buttons, or the keyboard fallback in section 8.

### Build the Android APK

1. **File > Build Profiles > Android** (already the active profile).
2. Confirm the scene list is `Game`, `Map_Palengke`, `Map_SchoolCampus`, with `Game` at index 0.
3. Development Build **off**, Build App Bundle **off**, IL2CPP, ARM64.
4. **Build** to `Builds/AgawanBase.apk`. The release candidate took 94 s and reported 0 errors and 15 warnings.

## 7. Install the release candidate

Copy `Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk` to the phone (USB, Bluetooth, or a chat app), allow **Install from unknown sources** for the app that opens it, and tap the file. Or, with a cable and USB debugging:

```
adb install -r "Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk"
adb shell am start -n com.zedrich.agawanbase/com.unity3d.player.UnityPlayerActivity
```

- Application id `com.zedrich.agawanbase`, versionName `1.0.0-rc1`, versionCode 2. Uninstall any older copy first.
- ARM64 only, Android 8.0 or newer, landscape.
- **Build App Bundle is off**, so the file is a real APK: `classes.dex`, `AndroidManifest.xml` and `resources.arsc` sit at the archive root. This is the guard against `DEF-15-04`, where Week 14 shipped an App Bundle wearing an `.apk` filename - do not install `Week_14/Abrera_Zedrich_PT6_Playtest.apk`.
- **The release candidate has not been installed on a handset.** No device was attached when it was frozen, so `adb devices` printed an empty list and the module's clean-install checkpoint is recorded **BLOCKED** in `Docs/Zedrich Abrera/Week_17/Evidence/clean_install.md`, with the evidence that stands in its place. Every runtime claim in the Week 17 documents comes from the Unity editor's play mode.
- The APK is **debug-signed** (Unity's debug keystore), which is appropriate for a course submission and not for a store upload.

## 8. Controls

| Action | Touch (phone) | Keyboard (Editor only) |
|---|---|---|
| Move | drag the **virtual joystick** (bottom-left) | `W A S D` or arrow keys |
| Sprint | hold the **SPRINT** button (bottom-right) | `Shift` |
| Catch, rescue, pick up the flag | automatic - walk into the character, the prison, the flag | same |
| Pause | the pause control (top-right) | - |
| Camera | the camera arrows (bottom-right) | - |
| Tag hints on/off | **TAG HINTS** on the pause panel | - |
| Sound on/off | **SOUND: ON / OFF** on the pause panel | - |

Other screens: **START** on the title opens **MAP SELECT**, then **match setup** (team size 1v1-4v4, AI skill Easy/Normal/Hard) and **START**. HOW TO PLAY and CREDITS are overlays that return to the title. **PROFILE** shows the local record. The pause screen offers **RESUME, RESTART, TITLE**; end screens offer **RESTART, TITLE**; the title offers **EXIT**.

## 9. How the HUD and indicators read

- **Freshness bar** (top-left) drains the longer you are outside your base and refills at home; it shows "SAFE AT BASE" inside your base.
- **Stamina bar** shows sprint energy; stamina lasts about 2 seconds and recharges after a short delay.
- **Team lines** show how many members of each team are free and whether each flag is at base, carried or dropped.
- **Rings over opponents**: green with a tick means you can tag them; red with a cross means they can tag you; grey or hidden means neutral. Your own character has a bobbing arrow and ring. Tag hints can be switched off on the pause screen.
- **Edge arrows** point to off-screen things that matter: the enemy flag, your flag when it is stolen, and a captured teammate.

## 10. Feature status

### Complete

| Area | Feature |
|---|---|
| Screens | Title (HOW TO PLAY, CREDITS, EXIT, START, PROFILE, version caption), Map Select, Match Setup, HUD, Pause, Victory / Game Over / Draw |
| Player input | Virtual joystick, hold-to-sprint button, pause button, camera arrows (keyboard fallback in the Editor) |
| Core rule | Freshness, safe bases, one tag-resolution authority, prison and freeze |
| Teams | 1v1, 2v2, 3v3, 4v4 with AI teammates and opponents; the human is always Blue |
| Rescue | A free teammate touching a prisoner frees them (2 s immunity, 3 s rescue cooldown) |
| Capture the flag | Pick up, carry, drop on capture, return after 10 s, win by carrying the flag home |
| End conditions | Flag home, all enemies imprisoned, or 180-second limit with tie-breaks |
| Sprint | Stamina system shared by the player and the AI |
| AI | Roles, vision and short memory, reaction delays, freshness-based utility decisions, Easy / Normal / Hard presets applied equally to both teams |
| Interface aids | Tag-status rings with icons, control arrow, edge-of-screen arrows, safe-area layout |
| Maps | Classic Open Field, Palengke, School Campus - each with its own geometry and hide spots |
| Hiding | Bushes, hedges and crate stacks; body and bars vanish, the controlled character's ground ring stays |
| Camera | Follow camera showing about 65 % of the arena width, clamped to the arena edges, pinch zoom |
| Profile | Local signed `profile.json`: name, record, fastest victories, leaderboard, reset |
| Feedback | Five one-shot sounds (catch, rescue, flag pickup, victory, defeat), a haptic on a catch, a persisted SOUND toggle |
| Accessibility | Every label and button measured: 144 x 144 canvas-px touch floor, 4.5:1 contrast, rect height vs font size |
| Build | Android ARM64 / IL2CPP **release** APK, Build App Bundle off, `classes.dex` at the archive root |

### Partial or unverified

- **Sprint speed ratio on a device** is not independently measured. The multiplier (1.6) is pinned by `SprintConfigTests`, so it cannot drift; the measured walk/sprint ratio on hardware remains unverified.
- **Device install, launch, frame rate, thermal behaviour and long sessions** are unverified - no handset was available (`clean_install.md`).
- **Rule discoverability** was the Week 14 playtest's two S2 findings; an instructions screen was added in Week 17, and no second round of testers has used it.

### Not done

- No music and no ambience - five one-shot sounds only.
- No on-screen match clock (the 180-second limit exists, is enforced and is written into the profile record, but is not displayed).
- No account, no cloud save, no leaderboard, no network call of any kind.

## 11. Build status

| Item | Result |
|---|---|
| Build result | **Succeeded**, 0 errors, 13 warnings, 24.8 s, November 19, 2026 |
| Output | `Builds_RC2/Abrera_Zedrich_PT7.apk` (staging folder, deleted); the deliverable copy is `Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk` |
| Size / md5 | **33,170,107 bytes (31.63 MB)** / **`ef38de8dd65100109791002a67738da1`** |
| Source commit | `84c0f2e` - the commit the APK was built from; tag `v1.0.0-rc1` marks the commit that ships it |
| Scripting backend | IL2CPP, ARM64 only, minimum API 26, managed stripping Minimal |
| Package shape | `classes.dex` and `AndroidManifest.xml` at the archive root of 419 entries, no `BundleConfig.pb` (Build App Bundle **off**) |
| Application id | `com.zedrich.agawanbase` |
| Version | `1.0.0-rc1`, versionCode 2, product name `AgawanBase` |

The release APK is smaller than Week 16's 68.30 MB because Week 16 shipped a **development** build with debug symbols and this is a **release** build. No size reduction is claimed as an optimisation.

## 12. Testing summary

All four gates below were re-run against the release candidate on November 19, 2026; the raw output pointers are in `Docs/Zedrich Abrera/Week_17/Evidence/readiness_results.md`.

| Gate | Result |
|---|---|
| EditMode unit tests (`Assets/Tests/EditMode/`) | **17 passed, 0 failed, 0 skipped** (0.62 s) |
| Accessibility audit (`Assets/Editor/A11yAudit.cs`) | **516 passed, 0 failed, 0 warnings, 24 not measurable** (Week 14 baseline: 423 / 0 / 24) |
| Play-mode quality scenarios (`Assets/Editor/QualityRun.cs`) | **16 passed, 0 failed, 0 blocked**, 0 exceptions, at the documented 4v4 roster |
| Release-candidate walk (`Assets/Editor/Week17Shots.cs`) | title, how-to, credits, match, ending, restart - **all six captured** |

Earlier evidence: 20 testable requirements and a traceability matrix (Week 15), and a usability playtest with five testers, 25 task attempts, 0 failures, 23 unaided, 2 facilitated and two S2 findings (Week 14).

## 13. Performance summary

Measured in Week 16 on the shipping render path (`Mobile_RPAsset`, render scale 0.80) with a Windows standalone **development** build, because no handset was attached:

| Metric | Value |
|---|---|
| Main thread, three 60 s runs at 4v4 / HARD | **0.59 - 0.63 ms** (spread 0.01 ms) against a 33.3 ms budget |
| GPU time | **0.48 ms** |
| Render events per frame | 17 |
| Managed allocation | about 320 B per frame |
| Prop texture memory after the Week 16 fix | 21,849 KB to 1,369 KB (**-20.0 MB**), with no measurable frame-time change |
| Windows player size after the fix | 181.41 MB to 171.41 MB |

A phone will be several times slower on the CPU and an order of magnitude slower on the GPU; the headroom is an argument, not a measurement on hardware.

## 14. Known limitations

- **Not device-verified.** Built and verified in the editor; never installed on a handset (`Docs/Zedrich Abrera/Week_17/Evidence/clean_install.md`, BLOCKED).
- **One profile per install**, local only. The save is tamper-*evident* (signed) but not encrypted: a determined local attacker can re-sign an edited file.
- **No music or ambience**, five one-shot sounds only.
- **No on-screen match clock.**
- The UI panel tree exists once per scene and is written by `Assets/Editor/ReleasePanelsBuild.cs`; editing a scene by hand drifts it from the builder.
- There is no PlayMode test assembly: the play-mode checks are harness-driven and their evidence is a log, not an assertion.
- The AI is tuned, not solved: no balance study exists beyond the 16 regression scenarios.

## 15. Authorized assets, credits and licences

No third-party art, models, audio, fonts or sprites were imported.

| Asset | Source | Authorization |
|---|---|---|
| Locations, characters, props and scenery | Built from Unity primitives by the editor scripts in `Assets/Editor` | Created inside the project |
| Shared colour materials, `IndicatorUnlit.shader` | Created in the project | Created inside the project |
| Tag rings, check mark, cross and arrows | Meshes generated by code (`IndicatorMeshes.cs`); no image files | Created inside the project |
| UI (buttons, bars, panels) and the default font | Unity UI (uGUI) with Unity's built-in font | Official Unity package / built-in, Unity Companion License |
| Touch and keyboard input | Unity Input System 1.20.0 | Official Unity package |
| Rendering | Universal Render Pipeline 17.6.0 | Official Unity package, Unity Personal licence |
| Audio | Five generated one-shot clips under `Assets/Audio/Generated/` | Produced for this project; no third-party music or sound |
| The original playground game | *Agawang Base*, a traditional Filipino game | This adaptation claims no rights over the game itself |

The same licence block is printed at runtime on the game's CREDITS screen, assembled from `Assets/Scripts/Core/ReleaseInfo.cs`.

**AI assistance** was used for documentation drafting and for part of the art and audio generation. It is disclosed here, on the in-game credits screen, and in every Week 13-17 document.

## 16. Documentation and evidence

| Folder | Contents |
|---|---|
| `Docs/Zedrich Abrera/Week_13/` | SA5 analysis, PT5 profile component, Week 13 reflection, APK |
| `Docs/Zedrich Abrera/Week_14/` | SA6 accessibility analysis, PT6 playtest report, Week 14 reflection, playtest APK; `Playtest/` holds the consent form, session script, observation sheet, results table and facilitator checklist |
| `Docs/Zedrich Abrera/Week_15/` | AS3 milestone report, Week 15 reflection, corrected APK, environment record, 20 requirements, acceptance criteria, test logs, traceability matrix, defect log |
| `Docs/Zedrich Abrera/Week_16/` | SA6 performance analysis, AS3 optimisation report, Week 16 reflection, optimised APK, bottleneck table, 22 per-frame CSVs, 20 player logs, the two diagrams, regression logs |
| `Docs/Zedrich Abrera/Week_17/` | **the release candidate**: `Abrera_Zedrich_PT7.pdf`, `_Technical_Documentation.pdf`, `_User_Guide.pdf`, `_Evidence_Index.pdf`, `Abrera_Zedrich_Week17_Reflection.pdf`, the APK, and `Evidence/` (feature-freeze checklist, build record, readiness results, issue disposition, clean-install record, secrets scan, six screenshots, run log, architecture diagram) |
| `Docs/Zedrich Abrera/Week_18/` | **the PA2 package**: `Abrera_Zedrich_PA2.apk` (byte copy of the release candidate), `Abrera_Zedrich_PA2.pdf`, `_Technical_Report.pdf`, `_User_Guide.pdf`, `_Evidence_Portfolio.pdf`, `_Originality_Declaration.pdf`, `Abrera_Zedrich_PA2_Presentation.pptx`, `Abrera_Zedrich_Week18_Reflection.pdf`, the Windows backup package, and `Evidence/` (release record, pre-submission checklist, defence pointers, E3 record, rehearsal log, nine demonstration captures, repository state, backup build log) |
| `Docs/REFERENCE/` | the course reference submission (Castillo, *SiliTubig Clash*), used as the structural model for Week 17 |

Every document from Week 14 on is written twice: `.md` is the source and `.html` is the same text with a print stylesheet and every screenshot inlined as a data URI. Both are generated into the PDF above them by `Docs/tools/MdToHtml.cs` and printed with a headless browser, so a PDF cannot drift from its HTML, and an HTML cannot lose its figures when an `Evidence/` folder moves. Week 17's sources are in `Week_17/others/`.

## 17. Release tag and commit

- Branch `main`, annotated tag **`v1.0.0-rc1`**, created in Week 17 and **published to `origin`** on 2026-11-19 alongside Weeks 16-18; the tag push landed at `5e63b1c`.
- Week 18 creates the annotated tag **`v1.0.0-pa2`** on the PA2 submission commit. It is a label on the *commit*, not on a new binary: the APK it ships is the Week 17 release candidate, byte for byte, and still reports `1.0.0-rc1`. Rebuilding only to print "1.0.0" would have shipped a build that inherits none of the verification behind `ef38de8d...`; the reasoning is in `Docs/Zedrich Abrera/Week_18/Evidence/release_record.md` section 2.
- All three tags are **published**. `git ls-remote --tags origin` returns `v1.0.0-pa2` -> `5e63b1c`, `v1.0.0-rc1` -> `07057bd` and `week16-final` -> `b4cff53`, so the instructor can resolve the submission commit and the binary's tag directly from the remote. The repository was local-only until 2026-11-19, when `origin/main` was still `d08fc9b`; `Week_18/Evidence/repo_state.txt` keeps that pre-push state as a record rather than erasing it.
- A commit id cannot identify the APK, because the APK had to exist before the commit that ships it did. The identity of what was verified is therefore the **APK md5** above; the commit the tag points at is recorded in `Docs/Zedrich Abrera/Week_17/Evidence/build_and_commit.txt`.
- The frozen Week 16 tree is commit `b4cff53`, tagged `week16-final`. A reviewer comparing it with `v1.0.0-rc1` will see only completeness closures (instructions, credits, exit, version caption), defect fixes and documentation - the three categories the feature-freeze checklist allows.

## 18. Next development steps

1. Install the release candidate on a handset and record the clean-install checkpoint (the one Week 17 requirement that is BLOCKED).
2. Profile it on that device: frame time, memory, thermal behaviour across a full match, and the 30 fps cap under sustained play.
3. Re-run the Week 14 playtest protocol against the build that has the instructions screen, to see whether the two S2 discoverability findings are actually closed.
4. Show the match clock on the HUD (the rule already exists).
5. Balance-study the AI at 3v3 and 4v4, and tune the three presets from data rather than feel.
