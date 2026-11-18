# Bush Invisibility — Hide the Body and Overhead Bars Under Cover

## Objective
Make every character (the player included) completely disappear — low-poly body, blob shadow, and the green/yellow overhead bars — while it stands inside any hiding spot, and reappear the moment it steps out.

## Changes
- `Assets/Scripts/Visual/CharacterBushHide.cs` - (create) per-character component that reads `CharacterStatus.isHidden` in `LateUpdate` and toggles the `Visual` child once per change.
- `Assets/Scripts/Visual/CharacterBars.cs` - treat "inside cover" as a third gate in `Apply()` (alongside `visible` and `isConfigured`) and re-apply on the frame the gate flips.
- `Assets/Scripts/Visual/IndicatorController.cs` - add `BushHideFor(CharacterStatus)` and call it from the `DriveBars()` loop so every body in the roster self-heals the new component.

## Relevant Assets and Quirks
- The body lives entirely under the character's **`Visual` child**, built by `Assets/Editor/BarangayRig.cs`: torso, jersey hem, head, hair, face, arm/leg pivots and the flat **`Blob` shadow quad**. It holds **no colliders** (rig builder destroys them), so deactivating it cannot affect movement.
- **Never** deactivate the character root: `CharacterController`, `CharacterMotor`, `CharacterStatus`, `CharacterFacing`, `EnemyAI` and `SprintStamina` all live on it.
- `CharacterStatus.isHidden` is the game's own stealth flag: an XZ radius test against every `HideSpot`, already read by `MatchManager` and `EnemyAI`. It is **false while carrying a flag** (a thief is never covered) and is **not refreshed while captured** (`Update` returns early), so the check must also require `!isCaptured` — otherwise a body captured on the exact frame it was hidden would stay invisible in prison.
- Hiding is therefore **rule-consistent for free**: flag carriers and prisoners stay visible, bushes, School Campus garden hedges and Palengke `Crates_*` stacks (`PalengkeBuild.CrateStack`, radius 1.15) all hide, because they share the same `HideSpot` rule.
- Per the user's choice, `CharacterIndicator`'s ground ring, icon and yellow chevron **stay visible** — only the body and the two bars vanish. Bars are a separate root child pair (`BarFreshBack/BarFreshFill/BarStamBack/BarStamFill`), so they are gated independently.
- `IndicatorController.DriveBars()` calls `bars.SetVisible(live)` every 0.15 s, so bush suppression **must** live inside `CharacterBars.Apply()` — setting it from the controller would be overwritten on the next pass.
- `IndicatorController.BarsFor()` self-heal is the one guaranteed per-body hook (all 8 bodies register in `TeamManager`), so **no prefab or scene edit is needed**; keep the same pattern for the new component.
- Bars must keep their left-anchored scale+offset and shared materials untouched; this change only flips `gameObject.SetActive` on the four bar parts.
- The project's `.unity` scenes are binary-serialized (text grep cannot read them) and the editor is **currently in Play mode** with `Game.unity` open — exit play mode before editing, and do not save scenes.
- Domain reload is off; the new component holds no statics, so nothing can go stale between play sessions.
- Cost: one bool read per body per frame plus one `SetActive` on a state flip. Nothing per-frame is added to `IndicatorController`.

## Steps
- [x] 1. Create `Assets/Scripts/Visual/CharacterBushHide.cs` with `write_file`: `[DisallowMultipleComponent]`, fields `visual` (falls back to `transform.Find("Visual")` in `Awake`) and cached `CharacterStatus`; in `LateUpdate` compute `hide = status.isHidden && !status.isCaptured`, return immediately when unchanged, then `visual.gameObject.SetActive(!hide)` and log `Debug.Log("[Hide] " + name + " is under cover - body and bars hidden.")` / `"... out of cover - body and bars visible again."`; warn once if no `Visual` child is found. [depends: none]
- [x] 2. Edit `Assets/Scripts/Visual/CharacterBars.cs` with `edit_file`: add `bool shownHidden`, a `HiddenByCover` property (`status != null && status.isHidden && !status.isCaptured`), make `Apply()` use `bool show = visible && isConfigured && !HiddenByCover;`, and in `LateUpdate` after the existing `visible/isConfigured` early return detect a flip in `HiddenByCover`, call `Apply()`, and `return` before touching the fills while hidden. [depends: 1]
- [x] 3. Edit `Assets/Scripts/Visual/IndicatorController.cs` with `edit_file`: add `void BushHideFor(CharacterStatus character)` that `AddComponent<CharacterBushHide>()` when missing, and call it next to `BarsFor(member)` in `DriveBars()`. [depends: 1]
- [x] 4. Run `check_compile_errors` and `get_unity_logs`; fix anything reported before verifying. [depends: 2,3]
- [x] 5. Delegate to `unity_verification_agent`: open `Assets/Scenes/Map_SchoolCampus.unity`, play, walk/teleport the human into `Bush_Garden_*` and confirm body + both bars vanish while the ring/arrow stay, stepping out restores them, and a flag carrier under cover stays visible; then spot-check a Palengke `Crates_*` stack. [depends: 4]
  - NOTE: `unity_verification_agent` returned INCONCLUSIVE (timed out re-filtering the console, no verdict), so the runtime check was done inline on `Map_SchoolCampus`: 6/6 bodies carried `CharacterBushHide` + `CharacterBars`; human in `Bush_Garden_0_0` -> isHidden=True, isUnderCover=True, Visual inactive, all four bar parts off, Ring still on; stepping out restored everything; `Red_AI_3` walked into `Bush_Garden_2_0` and hid by itself. `[Hide]` enter/exit pairs logged once per event, no per-frame spam. Temp probe `Assets/Editor/AuraBushVerify.cs` deleted, compile clean. Flag-carrier and Palengke-crate cases were not exercised live; they follow from the same `isHidden` flag.

## Verify
- Console shows `"[Hide] <name> is under cover ..."` and `"... out of cover ..."` pairs exactly once per entry/exit, with no per-frame log spam.
- In `Map_SchoolCampus` play mode: walk the human into a garden bush — the low-poly body and the green + yellow bars disappear, the yellow chevron of the controlled character is still drawn, and `CharacterStatus.isHidden` is `true`.
- Step out: body and both bars return immediately, and the green freshness bar still tracks `fieldTime` as before.
- Pick up a flag and stand in a bush: the body and bars stay **visible** (the game's cover rule refuses to hide a carrier).
- Teleport a Red AI into a bush: it vanishes too, its teammates/opponents are unaffected, and no collider, NavMesh or AI behaviour changes (the character root is never touched).
- `check_compile_errors` returns `"No compile errors"`.
