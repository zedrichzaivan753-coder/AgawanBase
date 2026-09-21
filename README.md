# Agawan Base 3D

A 3D top-down mobile prototype of the Filipino street game **"Agawan Base"**, built in Unity for Android.

| | |
|---|---|
| **Course / exam** | PEBSIT 004 – Mobile Application Development 2 · Preliminary Laboratory Examination (Design-to-Build Prototype) |
| **Category** | Philippine Games and Sports |
| **Author** | [Zedrich Zaivan B. Abrera] · [Section] |
| **Project name** | `PEBSIT004_Prelim_<Abrera>_AgawanBase3D` |
| **Version** | 0.1 (prototype) |

![Title screen](Docs/Evidence/R01_title.png)

---

## 1. Title and concept

In the traditional street game, two teams start from home bases (usually a pole or a tree) and run out to tag and capture each other. The rule that makes it work is **freshness**: *the player who left their base most recently can tag anyone who left earlier.*

**Agawan Base 3D** turns that rule into a small mobile action game. You control one Blue character in a top-down 3D barangay street court. Steal the Red team's flag and carry it back into your own base while still free, before the Red team does the same to your flag. Everyone else on both teams is AI.

Core rules in one place:

- Every character has a **fieldTime** (freshness) timer. It counts up while the character is outside its home base and resets to 0 inside it. Characters inside their own base are **safe**.
- When two opponents touch, the one with the **lower fieldTime** (the "fresher" one) wins. The other is teleported to the enemy **prison** and frozen. If both fieldTimes are equal, nobody is captured.
- A free teammate who touches a prisoner **rescues** them; the freed character gets 2 seconds of immunity.
- Touch the enemy flag to pick it up. Carry it into your own base to win. If the carrier is captured, the flag drops and returns to its base after 10 seconds.
- A team also wins if every enemy is imprisoned. The match has a 180-second limit (more free members wins, then the fresher team, otherwise a draw).

## 2. Intended users

Filipino students and casual mobile players, about 10 years old and up, who know the street game or want a quick, easy-to-learn action game that keeps a Philippine traditional game alive on a phone.

## 3. Development environment

| Item | Value |
|---|---|
| Game engine | Unity **6000.6.0f1** (Unity 6), C# |
| Render pipeline | Universal Render Pipeline (URP) 17.6.0, mobile renderer settings, Linear colour space |
| Packages used | Input System 1.20.0 · Unity UI (uGUI) 2.6.0 · URP 17.6.0 |
| Target platform | Android, ARM64, IL2CPP, minimum Android 8.0 (API 26), landscape |
| Development / test machine | Windows, NVIDIA GeForce GTX 1070, Direct3D11 |
| Scene | One scene: `Assets/Scenes/Game.unity` (Title, Match Setup, HUD, Pause and end screens are UI panels inside it) |
| Quality settings | Mobile level: no real-time shadows, no vSync, target frame rate 30 |

## 4. Project structure

```
AgawanBase/
├── Assets/
│   ├── Scenes/Game.unity           The only build scene
│   ├── Scripts/
│   │   ├── Core/                   CharacterStatus, CharacterMotor, SprintStamina, BaseZone,
│   │   │                           Team, TeamManager, MatchSettings
│   │   ├── Gameplay/               MatchManager (rules), EnemyAI, Flag, PlayerController,
│   │   │                           CharacterSpawner
│   │   ├── UI/                     GameManager (screens/states), HudController, MatchSetupUI,
│   │   │                           VirtualJoystick, SprintButton, OffScreenMarkers, SafeAreaFitter
│   │   └── Visual/                 FollowCamera, CharacterFacing, CharacterRigAnimator,
│   │                               IndicatorController, CharacterIndicator, IndicatorMeshes
│   ├── Editor/                     Editor-only helpers that build the scene and the APK
│   ├── Materials/                  19 shared colour materials
│   ├── Shaders/IndicatorUnlit.shader   Small unlit shader for the tag rings and icons
│   ├── Prefabs/                    BluePlayer, RedEnemy
│   └── Settings/                   URP assets (mobile and PC), build profiles
├── Packages/                       manifest.json, packages-lock.json
├── ProjectSettings/
└── Docs/
    ├── Part_D_Test_Results.md      Full test record and evaluation
    └── Evidence/                   Screenshots, probe_log.txt, R07_build.txt
```

How the main scripts fit together:

- **MatchManager** is the single place where game rules are decided (who wins a tag, rescue, flag pickup and return, elimination, time limit).
- **GameManager** runs the screen states: Title, MatchSetup, Playing, Paused, Victory, GameOver, Draw.
- **TeamManager** keeps each team's roster and assigns AI roles (Defender, Raider, Rescuer, Flex) by team size.
- **EnemyAI** drives *every* AI character, on both teams, with the same logic and the same difficulty values.
- **CharacterMotor** moves any character (player or AI) with the same code; **SprintStamina** is shared by both.

## 5. Setup and run instructions

### Open the project

