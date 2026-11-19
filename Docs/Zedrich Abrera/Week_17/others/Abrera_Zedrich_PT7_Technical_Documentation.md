# Abrera_Zedrich_PT7_Technical_Documentation

**Technical documentation - Agawan Base 1.0.0-rc1 - Week 17**
**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 - Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports (traditional Filipino street game *Agawang Base*)
**Release candidate:** `Abrera_Zedrich_PT7.apk`, 33,170,059 bytes, md5 `4b1152fe203ba6f679404d955fdaa59d`
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0, C#

This is the reference document for the code that ships in the release candidate. Every claim about a script names the file; every number names the evidence file it was read from. Section 14 lists what the build does **not** do, and section 17 lists the sources used.

## Contents

1. Concept
2. Theme-to-mechanics justification
3. Target player and scope
4. Platform and build configuration
5. Architecture
6. Scenes, and the single panel tree
7. The important scripts
8. Data flow
9. Service and security model
10. Controls and interface
11. Setup: open, play, build, install
12. Testing
13. Performance
14. Known limitations
15. Maintainability and how to extend it
16. Credits and licences
17. References

---

## 1. Concept

Two teams of one to four characters face each other across a walled neighbourhood arena: a barangay street court, a public market yard, or a school campus. The player controls one Blue character; every other character on both sides is AI. Each team has a home base and a flag. You win by carrying the enemy flag into your own base, by imprisoning every enemy, or by leading when the three-minute clock runs out.

The mechanism that makes the street game work is **freshness**. A character's field timer counts up while they are away from their base and resets the moment they touch it. Of two opponents who touch, the one who has been out **less** time - the fresher one - wins the tag, and the other is dragged to the enemy prison. Bases are safe ground; a free team-mate can walk into the prison and release a prisoner.

## 2. Theme-to-mechanics justification

The module's first procedure step asks whether the final mechanics still communicate the theme. Three rule-to-theme links survive in the shipping build, and each one is a rule in `MatchManager.cs`, not a piece of decoration:

| Theme element | Mechanism in the build | Where it lives |
|---|---|---|
| **Bases are home.** Leaving home is a risk you take and re-take | `BaseZone` marks safe ground; `CharacterStatus` resets the field timer to 0 inside the owner's base and sets `Frozen`/safe state | `Assets/Scripts/Core/BaseZone.cs`, `Assets/Scripts/Core/CharacterStatus.cs` |
| **"Agawan base" is a freshness race, not a damage race** | Touch resolution compares the two field timers and teleports the loser to the enemy prison; a tie captures nobody | `MatchManager.cs` (`tagDistance = 1.2`), `CharacterStatus.cs` (`IsImmune`) |
| **Rescue is a social act, so it costs something** | A free team-mate touching the prison frees the prisoner; the freed character gets 2 s immunity and rescue has a 3 s cooldown, so prison is not a revolving door | `MatchManager.cs` (`rescueDistance = 1.5`, `immunitySeconds = 2`, `rescueCooldown = 3`) |

Two further rules carry the same weight: the flag that **drops** when its carrier is caught and returns home after 10 s (`Flag.returnAfterSeconds`), and hiding places - bushes, hedges and crate stacks - in which a character's body and overhead bars disappear entirely, so stealing the flag can be played by stealth rather than by speed (`Assets/Scripts/Gameplay/HideSpot.cs`, `Assets/Scripts/Visual/CharacterBushHide.cs`).

Nothing in the release candidate is themed only by art direction. There is no costume, no music and no narrative text; the theme is carried by the three rules above, by the three Filipino locations, and by the props those locations are built from.

## 3. Target player and scope

- **Player:** Filipino students and casual mobile players aged roughly 10 and up, on a mid-range Android phone held in two hands in landscape.
- **Session:** one match is 180 s, a whole session from launch to ending is two to four minutes. There is no meta-progression beyond the local profile record.
- **Scope that ships:** three maps, 1v1 to 4v4, three AI skill presets, catch / prison / rescue, flag capture, three ending conditions, a local signed profile, accessibility-measured UI, five feedback sounds.
- **Scope that does not ship:** no network play, no account, no leaderboard, no cloud save, no music, no in-app purchase, no analytics.

## 4. Platform and build configuration

