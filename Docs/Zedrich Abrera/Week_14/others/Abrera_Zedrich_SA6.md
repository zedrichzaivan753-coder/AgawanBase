# SA6 — Usability and Accessibility Analysis

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development
**Module:** 5, Week 14 — Game Usability, Accessibility and Playtesting
**Project:** *Agawan Base* — a 3D mobile capture-the-flag game, two teams of two to four
**Engine:** Unity 6000.6.0f1, URP 17.6.0, Android (IL2CPP, ARM64, minSdk 26, landscape)

---

## 1. What I set out to do

Week 13 made the game *safe*: a player profile that is signed and verified, so a tampered record
is detected rather than displayed. Week 14 asks a different question, and a harder one for me
personally: **can somebody who has never played this game actually use it?**

I did not want to answer that with opinion. I have a build that plays well for me because I know
where every button is. That is precisely the knowledge a new player does not have, and it is
precisely the knowledge I cannot un-learn by trying harder. So the week's work is built on two
measurements and one rule:

| | |
|---|---|
| **Measurement 1** | A script that audited every label and button in all three scenes against WCAG 2.2 thresholds, before any changes (`Evidence/a11y_baseline.log`). |
| **Measurement 2** | The same script, unchanged, after the fixes (`Evidence/a11y_after.log`). |
| **The rule** | If the two files do not differ, nothing was fixed. The delta is the evidence; the prose is commentary. |

The third piece is the playtest in `PT6`, which is the only thing that can measure what no script
can: whether a person understands the game.

---

## 2. How the audit works, and what it does not do

`Assets/Editor/A11yAudit.cs` runs from the menu (`Tools ▸ Playtest ▸ Run Accessibility Audit`),
opens each of the three scenes in turn and walks every `Text` and `Button` under `Game/UI` —
including the panels that are inactive in the scene, because a hidden panel is still a panel a
player will meet.

**Contrast (C).** For each label it finds the nearest ancestor `Image` with opacity ≥ 0.5 and
computes the WCAG contrast ratio of the text colour against that backdrop, compositing the text's
own alpha over it. The formula is the one in WCAG 2.2: relative luminance from sRGB channels, then
`(L1 + 0.05) / (L2 + 0.05)`.

**Label fit (L).** A legacy uGUI `Text` whose line height exceeds its rect draws *nothing* — a
defect I hit in Week 6 when a 32 pt label in a 24 pt-tall box rendered as blank space
(`TeamMatchBuild.cs:212-216`). The audit checks `rect.height ≥ fontSize × 1.12` and separates
"clipped" (overflow set to Truncate: the label can vanish) from "spills" (overflow allowed: it
draws outside its box).

**Touch target (T).** Every `Button`, plus every `Graphic` that carries its own pointer handler —
which is how the two controls that matter most on a phone, the movement stick and the sprint pad,
get measured at all. They are not uGUI Buttons, so an audit that only looked at buttons would have
quietly reported on the easy half of the screen.

The floor is **144 × 144 canvas px**. That number needs stating honestly: the canvas is
`ScaleWithScreenSize`, 1920 × 1080, `match = 0.5`, so on a 1080p phone one canvas unit is about
one physical pixel, and Android's 48 dp minimum touch target is about 144 px at xxhdpi. **This is
a canvas-px measurement on a reference resolution, not a measurement taken from a device.** I have
not calibrated it against a physical ruler on a real phone, and the report does not pretend
otherwise.

**Debug read-out (D).** The sixth check is not a WCAG check at all. It reads the six serialized
debugging flags and fails if any is on, because a build handed to a playtester with an AI debug
panel visible is not the game I am trying to test.

### The limits, stated up front

- **24 labels are "not measurable"** — 8 per scene — because they are HUD text drawn straight over
  the 3D view with no backing panel. A contrast ratio against an unknown, moving backdrop is not a
  number, and I would rather report "not measurable" than invent one. `E2_hud_contrast.png` shows
  what those labels look like in play.
- Contrast is computed against the **panel colour composited over black**, which is the darkest
  plausible backdrop. For the light text this UI uses, that is the *most favourable* case — so a
  ratio that fails in the log fails on a device too, but a ratio that passes is a best case.
- No screen-reader pass, no colour-blind simulation, no text-scaling pass, no keyboard pass. Those
  are named as deferred work in §9 rather than glossed over.

---

## 3. The baseline: what the numbers said

`Evidence/a11y_baseline.log` — **345 passed, 69 failed, 0 warnings, 24 not measurable.**

