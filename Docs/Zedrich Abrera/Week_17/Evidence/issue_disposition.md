# Week 17 — issue disposition

Every issue that surfaced while freezing, building and verifying the release candidate, what was done
about it, and what it means for the artefact. Nothing here is left as "noted for later": at a feature
freeze an issue is either fixed, or carried as a known limitation with its reason stated.

Severity is about the release, not about effort: **A** = the APK is wrong for a reviewer or a player,
**B** = the evidence would be wrong or unrepeatable, **C** = cosmetic or process.

| Id | Sev | Issue | Found by | Disposition |
|---|---|---|---|---|
| DEF-17-01 | **A** | Credits screen printed version **`0.2`** while the title printed `1.0.0-rc1` | Reading the captured `E3_credits.png` | **Fixed**, APK rebuilt |
| DEF-17-02 | **A** | `productName` was `AgawanBase v1.0.0-rc1`, which moves the PlayerPrefs namespace and `persistentDataPath`, silently discarding saved settings and the player profile | The quality suite failing 3 cases after the roster came back as 1v1 | **Fixed**, APK rebuilt |
| DEF-17-03 | B | Screenshot harness produced stale frames: E1, E2, E3 byte-identical, all showing the title | Hashing the six captured files | **Fixed** in the harness, all six re-captured |
| DEF-17-04 | B | Harness's restart wait counted editor update ticks; 900 ticks elapsed in under a second, so a scene reload still in progress was declared failed and E6 was never taken | The `restart never reached a live match` note in `week17_shots.txt` | **Fixed** (waits on real time), re-run |
| DEF-17-05 | B | Harness's elimination step gave up the first time no free Red was returned, so one run ended without an ending | The `elimination win not reached` warning in the run log | **Fixed** (waits up to 20 s, caps at 12 captures), re-run |
| DEF-17-06 | B | Title screen's `RulesText` (*"Galingan mo boss"*) sat at centre −330, underneath the opaque PROFILE button, so it had never been visible at any aspect | Laying out the Week 17 title row | **Fixed** in step 3 — the whole title stack is now top-anchored; visible in `E1_title_release.png` |
| DEF-17-07 | **A** | `Panel_HowTo/Body` needed **734.5 px** of text in a **660 px** rect, so the last ~74 canvas units of the rules — the tail of *PAUSING AND SETTINGS* — rendered **behind the BACK button** | Verifying the release candidate in play mode, after the accessibility audit had passed it | **Fixed**, three scenes rebuilt, APK rebuilt |
| ENV-17-01 | C | Quality suite reported 13 passed / 3 failed / 1 blocked on two consecutive runs | `scenario.tsv` | **Not a game defect** — the 4v4 roster precondition was missing; see below |
| OBS-17-01 | C | `E6_restart.png` was byte-identical to `E4_match_running.png` | Hashing the captures | **Dissolved by the re-capture** — the two differ in the current run; see below |
| OBS-17-02 | C | Console fills with `Releasing render texture that is set as Camera.targetTexture!` | Editor console | **Not from the game** — see below |
| BLOCKED-17-01 | — | Install and launch on an Android handset from a clean state | — | **BLOCKED**, no device — see `clean_install.md` |

## DEF-17-01 — the credits screen said 0.2

`ReleasePanelsBuild.BuildCredits()` wrote the build line as a **string baked into the scene**:
`ReleaseInfo.Title + "  -  " + ReleaseInfo.BuildId`, evaluated when the builder last ran. The builder ran
before the version was unfrozen, so the scene captured the string `AGAWAN BASE  -  0.2  -  built …`, and
because a scene string never re-evaluates, the release candidate went on displaying `0.2` on its credits
screen while the title caption — which reads `Application.version` at runtime — correctly showed
`1.0.0-rc1`.

Fixed the same way the title caption was already fixed: `GameManager.creditsVersionLabel` is filled in
`Awake()` from `ReleaseInfo.Title` and `ReleaseInfo.BuildId`, so it cannot drift from the binary it is
running inside. The builder now wires that reference and warns if the `BuildLine` object is missing.
Confirmed in `E3_credits.png`, which reads `AGAWAN BASE  -  1.0.0-rc1  -  built 2026-11-19`.

## DEF-17-02 — the version was part of the app's identity

The build record originally listed `productName` as `AgawanBase v1.0.0-rc1`. Unity derives two things
from that string: the `PlayerPrefs` namespace and `Application.persistentDataPath`. So tagging the
release candidate with its version gave it:

- `persistentDataPath = …\LocalLow\Zedrich Zaivan B. Abrera\AgawanBase v1_0_0-rc1` — a **new** folder,
  so `profile.json` was gone and every player would have started from a blank record on "upgrade";
- an empty `PlayerPrefs` store, so the saved match settings reverted to defaults.

It was caught indirectly and that is worth being exact about: the quality suite suddenly scored 3
failures after being clean in Week 16, and the cause was the roster coming back as **1v1** instead of
the saved **4v4**. The product name — not any gameplay code — was what had moved the settings.

