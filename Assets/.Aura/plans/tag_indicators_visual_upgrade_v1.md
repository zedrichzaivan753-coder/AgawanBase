# Tag Status Indicators (Visual/UI upgrade)

## Objective
Add world-space, colourblind-safe indicators that show which character the player controls and, for each opponent, whether the player can tag them or would be tagged - reading the existing tag rule rather than re-implementing it, with zero gameplay/AI/balance change.

## Locked decisions
1. **No base special case** (Q1-a). An opponent parking in its own base is shown as **Danger** when its `fieldTime` 0 really would beat the player. The indicator never lies about the rule.
2. **One small custom shader** (Q2-a): `IndicatorUnlit.shader` with `ZTest [_ZTest]` so markers draw over props. No second camera, no pipeline change.
3. **Toggle in the Pause panel** (Q3-a), persisted in PlayerPrefs, coexisting with the existing settings owner `MatchSettings`.

## Verified ground truth
- **URP 17.6.0 is the active pipeline**, not Built-in: `QualitySettings.asset:51 customRenderPipeline` -> `Assets/Settings/Mobile_RPAsset.asset` (quality 0 = `Mobile`).
- **`m_UseSRPBatcher: 1`** -> `MaterialPropertyBlock` would BREAK batching. Use a **few shared materials** and swap `sharedMaterial` only on change (the proven `CharacterRigAnimator` pattern).
- **`m_RequireDepthTexture: 0`** and **`m_RendererFeatures: []`** -> a depth/edge outline and a renderer-feature outline are both impossible without a pipeline change. This is why option (b) inverted-hull is rejected.
- **`Mat_Blob`** is already `Universal Render Pipeline/Unlit`, transparent, `_ZWrite 0`, queue 3000 - but URP/Unlit exposes **no `_ZTest`**, so it cannot draw over props. Hence the new shader. `BarangayBuild.cs:720-743 Blob()` is the material convention to copy.
- **Camera is 100% static**: `Game/CameraRig` X 325 -> `Main Camera` local (0,27.7,3.3) rot X 90 => world (0,24.58,-13.18), pitch 55, ortho size 14. `grep CameraRig|Main Camera|Camera.main|orthographic` over all `Assets/Scripts` = **no matches**. So **billboarding is a constant computed once**, not a per-frame cost.
- **Occlusion is real but height-bounded**: `BasePost_Blue/Red` are 2.4 m x **7.0 m** at z 4.9, *inside* the arena. Prison posts are only 1.2 m, shorter than the 1.55 m character, so harmless. Camera occlusion culling culls nothing (`m_OcclusionCullingData: {fileID: 0}`).
- **Pixel budget**: 1600x720 at ortho 14 = **25.7 px/m**. A 0.24 m ring is ~6 px; a 0.42 m icon is ~11 px. Nothing thinner than 0.22 m is legible.
- Prefabs are `1.00 x 1.55 x 1.00`; only two exist (`BluePlayer.prefab`, `RedEnemy.prefab`). All AI spawn from `RedEnemy.prefab`.
- **Palette**: jerseys are blue (0.16,0.44,0.86) and red (0.83,0.23,0.18); backdrop is olive (0.38,0.5,0.32) / ground (0.42,0.46,0.3); **orange is already taken** by `Mat_WarmAccent` (0.86,0.58,0.24) on the jeepney, tricycles and banderitas. There is **no cyan, no magenta and no bright green** anywhere - those are the free channels.
- `MatchManager.ResolveTouch` is the tag rule; `tieEpsilon 0.01`, `tagDistance 1.2`, `tieIsNoCapture true`, `requiresBothOutsideBases false`.
- `Panel_Pause` holds `ResumeButton`/`RestartButton`/`TitleButton`, each with a `Label` child - the convention the new button follows.