69 failures across three scenes is 23 per scene, and they fall into three groups:

| Group | Per scene | What it was |
|---|---|---|
| Touch target | 12 | Every button in the game was too short. Title START / PROFILE 130 px, pause and end-screen buttons 125 px, the HUD pause button 120 px, map-select buttons 130 px, and the profile name field 100 px. |
| Contrast | 4 | The chosen setup button (white 34 pt on the bright amber `#F2B840`) at **1.80:1**; the map-select blurb at **4.36:1**; the profile name-field placeholder at **3.66:1**. |
| Debug flags | 7 | `HudController.showAiDebug`, `FollowCamera.logView`, `MatchManager.logRules`, `MatchManager.allowDebugDrop`, `CharacterSpawner.logRoster`, and `Flag.logFlagEvents` on both flags. |

Two things about that table are worth saying plainly.

**The touch failures were systematic, not accidental.** Not one button was too *narrow* — 480 px
wide is generous. They were all too *short*, by 14 to 44 px, because the UI was laid out to look
tidy in the editor rather than to be hit with a thumb while holding a phone in landscape. A
120 × 170 px pause button is comfortable with a mouse and awkward with a thumb, and the audit
cannot tell me it is *uncomfortable* — only that it is below the floor. That difference is what the
playtest is for.

**1.80:1 is not a near miss.** WCAG AA asks for 4.5:1. White on that amber is less than half of
it, and the option a player has *selected* was the hardest thing on the screen to read. The cause
was in code, not in the scene: `MatchSetupUI.Paint()` tinted the chosen button with the bright
`selectedColor` and never touched its label. `MapSelectUI.Paint()` had already solved the same
problem for its cards, with a comment explaining why the chosen card is a *dark* amber — I simply
had not applied the same rule one class over.

---

## 4. What I changed

`Assets/Editor/AccessibilityBuild.cs` applies the fixes to all three scenes from one rule set, so
the three copies cannot drift apart, and it is re-runnable. Run 1 reported 12 controls raised,
7 buttons re-spaced, 6 flags off per scene; the second run reported 0 of each, which is how I know
it is idempotent.

### F1 — Every touch target below 144 px (S2, fixed)

Every button's height was raised to 144 px. Growing a button in a fixed column can make it overlap
its neighbour, so a column that grew is re-stacked from its top button downwards with a 24 px gap —
the gap the title screen already used. The profile name field went from 100 to 144 px and the
record below it moved down to keep its spacing.

The first run of that script also made a mistake worth recording: it grouped buttons into columns
by x-coordinate, which merged the setup screen's BACK / START row with the Easy difficulty button
sitting directly above it, and pulled both nav buttons up 42 px. I caught it in the run log, added
a guard so **only columns that actually changed size get re-spaced**, and restored the two buttons
to their authored y. It is in the log, in the code comment, and here, because a tool that silently
edits three scenes has to be auditable when it is wrong.

### F2 — White label on the bright amber (S1, fixed in code)

`MatchSetupUI.Paint()` now tints the chosen option with `Color.Lerp(Color.black, selectedColor,
0.45f)` — the exact rule `MapSelectUI.Paint()` has used since the map screen existed. Measured
result: **1.80:1 → 7.26:1**.

### F3 — Map-select blurb too dim on the amber card (S3, fixed)

`Card_*/Blurb` brightened to `#EDF2FA`. Measured: **4.36:1 → 6.47:1**.

### F4 — Profile placeholder barely visible (S3, fixed)

The placeholder was white at 40 % alpha on a `#1F2633` field. It is now an opaque `#D1D9E6`.
Measured: **3.66:1 → 10.65:1**. It still reads as a hint because typed text is pure white.

### F5 — Six debugging aids on in a build a player would touch (S1, fixed)

`HudController.showAiDebug` is now `true` only under `#if UNITY_EDITOR` and `false` in every player
build, and the other five flags are switched off in all three scenes. This is the item I had
already written up as ship-blocking in Part D (D.5): the read-out lists each AI's role and its
current state on screen. **A playtester who can read enemy intent is testing a different game from
the one I am shipping**, and any finding about difficulty or clarity from such a session would be
worthless. It had to be off before the playtest build, not in Week 15.

### F6 — The game was silent (S1, fixed — see §5)

### F7 — No way to turn the sound off (S3, fixed)