1. Install **Unity Hub** and Unity **6000.6.0f1**. Add the **Android Build Support** module (with OpenJDK and Android SDK & NDK Tools).
2. In Unity Hub choose **Add → Add project from disk** and select the project folder (the one that contains `Assets`, `Packages` and `ProjectSettings`).
3. Open the project. The first open imports assets and resolves packages, so it can take a few minutes.
4. Open `Assets/Scenes/Game.unity`.

> **Note:** `Packages/manifest.json` also lists two AI development-tool packages that are downloaded from GitHub (see section 13). They are not used by the game code. If the first open shows package errors (for example no internet or Git not installed), remove those two lines from `manifest.json` and reopen; the game does not depend on them.

### Play in the Editor

Press **Play**. Use the mouse on the on-screen joystick and buttons, or the keyboard fallback in section 6.

### Build the Android APK

1. **File → Build Profiles → Android → Switch Platform** (already the active profile).
2. Check that **Scene List** contains `Scenes/Game`.
3. Click **Build** and save as `Builds/AgawanBase.apk` (or **Build And Run** with a phone connected).

### Install on a phone

1. Copy `AgawanBase.apk` to the phone (USB, Bluetooth or chat app), or use **Build And Run** with USB debugging on.
2. Allow **Install from unknown sources** for the app you use to open the file.
3. Open the APK and install. The game runs in landscape.

## 6. Controls

| Action | Touch (phone) | Keyboard (Editor only) |
|---|---|---|
| Move | Drag the **virtual joystick** (bottom-left) | `W A S D` or arrow keys |
| Sprint | Hold the **SPRINT** button (bottom-right) | `Shift` |
| Pause | Tap the **II** button (top-right) | – |
| Tag hints on/off | **TAG HINTS** button on the Pause screen | – |

Other screens: **START** on the title screen opens **Match Setup**; there choose **team size (1v1, 2v2, 3v3, 4v4)** and **AI skill (Easy, Normal, Hard)**, then **START**. The Pause screen offers **RESUME, RESTART, TITLE**; end screens offer **RESTART, TITLE**.

![Match Setup](Docs/Evidence/R02_matchsetup.png)

## 7. How the HUD and indicators read

- **Freshness bar** (top-left) drains the longer you are outside your base and refills at home; it shows "SAFE AT BASE" inside your base.
- **Stamina bar** shows sprint energy. Sprinting is 1.6 times faster; stamina lasts about 2 seconds, then recharges after a short delay.
- **Team lines** show how many members of each team are free and whether each flag is at base, carried or dropped.
- **Rings and icons over opponents:** green with a check mark means you can tag them; red with a cross means they can tag you; grey or hidden means neutral. Your own character has a bobbing arrow and ring. Tag hints can be switched off on the Pause screen.
- **Edge arrows** point to off-screen things that matter: the enemy flag, your flag when it is stolen, and a captured teammate.

![Gameplay HUD](Docs/Evidence/R02_hud.png)

## 8. Feature status

### Completed

| Area | Feature |
|---|---|
| Screens | Title, Match Setup, gameplay HUD, Pause, Victory / Game Over / Draw |
| Player input | Virtual joystick, hold-to-sprint button, pause button (keyboard fallback in the Editor) |
| Core rule | fieldTime freshness, safe bases, tag resolution in one place, prison and freeze |
| Teams | 1v1, 2v2, 3v3, 4v4 with AI teammates and opponents; human is always Blue |
| Rescue | A free teammate touching a prisoner frees them (2 s immunity, 3 s rescue cooldown) |
| Capture the flag | Pick up, carry, drop when the carrier is captured, return after 10 s, win by carrying the flag home |
| End conditions | Flag home, all enemies imprisoned, or 180-second limit with tie-breaks |
| Sprint | Stamina system shared by the player and the AI |
| AI | Roles, vision and short memory, reaction delays, freshness-based decisions, Easy / Normal / Hard presets (same for both teams) |
| Interface aids | Tag-status rings with icons, control arrow, edge-of-screen arrows, safe-area layout |
| Camera | Follow camera showing about 65% of the arena width, clamped to the arena edges, optional pinch zoom |
| Visuals | Low-poly barangay street court built from Unity primitives: houses, coconut trees, sari-sari store, tricycles, jeepney, banderitas, utility-pole bases; kid characters that face their walking direction |
| Build | Android ARM64 / IL2CPP APK builds successfully |

### Partially done

- **Sprint speed ratio (Test 3 – Partial):** the multiplier is correctly wired in the code (1.6), but the measured walk / sprint speeds in the test harness were affected by a timing artefact, so the ratio is not yet independently confirmed.

### Not done / not covered

- No sound effects or music.
- No on-screen match clock (the 180-second limit exists but is not displayed).
- On-device frame rate, heat and long-session play of the final APK were not measured.
- The HUD "DROPPED" flag text was not exercised in the tests.

## 9. Build status

