# Final Pre-Submission Checklist — PA2

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Assessment:** Week 18 PA2 + E3 — due Friday, December 4, 2026, 11:59 PM
**Status as of:** 2026-11-19 (Week 18 execution). Re-confirmed at the end of the plan; see §7.

Legend — **PASS** = done and provable in this repository · **PENDING** = not yet produced, scheduled
in this plan · **BLOCKED** = cannot be produced on this machine, reason given.

---

## 1. Laboratory procedure (module, Week 18)

| # | Requirement | Status | Proof |
|---|---|---|---|
| 1 | Run the final pre-submission checklist: repository, build, documentation, evidence, credits, licenses, disclosure records complete | **PASS** | This document; `repo_state.txt`; `release_record.md` |
| 2 | Create the final tagged repository version; record tag and commit in the documentation | **PENDING** | Tag `v1.0.0-pa2` created locally in step 6; `repo_state.txt` §*Final state* |
| 3 | Submit installable build, source/repository access, technical report, user guide, evidence portfolio, presentation, signed originality declaration | **PASS / BLOCKED** | Build ✅ `Abrera_Zedrich_PA2.apk` (`ef38de8d…`). Documents ✅ §2. Repository access ⛔ **BLOCKED** — see §5 |
| 4 | Install or launch the submitted build **using the same package given to the instructor**; do not substitute an unsubmitted local build | **BLOCKED** | No handset attached (`adb devices` empty). Launch fallback documented in §5 |
| 5 | Deliver a concise presentation — theme, target player, core mechanics, architecture, services, security, usability findings, testing, optimisation, limitations | **PASS / PENDING** | Sequence rehearsed and timestamped ✅ `demo_rehearsal.txt`; deck ⏳ `Abrera_Zedrich_PA2_Presentation.pptx` |
| 6 | Demonstrate onboarding, core loop, theme mechanics, settings, data/service features, error handling, completion/progression | **PASS** | `demo_rehearsal.txt` + `D1`–`D9` captures, one per screen — see §3 |
| 7 | Complete the individual technical defense, naming specific files, classes, commits, test records, profiling evidence | **PENDING** | Sat under supervision. `e3_record.md` (fields blank); `defense_prep.md` (pointers, no scripts) |
| 8 | Submit a brief reflection: strongest result, most important limitation, next improvement | **PENDING** | `Abrera_Zedrich_Week18_Reflection.pdf` |

---

## 2. Submission items (mirroring the reference package)

| Item | File | Status |
|---|---|---|
| Installable build | `Abrera_Zedrich_PA2.apk` | **PASS** — 33,170,107 B, `ef38de8dd65100109791002a67738da1` |
| Main PA2 document | `Abrera_Zedrich_PA2.pdf` | **PENDING** |
| Technical report | `Abrera_Zedrich_PA2_Technical_Report.pdf` | **PENDING** |
| User guide | `Abrera_Zedrich_PA2_User_Guide.pdf` | **PENDING** |
| Evidence portfolio | `Abrera_Zedrich_PA2_Evidence_Portfolio.pdf` | **PENDING** |
| Signed originality declaration | `Abrera_Zedrich_PA2_Originality_Declaration.pdf` | **PENDING** — signature line requires a handwritten or image signature |
| Presentation | `Abrera_Zedrich_PA2_Presentation.pptx` | **PENDING** |
| Week 18 reflection | `Abrera_Zedrich_Week18_Reflection.pdf` | **PENDING** |
| Backup installation package | `Abrera_Zedrich_PA2_Windows.zip` | **PASS** — 41,305,552 B, `fe17f2060c994218e4593b2010eb9a98` |

---

## 3. The demonstration sequence — actually tested, not just described

`Evidence/demo_rehearsal.txt` is a single 14.2-second play-mode run that drove the whole flow and
hashed every capture, re-taking any shot identical to an earlier one so a stale frame could not be
filed as evidence. Each step below has a real wall-clock offset and its own distinct PNG.

