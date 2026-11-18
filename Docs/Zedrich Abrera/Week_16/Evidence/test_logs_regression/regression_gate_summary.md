# Regression gate - Week 16

Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 16
Unity 6000.6.0f1 · URP 17.6.0 · Unity Test Framework 1.8.0
Gate run 2026-11-18, 15:51-15:57 UTC (23:51-23:57 local)

The gate exists to answer one question before anything is claimed as optimised: **did the
change break a rule that already worked?** It is run on the tree that produced the optimised
build, and every number below is copied from a file in this folder.

## Result

| Target | Required | Measured | Verdict |
|---|---|---|---|
| EditMode unit suite | 17 / 17 | **17 passed / 0 failed / 0 skipped** | PASS |
| Play-mode scenario suite | 16 / 0 / 0 | **16 passed / 0 failed / 0 blocked**, 0 exceptions | PASS |
| Accessibility audit | 423 / 0 / 24 | **423 passed / 0 failed / 0 warnings / 24 not measurable** | PASS |

All three targets pass. One target did **not** pass on the first attempt, and section 3 says
what it was and what was done about it - a gate that only ever reports green is not a gate.

## 1. Where each suite comes from

| Suite | Source | Runner | Output in this folder |
|---|---|---|---|
| EditMode, 17 cases | `Assets/Tests/EditMode/` - `MatchSettingsTests`, `PlayerProfileTests`, `SprintConfigTests`, `TeamRulesTests` | Unity Test Runner, EditMode (batch) | `editmode_regression.txt` |
| Play-mode, 16 cases | `Assets/Editor/QualityRun.cs`, menu `Tools > Week15 > Run Quality Scenarios`, label `week16_regression` | enters play mode, drives a real 4v4 match through the same public methods the rules use | `scenario_regression.tsv`, `scenario_regression_summary.txt` |
| Accessibility, 447 checks | `Assets/Editor/A11yAudit.cs`, menu `Tools > Playtest > Run Accessibility Audit` | edit mode, walks `Game.unity`, `Map_Palengke.unity`, `Map_SchoolCampus.unity` | `a11y_regression.log` |

The play-mode suite is not an NUnit assembly - it is an editor-driven harness that enters play
mode and asserts against the running game. That is why the plan's "16-case play-mode suite"
resolved to `QualityRun.cs` and why there is no PlayMode `.asmdef` in the project: there are
three assemblies in total (`AgawanBase.Runtime`, `AgawanBase.Editor`,
`AgawanBase.Tests.EditMode`) and the play-mode cases live in the editor assembly by design.

The harness writes to `Docs/Zedrich Abrera/Week_15/Evidence/test_logs_<label>/`; the files in
this folder are copies taken immediately after the run, unedited. The originals are still in
the Week 15 evidence folder under `test_logs_week16_regression/`.

## 2. The three runs

### EditMode - 17 / 17

Gated run 15:54 UTC, 0.571 s total, `resultState Passed`, no compile errors first. Full per-test
log with durations in `editmode_regression.txt`.

### Play-mode scenario suite - 16 / 0 / 0

`scenario_regression_summary.txt`:

- stopped because: the suite ran every case
- cases: 16 (16 passed, 0 failed, 0 blocked)
- exceptions and errors raised during the run: **0**
- `[Feedback]` lines captured: 71

Every rule the build claims is checked against the running match, not against a mock: the
prison holds the prisoner (`moved 0.000 m in 0.4 s, motor.IsFrozen=True`), a dropped flag
returns home after its timer, pause freezes the clock (`paused delta 0.00 s`), backgrounding
pauses the match, 3 catches in one frame leave the counter consistent, 50 rapid pause toggles
end at `timeScale 1.00`, 50 rapid pick-up attempts end in one consistent state, and win, loss
and draw each fire their own outcome and their own feedback clip.

### Accessibility - 423 / 0 / 24

`a11y_regression.log`, header `Unity 6000.6.0f1 2026-11-18 15:56:45 UTC edit mode`, last line
`TOTAL: 423 passed, 0 failed, 0 warnings, 24 not measurable` - identical to the Week 15 figure,
which is what a regression gate wants to see. The 24 are the checks the audit cannot make from
the scene (nothing is measured, so nothing is claimed): they are listed in the log.

## 3. The one failure, and the fix it earned

The first EditMode run was **16 / 17**:

```
PlayerProfileTests.MissingSave_LoadsDefaultProfile_WithoutAnError   FAILED
  a missing save is normal on a first run, not an error
  Expected: <empty>
  But was:  "the file is empty"
```

It was not a Week 16 regression - the optimisation changed four texture import settings.
It was proved order-dependent before anything was edited: **the same test run alone passes**
(1/1, 0.31 s), and in the full suite it fails. Cause, read out of `PlayerProfile.Load()`: it
reset `loaded` and the profile state but never cleared `lastError`, and the missing-file branch
returns before anything sets one - so `LastError` still held the reason the *previous* load
rejected a file. NUnit runs the fixture in name order, so `EmptySave_LoadsDefaultProfile`
("the file is empty", `PlayerProfile.cs:494`) always handed it that stale message.

Fixed in the product, not in the test: one line, `lastError = "";` at the top of `Load()`, so a
load reports only its own outcome. Re-run: **17 / 17**. No test file was modified - every file
under `Assets/Tests/EditMode/` still carries its Week 15 timestamp (21:26-21:27 local), and the
`dir /T:W` output is pasted into `editmode_regression.txt` as proof.

The leak was reachable in the game as well, not only in the fixture: any path that loads, sees a
rejected save, and loads again through the `SetDataDirectory` seam could read an error about a
different file. It took a full-suite run to appear, because domain reload is disabled in this
project and the static survives between sessions.

## 4. Honest limits of this gate

- **Nothing here is a performance measurement.** The suites are functional; the host was busy
  during the run (a stray `Client-Win64-Shipping.exe` holding 4,564,004 K beside Unity PID
  31432), and no frame time, allocation or memory figure is quoted from this gate.
- **The play-mode suite ran under that same host load** and still finished 16/0/0 with 0
  exceptions, inside the harness's own 300 s budget. That makes the pass credible under adverse
  conditions - it does not make it a timing experiment.
- **The accessibility audit is a rule check, not a user test.** 423 automated checks and 24
  not-measurable items are not a substitute for the Week 14 tester sessions.
- **The 16 cases are the Week 15 suite, unchanged.** Nothing was added to it for Week 16, so the
  gate proves the optimisation did not break the rules the game already had; it says nothing
  about optimised paths that have no case.
- The suites were run from the editor against the scene tree, not against the shipped `.exe`.
  The player artefact is checked separately (it launches, writes a CSV); it does not run these
  suites.
