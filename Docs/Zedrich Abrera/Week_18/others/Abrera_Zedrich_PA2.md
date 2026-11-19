# PA2 — Complete Thematic Mobile-Game Demonstration and Defense

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development
**Module:** Module 5 — Individual Thematic Mobile Game Development Laboratory
**Week:** 18 · **Assessment:** PA2
**Deadline:** Friday, December 4, 2026, 11:59 PM

---

## 1. Submission at a glance

| Item | Value |
|---|---|
| Game | **Agawan Base 3D** — a 3D capture-the-flag mobile game built from the Filipino street game *Agawang Base* |
| Theme | **Philippine Games and Sports** |
| Target player | Students and young adults who played *agawan base* as children, on a mid-range Android phone |
| Submitted build | `Abrera_Zedrich_PA2.apk` |
| Build MD5 | **`ef38de8dd65100109791002a67738da1`** |
| Build size | **33,170,107 bytes** (31.63 MB) |
| Version the app reports | `1.0.0-rc1  -  built 2026-11-19` |
| Source commit of that build | `84c0f2e` |
| Release tag | `v1.0.0-rc1` → `07057bd` |
| Final PA2 tag | `v1.0.0-pa2` on the PA2 submission commit |
| Engine | Unity 6000.6.0f1, URP 17.6.0, IL2CPP, ARM64, Android API 26+ |
| Backup package | `Abrera_Zedrich_PA2_Windows.zip` — MD5 `fe17f2060c994218e4593b2010eb9a98` |
| Repository | `main`, published at `https://github.com/zedrichzaivan753-coder/AgawanBase`; `v1.0.0-pa2` peels to `5e63b1c` — see §10 |

**The submitted APK is not a new build.** It is the Week 17 release candidate, copied byte for byte,
because every verification result, screenshot, audit and profiling number in this submission belongs
to those exact bytes. Rebuilding would have produced a file that inherits none of that evidence.
The full argument is in `Evidence/release_record.md` §2, and the consequence is stated plainly: the
app still reports `1.0.0-rc1` on its title screen, while the tag `v1.0.0-pa2` marks the submission
commit. Version strings in this project are not cosmetic — `DEF-17-02` recorded that putting the
version into `productName` silently orphaned every saved setting and player record.

---

## 2. The game

### 2.1 Theme

*Agawang Base* is a Filipino street game of two bases and two teams. One team defends its base while
the other tries to steal a member — or, in this adaptation, a flag — and the tension comes from the
moment you leave your own base undefended. Agawan Base 3D keeps that structure and makes it
playable on a phone in landscape, with a short 180-second match that fits a class break.

The theme is not a skin. The mechanics that make the street game recognisable are the mechanics the
code implements: a base you can be tagged near, a prisoner you can free, a flag that decides the
match, and short immunity after a rescue so a freed player is not tagged again instantly.

### 2.2 Target player and what that decided

The target player is someone who played *agawan base* as a child and now has a mid-range Android
phone and ten minutes. That single sentence drove concrete decisions:

- **Landscape, two thumbs.** Controls sit at the bottom corners, and every button is at least
  144 canvas px (≈ 48 dp) tall, so the controls are reachable while gripping a phone in two hands.
- **30 fps target, not 60.** A steady 30 suits a top-down chase game and roughly halves the thermal
  cost on a mid-range phone. The profile in §8 is measured against a 33.3 ms budget.
- **The rules teach themselves.** The HOW TO PLAY screen exists because the game does not assume the
  player knows the house rules; it explains tagging, rescuing and the flag in the same words the
  code uses.

### 2.3 Core loop

Landscape top-down chase: move with the on-screen stick, tag opponents, free your teammates, steal
the red flag and carry it home while your own base holds. A match is won by eliminating the opposing
team or by capturing the flag. Difficulty is Easy, Normal or Hard, applied **identically to both
teams** so the setting changes how demanding the match is rather than quietly handicapping the
player's side.

### 2.4 The rules, as implemented

These are the constants in code, not design intentions:

| Rule | Value | Where |
|---|---|---|
| Tag reach | 1.2 m | `MatchManager.cs:88` |
| Flag steal reach | 1.5 m | `MatchManager.cs:91` |
| Rescue reach | 1.5 m | `MatchManager.cs:110` |
| Immunity after rescue | 2 s | `MatchManager.cs:119` |
| Rescue cooldown | 3 s | `MatchManager.cs:123` |
| Match time limit | 180 s (0 disables) | `MatchManager.cs:128` |
| Dropped flag auto-returns | 10 s | `Flag.cs:44` |
| Team sizes | 1v1 to 4v4 | `MatchSettings` |

The two safeguards that keep a match from stalling or looping are worth naming: a just-freed player
ignores rescuers for the cooldown (`MatchManager.cs:416`), which stops endless chain-rescues, and a
dropped flag returns itself after ten seconds (`Flag.cs:181`), which stops a match becoming a
standoff over a flag nobody can reach.

---

## 3. The submitted build

### 3.1 Identity

| Field | Value |
|---|---|
| File | `Abrera_Zedrich_PA2.apk` |
| Size | 33,170,107 bytes |
| MD5 | `ef38de8dd65100109791002a67738da1` |
| Built | 2026-11-19, 15:20 local, 24.8 s |
| Type | **Release** — Development Build off, App Bundle off |
| Backend | IL2CPP, ARM64 only |
| Minimum Android | API 26 (Android 8.0) |
| `bundleVersion` / `versionCode` | `1.0.0-rc1` / **2** |
| Application id | `com.zedrich.agawanbase` |
| Scenes | `Game` (index 0), `Map_Palengke`, `Map_SchoolCampus` |

The archive holds **419 entries**, with `classes.dex` and `AndroidManifest.xml` at the root — no
`BundleConfig.pb` and no `base/dex/` prefix. That distinguishes a real APK from an Android App Bundle
wearing an `.apk` filename, which no handset will install (`DEF-15-04`).

### 3.2 What the app reports about itself

The title screen prints its own build identity, so the build in front of an examiner can be checked
against this document without any tooling:

```
1.0.0-rc1  -  built 2026-11-19
```

That string is produced by `ReleaseInfo.cs` and is visible in every title capture in this
submission, starting with Figure 1.

---

## 4. The demonstration sequence

The sequence below is not a script written from memory. It was driven end to end in one 14.2-second
run by `Assets/Editor/Week18Shots.cs`, which forced a repaint before each capture and rejected any
image identical to an earlier one — so a stale frame could not be filed as evidence. The record is
`Evidence/demo_rehearsal.txt`, with a wall-clock offset for every step, and all nine captures have
distinct MD5s.

![Figure 1. Onboarding — the title screen with the version caption that identifies the build, and the four entry points: START, PROFILE, HOW TO PLAY, CREDITS, EXIT. Captured 2.33 s into the rehearsal.](Evidence/D1_title.png)

![Figure 2. Onboarding — the instructions screen. It teaches tagging, rescuing and the flag in the same terms the rules code uses, because the game does not assume the player knows the house rules.](Evidence/D2_how_to_play.png)

![Figure 3. Credits, licences and the AI-disclosure block. The module requires credits, licences and disclosure to be verifiable, so they are a screen in the game and not only a line in a document.](Evidence/D3_credits.png)

![Figure 4. Theme mechanics — map selection across three maps: the classic open field, the Palengke, and the School Campus.](Evidence/D4_map_select.png)

![Figure 5. Settings — match setup. Team size from 1v1 to 4v4 and difficulty Easy, Normal or Hard, applied identically to both teams.](Evidence/D5_match_setup.png)

![Figure 6. The core gameplay loop running: top-down chase, on-screen stick, overhead freshness and stamina bars, and the base ring.](Evidence/D6_match_running.png)

![Figure 7. Settings during a match — the pause screen, with SOUND and TAG HINTS, both of which remember their state.](Evidence/D7_pause_settings.png)

![Figure 8. Completion — the end screen after a real elimination win, produced by the rules rather than forced.](Evidence/D8_end_screen.png)

![Figure 9. Progression — a live match again 3.4 s after RESTART, proving the loop closes and reopens without leaving the app.](Evidence/D9_restart.png)

What the sequence demonstrates, requirement by requirement:

| Module requirement | Demonstrated by |
|---|---|
| Onboarding | Figures 1–3 |
| Core gameplay loop | Figure 6 |
| Theme-related mechanics | Figures 4 and 6 |
| Settings | Figures 5 and 7 |
| Data or service features | The profile and settings persistence behind Figures 1 and 5 |
| Error handling | The end/restart transition (Figures 8–9) and the profile-integrity rejection in §6 |
| Completion or progression | Figures 8 and 9 |

---

## 5. Architecture at a glance

Five folders under `Assets/Scripts/`, compiled into three assemblies — `AgawanBase.Runtime`,
`AgawanBase.Editor`, `AgawanBase.Tests.EditMode`:

- **`Core`** — the data and rules primitives: `MatchSettings`, `MapDefinition`, `Team`, `TeamManager`,
  `PlayerProfile`, `CharacterStatus`, `CharacterMotor`, `SprintStamina`, `GameFeedback`, `ReleaseInfo`.
- **`Gameplay`** — `MatchManager` (the referee), `EnemyAI` split into `.Perception`, `.Scoring` and
  `.Actions` partials, `Flag`, `HideSpot`, `PlayerController`, `CharacterSpawner`, `MapNavigator`.
- **`UI`** — `GameManager` (the screen state machine), `HudController`, `MapSelectUI`, `MatchSetupUI`,
  `ProfileUI`, `OffScreenMarkers`, `VirtualJoystick`, `SafeAreaFitter`, `SprintButton`.
- **`Visual`** — presentation that must not own rules: `CharacterBars`, `IndicatorController`,
  `CharacterBushHide`, `CharacterFacing`, `CharacterRigAnimator`, `FollowCamera`.
- **`Diagnostics`** — `PerfCapture`, `PerfScenario`.

The separation that matters most in a defense is that **`Visual` reads rules and never writes them**.
`IndicatorController.cs:24` states the contract in a tooltip, and it exists because a presentation
layer that mutates match state is how a "won" match ends up disagreeing with the HUD.

The full diagram is `Docs/Zedrich Abrera/Week_17/Evidence/architecture.svg`, and the consolidated
technical account is the companion `Abrera_Zedrich_PA2_Technical_Report.pdf`.

---

## 6. Security and player data

The one piece of player data the game holds is the profile. It is stored as a **file**, not
`PlayerPrefs`, precisely so it can carry an integrity signature (`PlayerProfile.cs:80–95`): a
SHA-256 over a per-install random salt followed by the payload JSON.

What it does: a profile edited by hand no longer matches its signature, so it is rejected and set
aside as `profile.rejected.json` instead of being trusted (`PlayerProfile.cs:537–540`).

What it does **not** do: it cannot stop a determined attacker who controls the device, because the
salt is on that same device and there is no server to verify against. The source says so in its own
comment rather than overclaiming, and this document repeats it.

That limit was observed working in an unplanned place: the Windows backup package, sharing a
persistent data path with the editor, found an editor-written profile, could not verify it, refused
it and preserved it. That is recorded in `Evidence/windows_backup_build.md` §5 — a real rejection,
not a hypothetical one.

Secret handling is recorded in `Week_17/Evidence/secret_scan.md`, with the raw scan output kept
beside it.

---

## 7. Testing and evidence

| Area | Result | Where |
|---|---|---|
| EditMode tests | **17 / 17 passed**, 0 failed, 0 skipped | `run_tests`, re-run 2026-11-19 on this tree |
| Accessibility audit | **516 passed, 0 failed, 24 not measurable** | `Week_17/Evidence/readiness_results.md` |
| Balance / quality scenarios | **16 scenarios, 0 failures** at 4v4 | Week 16 records |
| Usability playtest | 5 testers, 25 task attempts, **0 failures**, 23 unaided, 5/5 noticed both sounds | Week 14 records |
| Demonstration sequence | 9 captures, 9 distinct MD5s, 14.2 s | `Evidence/demo_rehearsal.txt` |
| Performance | main thread **0.59–0.63 ms**, GPU **0.48 ms**, 17 render events, ~320 B/frame | §8 |
| Release readiness | 4 release screens verified in play mode, `VERDICT: working` | `Week_17/Evidence/readiness_results.md` |

---