| Item | Value |
|---|---|
| Engine | Unity 6000.6.0f1, URP 17.6.0, uGUI 2.6.0, Input System 1.20.0 |
| Build target | Android, active profile in the project |
| Scripting backend | IL2CPP |
| Architecture | ARM64 only |
| Minimum API | 26 (Android 8.0) |
| Orientation | Landscape left, locked |
| Managed stripping | Minimal |
| Build type | **Release** - Development Build off |
| Build App Bundle | **Off** (see §9) |
| `productName` | `AgawanBase` |
| `bundleVersion` / versionCode | `1.0.0-rc1` / `2` |
| Application id | `com.zedrich.agawanbase` |
| Scenes in build | `Game` (0), `Map_Palengke` (1), `Map_SchoolCampus` (2) |
| Quality level used | Mobile: no real-time shadows, vSync off, target 30 fps |
| Render pipeline asset | `Mobile_RPAsset`, render scale 0.80 |

The version is carried in exactly one place - `PlayerSettings.bundleVersion` - and read at runtime through `Application.version`. `Assets/Scripts/Core/ReleaseInfo.cs` deliberately does **not** contain a version string, so a label cannot disagree with the binary it is drawn by; `ReleaseInfo.BuildId` is `Application.version + "  -  built " + BuildStamp`, and that single string is what the title caption and the credits panel print.

## 5. Architecture

Three ideas hold the code together:

1. **One rules authority.** `MatchManager` decides every rule outcome - who wins a tag, what a rescue does, when a flag is picked up, dropped or returned, and how the match ends. Nothing else mutates those outcomes; UI and AI read state and ask for actions.
2. **One mover, one status.** `CharacterMotor` moves any character and `CharacterStatus` holds any character's freshness, prison and immunity state, whether that character is the human or an AI. There is no separate player physics and enemy physics to drift apart.
3. **State outside the scene.** RESTART and TITLE reload the scene, so anything that must survive a reload - the profile, the chosen team size, the skill preset, the sound preference - lives in `PlayerProfile` (`profile.json`) or `MatchSettings` (`PlayerPrefs`).

<!--INLINE:architecture.svg-->

The arrow from `MatchManager` to `PlayerProfile` is the match result being written at an ending: `GameManager.RecordResult` asks `PlayerProfile.RecordMatch`, which appends the match and rewrites the signed save file. The arrow into `TeamManager`/`EnemyAI` is the chosen team size and skill preset being read at spawn.

**Assemblies.** Three, and the split is deliberate:

| Assembly | Contents | Why separate |
|---|---|---|
| `AgawanBase.Runtime` | everything under `Assets/Scripts/` | the only code in the player build |
| `AgawanBase.Editor` | 27 editor scripts under `Assets/Editor/` - scene builders, audits, capture harnesses | cannot leak into a player build, and cannot be called by gameplay code |
| `AgawanBase.Tests.EditMode` | four test files under `Assets/Tests/EditMode/` | the 17 tests run without entering play mode |

There is no PlayMode test assembly; the play-mode checks are driven by editor harnesses (`QualityRun`, `A11yAudit`, `Week17Shots`) that invoke the real public game API, which is why their logs, not assertions, are the evidence.

## 6. Scenes, and the single panel tree

| Scene | Map | Notes |
|---|---|---|
| `Game.unity` | Classic Open Field | build index 0, the scene the app launches into |
| `Map_Palengke.unity` | Palengke | same panel tree, market geometry |
| `Map_SchoolCampus.unity` | School Campus | same panel tree, campus geometry |

Each map is a `MapDefinition` asset (`Assets/Config/Map_*.asset`) with a `mapId`, `displayName`, `sceneName`, preview sprite, accent colour and arena bounds; `MapSelectUI` reads those assets and `GameManager` loads the chosen scene. The three scenes are **not** three apps: each one contains the whole `Game/UI` panel tree, written by `Assets/Editor/ReleasePanelsBuild.cs`, which is rerunnable and warns rather than silently skipping when a panel it expects is missing.

That decision has a cost worth naming: the same UI exists three times, so a UI change must be applied by the builder to all three scenes, and the builder is the only supported way to do it. In exchange, any map can be loaded directly - from the map select screen, from a scene reload on RESTART, or by opening that scene in the editor - with no cross-scene bootstrap and no "missing manager" state.