## Changes
**Modify (3 scripts only)**
- `Assets/Scripts/Gameplay/MatchManager.cs` - add `public bool WouldWinTag(CharacterStatus a, CharacterStatus b)`; make `ResolveTouch` call it, so the fieldTime verdict exists in exactly one place. No behaviour change.
- `Assets/Scripts/Core/MatchSettings.cs` - add `TagHints` + key `match.tagHints` to `Load`/`Save` (this file is the project's declared single owner of PlayerPrefs).
- `Assets/Scripts/UI/GameManager.cs` - add `public void ToggleTagHints()` and a `tagHintsLabel` reference, mirroring the existing `Pause`/`Resume` handlers.
- `Assets/Prefabs/BluePlayer.prefab`, `Assets/Prefabs/RedEnemy.prefab` - attach `CharacterIndicator`; assign the 5 shared materials.
- `Assets/Scenes/Game.unity` - add `Game/Systems/IndicatorController`; add `Game/UI/Panel_Pause/TagHintsButton` with a `Label` child.

**Add (4 scripts + 1 shader + 5 materials)**
- `Assets/Scripts/Visual/IndicatorMeshes.cs` - static cached mesh builder: solid ring, 6-segment dashed ring, check, X, chevron. Built in code, no imported assets, shared by every character.
- `Assets/Scripts/Visual/CharacterIndicator.cs` - one character's ring + icon + arrow; state setter, bob, billboard.
- `Assets/Scripts/Visual/IndicatorController.cs` - scene singleton; finds the human once, computes verdicts on a staggered timer, pushes states.
- `Assets/Shaders/IndicatorUnlit.shader` - URP unlit, `ZWrite Off`, `Cull Off`, `ZTest [_ZTest]` (default `Always`), `UnityPerMaterial` CBUFFER so SRP batching still applies.
- `Assets/Materials/Mat_Indicator_{You, YouDim, CanTag, Danger, Neutral}.mat`.

## WouldWinTag - the single source of truth
```csharp
/// <summary>Would `a` capture `b` if they touched? The ONLY place the fieldTime verdict is written.
/// ResolveTouch calls this, and the tag indicators call this, so the ring can never disagree
/// with the rules. Range and the base rule stay where they already live (CheckCharacterTouches).</summary>
public bool WouldWinTag(CharacterStatus a, CharacterStatus b)
{
    if (a == null || b == null) return false;
    if (a.isCaptured || b.isCaptured) return false;      // prisoners cannot fight
    if (a.IsImmune || b.IsImmune) return false;          // a just-rescued character is untouchable
    if (tieIsNoCapture && Mathf.Abs(a.fieldTime - b.fieldTime) <= tieEpsilon) return false;  // draw
    return a.fieldTime < b.fieldTime;
}
```
`ResolveTouch` keeps its existing early-return draw branch and logging; only the `bool blueWins = ...` line is replaced by `WouldWinTag(blue, red)`. The indicator does **not** re-derive the rule - it adds only "is it near enough to matter".

## Indicator state table (relative to the player)
| State | Ring | Icon | Condition |
|---|---|---|---|
| **You** | magenta, solid, **large** | magenta chevron above head | `TeamManager.IsHuman(ch)` |
| **You (captured)** | magenta, **alpha 0.30** | chevron alpha 0.30, **bob stopped** | `ch.isCaptured` |
| **Can tag** | green, **solid** | green **check** | `free(me) && free(opp) && near && WouldWinTag(me, opp) && (opp.fieldTime - me.fieldTime) > tieEpsilon + hysteresis` |
| **Danger** | red, **dashed** | red **X** | same but `WouldWinTag(opp, me) && (me.fieldTime - opp.fieldTime) > tieEpsilon + hysteresis` |
| **Neutral** | grey, alpha 0.55 (**hidden** by default) | none | everything else: captured, immune, out of range, or inside the dead band |
| **Teammate** | *(none by default)* | *(none)* | optional small team marker |

`free(x) = !x.isCaptured && !x.IsImmune`; `near = FlatDistance(me, opp) <= warnRange`. Can-tag and Danger are mutually exclusive by construction (one fieldTime must be lower), so a ring is never ambiguous.

| Tuning value | Setting | Basis |
|---|---|---|
| `tagDistance` | **1.2** (read from `MatchManager`, never copied) | the real rule |
| `warnRangeMultiplier` | 2.0 -> `warnRange` **2.4 m** | warns just before contact |
| `hysteresis` (serialized) | **0.15 s** | widens the dead band to +/- 0.16 s with `tieEpsilon`, so near-equal fieldTimes read Neutral |
| `verdictHoldSeconds` | **0.2 s** | a verdict must persist before it is shown - the AI's `minStateTime` idea, applied to the ring |
| `refreshInterval` / stagger | **0.15 s** / `spawnIndex * 0.02` | the requested 0.1-0.2 s cadence, never per frame |
| ring outer/inner (tag) | **0.60 / 0.36 m** | 0.24 m thick ~ 6 px at 25.7 px/m |
| ring outer/inner (you) | **0.78 / 0.54 m** | visibly "larger", as specified |
| `ringY` | **0.04 m** | `Court_Slab` top is exactly 0.03 - avoids z-fighting |
| arrow | **0.50 x 0.40 m** at y **2.05 m** | 0.5 m above the 1.55 m head; ~12 px |
| icon | **0.42 m** at y **1.95 m** | above the head, never over the body |
| bob | **0.07 m @ 1.6 Hz** | the only per-frame work; freezes while paused via `Time.deltaTime` |
| colours | You (0.95,0.30,0.95) / green (0.25,1.00,0.35) / red (1.00,0.10,0.10) / grey (0.62,0.62,0.64) | magenta, green and cyan are the only channels this palette leaves free; orange is taken by `Mat_WarmAccent` |

## Attachment and 1v1-4v4 creation
`CharacterIndicator` lives on **both prefabs**, so every body builds its own markers in `Awake()` - nothing size-specific, and the hand-placed `BluePlayer` instance is covered by the same prefab:
```
<character>/Indicator          (CharacterIndicator - MeshFilter/MeshRenderer only, NO collider)
  |- Ring    y 0.04  world-identity rotation (cancels the character's yaw)
  |- Icon    y 1.95  constant billboard
  \- Arrow   y 2.05  created once, enabled only for the controlled character
```
- Translation happens in **`LateUpdate`**, after `CharacterMotor` and `MatchManager` have finished - the same reason `Flag.cs` uses `LateUpdate`.
- The ring's world rotation is set to identity so the character turning cannot spin it.
- `IndicatorController` resolves the human **once** by scanning `TeamManager.Members(Team.Blue)` for `IsHuman`, re-resolving when `TeamManager.rosterVersion` changes. Opponents come from the existing cached `TeamManager.Members(Team.Red)` registry - **no `FindObjectsOfType` in `Update`**.
- Because the markers are on the prefab, 1v1/2v2/3v3/4v4 are automatic: 1-4 Red AI each grow their own ring and icon.

## Steps
- [x] 1. Create `Assets/Shaders/IndicatorUnlit.shader` + the 5 `Mat_Indicator_*` materials via a temporary editor script, copying the `BarangayBuild.Blob()` conventions. *(done when: a test quad with `ZTest=Always` draws over the 7 m `BasePost_Blue` and over the jeepney; the file has a `UnityPerMaterial` CBUFFER)* [depends: none]
- [x] 2. Add `MatchManager.WouldWinTag` and route `ResolveTouch` through it. *(done when: `check_compile_errors` clean AND a 1v1 regression produces touch/draw/rescue log lines identical to the recorded baseline; `get_worst_cpu_frames` unchanged)* [depends: none]
- [x] 3. Add `IndicatorMeshes.cs` + `CharacterIndicator.cs`, attach to both prefabs, assign the 5 materials. *(done when: every character shows a ground ring, the human shows a bobbing magenta chevron, and both prefabs still contain ZERO colliders on the indicator objects)* [depends: 1]
- [x] 4. Add `IndicatorController.cs` in `Game/Systems`; wire `WouldWinTag` + the distance/free gates into the staggered refresh. *(done when: at 4v4 each opponent's ring matches the state table against a hand-checked `(me.ft, opp.ft, distance)` log, and the profiler shows 0 B/frame GC)* [depends: 2,3]
- [x] 5. Add `MatchSettings.TagHints` + `Panel_Pause/TagHintsButton` + `GameManager.ToggleTagHints()`. *(done when: the button label reads `TAG HINTS: ON/OFF`, toggling removes only the opponent rings, and the choice survives a Restart and an editor restart)* [depends: 4]
- [ ] 6. *(OPTIONAL)* flag-carrier icon, dimmed ring + cage icon for prisoners, small team mark on teammates. *(done when: each reads clearly and none of steps 1-5 regresses)* [depends: 5]

## Execution notes

Steps 1-5 are built and verified on disk. Step 6 (the optional extras) was **skipped** - it is marked OPTIONAL in the plan, so the run stops at step 5.

**What was created**
- `Assets/Shaders/IndicatorUnlit.shader` - URP unlit, `ZWrite Off`, `Cull Off`, `ZTest [_ZTest]` default Always, `UnityPerMaterial` CBUFFER so the SRP Batcher path stays intact. Compiles (materials 849-858 bytes, all reporting colour + `ztest=Always`).
- `Assets/Materials/Mat_Indicator_{You,YouDim,CanTag,Danger,Neutral}.mat` - five SHARED materials, never per-instance.
- `Assets/Scripts/Visual/IndicatorMeshes.cs`, `CharacterIndicator.cs`, `IndicatorController.cs`.

**What was modified**
- `MatchManager.cs` - `WouldWinTag` added at line 282; `ResolveTouch` now routes through it at line 314. Verified by grep.
- `MatchSettings.cs` - `TagHints` + key `match.tagHints` (default ON).
- `GameManager.cs` - `tagHintsLabel` + `indicatorController` fields, `ToggleTagHints()`, `RefreshTagHintsLabel()`, and the caption refresh inside `SetState`.
- `Assets/Prefabs/BluePlayer.prefab` and `RedEnemy.prefab` - `CharacterIndicator` added to both.
- `Assets/Scenes/Game.unity` - `Game/Systems/IndicatorController` (match + 5/5 materials, `sceneSaved=True`); `Game/UI/Panel_Pause/TagHintsButton` at y -380.

**Bug found and fixed during step 5.** `TagHintsButton` was duplicated from `TitleButton` to inherit the project's button styling - but a duplicated Button carries its persistent `onClick` listeners, so the new button arrived calling `GoToTitle on GameManager`. The wiring script's double-add guard refused to add a second listener, which would have left the button throwing the player back to the title screen. Logged evidence: `inherited listeners before = 1 [GoToTitle on GameManager]`, then `TagHintsButton.onClick rebuilt: listeners 1 -> 1 method=ToggleTagHints`. Two legitimate `GoToTitle` listeners remain elsewhere in the scene.

**Note on the pause-screen refresh.** `Time.timeScale` is 0 while paused, so `Time.deltaTime` is 0 and `IndicatorController`'s refresh timer never reaches its interval. Without `ForceRefresh()` from `ToggleTagHints()` the rings would not change until after Resume.

**Cleanup.** All 8 throwaway editor scripts used for setup/patching/wiring were deleted; `Assets/Editor` is back to its 7 original files. `check_compile_errors` clean.

**Correction to the plan's pixel budget.** The plan says 25.7 px/m from 1600x720 at ortho 14. The active URP asset has `m_RenderScale: 0.8`, which the plan omitted, so the real figure is ~20.6 px/m and a 0.24 m ring is nearer 5 px than 6. The chosen sizes are unchanged - they were picked at the pessimistic end already - but TC-I8's readability bar should be judged against ~20.6 px/m.

## Verify
- `check_compile_errors` clean and `EditorApplication.isPaused == false` before any Play-mode reading.
- `stats_get` draw calls at 2 vs 8 characters: **+2 renderers per character**; SRP batch count must NOT rise per-material (proves the shared-material rule held).
- `get_worst_cpu_frames` at 4v4: verdict refreshes <= 28/s and AI + movement still under the existing budget.
- `capture_editor_screenshot` in Play mode at 4v4, plus `capture_ui_canvas` on `Game/UI/Panel_Hud`, to prove no ring overlaps the joystick (x 120-380, y 120-380), sprint (1685-1885, 150-350) or pause button.

## Test cases (for the test record)
| # | Test | Expected |
|---|---|---|
| TC-I1 | Spawn 1v1/2v2/3v3/4v4 | Correct ring count; exactly ONE arrow; still one after Restart |
| TC-I2 | Log `(me.ft, opp.ft, distance)` beside the shown verdict at 4v4 for 60 s | Every verdict agrees with `WouldWinTag`; no contradictions |
| TC-I3 | Force fieldTime differences 0.05 / 0.16 / 0.40 s | Neutral / Neutral / Can tag-Danger; **0 flips** over 60 s |
| TC-I4 | Opponent in its own base while the player is stale | Reads **Danger** (no base special case) |
| TC-I5 | Player captured, then rescued | Markers dim + stop bobbing; opponent rings all Neutral; after immunity they resume |
| TC-I6 | Stand an opponent behind `BasePost_Blue` and `BasePost_Red` | Ring and icon still drawn (ZTest Always) |
| TC-I7 | Toggle tag hints off, then Restart, then restart the editor | Opponent rings gone, arrow stays, setting persists |
| TC-I8 | **Readability**: screenshot at 1600x720 worst-case | Solid vs dashed rings distinguishable; icon shapes legible at ~11 px; nothing thinner than 0.22 m |
| TC-I9 | **Colourblind**: grayscale + deuteranopia-filter the same screenshot | The three verdicts remain distinguishable by **dash pattern + icon shape alone**, with colour ignored |
| TC-I10 | Pause mid-match | Rings freeze (they do not keep changing while frozen); no marker redraws on top of `Panel_Pause` |
| TC-I11 | Perf at 4v4 | `stats_get` and `get_worst_cpu_frames`; +2 renderers/character; 0 GC allocations in the TagIndicator frames |

## Edge cases
- **I am captured** - my ring and chevron switch to `Mat_Indicator_YouDim` (alpha 0.30) and the bob stops, so "dimmed" reads as inactive, not as a colour change.
- **Both in bases** - both `fieldTime` are 0, so `|diff| <= tieEpsilon` is a draw -> **Neutral**. This is the rule being told honestly.
- **Nearly equal fieldTime** - the 0.15 s hysteresis plus the 0.2 s latch hold Neutral; the ring cannot strobe.
- **Opponent carrying the flag** - **no special case**; the tag rule never consults the flag. The optional flag-carrier icon covers it. Note `Flag` is a scene object, so the prefab cannot hold the reference: resolve it once from `MatchManager.FlagOf`, exactly as `CharacterRigAnimator.Start` resolves its flag.
- **A character is freed** - `IsImmune` is true for 2 s, so it reads Neutral for that window; the ring returns on the first refresh after immunity lapses.
- **Restart** - the reload rebuilds every indicator in `Awake`; `MatchSettings` is static and PlayerPrefs persists, so the toggle survives.
- **Pause** - `Time.deltaTime` is 0, so the bob freezes for free (the `Flag.cs` / `HudController` pattern). Gate the verdict refresh on `TeamManager.MatchActive`.
- **Camera angle** - fixed and static, so the billboard is a constant computed once. If the camera is ever animated, move the billboard into the per-frame path; nothing else changes.
- **Arrow behind a prop** - `ZTest [_ZTest]` on the new shader draws the marker over the 7 m `BasePost_*`. This is the whole reason option (a) needs the shader.

## Risks
- **`replace_in_file` is unsafe on this project** (it previously injected merge markers into `EnemyAI.cs`, `GameManager.cs`, `MatchManager.cs`). Rewrite whole files with `write_to_file`.
- **Bridge scene/prefab edits do not mark the asset dirty.** Any `set_property` / `duplicate_game_object` must be followed by a forced `EditorSceneManager.SaveOpenScenes()` or a `AssetDatabase.SaveAssets()`, or the wiring is silently discarded on the next scene load.
- **The `ResolveTouch` refactor must not change behaviour.** Diff the 1v1 touch logs against the baseline before proceeding past step 2.
- **No colliders** may be added to any indicator object, and no `StaticEditorFlags` - the indicators must not join the static batch or affect the 0 NavMeshAgents.