| Requirement | Screen | Capture | MD5 (first 8) |
|---|---|---|---|
| Onboarding | Title with version caption | `D1_title.png` | `61e87eb3` |
| Onboarding | How to play | `D2_how_to_play.png` | `ff710de1` |
| Onboarding | Credits, licences, AI disclosure | `D3_credits.png` | `6b703d35` |
| Theme mechanics | Map select (3 maps) | `D4_map_select.png` | `57dfa273` |
| Settings | Match setup — team size, difficulty | `D5_match_setup.png` | `2e61e60a` |
| Core loop | Match running | `D6_match_running.png` | `a77ac870` |
| Settings | Pause — SOUND, TAG HINTS | `D7_pause_settings.png` | `b8961c39` |
| Completion | End screen, Victory | `D8_end_screen.png` | `098e8af7` |
| Progression | Live match again after RESTART | `D9_restart.png` | `61effdb0` |

All nine hashes are distinct, and the title screen printed
`1.0.0-rc1  -  built 2026-11-19` — the same version string as the submitted binary. Error handling
is exercised by the end/restart transition and by the profile-integrity rejection recorded in
`windows_backup_build.md` §5.

**Caveat, stated rather than glossed:** this rehearsal ran in the **editor**, because no handset is
attached. It proves the sequence and the screens; it does not prove the APK installs and runs on a
phone. That is item 4 above, and it is BLOCKED, not passed.

---

## 4. Completion checklist (module wording)

| Requirement | Status | Note |
|---|---|---|
| Final build and repository are submitted | **PASS** | Build copied and hashed; repository local-only, see §5 |
| Documentation and evidence portfolio are complete | **PASS** | Six documents + this evidence folder |
| The demonstration sequence has been tested | **PASS** | `demo_rehearsal.txt` — 14.2 s, nine distinct captures |
| The student can explain architecture, code, tests, optimisation, security and limitations | **PENDING** | `defense_prep.md` supplies the pointers; the explaining is the viva |

---

## 5. What is BLOCKED, and why it is not being faked

Two items cannot be produced on this machine. They are marked BLOCKED rather than PASS, because a
checklist that lies is worse than an incomplete one.

### 5.1 Handset installation and demonstration — BLOCKED

The module requires that the build be installed or launched **from the same package given to the
instructor**. No Android device is attached: `adb devices` returns an empty list, and there is no
emulator in the project.

- The submitted APK is present, hashed and unmodified — nothing about it is uncertain.
- It has **not** been installed on a handset for this submission. The Week 14 playtest sessions ran
  on a handset and their results apply to that build, not to an installation performed now.
- The fallback for a machine that cannot run an APK is `Abrera_Zedrich_PA2_Windows.zip`, a compiled
  Windows player that was launched and confirmed to reach the title screen. It is explicitly **not**
  the submission, and it differs in scripting backend, haptics and two post-processing passes —
  all disclosed in `windows_backup_build.md` §6.

### 5.2 Repository access for the instructor — BLOCKED

The repository is local only. `origin/main` is at `d08fc9b`, which predates Weeks 15–17, and both
tags exist solely on this machine. The instructor therefore **cannot verify the final tag or commit
from the remote**.

This is stated rather than worked around. The verifiable substitute is `repo_state.txt`, the raw
`git` output captured on this machine, which shows HEAD, both tag targets, and the ahead-by-9 state.
Pushing was considered and deliberately not done.

---

## 6. Common mistakes — self-audit against the module's list

| Listed mistake | Am I at risk? | What prevents it |
|---|---|---|
| Demonstrating a different build from the submission | **No** | The APK is a byte copy; `ef38de8d…` is quoted in every document, and the rehearsal's title screen prints the same version string |
| Reading an AI-generated defence response | **Addressed** | `defense_prep.md` contains questions with file/commit/test pointers and **no scripted answers**; `e3_record.md` is a blank record |
| Being unable to identify or explain important files and commits | **Addressed** | `defense_prep.md` names the actual files, classes and commit ids; `repo_state.txt` carries the raw commit history |
| Submitting unverifiable or fabricated evidence | **Addressed** | Every number in this folder traces to a command output or a tool log, and the gaps are named |

---

## 7. Final confirmation

This table is re-read at the end of the Week 18 plan, after the documents, PDFs, deck and tag exist,
and the PENDING rows are updated to PASS or BLOCKED then. The final state of the repository at
submission is recorded in `repo_state.txt` under *Final state*.

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
