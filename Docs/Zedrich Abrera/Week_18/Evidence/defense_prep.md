# Defense Preparation — E3 Pointer Sheet

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Assessment:** E3 — Architecture, Integration, Optimization, Security, and Game-Development Examination

---

## How to use this sheet — read this first

The module lists **"Reading an AI-generated defense response"** as a common mistake, and the Week 18
purpose statement requires that the student *"explain the code and decisions without relying on
another person or an AI-generated response during the defense."*

**This document therefore contains no answers.** It is an index. Each question below is followed
only by *where to look* — the exact file and line, the commit, the test, or the evidence file that
holds the answer. The answering is the examination, and it has to be done from memory and from
reading the code during the viva.

Every pointer here was checked against the working tree, not recalled. If a line number has drifted,
the file is right and the number is stale — open the file.

---

## 1. Architecture

| Question | Where the answer lives |
|---|---|
| How is the game structured into layers? | `Assets/Scripts/` — five folders: `Core`, `Gameplay`, `UI`, `Visual`, `Diagnostics`. Assembly definitions: `AgawanBase.Runtime`, `AgawanBase.Editor`, `AgawanBase.Tests.EditMode` |
| How do screens transition? | `Assets/Scripts/UI/GameManager.cs` — the `State` enum and `ShowTitle`/`ShowHowTo`/`ShowCredits`/`ShowMapSelect`/`ShowMatchSetup`/`Pause`/`Resume`. Note which transitions are guarded (e.g. `Pause()` only from `Playing`) |
| Where is the match loop? | `Assets/Scripts/Gameplay/MatchManager.cs` — the rules block in the header comment, then the per-frame tagging / flag / rescue passes |
| How is the AI organised? | `EnemyAI.cs` plus three partials: `EnemyAI.Perception.cs`, `EnemyAI.Scoring.cs`, `EnemyAI.Actions.cs` — why split that way is in the file headers |
| How is a map defined and loaded? | `Assets/Scripts/Core/MapDefinition.cs`, `MapSettings.cs`; scenes `Game`, `Map_Palengke`, `Map_SchoolCampus` |
| What draws the architecture diagram? | `Docs/Zedrich Abrera/Week_17/Evidence/architecture.svg` |
| Which component reads the rules but must never write them? | `Assets/Scripts/Visual/IndicatorController.cs:24` (the tooltip states the contract) |

## 2. Integration

| Question | Where the answer lives |
|---|---|
| How does the map selection reach gameplay? | `MatchSettings` + `MapDefinition`; the player log line `[Map] loaded selection: Classic Open Field (id=classic, scene=Game)` |
| How are team size and difficulty persisted? | `Assets/Scripts/Core/MatchSettings.cs`, `PlayerPrefs`; log line `[Settings] loaded 1v1 (0 Blue AI + 1 Red AI) at NORMAL skill, tag hints ON.` |
| How is the version string surfaced in the UI? | `Assets/Scripts/Core/ReleaseInfo.cs`; evidence: title capture `D1_title.png` shows `1.0.0-rc1  -  built 2026-11-19` |
| What happens on RESTART? | `GameManager.Restart()`; timings in `Evidence/demo_rehearsal.txt` (restart reached a live match 3.4 s into the reloaded scene) |
| How do the three assemblies depend on each other? | the `.asmdef` files under `Assets/Scripts/`, `Assets/Editor/`, `Assets/Tests/EditMode/` |

## 3. Optimization

| Question | Where the answer lives |
|---|---|
| What did optimisation actually change? | Week 16 documentation + `Assets/Scripts/Diagnostics/PerfCapture.cs`, `PerfScenario.cs`; the marker names are recorded in `week16-profiling-conditions` evidence and the Week 16 documents |
| What are the measured numbers? | Main thread **0.59–0.63 ms**, GPU **0.48 ms**, **17** render events, ~**320 B/frame**, against a **33.3 ms** budget at render scale **0.80**; prop texture memory **21,849 KB → 1,369 KB** (−20.0 MB) |
| How was the mobile render path measured on a desktop GPU? | The Week 16 conditional-path technique — the conditions are written in the Week 16 evidence, not invented here |
| Where is the frame budget enforced? | `GameManager.targetFrameRate = 30`; `QualitySettings` values read by `ReportBuild()` in `Assets/Editor/BarangayUtils.cs` |
| Is any size reduction claimed? | No. Week 16 → 17 went 68.30 MB → 31.63 MB because dev → release, **not** because anything was optimised (`Week_17/Evidence/build_and_commit.txt`) |

## 4. Security

