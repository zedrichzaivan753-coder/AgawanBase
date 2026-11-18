# Session Script — Five Tasks (about 12 minutes of play)

One tester at a time. Read the wording in **bold** as it is written; anything in *italics* is for
you and is never read out. Keep the phone in your hand between tasks so you control what is on
screen.

---

## Before the tester arrives (2 minutes)

1. Install `Abrera_Zedrich_PT6_Playtest.apk` on the device.
2. Launch the game once, then close it, so the first start-up work is already done and the tester
   does not have to sit through it.
3. Turn the ringer to silent but leave **media volume at about 70 %** — the game now has sound
   feedback, and task 3 and 4 are partly about whether that sound is noticed.
4. Set auto-rotate to **landscape-locked** if the device offers it, and disable the screen
   timeout (or set it to 10 minutes).
5. Have ready: this script, `03_observation_sheet.md`, a stopwatch, and a pen.
6. Do **not** open the pause screen for them and do **not** explain the controls first. The point
   is what the game teaches on its own.

## Opening (2 minutes)

> **Thanks for coming. This takes about twenty minutes. I am testing a game I am building for a
> school project — a two-team capture-the-flag game. I want you to play it and tell me out loud
> what you are thinking: what you are looking at, what you expect to happen, and anything that
> confuses you. There are no wrong answers, and if something does not work, that is my bug, not
> yours. You will appear in my write-up only as a code, not by name. Any questions before we
> start?**

Then hand over the phone with the game on the title screen.

> **One more thing: please keep talking. Silence is the one thing I cannot use.**

---

## The five tasks

Do them in order. Give only the prompt; give help only under the rules in
`05_facilitator_checklist.md`.

### Task 1 — Set a profile name (about 2 minutes)

> **First, set a name on your player profile. Use a made-up name, not your real one — for
> example a nickname.**

*What this measures:* whether the route into the profile screen is discoverable, whether the name
field is reachable and typable, and whether the SAVE / BACK path is obvious.

*Record:* did they find PROFILE without being told which button; did the keyboard cover the
field; did they know saving worked.

### Task 2 — Choose School Campus at 2v2 on Hard (about 2 minutes)

> **Now start a match on the School Campus map, two against two, on Hard difficulty.**

*What this measures:* the map-select and match-setup screens, and whether the chosen options are
visible as chosen (this is where the contrast fix in this build lives).

*Record:* did they notice which card and which buttons were selected; did they press START
before finishing the choices; any wrong turn.

### Task 3 — Catch an enemy (about 3 minutes)

> **In this game you catch an enemy by running into them. Catch one.**

*What this measures:* the joystick and sprint pad — reach, size, and whether sprint is found;
whether the catch is noticed (sound and the on-screen alert).

*Record:* whether sprint was used at all; whether they commented on hearing something when the
catch happened; whether the joystick felt too small or too far from the thumb.

### Task 4 — Rescue a team-mate (about 3 minutes)

> **When a team-mate gets caught they go into the cage. Get one of them out.**

*What this measures:* whether the rescue rule is discoverable from the game alone, whether the
prison is findable on the map, and whether the rescue sound confirms it.

*Record:* how they worked out the rule (guessed, tried, or read the HUD); whether they knew the
rescue had worked.

### Task 5 — Win a match (until the match ends, at most 5 minutes)

> **Now try to win the match. You can stop whenever you like, even if you have not won yet.**

*What this measures:* the flag rules end-to-end, the sprint-with-flag slowdown, the end screen,
and whether the win or loss is unambiguous.

*Record:* did the win condition have to be explained; did they notice the stamina bar emptying
while carrying the flag; did the end screen make the result clear.

---

## Closing questions (2 minutes)

Ask in this order, and write the answers down as close to verbatim as you can.

1. **In your own words, what was the game asking you to do?**
2. **What was the hardest thing to touch or read on the screen?**
3. **Was there a moment where you did not know what had just happened?**
4. **Did you notice any sound? What did it tell you?**
5. **If you could change one thing, what would it be?**

Then close:

> **That is everything. Thank you — this genuinely changes what I fix next.**

Do not defend the design, and do not explain a feature they missed. Write the miss down instead;
a feature nobody finds is a defect, whatever the code does.
