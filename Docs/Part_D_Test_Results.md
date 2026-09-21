# Part D - Build, Testing, Troubleshooting and Evaluation

**Project:** AgawanBase | **Editor:** Unity 6000.6.0f1 (WindowsEditor) | **Pipeline:** URP (`UniversalRenderPipelineAsset`), Linear colour space
**Target:** Android, ARM64 / IL2CPP, minSdk 26, landscape | **Scene under test:** `Assets/Scenes/Game.unity` (the only enabled build scene)
**Raw evidence:** `Docs/Evidence/probe_log.txt` | **Screenshots:** `Docs/Evidence/*.png` | **Build facts:** `Docs/Evidence/R07_build.txt`

---

## D.1 Test environment, method and build configuration

Recorded at baseline (`probe_log.txt:1-62`):

| Fact | Value |
|---|---|
| Unity version | 6000.6.0f1 (WindowsEditor) |
| GPU / graphics API | NVIDIA GeForce GTX 1070 / Direct3D11 |
| Render pipeline / colour space | `UniversalRenderPipelineAsset` / Linear |
| `check_compile_errors` | **clean** (no errors) |
| Active build target | Android |
| Scripting backend | IL2CPP |
| Architectures | ARM64 |
| Min SDK / orientation | `AndroidApiLevel26` / `LandscapeLeft` |
| Product / company | `AgawanBase` / `DefaultCompany` |
| Enabled build scenes | 1 - `Assets/Scenes/Game.unity` |
| Scene inventory at Title | 501 GameObjects, **0** missing-script components |
| Key scene objects | all 29 present (`Game/MatchManager`, both bases, both flags, both prisons, `BluePlayer`, `CameraRig/Main Camera`, all five UI panels, joystick, HUD fills and labels) |
| Serialized null-reference scan | 10 component types scanned; no null `UnityEngine.Object` fields |
| Game view | 1139 x 558 (aspect 2.041) |

**Method.** Several rows turn on a single frame - one capture, one rescue, one win - and cannot be produced by clicking around in the Editor while also photographing that exact frame. Those rows were driven by a temporary editor-only harness (`Assets/Editor/PartDProbe.cs`), which drove the **real** UI components through their **real** handlers - `Button.onClick`, `VirtualJoystick.OnDrag`, `SprintButton.OnPointerDown` - and then read the real game objects back. Screenshots were written to disk with `ScreenCapture.CaptureScreenshot`.

**No project code, scene or serialized field was modified at any point.** The harness was deleted at the end of the run.

**One exception to the "no nulls" scan, which is not a defect.** `OffScreenMarkers.match` reads `NULL` on `Game/UI/Panel_Hud`. `OffScreenMarkers` resolves that reference itself under an explicit null guard (`OffScreenMarkers.cs:117,120`), exactly as `FollowCamera.target` does. Both are lazy self-resolution, not missing wiring.

---

## D.2 Required Test Record