| Item | Result |
|---|---|
| Build result | **Succeeded, 0 errors** (about 99 seconds), recorded 2026-09-21 |
| Output | `Builds/AgawanBase.apk` – **36.64 MB** (36.6% of the 100 MB limit) |
| Manifest (read back with `aapt`) | minimum SDK 26, target SDK 36, `arm64-v8a` only, landscape only, OpenGL ES 3.1 |
| Scripting backend | IL2CPP |
| Application identifier | `com.UnityTechnologies.com.unity.template.urpblank` (template default – **see Known issues**) |

The full build record is in `Docs/Evidence/R07_build.txt`. The 696 MB "total size" that Unity's build report shows includes the IL2CPP intermediate folder and is not the shippable size.

The game was also installed and played on an Android phone during development; that testing found the "field looks too small" and "characters do not face their walking direction" problems, which led to the follow camera and the character-facing script.

## 10. Testing summary

Full details, expected versus actual values and all evidence are in **`Docs/Part_D_Test_Results.md`**. Tests were run in the Unity Editor using an editor-only probe that drove the real UI handlers (`Button.onClick`, `VirtualJoystick.OnDrag`, `SprintButton.OnPointerDown`) and then read the real game objects back. The probe was deleted afterwards and no project code or scene was changed.

| # | Test | Status |
|---|---|---|
| 1 | Title screen and baseline (compiles, one build scene, Title only) | Pass |
| 2 | Screen flow: Title → Match Setup → Gameplay | Pass |
| 3 | Input: virtual joystick and sprint button | **Partial** |
| 4 | Gameplay event: tie, capture and rescue | Pass |
| 5 | Score / status read-out (bars, counters, labels) | Pass |
| 6 | State transitions: pause, resume, restart, victory, game over | Pass |
| 7 | Android build (size, manifest, ARM64) | Pass |
| 8 | Camera framing and clamping | Pass |

Seven of eight tests pass; Test 3 is Partial for a measurement reason, not a known gameplay bug. Raw measurements are in `Docs/Evidence/probe_log.txt`.

| Capture and prison | Victory |
|---|---|
| ![Capture](Docs/Evidence/R04_capture.png) | ![Victory](Docs/Evidence/R06_victory.png) |

## 11. Known issues

| # | Issue | Severity | Status |
|---|---|---|---|
| 1 | **AI debug read-out is switched on.** `HudController.showAiDebug` is `true`, so small text at the top-left of the HUD shows every AI's role and state (for example `DEFENDER  PATROL`). In a game about not knowing where enemies will go, this gives the player information they should not have. Proposed fix: untick **Show Ai Debug** on `Game/UI`, or guard it with `#if UNITY_EDITOR` (see `Docs/Part_D_Test_Results.md`, D.3–D.5). | High | Open |
| 2 | **Template application identifier.** The APK uses `com.UnityTechnologies.com.unity.template.urpblank`. Set a real package name in Player Settings before any store upload. | Medium | Open |
| 3 | **Other debug switches are on:** `FollowCamera.logView`, `MatchManager.logRules`, `MatchManager.allowDebugDrop`, `CharacterSpawner.logRoster`, `Flag.logFlagEvents`. They write to the log; `allowDebugDrop` is a gameplay hook and should be off in a release. | Low | Open |
| 4 | **Sprint speed ratio not independently measured** (Test 3, Partial). | Low | Open |
| 5 | **No visible match clock** although the match has a 180-second limit. | Low | Open |
| 6 | **Stale label text.** `FlagLabel` and `AlertLabel` keep old strings while hidden. Players never see it; it only matters to tests that read `.text` without checking `.activeSelf`. | Info | Not a defect |

## 12. Authorized assets and credits

No third-party art, models, audio, fonts or sprites were imported.

| Asset | Source | Authorization |
|---|---|---|
| Arena, characters, props and scenery | Built from Unity primitives (planes, cubes, cylinders, capsules, spheres) by the Editor scripts in `Assets/Editor` | Created inside the project (AI-assisted, see section 13) |
| 19 colour materials (`Mat_*`) | Created in the project | Created inside the project |
| `IndicatorUnlit.shader` | Created in the project | Created inside the project |
| Tag rings, check mark, cross and arrows | Meshes generated by code (`IndicatorMeshes.cs`); no image files | Created inside the project |
| UI (buttons, bars, panels) and default font | Unity UI (uGUI) with Unity's built-in font | Official Unity package / built-in |
| Touch and keyboard input | Unity Input System 1.20.0 | Official Unity package |
| Rendering | Universal Render Pipeline 17.6.0 | Official Unity package |
| Audio | none used | – |

A cut-paper style illustration and a photo of children playing the game were used **only as visual inspiration** and were not imported into the project.

## 13. Next development steps

1. Fix the open issues above (turn off the AI debug read-out and the other debug switches, set a real package name).
2. Re-measure sprint speed to close Test 3, and add a test for the HUD "DROPPED" flag text.
3. Test the final APK on several Android phones and screen ratios, and measure frame rate at 4v4 and over a long session.
4. Add a visible match clock and sound effects.
5. Balance the AI at 3v3 and 4v4 and tune the difficulty presets.
6. Add optional extras: character customization, more arenas, and a short tutorial screen.