## 7. The important scripts

| File | Responsibility | Why it matters in a defence |
|---|---|---|
| `Assets/Scripts/Gameplay/MatchManager.cs` | every rule decision: tag (`tagDistance 1.2`), prison transfer, rescue (`1.5` / 2 s immunity / 3 s cooldown), flag (`flagDistance 1.5`), win by flag, win by elimination, draw at `matchTimeSeconds 180`, and the events the HUD and audio subscribe to | the single place a rule can be argued with; if a match ends wrongly, this file explains why |
| `Assets/Scripts/UI/GameManager.cs` | the screen state machine - `Title, Profile, HowTo, Credits, MapSelect, MatchSetup, Playing, Paused, Victory, GameOver, Draw` - pause and `timeScale`, restart, quit, and the version labels filled in `Awake()` | owns what the player sees and what is allowed to be on screen at once |
| `Assets/Scripts/Core/CharacterStatus.cs` | freshness timer, safe-at-base, frozen in prison, immunity window | the theme's central rule is this file's timer |
| `Assets/Scripts/Core/CharacterMotor.cs` | one movement implementation for player and AI, sprint multiplier from `SprintStamina` | prevents the "the AI moves differently" class of bug |
| `Assets/Scripts/Core/SprintStamina.cs` | stamina drain, recharge delay, `carrySpeedMultiplier`; the carry penalty is a **regen** penalty by design | a deliberate balance decision, pinned by `SprintConfigTests` so it cannot drift silently |
| `Assets/Scripts/Core/MatchSettings.cs` | team size 1-4, difficulty preset, tag hints; the only class in the project that touches `PlayerPrefs`; clamps an out-of-range difficulty instead of falling through to HARD's numbers | the Week 15 DEF-15-03 fix lives here |
| `Assets/Scripts/Core/PlayerProfile.cs` | name, record, fastest victories, leaderboard, signed `profile.json`, `RecordMatch`, `ResetProfile`, `Load()` clearing `lastError` first | the Week 13 security work and the Week 16 leak fix |
| `Assets/Scripts/Gameplay/EnemyAI.cs` (+ `.Scoring`, `.Actions`, `.Perception`) | utility scoring per decision tick, vision radius, short memory, reaction delay; roles Defender / Raider / Rescuer / Flex from `TeamManager` | the AI is the opponent; the split files are why a decision can be traced |
| `Assets/Scripts/Gameplay/Flag.cs` | `AtBase / Carried / Dropped` state, pick-up, drop on capture, return after 10 s, tilt animation | the win condition in code |
| `Assets/Scripts/Gameplay/HideSpot.cs`, `Assets/Scripts/Visual/CharacterBushHide.cs` | bush hiding: body and overhead bars vanish, the controlled character's ground ring stays | the stealth mechanic, and the one place where "invisible" had to be defined precisely |
| `Assets/Scripts/Core/GameFeedback.cs` | five one-shots (catch, rescue, flag, victory, defeat), one haptic on a catch, persisted SOUND toggle in `PlayerPrefs["audio.feedback"]` | feedback that is audible and can be switched off |
| `Assets/Scripts/UI/HudController.cs` | freshness and stamina bars, free counts, flag state lines, capture counts; AI debug read-out under `#if UNITY_EDITOR` | display only - it never decides a rule |
| `Assets/Scripts/Visual/IndicatorController.cs`, `IndicatorMeshes.cs` | tag-status rings, check mark, cross, control arrow, all generated as meshes - no image files | the code-built art that also keeps the APK free of imported sprites |
| `Assets/Scripts/UI/OffScreenMarkers.cs`, `SafeAreaFitter.cs` | edge arrows to the enemy flag, your stolen flag and a captured team-mate; notch and cutout padding | the two things that make a top-down game playable on a phone |
| `Assets/Editor/ReleasePanelsBuild.cs` | writes the title row, both reading panels and both version labels into all three scenes | how the Week 17 screens exist at all |
| `Assets/Editor/A11yAudit.cs` | scores every uGUI button and label in all three scenes: 144 x 144 canvas-px touch floor, rect height against font size, WCAG contrast against the nearest opaque panel | the accessibility claim is a measurement, not an opinion |
| `Assets/Editor/Week17Shots.cs` | the release-candidate walk: title, how-to, credits, match, ending, restart, with hashing and repaint forcing | the six screenshots in the evidence set |

