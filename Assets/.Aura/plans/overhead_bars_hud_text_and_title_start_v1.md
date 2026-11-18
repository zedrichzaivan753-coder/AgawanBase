# Overhead Freshness/Stamina Bars, Top-Left HUD Text, and Title-Screen Start

## Objective
Move the green freshness and yellow stamina bars out of the screen HUD and onto every character in the field, tidy the remaining HUD text into one block in the upper-left corner, and make every launch start on the title screen.

## Changes
- `Assets/Scripts/Visual/CharacterBars.cs` - (create) per-character world-space bar pair driven by `CharacterStatus.Freshness01` and `SprintStamina.Stamina01`.
- `Assets/Scripts/Visual/IndicatorController.cs` - owns the shared bar materials and hands bars to every roster member, exactly as it already hands out `CharacterIndicator`.
- `Assets/Materials/Mat_BarBack.mat`, `Mat_BarFresh.mat`, `Mat_BarStamina.mat` - (create) three shared materials on the existing `Agawan/IndicatorUnlit` shader.
- `Assets/Scripts/UI/HudController.cs` - stops driving the two screen bars; the `Image` fields stay in code but are left empty.
- `Assets/Scenes/Game.unity`, `Assets/Scenes/Map_Palengke.unity`, `Assets/Scenes/Map_SchoolCampus.unity` - delete `Panel_Hud/FreshnessBarBg` + `StaminaBarBg`, re-stack the text top-left, wire the bar materials.
- `Assets/Scripts/UI/GameManager.cs` - reset the two start flags on play entry so the title screen is never skipped.

## Relevant Assets and Quirks
- `Game/UI/Panel_Hud` is a full-screen stretch panel; its children are anchored top-left `(0,1)`, pivot `(0,1)`, `x = 50`. Current text: `FreshnessLabel` ("SAFE AT BASE" / "9.6 s in the field", 700x52 at y -112), `StaminaLabel` ("STAMINA 100%"), `BlueStatusLabel` (y-left), `RedStatusLabel` (top-RIGHT, y -112), `AIDebugLabel` (1000x64 at y -306, horizontal overflow spills off the right edge), `FlagLabel`, `AlertLabel`.
- `HudController` wiring today: `freshnessFill = Panel_Hud/FreshnessBarBg/FreshnessFill`, `staminaFill = Panel_Hud/StaminaBarBg/StaminaFill`, plus the five labels above; `showAiDebug = true`.
- Bar values: `CharacterStatus.Freshness01` = `1 - fieldTime/20` (1 = fresh, 0 = stale). `SprintStamina.Stamina01` is 0..1 and the component is on the human **and every AI** ("the Blue player and both Red enemies all use this same script"), so the yellow bar has a real value for all bodies.
- Existing head furniture from `CharacterIndicator`: ground ring 0.04, icon `iconY = 1.95`, chevron `arrowY = 2.05` (character height 2.0). The bars go above those, at y 2.26 and 2.40.
- Materials must reuse `Assets/Shaders/IndicatorUnlit.shader` (`_BaseColor`, `_ZTest`; default `_ZTest 8` = Always). Use `_ZTest 4` (LEqual) for the bars so a bar behind a stall wall is hidden instead of drawing through it.
- `IndicatorController` already refreshes all markers 6.7x a second while `TeamManager.MatchActive` and self-heals a missing component per character (`IndicatorFor`). The bars ride that same pass - no per-frame scene scans.
- Camera never rotates (fixed 55 deg pitch follow rig), so `IndicatorController.BillboardRotation` gives a constant quaternion for the bars.
- Mesh cost rules from `CharacterIndicator`: `MeshFilter` + `MeshRenderer` only, `ShadowCastingMode.Off`, no shadows, `LightProbeUsage.Off`, `ReflectionProbeUsage.Off`, shared materials only (SRP Batcher is on).
- Title flow already works in code and wiring: `GameManager.Start()` -> `GameState.Title`; `Panel_Title/StartButton` -> `ShowMapSelect` -> map card -> `LoadMap` -> `Panel_MatchSetup` -> `StartGame`.
- Why play skips the title: domain reload is off (`m_EnterPlayModeOptions: 3`), so the `static bool startPlayingOnLoad` / `startOnMatchSetup` in `GameManager` keep their value from the previous play session.
- All three scenes must be edited: `Game.unity`, `Map_Palengke.unity`, `Map_SchoolCampus.unity`. Do not touch `Game/Environment` or the NavMesh.