## 8. Optimisation

Week 16 profiled the shipping path and made one change worth naming: **prop texture memory fell from
21,849 KB to 1,369 KB, a reduction of 20.0 MB**, by removing texture memory the low-poly art was not
using. The measured frame cost afterwards was a main thread of **0.59–0.63 ms** and a GPU time of
**0.48 ms** across **17 render events**, with about **320 bytes** allocated per frame, against a
**33.3 ms** budget at render scale **0.80**.

One point of honesty that the technical report repeats: the Week 16 → Week 17 download shrank from
68.30 MB to 31.63 MB because the build changed from development to release. **That is not claimed as
an optimisation.** Nothing was removed from the game; debug symbols and development checks were.

---

## 9. The Windows backup package

`Abrera_Zedrich_PA2_Windows.zip` (39.39 MB, MD5 `fe17f2060c994218e4593b2010eb9a98`) is a compiled
Windows player built from the same tree, provided because **no Android handset is attached to the
machine that produced this submission** (`adb devices` is empty). It was launched and confirmed to
reach the title screen, printing the same `1.0.0-rc1  -  built 2026-11-19` string as the APK.

It is a **backup**, and it differs from the submission in three disclosed ways: it runs **Mono**
rather than IL2CPP, it has **no haptics** (`GameFeedback.cs:174` guards the catch buzz behind
`UNITY_ANDROID || UNITY_IOS`), and two post-processing passes — Gaussian/Bokeh depth of field and
Panini projection — are stripped from the Standalone build. Detail and the player log excerpt are in
`Evidence/windows_backup_build.md` §6.

A backup that is quietly different from the submission is worse than none, so the differences are
listed rather than glossed.

---

## 10. Limitations and honest gaps

Written down because the module asks for limitations and because the alternative is a document that
claims more than the evidence supports.

1. **The APK was not installed on a handset for this submission.** No device is available. The
   submitted file is hashed and unmodified, but "it installs and runs on a phone" is not something
   this Week 18 submission demonstrates. It is marked **BLOCKED**, not passed.
2. **The repository was local-only until 2026-11-19.** `origin/main` sat at `d08fc9b`, which predates
   Weeks 15–17, so at the time of writing the instructor could not verify the final tag or commit from
   the remote. It has since been published: `v1.0.0-pa2` peels to `5e63b1c`, and `v1.0.0-rc1` and
   `week16-final` resolve with it, with `origin/main` carrying the PA2 commit plus the documentation
   commits after it. `Evidence/repo_state.txt` keeps the pre-push state as a record.
3. **E3 is sat under supervision** and had not been sat when this document was written.
   `Evidence/e3_record.md` is a record with fields, and `Evidence/defense_prep.md` holds pointers to
   files and commits, never scripted answers.
4. **Two Week 14 discoverability findings were never re-tested** against the rebuilt instructions
   screen.
5. **AI balance rests on 16 regression scenarios**, not on a large-scale study.
6. **The Windows backup has not been playtested on a desktop.** The Week 14 usability data applies to
   the handset APK, not to this package.

---

## 11. What is in this folder

| Path | What it is |
|---|---|
| `Abrera_Zedrich_PA2.apk` | The submitted build, byte copy of the Week 17 release candidate |
| `Abrera_Zedrich_PA2.pdf` | This document |
| `Abrera_Zedrich_PA2_Technical_Report.pdf` | Architecture, systems, security, testing, optimisation |
| `Abrera_Zedrich_PA2_User_Guide.pdf` | Installation, controls, rules, troubleshooting |
| `Abrera_Zedrich_PA2_Evidence_Portfolio.pdf` | Every artefact from Weeks 13–18, plus the unproven list |
| `Abrera_Zedrich_PA2_Originality_Declaration.pdf` | Authorship, third-party and AI disclosure, signature |
| `Abrera_Zedrich_PA2_Presentation.pptx` | The defense deck |
| `Abrera_Zedrich_Week18_Reflection.pdf` | Week 18 reflection |
| `Abrera_Zedrich_PA2_Windows.zip` | Backup installation package |
| `Evidence/` | Records, captures, rehearsal log, repository state |
| `others/` | Markdown and HTML sources for these documents |

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