## 8. Data flow

**A match, from tap to save.**

1. `GameManager` shows `Title`. `Awake()` has already written `versionLabel` from `ReleaseInfo.BuildId`.
2. START moves to `MapSelect` (three `MapDefinition` assets) and then `MatchSetup`, which reads and writes `MatchSettings` - team size 1-4, difficulty Easy/Normal/Hard, tag hints on/off - and each write goes to `PlayerPrefs` and is saved immediately.
3. The chosen map's scene is loaded. On load, `CharacterSpawner` builds the roster from `MatchSettings`: the human plus `TeamSize - 1` Blue AI against `TeamSize` Red AI, and `TeamManager` assigns roles by team size.
4. `MatchManager` runs the rules. Its events - `CharacterCaptured`, `CharacterRescued`, `Flag.PickedUp`, `PlayerWon`, `PlayerLost`, `MatchDrawn` - are consumed by `HudController` (numbers), `IndicatorController` (rings) and `GameFeedback` (clip plus haptic). Nothing consumes an event to change a rule.
5. At an ending, `GameManager.RecordResult` calls `PlayerProfile.RecordMatch`, which appends the match, recomputes wins/losses/draws and fastest victories, and rewrites `profile.json` with its signature. The end screen shows the ending; RESTART reloads the scene with the same `MatchSettings`.

**Where state lives.**

| State | Store | Survives scene reload | Survives app restart | Survives reinstall |
|---|---|---|---|---|
| Team size, difficulty, tag hints, sound on/off | `PlayerPrefs` under `match.*` and `audio.feedback` | yes | yes | no |
| Display name, record, fastest victories, leaderboard | `profile.json` in `Application.persistentDataPath`, signed | yes | yes | no |
| Match in progress (prisoners, flags, clock) | scene objects | no - by design, RESTART is a new match | no | no |

The `PlayerPrefs` namespace and `persistentDataPath` are both derived from `productName`. That is exactly why `productName` is `AgawanBase` and not `AgawanBase v1.0.0-rc1`: putting the version in the product name would have given 1.0.0-rc1 a fresh, empty store and silently abandoned every saved setting and the whole profile (`DEF-17-02` in `Week_17/Evidence/issue_disposition.md`).

## 9. Service and security model

**There is no service layer, and that is the design.** The game makes no network call of any kind: no account, no cloud save, no leaderboard upload, no analytics, no ads, no remote config. `AgawanBase.Runtime` contains no `UnityWebRequest`, no `HttpClient` and no socket. Consequently:

- There is no credential, token, API key or endpoint in the repository, in the APK, or in the save file. The Week 17 secrets scan over the whole tree returned twelve matches, all of them prose in plan documents describing the scan itself (`Week_17/Evidence/secret_scan.md`).
- The OWASP MASVS storage concerns reduce to one local file and one `PlayerPrefs` store, which is why the profile can be defended in a single paragraph.
- The APK is signed by Unity's own **debug** keystore, not by a checked-in keystore. That is appropriate for a course submission and would not be appropriate for a store upload.

**The profile is tamper-evident, not encrypted.** `PlayerProfile` serialises to `profile.json` and writes a signature alongside the payload; `Load()` recomputes it and refuses a file that does not match, reporting the reason through `lastError`. A local attacker with the file in hand can edit the record and re-sign it, because the key ships inside the app. That is stated as the known limit rather than dressed up as encryption: nothing of value crosses a device boundary here, and the threat the signature addresses is accidental truncation and casual editing, not a determined local attacker.

**Package integrity.** Build App Bundle is **off**, so the shipped file is a real APK with `classes.dex`, `AndroidManifest.xml` and `resources.arsc` at the archive root. This is the direct guard against `DEF-15-04`, where Week 14 shipped an Android App Bundle wearing an `.apk` filename - a bundle keeps `classes.dex` inside `base/dex/` and no handset will install it. The check is repeated for this exact binary in `Week_17/Evidence/readiness_results.md` §5.