A SOUND button on the pause screen, wired to `GameFeedback.ToggleSound()`, remembering the choice
in `PlayerPrefs["audio.feedback"]`. Its label reads `SOUND: ON` / `SOUND: OFF` rather than
depending on colour or an icon, because "the button looks different" is not a state a player can
read reliably at arm's length.

![E1 — the pause screen in play mode: the new SOUND: ON button in the top-right corner, and the four-row button column re-spaced after every button grew to 144 px.](Evidence/E1_pause_sound.png)

---

## 5. Why sound and haptics are an accessibility fix, not polish

Before this week there was not a single `AudioClip` in the project. Catching an enemy, rescuing a
team-mate, stealing a flag and losing a match all produced the same feedback: a number on the HUD
changed. On a phone, held at arm's length, in a room that is not silent, that is not enough — and
for a player who struggles to read a fast-changing HUD, it is nothing at all.

`Assets/Scripts/Core/GameFeedback.cs` is one component that listens and never decides:

| Game event | Feedback |
|---|---|
| `MatchManager.CharacterCaptured` | catch clip + one `Handheld.Vibrate()` buzz |
| `MatchManager.CharacterRescued` | rescue clip |
| `Flag.PickedUp` | flag clip |
| `MatchManager.PlayerWon` | victory stinger |
| `MatchManager.PlayerLost` | defeat stinger |

Getting there needed two new events, and the reason is worth recording. The only existing hook was
`CaptureCountChanged`, which fires once with a *count* and only for Red captures — so a listener
could not tell a catch from a rescue, or one catch from the next. Adding
`CharacterCaptured`/`CharacterRescued` (raised at the end of `Capture()` / `Rescue()`, after the
rules have finished) and `Flag.PickedUp` (raised in the one place a pick-up is decided) keeps the
house rule that a rule lives in one place and everything else listens.

The five clips were generated for this project (catch, rescue, flag pick-up, victory, defeat) and
are 15–29 KB each.

### The evidence that it fires

A claim that "the setter plays a sound" is not evidence. `Evidence/feedback_lines.txt` is a
transcript taken from a **real AI match** (`Tools ▸ Week14 ▸ Record AI Match Feedback`), not from
me invoking handlers:

```
catch - Blue_AI_3 (Blue) is locked up -> Catch
catch - Red_AI_3 (Red) is locked up -> Catch
catch - Blue_AI_1 (Blue) is locked up -> Catch
rescue - Red_AI_3 (Red) is free -> Rescue
catch - Red_AI_4 (Red) is locked up -> Catch
rescue - Red_AI_4 (Red) is free -> Rescue
flag picked up by Red_AI_4 -> FlagPickup
defeat - your flag was carried home -> Defeat
```

Four of the five kinds fired. **Victory did not**, because the player's team lost that match. It
shares the same `Play()` call and the same wiring pattern as defeat, but I have not observed it and
I am not going to write that I have. It is on the deferred list in §9 and the playtest will almost
certainly produce one.

![E3 — the match one second after a capture: the free-count on the HUD has already moved, and the catch line exists in the console at that moment.](Evidence/E3_catch_feedback.png)

---

## 6. WCAG 2.2 mapping

| SC | Level | What it asks | How this game stands | Status |
|---|---|---|---|---|
| 1.4.1 Use of Colour | A | Colour is not the only way information is conveyed | Chosen setup/map options are also spelled out in the summary line; the SOUND button says ON/OFF in words; team sides use position and labels, not only blue/red | Meets |
| 1.4.3 Contrast (Minimum) | AA | 4.5:1 for normal text | Measured per label; 12 failures found, all fixed | Was 12 failures → **0** |
| 1.4.4 Resize Text | AA | Text can be enlarged to 200 % | **Not met.** The UI is fixed-size uGUI at a 1920 × 1080 reference; there is no in-game text-size setting and no OS-level scaling path | Deferred, Week 15 |
| 1.4.11 Non-text Contrast | AA | 3:1 for component boundaries | Buttons are solid `#33425C` on a 0.75-alpha black backdrop; the stick and sprint pad are solid discs | Meets (by inspection; not scripted) |
| 1.4.13 Content on Hover or Focus | AA | Hover content dismissible | No hover content on touch | N/A |
| 2.1.1 Keyboard | A | Everything operable by keyboard | **Not met.** Touch and on-screen controls only | N/A for a touch-only game; noted |
| 2.2.2 Pause, Stop, Hide | A | User can pause moving content | The pause screen stops the match | Meets |
| 2.4.7 Focus Visible | AA | Focus is visible | Buttons use a colour-tint transition on press; there is no keyboard focus in play | Partly (touch only) |
| 2.5.5 Target Size (Enhanced) | AAA | 44 × 44 px | **144 px floor**, i.e. ~48 dp, above the 44 px bar | Meets after this week |
| 2.5.8 Target Size (Minimum) | AA (2.2) | 24 × 24 px | 144 px floor | Meets after this week |
| 2.5.2 Pointer Cancellation | A | Act on release, not press | uGUI `Button` fires on release; the stick reads drag, not tap | Meets |
| 3.2.4 Consistent Identification | AA | Same function, same label | Built by one builder script per screen, so labels cannot drift | Meets |
| 4.1.2 Name, Role, Value | A | Controls expose accessible names | **Not met.** uGUI text is not exposed to TalkBack; no accessibility plug-in installed | Deferred, Week 15 |