Fixed by keeping the identity stable: `productName` is `AgawanBase`, and the version lives in
`bundleVersion` (`1.0.0-rc1`), which is what `Application.version`, the title caption and the credits
line all read. The APK was rebuilt, and the MD5 in `build_and_commit.txt` is the new one.

## ENV-17-01 — three scenario failures that were not failures of the game

Two runs of the Week 15 suite reported 13 passed / 3 failed / 1 blocked:

- `TC-CORE-05 rescue frees a prisoner` — *blocked* ("the roster did not provide both") and then
  *failed* ("the prisoner reference was lost"). The case needs **two Reds**: one to imprison and one to
  walk in and free them. A 1v1 roster has one.
- `TC-LIFE-01 resume continues the match` — "clock did not advance in 0.7 s after Resume".
- `TC-LIFE-03 backgrounding pauses the match` — "State was Victory".

All three are the same precondition. With a single Red on the field, the suite's own capture eliminates
the entire red team, so the match is already over when the life-cycle cases run: the clock is frozen
because the match ended, and the state is `Victory` because the game did exactly what the rules say.

With the documented **4v4** roster restored the same untouched code scored **16 passed, 0 failed,
0 blocked** — identical to the Week 16 gate. Recorded here so the next person who runs the suite into a
1v1 default does not go looking for a regression that is not there. The suite's precondition is 4v4;
`MatchSettings.SetTeamSize(4)` restores it.

## DEF-17-07 — the rules ran out of their box

The instructions screen is the one screen the module's completeness bar demands and the only one a
first-time player reads before touching anything. Its body text was authored at **26 px inside a
1560 × 660 rect at y 0**, and a legacy `Text` with `verticalOverflow = Overflow` does not clip: it
draws down from the top edge of the rect and straight out of the bottom. Measured, the paragraph
needed **734.5 px**, so roughly **74 canvas units** — one and a half lines, the end of the *PAUSING
AND SETTINGS* paragraph — sat behind the BACK button, whose top edge is at −331.6 while the text ran
to −404.5.

It was not caught earlier because every gate measured something else: the accessibility audit checks
contrast, touch-target size and *rect* height against font size, all of which were fine; the
screenshot review read the text as legible, and it was — for the first twenty-two lines. It took
play-mode verification reporting the panel's own numbers to surface it.

Fixed in the builder rather than by hand, so the same mistake cannot come back:

- the box moved to **1560 × 600 at y +30**, which stops it above the BACK button with 102.8 px to
  spare on a 20:9 phone (canvas 966 tall, BACK top −299) and 135.4 px at the editor's aspect;
- `ReleasePanelsBuild.FitText` now compares `Text.preferredHeight` against the box and decrements the
  font size until it fits, logging `Body fitted at font 21: 526.2 px inside a 600 px box` for each of
  the three scenes, and warning loudly if even the minimum does not fit.

The font settled at **21 px**; 22 px needs 642.7 px of text at this width, and widening the box to
1740 px saves a single line while eating the side margins on a 16:9 device, so the narrower box at a
smaller size is the better trade. All three scenes were rebuilt from the builder, `E2_how_to_play.png`
was re-taken and shows the rules ending clear of the button, and the APK was rebuilt — `ef38de8d…`
in `build_and_commit.txt`.

## OBS-17-01 — E4 and E6 were the same bytes, and now are not

In the capture run that preceded `DEF-17-07`, `E4_match_running.png` and `E6_restart.png` both hashed
to `1a24411b…`. That was not a stale frame: the harness hashes every shot, forces a Game View repaint,
and re-requests a shot that repeats an earlier one — it retried three times and got the same bytes
each time. The restarted match really did begin from an identical state, so its first seconds rendered
identically to the first match's, and the pair was kept and disclosed rather than dressing E6 up to
look different.

After the panel fix the six screens were captured again and the pair is **distinct**: E4
`aed46379…`, E6 `40a7cef2…`. The AI's opening positions are not identical frame-for-frame between
runs, which is the only difference; nothing in the restart path changed. Either outcome is sound
evidence, because the harness re-requests any shot whose hash it has already seen, so a repeated
frame cannot pass as a new one. The proof that E6 is a *fresh* match stays textual in both cases:
`week17_shots.txt` records *"restart reached a live match again 3.4s into the reloaded scene"*, and
`Time.timeSinceLevelLoad` only reads 3.4 s if the scene was reloaded.

## OBS-17-02 — the render-texture console noise

`Releasing render texture that is set as Camera.targetTexture!` appears repeatedly in the editor console.
The stack trace names the tool that captures canvas screenshots, not the game; it appears only while a
capture is running and never in a player build. Cosmetic, no action.

The three builds of the APK reported **28**, then **15**, then **13** warnings, and 0 errors each time.
The sources are nearly the same; the difference is build-cache state in IL2CPP and the asset pipeline
(the rebuild took 24.8 s against the first build's 331 s for exactly that reason), and no warning in
any of the three lists is an error.