| # | Test | Expected | Actual | Status | Action | Evidence |
|---|---|---|---|---|---|---|
| 1 | Title screen and baseline | Project compiles, one enabled build scene, Title screen shows with only the Title panel active | `check_compile_errors` clean; 1 enabled scene (`Game.unity`); `state=Title`, `titleActive=True`, all other panels `False`, `matchClock=0.000`, `timeScale=1.000` | **Pass** | None | `R01_title.png`, `probe_log.txt:1-64` |
| 2 | Screen flow: Title -> Match Setup -> Gameplay | START opens Match Setup; the setup START begins a match and the arena, bases, flags, HUD and roster appear | Route B (choice changed -> scene reloaded): `PLAYING after 0.292 s`, and `0.014 s` / `0.023 s` on repeats. Route A (choice unchanged -> no reload): `PLAYING after 0.009 s` / `0.010 s`. Both routes produced `arenaGround/baseBlue/baseRed/flagBlue/flagRed/hudPanel = True` and roster `BluePlayer[Blue/HUMAN], Blue_AI_1[Blue], Red_AI_1[Red], Red_AI_2[Red]` (4 characters, 2 per team) | **Pass** | None | `R02_matchsetup.png`, `R02_hud.png`, `probe_log.txt:65-74,161-163,229-234,260-265` |
| 3 | Input: virtual joystick and sprint button | Stick direction maps 1:1 to movement; releasing stops the character; holding sprint drains stamina and moves faster | 1:1 mapping confirmed in all four directions (`dir=(0,1)->Value=(0,1)`, etc.). Displacement over 0.6 s: UP `+5.388 m`, RIGHT `+3.800 m`, DOWN `-3.599 m`, LEFT `-3.599 m`, all `movedTheRightWay=True`. Release -> `moved=0.000 m` (stopped dead). Stamina emptied at `t=1.412 s` and the lock-out held while the button stayed pressed (`IsSprinting=False CanSprint=False`); the bar tracked `SPRINTING 53% -> 17% -> 0%` then `STAMINA 0%`; refill began after the `0.6 s` regen delay. **However** the measured speeds were `8.961 m/s` walking and `9.861 m/s` sprinting (ratio **1.100**) against a configured `sprintSpeedMultiplier` of **1.600** and `moveSpeed` of `6.0` - see the note below | **Partial** | Re-measure walk/sprint displacement without the scripted-teleport timing artefact (below). Direction, mapping, stop-on-release and the stamina lock-out need no action | `R03_stamina.png`, `probe_log.txt:97-146` |
| 4 | Gameplay event: touch, capture and rescue | Equal `fieldTime` captures nobody; the staler character is imprisoned and frozen; a free team-mate at `rescueDistance` frees the prisoner | Tie: `Blue_AI_1` 4.00 vs `Red_AI_1` 4.00 within `tagDistance 1.2` -> `blueCaptured=False redCaptured=False` (reproduced twice). Capture: staler `Blue_AI_1` (9.00) vs fresher `Red_AI_1` (1.00) -> Blue captured, teleported to `PrisonForBlue` (13.000,-6.500), `frozen=True`, `immunity=0.000`; Red stayed free. Rescue: rescuer `BluePlayer` 1.0 m from the prisoner (< `rescueDistance 1.5`) -> freed at (10.900,-6.500), `fieldTime` zeroed, `immunityTimer=1.636` (of 2.0), `rescueCooldownTimer=2.636` (of 3.0), `frozen=False`; the **rescuer** got `immunityTimer=0.000` | **Pass** | None | `R04_capture.png`, `probe_log.txt:148-149,177-178,196-211,270-283` |
| 5 | Score / status read-out | Freshness bar and label track time outside a base, reset inside one; stamina bar tracks sprint; team counters and status labels move with a capture and a rescue | Outside: `fieldTime 0.830 -> 3.331`, `Freshness01 0.959 -> 0.833`, fill `0.960 -> 0.835`, label `'3.3 s in the field'` (formula `1 - fieldTime/20` verified at every sample). Inside `Base_Blue`: `fieldTime=0.000`, `Freshness01=1.000`, fill `1.000`, label `'SAFE AT BASE'`. Counters before / after capture / after rescue: `blueFree 2/2 redFree 2/2 blueCaptured=0` -> `blueFree 1/2 blueCaptured=1` -> `blueFree 2/2 blueCaptured=0`, with `blueText` reading `'BLUE  2/2 free  \|  flag: AT BASE'` -> `'BLUE  1/2 free  \|  flag: AT BASE'` -> `'BLUE  2/2 free  \|  flag: AT BASE'`. Stamina bar tracked sprint as above | **Pass** | None | `R05_hud_outside.png`, `R05_hud_inside.png`, `probe_log.txt:112-146,214-227,266-286` |
| 6 | State transitions: pause, resume, restart, victory, game over | Pausing freezes the match and swaps HUD for the pause panel; resume restores; restart begins a fresh match; carrying the red flag home wins; an enemy carrying your flag home loses | Pause: `state=Paused timeScale=0.000 pausePanelActive=True hudActive=True joystickActive=False`, and over ~1 s `matchClock 24.125 -> 24.125 (delta 0.000) playerMoved=0.000 m` - both the clock and the player froze. Resume: `state=Playing timeScale=1.000`. Restart: `PLAYING after 0.221 s` / `0.193 s` (pause panel) and `0.191 s` (victory screen). Victory: human picked the red flag up (`redFlagState=Carried`), carried it into `Base_Blue` -> `state=Victory outcome=FlagCarriedHome endPanelActive=True`, `resultText='VICTORY! / You carried the red flag home.'`. Game over: `Red_AI_1` carried `Flag_Blue` into `Base_Red` -> `state=GameOver outcome=FlagCarriedHome`, `resultText='GAME OVER / The enemy carried your flag home.'`. Also observed unattended: a real AI match ended `Red_AI_2 (Red) carried the Flag_Blue into its own base -> Red WINS` at clock 16.2 s, and two further matches ended by elimination (11.0 s, 22.1 s) | **Pass** | None | `R06_pause.png`, `R06_victory.png`, `R06_gameover.png`, `R06_end_panel.png`, `probe_log.txt:236-259` |
| 7 | Android build | A real ARM64 / IL2CPP APK builds, is under the 100 MB cap, and its manifest has min SDK 26, ARM64 and landscape | `result=Succeeded errors=0` in ~99 s. `Builds/AgawanBase.apk` = **38,415,617 bytes = 36.64 MB** (**36.6%** of the 100 MB cap). APK manifest read back with `aapt`: `sdkVersion:'26'`, `native-code:'arm64-v8a'`, `android:screenOrientation=0` (= landscape). Zip contains only `lib/arm64-v8a/` (no `armeabi-v7a`), including `libil2cpp.so` and `libunity.so` | **Pass** | Rebrand the application identifier from the URP template default `com.UnityTechnologies.com.unity.template.urpblank` before any store upload | `R07_build.txt`, `probe_log.txt:287-296` |
| 8 | Camera framing and clamping | The camera shows the same arena width the camera plan measured (22.4 m) and never frames past the arena edge | `orthographicSize=5.493` at aspect 2.041; `VisibleWidth=22.425 m` = **65.000%** of the 34.500 m arena (the plan's own figure: `34.5 x 0.65 = 22.4 m`); `VisibleDepth=13.412 m`. All five probes `insideClamp=True`: EAST/WEST wall rig x `+/-11.037` against an allowed `+/-11.038`; NORTH rig z `6.794`, SOUTH rig z `-8.420`, both inside the allowed `-8.794..6.794`; CENTRE rig `(0.000,1.500)`, exactly on `followLookAhead` | **Pass** | None | `R08_camera.png`, `probe_log.txt:165-175` |

### Notes on the two rows that need them

**Row 3 - why it is Partial, and why it is a measurement problem rather than a sprint bug.**
Three of the four directional strokes moved exactly `3.599 m` in `0.6 s`, i.e. `6.0 m/s` - precisely the configured `moveSpeed 6.0`. The UP stroke and the whole WALK phase each read `~1.79 m` high instead. The derived speeds (`8.961 m/s`, `9.861 m/s`) therefore exceed the configured walking speed, so the walk/sprint ratio computed from them (`1.100`) is not a trustworthy measurement of the sprint multiplier.

The multiplier itself is wired correctly, and I confirmed that in source rather than assuming it:
- `CharacterMotor.cs:75` - `Vector3 motion = new Vector3(input.x, 0f, input.y) * (moveSpeed * speedMultiplier);`
- `SprintStamina.cs:144` - `motor.speedMultiplier = (IsSprinting ? sprintSpeedMultiplier : 1f) * carrySpeed;`

The probe also logged `IsSprinting=True` with `speedMultiplier=1.600` *during* the sprint phase (`probe_log.txt:121`), so the value reaching the motor was correct at the time. What is unresolved is the timing of the scripted measurement (the first phase after a teleport reports ~0.3 s more game time than the probe's own elapsed figure), not the sprint itself. Direction, 1:1 stick mapping and stop-on-release are all firmly verified; the absolute speed and the sprint ratio are recorded as unestablished.

**Row 6 - one earlier attempt was invalidated, correctly.**
A first capture attempt accidentally produced a total Blue elimination (`BLUE 0/2 free`) which advanced the game to `GameOver` and replaced the HUD with the end panel. That is correct game behaviour, but it is not evidence of a capture, so the attempt was discarded and re-run with the rules parked around the measurement. The `R04_capture.png` on disk is the re-run: it was written while `state=Playing` and `blueText='BLUE  1/2 free'`. The elimination itself is reported under row 6 as an observed ending.

### Actions arising

- **Row 3 (open):** re-measure walk and sprint displacement in a way that does not include the first frame after a scripted teleport, then state the sprint ratio against `sprintSpeedMultiplier 1.600`. Nothing is known to be broken; the ratio is simply not yet evidenced.
- **Row 7 (open, cosmetic but ship-blocking for a store upload):** set a real application identifier. Currently `com.UnityTechnologies.com.unity.template.urpblank`, the Unity URP template default.
- **D.5 (open, highest priority):** switch the AI read-out off, ideally with the build-time guard proposed in D.5 rather than by hand.
- **Rows 1, 2, 4, 5, 6, 8:** no action; behaviour matched the expected result.

---

## D.3 Defect found

**The AI debug read-out ships switched on.** `HudController` is serialized with `showAiDebug: true` on `Game/UI`, and its `aiDebugLabel` is wired to `Game/UI/Panel_Hud/AIDebugLabel`. During a live match the HUD was measured displaying every AI's assigned job and current state directly to the player:

```
aiDebugVisible=True  showAiDebug=True
aiDebugText='BLUE:   Blue_AI_1  DEFENDER  PATROL
             RED:   Red_AI_1  DEFENDER  RESCUE   Red_AI_2  FLEX  RETREAT'
```

(`probe_log.txt:203-204` and `:267-268`; the same was measured again at `:221-222` and `:281-286`.)

This contradicts the field's own tooltip, which is unambiguous about the intent (`HudController.cs:55-57`):

> `[Tooltip("Switch the AI read-out on. Leave it off for the shipped build: it is a debugging aid, and each line names a team-mate the player is not meant to be watching.")]`
> `public bool showAiDebug = true;`

**Why it matters for this game specifically.** AgawanBase is a tag game whose whole tension rests on the player not knowing which enemy is coming for them and why. The read-out hands over exactly that: it names which opponent is the `DEFENDER` guarding their flag, which is on `RESCUE`, which is in `PRISON`, and which is on a `CHASE`. A player can read their opponent's intent off the screen. It also leaks team-mates the player is not supposed to be tracking.

**Same class of leftover, all serialized `true`:** `FollowCamera.logView`, `MatchManager.logRules`, `MatchManager.allowDebugDrop`, `CharacterSpawner.logRoster`, `Flag.logFlagEvents`. These are lower severity - they write to the log rather than the screen - but `allowDebugDrop` is a live gameplay hook, not just a log flag, and should not ship enabled.

---

## D.4 Root cause

The flag is a serialized scene value that was switched on during the Step 7 verification pass - when the AI `AiRole`/`AiState` behaviour was being checked - and never switched back off. It is not a code regression; the code does what it is told.

What let it survive is that **nothing stops it at build time**. `HudController.UpdateAiDebug` gates the read-out on exactly two conditions (`HudController.cs:245-249`):

```csharp
if (aiDebugLabel == null) return;
bool show = showAiDebug && TeamManager.MatchActive;
if (aiDebugLabel.gameObject.activeSelf != show) aiDebugLabel.gameObject.SetActive(show);
```

`showAiDebug` is a plain serialized `bool`, and the field's default is `true`. There is no `#if UNITY_EDITOR`, no `#if DEVELOPMENT_BUILD`, and no `Debug.isDebugBuild` guard anywhere on the path. So the read-out is not an editor convenience that happens to be on - it is a shipping feature that is enabled by default, and the IL2CPP release APK built in row 7 contains it. Confirming this is what makes the defect worth a D.3 rather than a passing note: a rebuild with "Development Build" unticked would not have removed it.

---

## D.5 Proposed correction (not applied)

I did **not** apply this. It is recorded as a proposal, per decision 3 (option B, document-and-propose only), so nothing in the scene or the code was touched.

**The one-line change that applies it:** in the Inspector for `Game/UI`, untick **HudController -> Show Ai Debug** (and, for the other four, untick `FollowCamera.logView`, `MatchManager.logRules`, `MatchManager.allowDebugDrop`, `CharacterSpawner.logRoster` and `Flag.logFlagEvents`).

**The durable fix, which I recommend over the one-line change:** the reason this was able to ship is that the safety of the read-out depends on a hand-ticked checkbox. Making the non-editor case impossible to get wrong is a two-token edit to `HudController.cs:57`:

```csharp
#if UNITY_EDITOR
    public bool showAiDebug = true;      // editor default: on, so the tools stay useful
#else
    public bool showAiDebug = false;     // any player build: always off
#endif
```

That keeps the debug aid convenient while the project is being worked on and makes it structurally unavailable in a build, so a future verification pass cannot leave it on again.

---

## D.6 Evaluation

**Overall: the prototype passes its required test record.** Seven of the eight rows are a clean Pass; row 3 is a Partial for a measurement reason rather than a gameplay one. The project compiles, the screen flow works through both the reload and the no-reload route, the controls respond 1:1 and stop on release, the tag/capture/rescue rules obey their configured constants, the HUD reflects live game state, all six state transitions are correct, the camera frames the arena as designed, and a genuine ARM64/IL2CPP APK builds at 36.64 MB against a 100 MB cap.

**What the rules evidence actually shows.** The interesting rows are the ones where the rules could have been subtly wrong and were not. The tie case is the sharpest: two characters with identical `fieldTime 4.00` standing `1.0 m` apart inside a `tagDistance` of `1.2` produced no capture at all, reproduced twice - so `tieEpsilon 0.01` / `tieIsNoCapture` behaves as specified and a symmetric collision cannot steal a character. The capture then picked the *staler* character and left the fresher one free, which is the rule inverted from the obvious reading of "tag". The rescue released the prisoner with `fieldTime` zeroed, `1.636 s` of a `2.0 s` immunity and `2.636 s` of a `3.0 s` cooldown, and correctly gave the rescuer no immunity - the same asymmetry the log line calls out explicitly. Counters move consistently with all of it, and the freshness formula `1 - fieldTime/20` matched the bar fill at every one of six samples to three decimals.

**Defects.** One ship-blocking issue, D.3/D.4/D.5: the AI job-and-state read-out is serialized on and is not editor-gated, so it ships, and it leaks each AI's role to the player in a game whose tension depends on that being hidden. It is a one-line Inspector change to fix now, and a two-token conditional to fix permanently. Secondary, recorded but not raised to a defect: the bundle identifier is still the URP template default `com.UnityTechnologies.com.unity.template.urpblank` and should be rebranded before any store upload.

**One deliberate non-finding.** `FlagLabel.text` and `AlertLabel.text` retain stale strings such as `'YOU HAVE THE FLAG!  Get back to your base!'` and `'CAPTURED - waiting for a rescue'` while their GameObjects are inactive and both flags read `AT BASE`. `UpdateCarryBanner` (`HudController.cs:150`) and `UpdateAlert` (`HudController.cs:217`) hide those labels with `SetActive(false)` without clearing `.text`. The player never sees the stale text, so this is not a visible defect - but it will mislead any future test that reads `.text` without also checking `.activeSelf`. The probe records both from this run on.

**Residual risk and what I could not establish.** Row 3's absolute speed and sprint ratio are the only genuinely open measurement; the source path is correct, so what remains is instrumenting the walk/sprint displacement without the scripted-teleport timing artefact. Beyond that, the run did not cover: the HUD `"DROPPED"` flag string, long-session play, or performance on real Android hardware - the build was verified as an artifact, not as an installed and played APK.

---

## Appendix - evidence files

| File | Row | Contents |
|---|---|---|
| `Docs/Evidence/probe_log.txt` | all | Raw measurements: build settings, scene inventory, null-reference scan, timings, positions, fillAmounts, counters, label strings |
| `Docs/Evidence/R01_title.png` | 1 | Title screen |
| `Docs/Evidence/R02_matchsetup.png` | 2 | Match Setup screen |
| `Docs/Evidence/R02_hud.png` | 2 | Gameplay HUD after the match started |
| `Docs/Evidence/R03_stamina.png` | 3 | Stamina exhausted, lock-out in force |
| `Docs/Evidence/R04_capture.png` | 4 | Prisoner in the cage, `blueFree=1/2`, HUD live |
| `Docs/Evidence/R05_hud_outside.png` | 5 | Freshness bar outside the base |
| `Docs/Evidence/R05_hud_inside.png` | 5 | Freshness bar reset inside `Base_Blue` |
| `Docs/Evidence/R06_pause.png` | 6 | Pause screen, `timeScale=0` |
| `Docs/Evidence/R06_victory.png` | 6 | Victory end panel |
| `Docs/Evidence/R06_gameover.png` | 6 | Game Over end panel |
| `Docs/Evidence/R06_end_panel.png` | 6 | End panel from the unattended AI match |
| `Docs/Evidence/R07_build.txt` | 7 | Build result, APK size, manifest read-back, zip contents |
| `Docs/Evidence/R08_camera.png` | 8 | Camera framing at the phone aspect |