Two of those rows are the ones I want on the record: **1.4.4 and 4.1.2 are genuinely not met**, and
neither of them is a small fix on a canvas that is authored at a fixed reference resolution with
legacy uGUI `Text`.

---

## 7. The delta

| | Baseline | After |
|---|---|---|
| Passed | 345 | **423** |
| Failed | **69** | **0** |
| Warnings | 0 | 0 |
| Not measurable (HUD text over the 3D view) | 24 | 24 |
| Debug flags on | 21 (7 × 3 scenes) | **0** |

Both logs end in a `TOTAL:` line; both were produced by the same script, on the same three scenes,
with the same thresholds. The after-run also measures the new SOUND button, which is why the
passed count rises by 78 rather than 69: 69 failures became 69 passes, plus 3 scenes × 3 checks for
the new button.

The audit's own failures are also gone from the console, and `check_compile_errors` is clean.

![E2 — the in-match HUD after the fix. There is no AI debug read-out anywhere on screen, and the labels drawn straight over the 3D view are the 24 checks this audit reports as "not measurable" rather than guessing a ratio.](Evidence/E2_hud_contrast.png)

---

## 8. What this analysis does not claim

- It does not claim the game is **accessible**. It claims the game now passes the three thresholds
  I could measure automatically, and that two WCAG criteria remain unmet and are named.
- It does not claim the numbers were measured **on a device**. They were measured on the canvas at
  the reference resolution, and the conversion to dp is an assumption stated in §2.
- It does not claim the playtest in `PT6` agrees with it. Where the testers' findings contradict
  anything here, PT6 wins and the contradiction gets written up.
- A 0-failure audit is a floor, not a ceiling. The audit cannot see a button that is the right size
  in the wrong place, a label that is legible but meaningless, or a sound nobody notices — which is
  exactly why the sound-noticed count in `04_results_table.md` exists.

---

## 9. Deferred work

| # | Item | Why deferred | Week |
|---|---|---|---|
| D1 | Text scaling (WCAG 1.4.4) | Needs a layout pass on a fixed-reference canvas, not a scale factor | 15 |
| D2 | Screen-reader names (WCAG 4.1.2) | Needs an accessibility plug-in and a per-control labelling pass | 15 |
| D3 | Victory stinger never observed firing | Not produced by the recorded AI match; playtest will exercise it | 15 (with PT6 results) |
| D4 | The 24 HUD labels drawn over the 3D view | Unmeasurable by a colour check; needs a frame-capture contrast pass or a scrim behind the HUD | 15 |
| D5 | Colour-blind check on the two team colours | Needs a simulation, not a ratio | 15 |
| D6 | Sprint speed while carrying the flag (`8.961` measured vs `9.861 m/s` expected, ratio 1.100 against a configured 1.600) | Carried over from Part D; still unmeasured on hardware | 15 |

---

## 10. References

- World Wide Web Consortium. (2024). *Web Content Accessibility Guidelines (WCAG) 2.2.*
  https://www.w3.org/TR/WCAG22/
- World Wide Web Consortium. (2023). *Understanding Success Criterion 1.4.3: Contrast (Minimum).*
- Google. (2024). *Material Design — Accessibility: touch target size (48 dp).*
- Unity Technologies. (2024). *Canvas Scaler* and *Text* (legacy uGUI) documentation.
- Huang, Z., & Benyoucef, M. (2023). A systematic literature review of mobile application
  usability. *Universal Access in the Information Society, 22*, 715–735.
- Weichbroth, P. (2024). Usability testing of mobile applications: A methodological framework.
  *Applied Sciences, 14*(5), 1792.
