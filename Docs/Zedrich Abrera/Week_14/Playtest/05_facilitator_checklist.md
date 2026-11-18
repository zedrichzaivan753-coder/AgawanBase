# Facilitator Checklist

Everything here is either preparation that protects the data or a rule that protects the session
from me. The rules exist because a facilitator who helps is a facilitator who hides defects.

---

## Before the first session

- [ ] APK installed: `Abrera_Zedrich_PT6_Playtest.apk` (dev build, no backup folder shipped).
- [ ] Game launched once and closed, so the first-run work is already done.
- [ ] **Media volume at about 70 %**, ringer silent. The feedback layer is part of this week's
      work; a silent phone measures nothing.
- [ ] Auto-rotate locked to landscape; screen timeout at least 10 minutes.
- [ ] Profile reset to a blank state: **PROFILE → RESET PROFILE**, or reinstall the APK.
- [ ] Consent form printed (one per tester) and signed **before** the phone is handed over.
- [ ] Observation sheet printed (one per tester) and session script to hand.
- [ ] Stopwatch or phone timer ready; do not use the phone being tested for timing.
- [ ] Room quiet enough to hear the game's sounds. If it is not, write that on the sheet — it
      changes what the sound findings mean.

## Before each session

- [ ] Tester code assigned (T1…T5) and written on the consent form **and** the observation sheet.
- [ ] Game relaunched to the title screen — every session starts from the same place.
- [ ] Screen brightness at about 60 %, on auto-brightness off, so two testers on the same device
      are not comparing different screens.
- [ ] Tester told the opening script, including "silence is the one thing I cannot use".

## During the session — the help rules

These are not suggestions; a session that breaks them cannot be cited as evidence.

1. **Say the prompt once, exactly as written.** Do not rephrase it into something clearer; the
   tester's inability to act on the written prompt is the finding.
2. **Help only after 60 seconds with no progress**, or immediately if the tester asks directly.
3. When you help, give the **smallest** possible hint, in this order:
   a. repeat the prompt;
   b. point at the screen region ("something on this screen does");
   c. name the control ("the PROFILE button");
   d. only then do it for them.
4. **Mark the task as "helped" the moment you give hint (b) or beyond.** There is no such thing
   as "a tiny hint" in the results.
5. **Never explain a rule the game is supposed to teach** (that touching the cage rescues, that
   touching an enemy tags them, how the flag wins the match). If the tester asks, say *"what do
   you think it does?"* first — their guess is data.
6. **Write the time, the comment and the hesitation down as they happen.** Do not reconstruct
   from memory after the session.
7. **Do not defend the design**, and do not say "that's coming in the next version". Write it
   down.

## During the session — what to photograph

Screen only, never the tester:

- [ ] the screen at the moment of a hesitation that you wrote down;
- [ ] the pause screen if the tester found the SOUND button;
- [ ] the end screen, win or loss.

Name each file `T__-task__-what.png` and keep them beside the observation sheet.

## After each session

- [ ] Observation sheet completed, including the facilitator's own note and a severity per finding.
- [ ] Screenshots named and filed under `Evidence/playtest/`.
- [ ] Anything the tester asked to have removed is removed, now, before they leave the room.
- [ ] Consent form signed and kept separately from the notes.
- [ ] Game reset to the title screen and profile cleared for the next tester.

## After the last session

- [ ] Every sheet transcribed into `04_results_table.md`, including the "seen by" counts.
- [ ] Every finding has a severity and either a fix or a week number — no row left blank.
- [ ] The sound-noticed count filled in.
- [ ] Findings that contradict something I already wrote in SA6 are **written up as a
      correction**, not quietly deleted. A test that changes nothing was not a test.
- [ ] Named in `Abrera_Zedrich_PT6.md` and summarised in the Week 14 reflection.

## Standing reminders

- **The AI debug read-out must be off in anything a tester touches.** A tester who can read enemy
  role and state is testing a different game. This build gates it behind `#if UNITY_EDITOR` and
  the scene value is off; if the pause screen or the HUD ever shows a list of AI jobs and states,
  stop the session and rebuild.
- **Do not demo the game before the session.** A tester who has watched me play is not a
  first-time player.
- **Do not run two testers at once.** The second one learns from the first one's mistakes, which
  is exactly the learning the game is supposed to supply.
