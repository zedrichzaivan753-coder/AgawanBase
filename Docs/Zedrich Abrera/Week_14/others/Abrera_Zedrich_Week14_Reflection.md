# Week 14 Reflection — Usability, Accessibility and Playtesting

**Zedrich Zaivan B. Abrera**
MOBILE DEV 002 — Advanced Mobile Application Development, Module 5, Week 14
*Agawan Base* — 3D mobile capture-the-flag

---

## The thing I got wrong for thirteen weeks

I thought I had been building this game with feedback in mind. The HUD counts free team-mates, the
alert banner says when somebody is caught, the flag pole tips over when it is dropped. Plenty of
feedback, all of it readable.

There was **not one audio clip in the project**. Nothing beeped, nothing buzzed, nothing played.
Thirteen weeks of work and the game had never made a sound, and I had never noticed — because I was
the only person who had ever played it, and I already knew what had just happened. The screen did
not have to tell me anything; I was telling the screen.

That is the shape of the whole week for me. The lack of sound was not a feature I forgot to add; it
was a feature I could not perceive as missing from inside my own knowledge of the game.

## Measuring first was not bureaucracy, it was the only way I could see

The plan for this week started with an audit script rather than with a list of fixes, and that
ordering is the single most useful decision I made. `A11yAudit` measured contrast, label fit and
touch-target size on every text and button in all three scenes and wrote 69 failures to a file.
Sixty-nine. I would have guessed five.

Every one of them was real:

- **Not one button in the game was tall enough** — 100 to 130 px where Android asks for about 48 dp,
  which is roughly 144 px at this canvas scale. They looked fine in the editor's Game view at
  1920 × 1080 on a desktop monitor. They were being judged with a mouse.
- **The selected option was the least readable thing on the match-setup screen**: white 34 pt text
  on the bright amber `#F2B840` measured **1.80:1**, when WCAG AA asks for 4.5:1. The button the
  player had *just chosen* was the one they could not read.
- The profile placeholder, the map blurb, and six debugging flags that would have been live in any
  build a tester touched.

The 1.80:1 one is the finding I am most embarrassed by, in an instructive way. I had **already
solved that exact problem** in `MapSelectUI.Paint()`, where the chosen card is tinted with a *dark*
amber, and I had written a comment explaining why: white names and pale descriptions have to stay
readable on it. I solved it for the map cards and then wrote the setup screen with the bright
colour and never connected the two. Two classes, one rule, and I only held it in my head in one of
them.

## Accessibility is not a checklist, and my own tool proved it by being wrong

The audit is a file of numbers, and I trusted it because it measured rather than opined. Then the
*fix* script it drove edited three scenes wrongly, and I only caught it because I read the log
instead of the summary line.

The builder grouped buttons into columns by x-coordinate to re-space them after growing their
height. On the match-setup screen, the BACK button and the Easy difficulty button share an
x-coordinate — they are in different rows, doing different jobs — so the tool decided they were a
column and pulled BACK and START up 42 px, into the difficulty row. The summary said "7 buttons
re-spaced" and looked like success. The detail lines said `re-spaced BackButton under EasyButton`.

I added a guard so only columns that actually changed size get re-spaced, restored the two buttons,
and recorded the mistake in the code comment, in the run log, in SA6 §4 and here. A tool that
rewrites three scenes on every run has to be auditable when it is wrong, and the summary line is
not where you find that out.

There is a second, quieter lesson in the same episode: **an automated check is a floor**. It said
my buttons were the right size and my colours passed, and it also said nothing at all about 24 HUD
labels that sit over the 3D view with no backing panel. A contrast ratio against a moving backdrop
is not a number I can compute, so the audit reports "not measurable" and I wrote that limit into
the report rather than inventing a value. The honest version of "0 failures" is "0 failures among
the things this check can see".

## Sound is a usability feature, not polish

Adding audio was more work than I expected, and less about audio than about architecture.

The game's only hook for a capture was `CaptureCountChanged`, which fires once with a *count* and
only for Red captures. From a listener's point of view that is useless: it cannot tell a catch from
a rescue, or one catch from the next, and half the catches are invisible to it entirely. So before
a single sound could play, the rules had to expose two honest events —
`CharacterCaptured`, `CharacterRescued` — raised at the end of `Capture()` and `Rescue()`, after the
rules have finished, plus `Flag.PickedUp` in the one place a pick-up is decided.

That is the project's own rule reasserting itself: one rule in one place, everything else listens.
It also taught me the difference between a state and an event. A count is a state. A catch is an
event. Trying to hear a catch from a count is exactly how I ended up with thirteen silent weeks.

Then there is the part I am proudest of and it is not the sound. It is that
`Evidence/feedback_lines.txt` exists at all. Instead of writing "audio feedback added", I wrote a
harness that starts a real AI match, runs it at 3× speed and records what the game actually logged:

```
catch - Blue_AI_3 (Blue) is locked up -> Catch
rescue - Red_AI_3 (Red) is free -> Rescue
flag picked up by Red_AI_4 -> FlagPickup
defeat - your flag was carried home -> Defeat
```

Four of the five. **Victory never fired**, because the player's team lost that match. I could have
written "victory stinger implemented" and nobody would have checked. I wrote instead that it shares
the same `Play()` call and the same wiring as defeat, that I have not observed it, and that it is
deferred — because a report that quietly rounds four observed events up to five is a report whose
other numbers I would have to start doubting too.

## The debugging aid that would have made the test meaningless

`HudController.showAiDebug` was serialized `true`, with no editor guard, and it lists every AI's
role and its current state on screen. I had already called it ship-blocking in Part D. This week
moved it from "should fix" to "must fix before anyone plays".

The reasoning is not about the player's experience. It is about **what a test is**. If a tester can
read that the red character approaching them is a Chaser in the Chase state, then their session is
measuring a game with a cheat sheet attached, and every conclusion I draw about difficulty, clarity
or discoverability describes that game — not the one I ship. A playtest with the debug read-out on
would have produced findings that felt like success and meant nothing. It is exactly the failure
mode of a developer who cannot see his own game: the tool that makes it easy for me makes the test
worthless for everyone else.

## What I am still unsure about

Whether sound actually helps anyone here is not settled, and I do not want to claim it is. I added
a sound, I proved the sound path fires, and I have no idea yet whether a player notices it — which
is why the results table counts "noticed the catch sound" per tester, and why a low count is
recorded as a finding about the sound rather than about the testers. `PT6` §8 writes my five
predictions down before the sessions, including the one where I expect to be wrong.

Two WCAG criteria remain genuinely unmet and I am not dressing them up: **1.4.4 Resize Text** and
**4.1.2 Name, Role, Value**. The UI is authored at a fixed reference resolution with legacy uGUI
`Text`, and screen readers can see none of it. Both need a layout pass and an accessibility plug-in
— real work, not a colour change — and both are scheduled in Week 15 with the reason written down.

And the uncomfortable one: this build has never been played on a physical device by me. The first
device run is the playtest itself. I have a studio-side audit and three in-editor screenshots, and
those are the honest names for what I have.

## What I would do differently

I would have built the audit in Week 6, not Week 14. Every one of the 69 failures was cheap to fix
and had been sitting in the project for weeks, invisible because I was measuring with my own eyes
and my own expectations. Sixty-nine failures, one afternoon to fix, thirteen weeks to notice.

And I would have asked somebody to play it far earlier — not to be polite about it, but because the
things I cannot see about my own game are precisely the things I cannot see, and no amount of care
makes them visible from where I am standing.

---

I confirm that this reflection describes my own development experience and that all assistance, including AI assistance, has been disclosed.