| Question | Where the answer lives |
|---|---|
| How is the player profile protected against editing? | `Assets/Scripts/Core/PlayerProfile.cs` — SHA-256 over salt + payload JSON. Read `:91–95` for the honest statement of what this does and does not prove |
| Where exactly is the signature computed and checked? | `PlayerProfile.cs:627` `ComputeSignature`, and the check at `:537` that produces the rejection message at `:540` |
| What happens when a save fails its check? | The file is rejected, set aside as `profile.rejected.json`, and a fresh profile starts. Real observed instance: `windows_backup_build.md` §5 |
| How is that tested? | `Assets/Tests/EditMode/PlayerProfileTests.cs`, including `WriteRawForTests(payloadJson, correctSignature)` at `PlayerProfile.cs:441`, which writes a deliberately bad signature |
| Are there secrets in the repository? | `Docs/Zedrich Abrera/Week_17/Evidence/secret_scan.md` and `secret_scan_raw.txt` |
| Why use a file rather than `PlayerPrefs`? | `PlayerProfile.cs:80–95` — the reasoning is in the source |

## 5. Game development — the rules

| Question | Where the answer lives |
|---|---|
| How is a tag detected? | `MatchManager.cs:274` against `tagDistance = 1.2f` at `:88` |
| How is a flag stolen? | `MatchManager.cs:533` against `flagDistance = 1.5f` at `:91` |
| How does a rescue work? | `MatchManager.cs:399–444`; `rescueDistance = 1.5f` at `:110`, `immunitySeconds = 2f` at `:119`, `rescueCooldown = 3f` at `:123`; the release itself is `CharacterStatus.Release(...)` at `CharacterStatus.cs:193` |
| Why can't a rescue chain forever? | `MatchManager.cs:416` and `EnemyAI.Scoring.cs:137` both read the cooldown |
| What stops a match stalling on a dropped flag? | `Flag.cs:44` `returnAfterSeconds = 10f`, applied at `:181` |
| Is there a time limit? | `MatchManager.cs:128` `matchTimeSeconds = 180f`, checked at `:608`; `0` disables it |
| How does difficulty work? | Applied equally to both teams — the presets and where they are read |
| How is hiding in bushes implemented? | `Assets/Scripts/Gameplay/HideSpot.cs`, `Assets/Scripts/Visual/CharacterBushHide.cs` |

## 6. Testing

| Question | Where the answer lives |
|---|---|
| What is covered by automated tests? | `Assets/Tests/EditMode/` — `MatchSettingsTests`, `PlayerProfileTests`, `SprintConfigTests`, `TeamRulesTests` |
| What is the current result? | `run_tests` EditMode: **17 total, 17 passed, 0 failed, 0 skipped** — re-run on this tree on 2026-11-19 after `Week18Build.cs` was added |
| Where is the accessibility evidence? | Week 17 `readiness_results.md`: **516 passed, 0 failed, 24 not measurable**, with the pre-fix and post-fix runs both recorded |
| Where is the balance evidence? | Week 16 quality scenarios: **16 scenarios, 0 failures**, at 4v4 |
| Where is the usability evidence? | Week 14 playtest: 5 testers, 25 task attempts, 0 failures, 23 unaided, 2 facilitated, 5/5 noticed both sounds |
| How was the demonstration sequence proven to work? | `Evidence/demo_rehearsal.txt` — nine captures with distinct MD5s and real timestamps |

## 7. Commits, tags and defects

| Question | Where the answer lives |
|---|---|
| Which commit is the binary built from? | `84c0f2e` — `Week_17/Evidence/build_and_commit.txt` |
| Which commit is tagged `v1.0.0-rc1`? | `07057bd` — `Evidence/repo_state.txt` |
| What is HEAD? | `983d758`, recorded at the start of Week 18 in `Evidence/repo_state.txt` |
| Is the repository pushed? | No — `origin/main` is `d08fc9b`, branch `ahead 9`. Stated in `release_record.md` §5 |
| What defects were found and fixed? | `Week_17/Evidence/issue_disposition.md` — `DEF-17-01` (credits printed `0.2`), `DEF-17-02` (version in `productName` orphaned saves), `DEF-17-07` (how-to text behind the BACK button), `DEF-15-04` (an App Bundle named `.apk`) |
| Why does the app still say `1.0.0-rc1` at submission? | `release_record.md` §2 — the submitted binary is not rebuilt, so it keeps its own version string; the *commit* carries the tag `v1.0.0-pa2` |

## 8. Known limitations to be ready on

These will be asked. They are already written down, so they are not a surprise:

1. **No handset installation for this submission** — `adb devices` is empty; PA2 records it BLOCKED.
2. **The Windows backup differs from the APK** — Mono instead of IL2CPP, no haptics, and two
   post-processing passes stripped. `windows_backup_build.md` §6.
3. **Repository was local-only until 2026-11-19** — it is now published: `origin/main` is `5e63b1c`
   and all three tags resolve remotely, so the commit is verifiable.
4. **Two Week 14 discoverability findings were never re-tested** against the rebuilt instructions
   screen.
5. **AI balance rests on 16 regression scenarios**, not on a large-scale study.

---

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
*Pointer sheet only — deliberately contains no drafted answers.*
