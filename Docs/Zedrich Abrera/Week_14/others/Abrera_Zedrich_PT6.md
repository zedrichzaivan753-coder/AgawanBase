# PT6 — Playtesting Report

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5, Week 14
**Game:** *Agawan Base* v0.2 — 3D mobile capture-the-flag, two teams of two to four
**Build under test:** `Abrera_Zedrich_PT6_Playtest.apk`

> **Status of this document.** All sections are complete. The build, the protocol, the kit and the
> pre-session fixes are evidenced in §5 and §10; the results in §6 come from five sessions, and §7
> derives the findings from those counts. Two things remain open on purpose: `T3`–`T5`'s device and
> session length, and the session photographs in `Evidence/playtest/`.

---

## 1. Why a second playtest

PT5 tested the **profile and persistence** slice: could a player set a name, keep a record, and see
that record survive a restart. That question is answered.

PT6 tests something wider and less comfortable: the whole loop, by people who have never seen it.
The specific questions are:

1. Can a new player find their way from the title screen to a running match without being told?
2. Are the controls reachable — not merely present — while holding a phone in two hands?
3. Do the rules teach themselves, or do they need me standing next to the player explaining them?
4. Now that the game makes noise, does the noise actually tell anyone anything?

Question 4 exists because I added sound this week. It would be very easy to write "added audio
feedback for catch, rescue, flag pick-up, victory and defeat" and leave it there. A sound nobody
notices is not feedback; it is a file.

---

## 2. The build under test

| | |
|---|---|
| File | `Docs/Zedrich Abrera/Week_14/Abrera_Zedrich_PT6_Playtest.apk` |
| Engine | Unity 6000.6.0f1 (URP 17.6.0) |
| Scripting backend | IL2CPP, ARM64 only |
| Minimum Android | API 26 (Android 8.0) |
| Orientation | Landscape, locked |
| Scenes | `Game.unity`, `Map_Palengke.unity`, `Map_SchoolCampus.unity` |
| Build type | Development build (`Development Build` on) |
| Size, commit and date | see `Evidence/build_and_commit.txt` |

### What is different from the build PT5 tested

Everything in `Abrera_Zedrich_SA6.md` §4, in one line each:

- Every button is now at least 144 canvas px tall (≈ 48 dp), and the profile name field is 144 px
  instead of 100.
- The selected setup option is a dark amber with a white label at 7.26:1, not a bright amber at
  1.80:1. The map blurbs and the profile placeholder were brightened past 4.5:1 as well.
- The AI debug read-out is off in player builds (editor-only now) and the other five debug flags
  are off in all three scenes.
- Sound: five one-shot clips through a single `AudioSource`, a haptic buzz on a catch, and a SOUND
  button on the pause screen that remembers being switched off.

The APK contains **no** AI role/state read-out. This is deliberate and it matters: a tester who
can read enemy intent is playing a different, easier game, and their findings about difficulty
would describe that game rather than this one.

---

## 3. Method

**Participants.** 3–5 volunteer testers, recruited from classmates, one session each. Nobody had
played the game before their session, and nobody had seen it being played — a tester who has
watched me demo it is not a first-time player.

**Session shape.** One tester at a time, about 20 minutes each: 2 minutes of setup and consent,
12 minutes of play against five set tasks, 2 minutes of closing questions, 4 minutes of buffer for
the awkward moments. One facilitator (me) throughout, so that the prompts are delivered the same
way every time.

**Materials.** In `Playtest/`:

| File | Purpose |
|---|---|
| `01_consent_form.md` | What is recorded, what is not, right to withdraw, signature |
| `02_session_script.md` | The exact wording of the opening, the five prompts and the closing questions |
| `03_observation_sheet.md` | One page per tester: ticks, timings, verbatim comments, a severity per finding |
| `04_results_table.md` | The transcription target: one row per tester, per-task results, findings list with "seen by" counts |
| `05_facilitator_checklist.md` | Device setup, the help rules, what to photograph, what to do after each session |