## Steps
- [x] 1. Create `Assets/Scripts/Visual/CharacterBars.cs`: build under the character root a back quad + green fill quad at y 2.40 and a back quad + yellow fill quad at y 2.26 (0.90 x 0.085 m, billboarded with the rotation passed in); each frame set `fill.localScale.x = value01` and `fill.localPosition.x = -0.45f * (1f - value01)` from `CharacterStatus.Freshness01` (green) and `SprintStamina.Stamina01` (yellow), with `Debug.LogWarning` when a body has no `SprintStamina`. [depends: none]
- [x] 2. Create `Mat_BarBack` (0,0,0,0.55), `Mat_BarFresh` (0.20,0.85,0.30,1) and `Mat_BarStamina` (1.00,0.78,0.12,1) in `Assets/Materials` with shader `Agawan/IndicatorUnlit` and `_ZTest = 4` (via `execute_script`). [depends: none]
- [x] 3. Edit `Assets/Scripts/Visual/IndicatorController.cs`: add `barBackMaterial` / `barFreshMaterial` / `barStaminaMaterial` fields, a `BarsFor(CharacterStatus)` self-heal mirroring `IndicatorFor`, and in `Refresh()` walk `TeamManager.Members(Team.Blue)` and `TeamManager.Members(Team.Red)` calling `SetVisible(live)` on each character's bars. [depends: 1,2]
- [x] 4. Wire those three materials into `Game/Systems/IndicatorController` in `Game.unity`, `Map_Palengke.unity` and `Map_SchoolCampus.unity` with one `execute_script` pass (open, assign, save each scene). [depends: 2,3]
- [x] 5. In all three scenes: delete `Game/UI/Panel_Hud/FreshnessBarBg` and `Game/UI/Panel_Hud/StaminaBarBg`, clear `HudController.freshnessFill` / `staminaFill`, and stack the text left-aligned at `x = 50` - FreshnessLabel y -28 (font 30), StaminaLabel y -68 (28), BlueStatusLabel y -106 (26), RedStatusLabel y -142 (26, moved over from the right edge), AIDebugLabel y -182 (20, word wrap on, 900x120) so nothing spills off-screen. [depends: none]
- [x] 6. Edit `Assets/Scripts/UI/GameManager.cs`: add `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetPlaySessionFlags()` clearing `startPlayingOnLoad` and `startOnMatchSetup`, and `Debug.Log("[Title] starting on the title screen")` in the Title branch of `Start()`. [depends: none]
- [x] 7. Verify: `check_compile_errors` clean, then `unity_verification_agent` in `Map_Palengke` and `Map_SchoolCampus` - launch lands on `Panel_Title`, START -> map select -> match setup -> match, and a screenshot shows green + yellow bars over every character, no HUD bars, roster text inside the top-left corner. [depends: 4,5,6]
  - Note: compile check clean. Bars confirmed on 8/8 bodies (green tracks fieldTime, refills in base, frozen while captured; yellow drains sprinting and refills after) and the HUD bullet confirmed (no bar objects, five text lines inside the top-left block, widest right edge 640 px of 1139). The launch-flow bullet was contradicted only in its WORDING: a map card click only selects, the map list's START MATCH opens match setup, and the match setup's START begins the match - Title -> map list -> match setup -> match does work, with one confirm press per screen by design.

## Verify
- Enter play mode: `Panel_Title` is up on every launch, START opens the map list, a card leads to match setup, START MATCH begins the match.
- In the live match each of the 7-8 bodies carries a green bar (shrinks the longer it is outside its base, refills on entering base) and a yellow bar (shrinks while sprinting, refills after the sprint stops); a captured body's green bar stays frozen where it was.
- HUD no longer shows the two bars; "SAFE AT BASE" / "STAMINA 100%" / "BLUE n/n free | flag: AT BASE" / "RED n/n free | flag: ..." and the roster lines all sit in the upper-left block and none of them runs off the right edge.
