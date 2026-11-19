# Week 17 Reflection - Treating the Game as a Release Candidate

**Abrera, Zedrich Zaivan B.** · MOBILE DEV 002, Module 5 · **Agawan Base 1.0.0-rc1** (versionCode 2, APK md5 `ef38de8dd65100109791002a67738da1`), branch `main`, annotated tag `v1.0.0-rc1`
**Evidence leaned on:** `Week_17/Evidence/build_and_commit.txt`, `issue_disposition.md`, `readiness_results.md`, `Assets/Scripts/Core/ReleaseInfo.cs`

---

## What changed when I treated the game as a release candidate rather than a prototype?

A prototype is judged by what it does; a release candidate is judged by what it *claims*. Writing `1.0.0-rc1` into `PlayerSettings.bundleVersion` turned every sentence I had written about the game into something a stranger could check against one file, and three of them were false.

The game had no instructions - the title's only guidance was the flavour line *"Galingan mo boss"* - and I never noticed because I had never played it without already knowing the rules. It had no credits anywhere in code or scenes. It could not be exited: `Application.Quit` existed only in the Week 16 profiling harness. All three are now panels written into every scene by `Assets/Editor/ReleasePanelsBuild.cs`. The fourth addition was the smallest and most useful: a title caption filled at runtime from `ReleaseInfo.BuildId`, which reads `Application.version`, so the binary in my hand could be compared with the documents describing it.

## Which issue was hardest to resolve before feature freeze?

`DEF-17-02`, because nothing about it looked like a bug. The build record listed `productName` as `AgawanBase v1.0.0-rc1`, and Unity derives the `PlayerPrefs` namespace and `Application.persistentDataPath` from that string: the release candidate had an empty settings store and a new profile folder, so anyone upgrading from Week 16 would have started from a blank record with no error and no crash. It is the defect a reviewer finds by installing over the previous build - the one check I could not run, with no handset attached.

I did not find it by reading my settings. The Week 15 quality suite, clean for two weeks, suddenly reported 13 passed / 3 failed / 1 blocked, all three pointing at a roster of one Red instead of four. I had not broken gameplay code; I had broken the *identity* of the app.

Second-hardest was my own evidence: the first screenshot run produced six files of which three were the same picture of the title (`DEF-17-03`), because `ScreenCapture.CaptureScreenshot` photographs whatever the Game View last presented. `Assets/Editor/Week17Shots.cs` now forces a repaint, hashes every file and re-requests a repeat.

## What evidence shows that the build is ready for demonstration?

Four executable gates, all re-run against this APK on November 19, 2026, with raw output in `readiness_results.md`: EditMode **17 passed / 0 failed**; the accessibility audit **516 passed / 0 failed / 24 not measurable**, up from 423 because the two new screens added 93 measurable labels and buttons that clear the 144 x 144 touch floor and the 4.5:1 contrast rule; the Week 15 quality suite **16 / 0 / 0** with no exceptions; and `classes.dex` at the APK root, the guard against the Week 14 bundle-named-`.apk`.

Behind those numbers is what I would actually show: one session walking title, HOW TO PLAY, CREDITS, a live 4v4 match, an elimination victory and a restart, with the run log recording the version string it read and the 3.4 s of `timeSinceLevelLoad` that proves the restart reloaded the scene. All four gates were run again after the last fix, and the only number that moved was the APK's md5.

![E3 - the CREDITS screen, whose build line is filled at runtime from `ReleaseInfo` after DEF-17-01 had it printing a stale "0.2".](Evidence/E3_credits.png)

## What known limitation will I disclose during the defense?

The game has never run on a phone. `adb devices` printed an empty list, so the install-and-run checkpoint is recorded **BLOCKED**, not passed, and every runtime claim comes from play mode on a desktop GTX 1070; a mid-range phone will be several times slower on the CPU and an order of magnitude slower on the GPU, and Week 16's 0.60 ms / 0.48 ms headroom is an argument, not a proof. Two smaller ones: the profile is tamper-*evident*, not encrypted, because the signing key ships inside the app; and the two Week 14 findings on rule discoverability were answered with an instructions screen that no second round of testers has used. Adding a screen is not the same as fixing discoverability, and claiming otherwise would be the same mistake as printing a version number that is not the version of the build.

---

I confirm that this reflection describes my own development experience and that all assistance, including AI assistance, has been disclosed.

*Zedrich Zaivan B. Abrera · Week 17*