## 10. Controls and interface

| Action | Touch (phone) | Keyboard (editor only) |
|---|---|---|
| Move | drag the virtual stick, bottom left | `W A S D` or arrow keys |
| Sprint | hold SPRINT, bottom right (drains stamina) | `Shift` |
| Catch, rescue, pick up the flag | automatic - walk into the character, the prison, the flag | same |
| Pause | the pause button, top right | - |
| Tag hints on/off | TAG HINTS on the pause panel | - |
| Sound on/off | SOUND: ON / OFF on the pause panel | - |

![E2 - the instructions screen, reachable from the title without being told. It is the screen that closes the module's "clear instructions or tutorial" gap; the two reading screens together account for the 93 additional labels and buttons this build scores over the Week 14 baseline.](Evidence/E2_how_to_play.png)

Screens, in the order a new player meets them: **Title** (HOW TO PLAY, CREDITS, EXIT, START, PROFILE, version caption) - **How to play** - **Credits** - **Profile** - **Map select** - **Match setup** - **Playing** (HUD, joystick, SPRINT, pause) - **Paused** (RESUME, RESTART, TITLE, TAG HINTS, SOUND) - **Victory / Game Over / Draw** (RESTART, TITLE). HOW TO PLAY and CREDITS are overlays: they return to the title and nothing else, so they cannot be opened in the middle of a match and cannot interrupt one.

Every layout is measured, not eyeballed: `Assets/Editor/A11yAudit.cs` scores all 516 measurable labels and buttons in the three scenes against a 144 x 144 canvas-px touch floor, a rect height of at least 1.12 x font size, and 4.5:1 contrast against the nearest opaque panel. The title stack is entirely top-anchored with its own pivot, because the previous centre-anchored layout had pushed a line of text underneath the opaque PROFILE button at every aspect (`DEF-17-06`).

## 11. Setup: open, play, build, install

**Open the project.**

1. Unity Hub, Unity **6000.6.0f1**, with Android Build Support (OpenJDK, SDK and NDK tools).
2. Add the project folder (the one holding `Assets`, `Packages`, `ProjectSettings`) and open it. First import takes a few minutes.

**Play in the editor.** Open `Assets/Scenes/Game.unity`, press Play. The mouse drives the on-screen stick and buttons; `W A S D` and `Shift` also work.

**Build the APK.**

1. File > Build Profiles > Android. The profile is already active.
2. Confirm the scene list is `Game`, `Map_Palengke`, `Map_SchoolCampus`, with `Game` at index 0.
3. Development Build **off**, Build App Bundle **off**, IL2CPP, ARM64.
4. Build to `Builds/AgawanBase.apk`. On this machine it took 94 s and reported 0 errors, 15 warnings.

**Install.**

```
adb install -r "Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk"
adb shell am start -n com.zedrich.agawanbase/com.unity3d.player.UnityPlayerActivity
```

Or copy the APK to the phone, allow installing from that source, and open the file. Minimum Android 8.0, ARM64 only, landscape.

**Note on packages.** `Packages/manifest.json` lists two AI development-tool packages fetched from GitHub. The game does not use them; if the first open reports package errors, remove those two lines and reopen.

## 12. Testing

Five layers, each answering a different question. All of the numbers below were re-run against the release candidate on November 19, 2026.

| Layer | Question it answers | Result | Where |
|---|---|---|---|
| **EditMode unit tests** | do the pure rules do what they say, with no play mode involved? | **17 passed, 0 failed, 0 skipped** in 0.62 s | `Assets/Tests/EditMode/` (team membership and its inverse, capture and rescue state, flag ownership, win conditions, difficulty presets, sprint config) |
| **Accessibility audit** | is every label readable and every button big enough? | **516 passed, 0 failed, 0 warnings, 24 not measurable** (Week 14 baseline 423/0/24) | `Assets/Editor/A11yAudit.cs`, log in `Week_14/Evidence/a11y_audit.log` |
| **Play-mode quality scenarios** | does the running game survive the edge cases? | **16 passed, 0 failed, 0 blocked**, 0 exceptions | `Assets/Editor/QualityRun.cs` - core rules, pause/resume/background/restart, 50 rapid catches, 50 rapid pause toggles, 50 rapid pick-up attempts, debug read-out guard |
| **Usability playtest** | can five people who have never seen it play it? | **25 task attempts, 0 failures, 23 unaided, 2 facilitated**; two S2 findings, both about rule discoverability | `Week_14/Playtest/`, `Week_14/others/Abrera_Zedrich_PT6.md` §6-7 |
| **Release-candidate walk** | does the whole demonstration sequence run end to end? | **six screens reached and captured**, ending and restart included | `Assets/Editor/Week17Shots.cs`, `Week_17/Evidence/week17_shots.txt` |

Two things about this table are worth stating plainly. The playtest's two findings - that the rescue rule and the catch rule are not discoverable from the screen alone - are the reason the release candidate has an instructions screen at all; the fix was an instructions panel, not a rule change, and the findings are still open as a design question for the defence. And the quality suite's red runs (13 / 3 / 1, twice) are recorded as an environment precondition rather than deleted: at a 1v1 roster the suite's own capture eliminates the only Red, so the match is already over when the life-cycle cases run. At the documented 4v4 precondition the same untouched code scores 16 / 0 / 0.

## 13. Performance

Measured in Week 16 on the shipping render path, with a Windows standalone development build because no handset was attached (`Week_16/Evidence/`, 22 per-frame CSVs).

| Metric | Value | Budget |
|---|---|---|
| Main thread, mean of three 60 s runs | **0.59 - 0.63 ms** (spread 0.01 ms) | 33.3 ms at the 30 fps cap |
| GPU time | **0.48 ms** | 33.3 ms |
| Render events per frame | 17 | - |
| Managed allocation | about 320 B per frame (~10 KB/s at 30 fps) | - |
| Render pipeline | `Mobile_RPAsset`, render scale 0.80, no real-time shadows | - |

That is roughly 1.8 % of the frame budget, and the honest reading is that the scene the game actually plays on already meets 30 fps with about fifty times the headroom it needs. The one real cost found was memory, not frame time: five 2048 x 2048 BaseColor maps on generated props cost 21,849 KB of runtime texture memory for a bench, a tricycle, a stall and a crate stack, and dropping four of them to 512 brought that to 1,369 KB (-20.0 MB) with no measurable frame-time change - measured both ways, on an idle host, after an unrelated 5.4 GB process had been mistaken for a regression.

The release APK is 31.63 MB against Week 16's 68.30 MB because Week 16 shipped a **development** build with debug symbols and this is a **release** build. No size reduction is claimed as an optimisation.

![E4 - a match in the release candidate: the freshness and stamina bars top-left, team and flag lines, the joystick bottom-left, SPRINT and camera arrows bottom-right, the pause control top-right. Both flags are at their bases and the roster is the 4v4 the quality suite requires.](Evidence/E4_match_running.png)

## 14. Known limitations

| # | Limitation | Consequence | Where it is recorded |
|---|---|---|---|
| 1 | **Not device-verified.** No handset was attached this week; `adb devices` printed an empty list | every runtime claim in this package comes from the editor's play mode on a desktop GPU; a mid-range phone will be several times slower on the CPU and an order of magnitude slower on the GPU | `Week_17/Evidence/clean_install.md` (BLOCKED) |
| 2 | **One profile per install**, local only, tamper-evident not encrypted | two players sharing a phone share a record; a determined local attacker can re-sign an edited file | `Week_13/others/Abrera_Zedrich_PT5.md` |
| 3 | **No audio beyond five one-shots** | no music, no ambience, no positional audio | `assets`: `GameFeedback.cs`, `Assets/Audio/Generated/` |
| 4 | **Rule discoverability is still a design question** | two of five playtesters needed a prompt to find the rescue and catch rules; the instructions screen mitigates it but nobody has been re-tested since it was added | `Week_14/others/Abrera_Zedrich_PT6.md` §7 |
| 5 | **The UI exists three times**, once per scene, written by a builder | a UI change must go through `ReleasePanelsBuild.cs`; editing one scene by hand creates drift | §6 of this document |
| 6 | **No PlayMode test assembly** | the play-mode checks are harness-driven and their evidence is a log, not an assertion that fails a build | §5 of this document |
| 7 | **Debug-signed, ARM64 only** | fine for a course submission, not for a store upload; no 32-bit ARM device is supported | `Week_17/Evidence/build_and_commit.txt` |
| 8 | **The AI is tuned, not solved** | it plays all three presets competently at 1v1-4v4, but no balance study exists beyond the 16 regression scenarios | `Week_16/Evidence/` |

## 15. Maintainability and how to extend it

- **A new map** is one `MapDefinition` asset plus one scene containing the panel tree; `MapSelectUI` and `GameManager` pick it up from the asset, and `ProjectSettings/EditorBuildSettings` needs the scene added. `Assets/Editor/MapBuild.cs`, `PalengkeBuild.cs` and `SchoolCampusBuild.cs` show the three existing builders, and `RebuildMapsRunner.cs` reruns them.
- **A new rule** goes into `MatchManager` and nowhere else; if it needs a new fact about a character, it belongs in `CharacterStatus`. Adding it to `HudController` or to `EnemyAI` is how the project would acquire two disagreeing rule sets.
- **A new screen** is a panel in `ReleasePanelsBuild`, a state in `GameManager.GameState`, and an entry in `A11yAudit`'s coverage (which needs no change - it walks the scenes).
- **A new save field** goes through `PlayerProfile`, which owns the file format and the signature; anything that must survive only a scene reload belongs in `MatchSettings` instead.
- **Before any release:** re-run `A11yAudit.Run()`, `QualityRun.Run()` and the EditMode suite, and re-check `classes.dex` at the APK root. Those four checks are the gate this week used, in that order of cheapness.

## 16. Credits and licences

| Item | Source | Licence / authorisation |
|---|---|---|
| Game design, programming, art direction, documentation | Zedrich Zaivan B. Abrera | author |
| Engine and rendering | Unity 6000.6.0f1, Universal Render Pipeline 17.6.0 | Unity Personal licence; Unity and the Unity logo are trademarks of Unity Technologies |
| Interface text font | Unity's built-in uGUI legacy font | redistributed with the editor under the Unity Companion License |
| Input | Unity Input System 1.20.0 | official Unity package |
| Geometry, materials, textures | built inside the project from Unity primitives and generated assets; 19 shared `Mat_*` materials; `IndicatorUnlit.shader` written for this project | produced for this project, no third-party art |
| Tag rings, check mark, cross, arrows | generated meshes (`IndicatorMeshes.cs`) - no image files | produced for this project |
| Audio | five generated one-shot clips under `Assets/Audio/Generated/` | produced for this project; no third-party music or sound |
| Original street game | *Agawang Base*, a traditional Filipino playground game | this adaptation claims no rights over the game itself |
| AI assistance | used for documentation drafting and part of the art and audio generation | disclosed here, in the credits panel of the game, and in the project README |

The credits panel inside the game prints the same block at runtime from `ReleaseInfo.Licences`, so the licence statement travels with the build rather than only with the documents.

## 17. References

The reference set for this course is the SiliTubig Clash submission in `Docs/REFERENCE/CASTILLO_Janelle_Ann_23-10260_SiliTubigClash`; its Week 17 package is the structural model for this documentation set.

- Google. (2024). *Application bundling and APK structure*. Android Developers. https://developer.android.com/guide/app-bundle
- Huang, Z., & Benyoucef, M. (2023). A systematic literature review of mobile application usability. *Universal Access in the Information Society, 22*, 715-735. https://doi.org/10.1007/s10209-022-00903-w
- OWASP Foundation. (2024). *OWASP Mobile Application Security*. https://mas.owasp.org/
- Unity Technologies. (2024). *Unity Profiler documentation*. https://docs.unity3d.com/Manual/Profiler.html
- Unity Technologies. (2024). *Building and distributing for Android*. https://docs.unity3d.com/Manual/android-building.html
- Unity Technologies. (2023). *Optimize your mobile game performance*. https://unity.com/resources/optimize-mobile-game-performance-unity-2022lts
- Weichbroth, P. (2024). Usability testing of mobile applications: A methodological framework. *Applied Sciences, 14*(5), 1792. https://doi.org/10.3390/app14051792
- World Wide Web Consortium. (2024). *Web Content Accessibility Guidelines 2.2*. https://www.w3.org/TR/WCAG22/