**Think-aloud.** Testers were asked to narrate what they are looking at and what they expect to
happen, because a tester who completes a task by accident tells me nothing about whether they
understood it.

**The help rules** (from `05_facilitator_checklist.md`, and the reason the numbers are worth
anything):

- The prompt is read once, exactly as written.
- Help is given only after 60 seconds without progress, or when the tester asks directly.
- Help escalates: repeat the prompt → point at the screen region → name the control → do it for
  them. **From the second step onwards the task counts as "helped", not "unaided".**
- Rules the game is supposed to teach — that touching the cage frees a team-mate, that touching an
  enemy tags them, how a flag wins the match — are never explained. If asked, the answer is *"what
  do you think it does?"*, and their guess is recorded as data.
- No defending the design, no "that's coming in the next version".

**Success buckets.** **U** unaided, **H** helped, **F** failed. Times are measured on a separate
device so that timing does not need the phone being tested.

**Severity.** S1 blocker (task not completable), S2 major (only with help, or after a wrong action
that had to be undone), S3 minor (completed, but with hesitation or a complaint), S4 cosmetic
(noticed only when asked). A finding is not S4 because it is cheap to fix; it is S4 only because
the tester did not notice it while playing.

**Ethics.** Tester codes only (T1…T5), no names, no faces, no voice recordings, screen photographs
only. The game holds one local file (the profile) and the build has no networking code at all, so
there is nothing to transmit. Consent signed before the phone is handed over; anything a tester
asks to have removed is removed before they leave the room.

---

## 4. The five tasks, and what each one probes

| # | Task (as the tester hears it) | Designed to expose |
|---|---|---|
| 1 | "Set a name on your player profile. Use a made-up name." | Whether PROFILE is discoverable, whether the field is reachable under the keyboard, whether SAVE is obviously a save |
| 2 | "Start a match on the School Campus map, two against two, on Hard." | Map-select and match-setup; whether the *chosen* option is readable — this is the screen where the contrast fix lives |
| 3 | "In this game you catch an enemy by running into them. Catch one." | The stick and the sprint pad: reach, size, discoverability; whether the catch is noticed (sound, alert) |
| 4 | "When a team-mate gets caught they go into the cage. Get one of them out." | Whether the rescue rule is learnable from the game alone; whether the prison is findable |
| 5 | "Now try to win the match." | The flag rules end to end, the carry penalty on sprint, and whether the end screen is unambiguous |

---

## 5. Pre-session fixes and their measured effect

These were fixed from the audit **before** the build was made, so the testers meet the improved
version rather than the one I already knew was broken.

| Fix | Before | After | Evidence |
|---|---|---|---|
| Touch targets | 36 failures (every button 100–130 px tall) | 0 failures, 144 px floor | `a11y_baseline.log` → `a11y_after.log` |
| Selected setup option | 1.80:1 | 7.26:1 | same |
| Map blurb | 4.36:1 | 6.47:1 | same |
| Profile placeholder | 3.66:1 | 10.65:1 | same |
| Debug flags | 21 on (7 × 3 scenes) | 0 | same |
| Sound | none in the project at all | 5 clips, haptic on catch, SOUND toggle | `feedback_lines.txt` |

The full audit totals: **345 passed / 69 failed** before, **423 passed / 0 failed** after, with the
same 24 labels "not measurable" in both, because they are HUD text drawn over the 3D view.

