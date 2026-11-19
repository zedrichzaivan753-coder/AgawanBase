# Week 18 Reflection

**Agawan Base 3D** · Theme: **Philippine Games and Sports** · Build **`1.0.0-rc1`** (MD5 `ef38de8d…`)
Branch **`main`** · Tags **`v1.0.0-rc1`** → `07057bd`, **`v1.0.0-pa2`** on the PA2 commit · Source commit **`84c0f2e`**

---

## What is the strongest result in my completed game, and what evidence proves it?

That a player's profile can be **proved intact or proved tampered with**. `PlayerProfile.cs` stores
the profile as a file rather than in `PlayerPrefs` so it can carry an integrity signature — SHA-256
over a per-install salt and the payload, written at `:627` and checked at `:537`. An edited file is
refused, kept as `profile.rejected.json`, and a fresh profile starts.

Two things prove it, and neither is my description. `PlayerProfileTests.cs` covers the contract,
including a deliberately bad signature written through `WriteRawForTests(...)` at `:441`. And I
watched it happen to a file it was never aimed at: the Windows backup package found an editor-written
profile, could not verify it, and rejected it — recorded in `windows_backup_build.md` §5. I would
rather report the accident than pretend the check only ever ran in a test.

Around that sit the numbers: **17/17** EditMode tests, **516 passed, 0 failed, 24 not measurable** on
the accessibility audit, and **16 scenarios, 0 failures** in the balance gate.

![Figure 1. The core loop, captured at 7.55 s into the Week 18 demonstration rehearsal. Nine screens were captured in 14.2 seconds and every one has a distinct hash, because the harness re-takes any shot that repeats an earlier one.](Evidence/D6_match_running.png)

## Which technical decision would I change if I restarted the project?

I would put **all persistent data in one signed file from week one**, instead of starting with
`PlayerPrefs` and moving later. `DEF-17-02` recorded that I had let the version string into
`productName`, and Unity derives both the `PlayerPrefs` namespace and `persistentDataPath` from that
name. Changing the version did not relabel the app — it silently abandoned every saved setting and
player record. Nothing crashed; the data was simply gone, which is far worse to notice, and it cost a
rebuild and a re-verification of the release candidate. One persistence mechanism, chosen early,
would have avoided it.

## What limitation is most important to acknowledge?

**The submitted build has not been installed on a phone for this submission.** No Android device is
attached — `adb devices` is empty — so `final_pre_submission_checklist.md` marks it **BLOCKED**, not
passed. The file itself is certain: a byte-for-byte copy of the Week 17 release candidate, MD5
`ef38de8d…`. But a hash is not a phone.

The quieter limitation is that two Week 14 discoverability findings were never re-tested after I
rebuilt the instructions screen for `DEF-17-07` — so on the one area I changed, I have no fresh
evidence about the other. Both sit on the unproven list, which exists so the gaps cannot be assumed
closed.

## What did I learn about my own development process across Weeks 13–18?

That evidence must be captured **when the work happens**. An early run produced three identical title
screens that I filed as the how-to, credits and match screens — the Game View had not repainted, and
nothing about the three files looked wrong. `Week18Shots.cs` now forces a repaint before every
capture and rejects any image repeating **any** earlier hash, re-taking it. The Week 18 rehearsal
produced nine captures with nine distinct hashes, which I could prove rather than assert.

Second, writing down what blocked me was worth as much as writing down what worked. Every week had
something unfinished, and marking those BLOCKED is why I can defend the rest. Third, I stopped
guessing where I could measure: `DEF-17-07` was 734.5 px of text in a 660 px box hidden behind a
button, missed by both the audit and my own screenshot review. `FitText` now measures the paragraph
and logs the result.

I confirm that this reflection describes my own development experience and that all assistance,
including AI assistance, has been disclosed.

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
