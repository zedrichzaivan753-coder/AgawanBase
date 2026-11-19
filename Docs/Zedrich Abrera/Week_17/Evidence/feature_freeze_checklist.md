# Week 17 — Feature-freeze checklist

**Game** Agawan Base v0.2 (release candidate 1.0.0-rc1)
**Student** Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 17
**Frozen tree** commit `b4cff53`, annotated tag `week16-final`
**Date frozen** November 19, 2026
**Procedure reference** Week 17 Laboratory, step 1 ("major new features should not be introduced
after the progress checkpoint") and step 4 ("clean the repository")

---

## 1. What "feature freeze" means for this project

Every mechanic the game will be demonstrated with already exists and is measured. From this
commit on, the only changes allowed into the tree are:

1. **Completeness gaps required by the module itself** — Week 17 procedure step 2 lists
   *instructions, credits, settings* and *exit or restart* as part of the finished game, and the
   build had none of the first two and no exit control. These are closing gaps, not new features.
2. **Defect corrections** that keep an already-accepted behaviour working.
3. **Release mechanics** — version number, build configuration, documentation.

Anything else (a new map, a new game rule, a new AI behaviour, new art) is out of scope until the
defence is over. This is written down here so that a reviewer comparing the tag `week16-final`
with `v1.0.0-rc1` can see exactly what changed and check it against these three categories.

## 2. Frozen scope — the accepted change sets of Weeks 13-16

| Week | Accepted change set | Where it lives | Accepted on the strength of |
|---|---|---|---|
| 13 | Signed local profile: display name, record, fastest victories, tamper-evident save | `Assets/Scripts/Core/PlayerProfile.cs` | PT5 check log, 17 EditMode tests |
| 14 | Accessibility pass: 144 canvas-px touch floor, ≥ 4.5:1 label contrast, debug read-out editor-only | `Assets/Editor/A11yAudit.cs`, uGUI in all three scenes | 345/69 → 423/0 audit, PT6 |
| 14 | Five feedback sounds, catch haptic, persisted SOUND toggle | `Assets/Scripts/Core/GameFeedback.cs`, pause panel | Recorded `[Feedback]` lines in `Week_14/Evidence/` |
| 15 | DEF-15-01 background pause, DEF-15-03 difficulty clamp, DEF-15-04 APK packaging | `GameManager.cs`, `MatchSettings.cs`, Android build settings | Traceability matrix + defect log, `Week_15/Evidence/` |
| 16 | Profiling harness and scenario driver | `Assets/Scripts/Diagnostics/PerfCapture.cs`, `PerfScenario.cs` | 22 per-frame CSVs in `Week_16/Evidence/raw_csv/` |
| 16 | Prop texture cap 2048 → 512 (−20.0 MB runtime texture memory) | texture import settings under `Assets/Art/` | A/B pair `perf_baselclean_*` / `perf_fix1clean_*` |
| 16 | Profile error leak: `PlayerProfile.Load()` clears `lastError` first | `PlayerProfile.cs` | EditMode 17/17 after the fix |

## 3. The three gaps this week closes (and nothing else)

| Gap | Module requirement | Fix |
|---|---|---|
| No instructions screen — the title's only guidance is the flavour line *"Galingan mo boss"* | "Title or start screen and clear instructions or tutorial" | `Game/UI/Panel_HowTo` written into all three scenes |
| No credits anywhere in code or scenes | "settings, credits, and exit or restart behavior" | `Game/UI/Panel_Credits` with version, author, licences, AI disclosure |
| No exit control; `Application.Quit` existed only in `PerfCapture.cs` | "exit or restart behavior" | EXIT button on the title, `GameManager.QuitGame()` |
| No version shown in the running game | "unique version number and build identifier" | `VersionLabel` on the title, `ReleaseInfo.BuildId` |

## 4. Freeze checklist

- [x] The accepted build runs — Play mode on `Game.unity`, `Map_Palengke.unity`, `Map_SchoolCampus.unity`
- [x] The starting commit is recorded and tagged: `b4cff53` = `week16-final`
- [x] The four accepted change sets above are complete and committed
- [x] No half-finished mechanic is left switched on in any scene
- [x] Debug read-out is editor-only; the other six debug flags are off in all three scenes
- [x] Android is the active build target, IL2CPP, ARM64, min API 26, Build App Bundle **off**
- [x] Repository secrets scan is clean — see `secret_scan.md`
- [x] Generated browser artefacts are no longer versioned — see `secret_scan.md` §3
- [x] Every Week 13-16 evidence file is present in `Docs/Zedrich Abrera/`

## 5. What is explicitly **not** in the release candidate

- No music and no ambience (only the five Week 14 one-shots).
- No account, no cloud save, no leaderboard, no network call of any kind.
- No device profiling and no device install: no handset was attached this week, so the
  clean-install case is recorded **BLOCKED** in `clean_install.md`.
- The profile remains one-per-install and tamper-*evident* rather than encrypted.