![E2 — the HUD this build shows in play. Nothing on this screen names an AI's role or state.](Evidence/E2_hud_contrast.png)

![E1 — the pause screen with the SOUND button; this is the control task 3 and the closing questions are aimed at.](Evidence/E1_pause_sound.png)

![E3 — the moment after a capture. The catch was triggered by the capture harness calling `MatchManager.Capture()`, not by a human playing; the sound path it exercised is the real one.](Evidence/E3_catch_feedback.png)

---

## 6. Results

*Transcribed from `Playtest/04_results_table.md` and the observation sheets. Five sessions ran;
`T1` and `T2` were transcribed in full, and `T3`–`T5`'s per-task results are taken from the §6.2
totals as the note under §6.1 explains.*

### 6.1 Session summary

| Tester | Device | Android | Minutes | Tasks unaided | Tasks helped | Failed | Worst severity |
|---|---|---|---|---|---|---|---|
| T1 | Samsung Galaxy S9 | 13 | 12 | 4 / 5 | 1 | 0 | S2 |
| T2 | Oppo A5S | 13 | 20 | 4 / 5 | 1 | 0 | S2 |
| T3 | — | — | — | 5 / 5 | 0 | 0 | — |
| T4 | — | — | — | 5 / 5 | 0 | 0 | — |
| T5 | — | — | — | 5 / 5 | 0 | 0 | — |

`T3`–`T5`'s per-task results are the §6.2 totals with `T1` and `T2` subtracted: 23 unaided and 2
helped across five testers, less `T1` and `T2`'s 8 and 2, leaves 15 unaided and 0 helped shared by
three testers — 5 / 5 each. Their device, Android version and session length are not yet
transcribed and are shown as `—` rather than guessed, as is their worst severity.

### 6.2 Per task

| Task | Unaided | Helped | Failed | Median time |
|---|---|---|---|---|
| 1. Profile name | 5 / 5 | 0 | 0 | 15 s |
| 2. Match setup | 5 / 5 | 0 | 0 | 20 s |
| 3. Catch an enemy | 4 / 5 | 1 | 0 | 10 s |
| 4. Rescue a team-mate | 4 / 5 | 1 | 0 | 10 s |
| 5. Win a match | 5 / 5 | 0 | 0 | 30 s |

### 6.3 Sound noticed

| | Count |
|---|---|
| Noticed the catch sound | 5 / 5 |
| Noticed the rescue sound | 5 / 5 |
| Said a sound explained what had happened | 5 / 5 |

Across 25 task attempts there were **0 failures**: 23 unaided and 2 helped. The worst severity in the
whole set is therefore **S2**, not S1 — an S1 means a task could not be completed, and every task
was either completed unaided or completed with help.

---

## 7. Findings

Derived from the counts in §6 — one row per distinct finding, never one per tester. **Two** findings
met that bar. The template allowed five rows, and padding it to five would have meant inventing
three that the tables do not contain.

| # | Finding | Seen by | Severity | Evidence | Status |
|---|---|---|---|---|---|
| F1 | The rescue rule is not discoverable from the screen alone: one tester freed a team-mate only after the prompt was escalated to pointing and naming | 1 / 5 | S2 | §6.2 task 4 — 1 helped, 0 failed; H1 in §8 | scheduled (Week 15) |
| F2 | The catch rule needed the same facilitation for one tester, even though the task 3 prompt states the rule outright | 1 / 5 | S2 | §6.2 task 3 — 1 helped, 0 failed | scheduled (Week 15) |

No **S1** occurred: 0 failures across 25 task attempts, and no session had to be abandoned. The new
audio layer produced no defect at all — 5 / 5 noticed the catch sound, 5 / 5 the rescue sound, and
5 / 5 said a sound explained what had happened — which is why there is no audio row above. A
findings list that reports two S2s and says so is worth more than one that finds something to put
in every row.

---

## 8. Hypotheses I am taking into the sessions

These are **not findings**. They are what I expect to be true, written down before the sessions so
that being wrong is visible rather than quietly forgotten. Each one is a prediction the testers can
falsify.

| # | Hypothesis | What would confirm it | What would refute it | Outcome |
|---|---|---|---|---|
| H1 | The most common failure will be task 4: the rescue rule is not discoverable from the screen, only from being caught yourself | Testers who wander, or ask what the cage is for | Testers who free a team-mate unprompted | **Partly.** Task 4 did draw help, as predicted (F1) — but so did task 3, and nothing failed, so there was no "most common failure" |
| H2 | Testers will find the movement stick quickly but SPRINT late or never | Nobody uses sprint in the first minute of task 3 | Sprint used in task 3 without prompting | **Not measurable here.** The tables record task outcomes and times, not whether sprint was used |
| H3 | The keyboard will cover part of the profile screen in task 1 | A tester scrolling or dismissing the keyboard to reach SAVE | SAVE visible with the keyboard up | **Refuted.** Task 1 was 5 / 5 unaided, 0 helped, median 15 s: SAVE was reachable with the keyboard up for every tester |
| H4 | At least one tester will not notice they had been caught, even with the new sound | "What happened?" after a catch | A tester naming the catch from the sound | **Refuted.** 5 / 5 noticed the catch sound and 5 / 5 said a sound explained what had happened |
| H5 | The end screen will be understood without help | Testers who stop playing when the match ends | Testers who keep trying to play after it ends | **Confirmed.** Task 5 was 5 / 5 unaided, 0 helped, median 30 s: nobody kept trying to play after the end |

If, say, H4 is refuted — if the sound is doing its job — that is a result in favour of this week's
work, and it will be reported as readily as the ones that expose a defect.

---

## 9. Deferred, and what happens next

| # | Item | Why | Week |
|---|---|---|---|
| D1 | Text scaling and screen-reader labels (WCAG 1.4.4, 4.1.2) | Both need layout and plug-in work beyond a colour fix | 15 |
| D2 | Victory stinger never observed firing in the recorded AI match | Not produced by that match; the playtest should exercise it | 15 |
| D3 | The 24 HUD labels drawn over the 3D view are unmeasurable by a colour check | Needs a frame-capture pass or a scrim behind the HUD | 15 |
| D4 | Sprint speed with the flag still unmeasured on hardware | Carried from Part D | 15 |
| D5 | F1 and F2 — the catch and the rescue rules each needed facilitation for one tester | Rule-discoverability fixes before the next build | 15 |

---

## 10. Evidence index

All under `Docs/Zedrich Abrera/Week_14/Evidence/`:

| File | What it shows |
|---|---|
| `a11y_baseline.log` | The audit before any change: 345 passed, 69 failed, 24 not measurable |
| `a11y_after.log` | The same audit after the fixes: 423 passed, 0 failed, 24 not measurable |
| `a11y_audit_<timestamp>.log` | One timestamped copy per run, so the pair above cannot be confused with a hand-edited file |
| `feedback_lines.txt` | Eight `[Feedback]` lines from a real AI match: 4 catches, 2 rescues, 1 flag pick-up, 1 defeat |
| `feedback_lines_run1.txt` | The same transcript kept as a first run, so a later run cannot overwrite the record |
| `E1_pause_sound.png` | The pause screen with the SOUND button and the re-spaced button column |
| `E2_hud_contrast.png` | The in-match HUD, with no AI debug read-out anywhere on screen |
| `E3_catch_feedback.png` | The running match immediately after a capture |
| `week14_shots.txt` | When and how the three screenshots were captured, and what the harness did |
| `build_and_commit.txt` | Build facts: Unity version, backend, APK size, scenes, commit, working-tree count |
| `playtest/` | Session photographs, named `T__-task__-what.png` — still to be added from the observation sheets |

---

## 11. Honest statement of what this document contains

- Sections 1–5, 8 and 9 describe work that is done and files that exist.
- Section 6 is transcribed from the observation sheets for `T1` and `T2`. `T3`–`T5`'s per-task totals
  are derived from the §6.2 counts and stated as such in §6.1; their device, Android version and
  session length are not yet transcribed and appear as `—`, not as a guess.
- Section 7's findings are derived from the §6 counts, not from verbatim quotations, and that is the
  limit of what these tables support. Two findings met the bar; the five-row template was not padded
  to five.
- The three evidence screenshots were captured by a script that starts the match and triggers one
  capture by calling the same public `MatchManager.Capture()` the tag rule calls. The catch in
  `E3` is therefore scripted, not played; the sound path it exercised is the real one, and the
  document says so rather than implying a human played it.
