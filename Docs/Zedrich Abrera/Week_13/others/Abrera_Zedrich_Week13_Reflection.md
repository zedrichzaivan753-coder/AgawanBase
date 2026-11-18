# Week 13 Reflection - Secure Player Data and Responsible Development

**Game:** AgawanBase
**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Week:** 13
**Date:** November 18, 2026

The structure below follows the module's Weekly Self-Assessment and Reflection Guide: the four
Week 13 prompts in order, then the required authenticity declaration.

---

## 1. What I built this week, and which decisions were actually mine

The deliverable was a player profile: a name, a win/loss/draw record, a win streak and a local
best-times board of the five fastest victories, reachable from a PROFILE button on the title
screen and remembered between launches. That is the surface. The decisions underneath it were the
part I had to make myself.

The first one was where the data should live. The project already stores two things in
`PlayerPrefs`: team size and difficulty in `MatchSettings`, and the chosen map in `MapSettings`.
The easy move was to add four more keys and be done. I did not, because `PlayerPrefs` values
cannot carry an integrity check - they are plain key-value pairs written by the platform, and
there is no place to put a signature over them. A profile is also a growing record, not a
preference, and a record that a player can inspect, copy or delete is better as a file. So the
profile goes to its own JSON file at `Application.persistentDataPath/AgawanBase/profile.json` and
`PlayerPrefs` keeps only what it was already good at. That split is written down in SA5 section 4
so the two stores do not look accidental.

The second decision was what "secure" should mean here. There is no account, no server, no
network permission and no leaderboard, so there is nobody to authenticate against. I could have
encrypted the save, and it would have looked more serious. It would also have been theatre: the
key would have to ship inside the same APK as the file, so an attacker who can read the file can
read the key. What I can honestly do is make an edited file **detectable**. So
`PlayerProfile.ComputeSignature` takes SHA-256 over the canonical payload JSON, salted with a
16-byte value generated at runtime and kept in `PlayerPrefs["profile.salt"]`. The salt is not in
the source, so two installs of the same build sign differently and a compile of the game does not
contain the key. I chose detection over a false promise of prevention, and I said so on the
screen, in the README and in the PT5 known limit instead of quietly implying the record is
protected.

The third decision was smaller but I think it is the one that will matter most to a player: RESET
takes two presses. One tap that wipes a record someone spent an evening building is a defect
waiting to happen on a phone, and a confirmation dialog on a touchscreen gets dismissed by habit.
Two presses within four seconds, with the button renaming itself to `PRESS AGAIN` and the status
line naming what will be lost, is honest about the cost without being in the way.

---

## 2. How the build handles the data, and where I stopped claiming things

Everything the game stores is in the SA5 data-inventory table, and the important half of that
table is the "not collected" row: no account, no e-mail, no phone number, no contacts, no
location, no advertising id, no device identifier, no analytics. `Packages/manifest.json` has no
Firebase, PlayFab or Unity Services package in it, and the game makes no `UnityWebRequest` call.
That is not a policy statement I wrote in a document - it is what the build does.

Where I stopped claiming things is the important part of this week for me. The save is
tamper-evident, not tamper-proof. The salt is on the same device as the file. A determined local
attacker can re-sign an edited save and the game will accept it. There is no server to check
against, so that is the true ceiling for an offline single-player game, and I wrote it into the
check log as `L15 KNOWN LIMIT` rather than dressing it up as a pass. The same honesty applies to
the README: no audio assets exist, phone performance has not been measured on hardware, the APK
is debug-signed, and the AI debug read-out is still switched on. Every one of those is listed as
a limitation, because a document that only lists what works is not evidence of anything.

---

## 3. A problem I hit, and how the checks found it

The bug that taught me the most was in the signature itself, and it was found by the checks rather
than by me reading the code.

The first version of the test seam `WriteRawForTests` signed the raw text it had been handed. The
reader, in `TryParse`, did something slightly different: it parsed the envelope, then re-serialised
the payload with `JsonUtility.ToJson(file.payload)` and recomputed the signature over **that**. So
the check depended on whitespace and key order. A save written by the game round-tripped fine,
which is exactly why the mistake was invisible in normal play. But a save planted by the harness
for check L12 - impossible numbers with a *correct* signature - was rejected as tampered, because
the pretty-printed text I planted was not byte-identical to the canonical re-serialisation.

The fix was to make the contract explicit on both sides: the writer signs the canonical form, and
the test seam was changed to sign `JsonUtility.ToJson(parsedPayload)` rather than the raw string.
After that, L11 (a save whose signature deliberately does not match) is rejected with *"the
signature does not match the contents (the file was edited by hand)"*, and L12 passes the
signature gate and is then clamped to `wins=0 losses=0 bestStreak=9`. Two checks pointing in
opposite directions, which is what tells me the reader is doing what I think it is doing.

I would not have caught this by playing the game. I caught it because I wrote a check whose whole
purpose was to make a correct-looking file that should still be distrusted.

The second thing these checks changed was my opinion of the harness itself. Fourteen of the
fifteen checks run against a scratch directory - `.../AgawanBase_pt5_scratch` - which is deleted
afterwards, so proving that an unwritable folder fails gracefully (L14) never risks the player's
real save. That separation is a design decision I would now make from the start on any test that
touches disk.

---

## 4. What I would do differently, and what carries forward

**The commit discipline.** This is the honest failure of the week. The repository HEAD is still
`d08fc9b` from September, and the Week 13 work sits in the working tree with more than a hundred
other changed and untracked paths. Everything is verifiable - the log has measured values, the
screenshots are real, the APK exists - but a marker who wants to check my work cannot diff this
week against last week. One commit per feature, with the id pasted into the document, is what I
should have been doing since Week 9, and it is what I will do from Week 14.

**Deciding the failure cases before writing the feature, not after.** The quarantine path
(`profile.rejected.json`) and the atomic `.tmp` replace both came out of the check list, not out of
the first draft of `PlayerProfile`. If I had written the failure table first, as SA5 section 8 now
does, the code would have had those from the beginning.

**What carries forward is the shape of a good claim.** The strongest sentence in the whole
submission is not a design description, it is `L12 IMPOSSIBLE NUMBERS IN A CORRECTLY SIGNED SAVE
ARE CLAMPED, NOT TRUSTED  [wins=0 losses=0 bestStreak=9]`. It is specific, it is measurable, and it
is falsifiable - anyone can rerun it from `HOW_TO_RERUN.txt`. Week 14 is a usability week, and I
intend to write its findings the same way: not "the UI is understandable" but "three of five
testers found the two-press RESET without being told what it did", or the number that turns out to
be true.

---

## Declaration

I confirm that this reflection describes my own development experience and that all assistance, including AI assistance, has been disclosed.
