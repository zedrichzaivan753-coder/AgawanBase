# PA2 — Technical Report

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Week:** 18 · **Assessment:** PA2 — technical report
**Game:** Agawan Base 3D · **Build:** `1.0.0-rc1` · MD5 `ef38de8dd65100109791002a67738da1`

---

## 1. Scope and provenance

This report is a **consolidation of work already documented in Weeks 15, 16 and 17**, not a new set
of claims. Every number here traces to a record produced earlier in the module, and the source is
named beside it. Where an earlier document already argued a point, this report compresses the
argument and cites the file rather than re-deriving it.

Nothing in this report was measured in Week 18. Week 18 produced no new build and no new
optimisation — it produced the demonstration, the package and the defense materials.

---

## 2. Platform and build configuration

| Setting | Value |
|---|---|
| Engine | Unity **6000.6.0f1**, URP **17.6.0** |
| Platform | Android, release |
| Scripting backend | IL2CPP |
| Architecture | ARM64 only |
| Minimum API | 26 (Android 8.0) |
| Managed stripping | Minimal |
| Orientation | Landscape left, locked |
| App bundle | **Off** — the artefact is a real APK, `classes.dex` at archive root |
| Development build | **Off** |
| `bundleVersion` / `versionCode` | `1.0.0-rc1` / 2 |
| Application id | `com.zedrich.agawanbase` |
| Scenes | `Game` (0), `Map_Palengke`, `Map_SchoolCampus` |
| Target frame rate | 30 (`GameManager.targetFrameRate`) |

Source: `Week_17/Evidence/build_and_commit.txt`.

The App Bundle setting has its own defect history. A Week 14 artefact was an Android App Bundle
carrying an `.apk` filename — no handset will install one, and nothing about the file said so
(`DEF-15-04`). Since then the build is checked by reading the archive's entry list: this build has
**419 entries** with `classes.dex` and `AndroidManifest.xml` at the root.

---

## 3. Architecture

![Figure 1. Assembly and system architecture of Agawan Base 3D, drawn in Week 15 and carried forward unchanged. Three assemblies, five script folders, and the rule that the visual layer reads match state and never writes it.](Evidence/fig_architecture.png)

Three assemblies, five folders under `Assets/Scripts/`:

| Assembly | Contents | Why separate |
|---|---|---|
| `AgawanBase.Runtime` | `Core`, `Gameplay`, `UI`, `Visual`, `Diagnostics` | Shipping code only |
| `AgawanBase.Editor` | Builders, harnesses, audits | Editor-only types cannot leak into the player |
| `AgawanBase.Tests.EditMode` | `Assets/Tests/EditMode/` | Tests compile against runtime without editor tooling |

### 3.1 Layer responsibilities

- **`Core`** holds rules data and primitives: `MatchSettings`, `MapDefinition`, `MapSettings`,
  `Team`, `TeamManager`, `PlayerProfile`, `CharacterStatus`, `CharacterMotor`, `SprintStamina`,
  `GameFeedback`, `ReleaseInfo`.
- **`Gameplay`** holds behaviour: `MatchManager` is the referee; `EnemyAI` is split into
  `.Perception`, `.Scoring` and `.Actions` partials; plus `Flag`, `HideSpot`, `PlayerController`,
  `CharacterSpawner`, `MapNavigator`.
- **`UI`** holds `GameManager` — the screen state machine — and the screens themselves.
- **`Visual`** holds presentation: bars, indicators, bush-hiding, facing, rig animation, camera.
- **`Diagnostics`** holds `PerfCapture` and `PerfScenario`.

### 3.2 The boundary that matters

`Visual` **reads** match state and never writes it. `IndicatorController.cs:24` states the contract
in a tooltip, and the class exists downstream of `MatchManager` for exactly that reason: a
presentation layer that mutates match state is how a HUD ends up disagreeing with the referee about
whether a match is over.

### 3.3 Screen state machine

`GameManager` exposes `State` and guarded transitions — `ShowTitle`, `ShowHowTo`, `ShowCredits`,
`ShowMapSelect`, `ShowMatchSetup`, `Pause`, `Resume`, `Restart`, `QuitGame`. The guards matter:
`Pause()` only fires from `Playing`, `Resume()` only from `Paused`, so a double-tap cannot desync the
HUD from the presentation stack. In a player build `QuitGame()` logs the request and does nothing
else, which is why the rehearsal's EXIT step is honest about being editor-only.

---

## 4. Match rules

`MatchManager` is the single authority. The constants are serialized fields with defaults, so they
are visible in the inspector and quotable in a defense:

| Rule | Value | Line |
|---|---|---|
| Tag reach | 1.2 m | `MatchManager.cs:88` |
| Flag steal reach | 1.5 m | `MatchManager.cs:91` |
| Rescue reach | 1.5 m | `MatchManager.cs:110` |
| Immunity after rescue | 2 s | `MatchManager.cs:119` |
| Rescue cooldown | 3 s | `MatchManager.cs:123` |
| Match time limit | 180 s; 0 disables | `MatchManager.cs:128` |
| Dropped flag auto-return | 10 s | `Flag.cs:44`, applied `Flag.cs:181` |

Three decisions are worth defending:

1. **Rescue is not free.** A freed player carries `rescuedCooldownTimer`, and both
   `MatchManager.cs:416` and `EnemyAI.Scoring.cs:137` read it. Without it, two rescuers could bounce
   one prisoner forever and the match would never resolve.
2. **Immunity is short and deliberate.** Two seconds is enough to escape the cluster, not enough to
   become invulnerable (`CharacterStatus.Release(...)` at `CharacterStatus.cs:193`).
3. **The flag returns itself.** A dropped flag that nobody can reach would stall the match
   indefinitely; the `returnAfterSeconds` timer is the safety net, and `Flag.cs:15` says so in the
   file header.

The time limit is the outermost guard: `matchTimeSeconds` of 180 resolves a stalemate, and setting
it to 0 disables the limit entirely (`MatchManager.cs:608`).

---

## 5. Enemy AI

`EnemyAI` is one class across three partial files, split by concern:

- **`EnemyAI.Perception.cs`** — what the AI can see, and how long it remembers it. Perception memory
  is what stops an AI forgetting a player the instant they leave the cone.
- **`EnemyAI.Scoring.cs`** — utility scoring over candidate actions. Reading
  `EnemyAI.Scoring.cs:137` shows the rescue cooldown is part of the score, not just the rule.
- **`EnemyAI.Actions.cs`** — execution. The stop distances here are deliberately *inside*
  `MatchManager`'s reach distances (`EnemyAI.Actions.cs:84` and `:190`), so an AI that stops "at"
  the flag is genuinely within pick-up range rather than one step short and repeatedly failing to
  steal.

Difficulty is Easy, Normal, Hard, and it is applied **identically to both teams**. That is a design
position: the setting changes how demanding the match is, not which side the game favours. The
balance evidence is the 16 regression scenarios at 4v4 (Week 16) — a regression net, not a
large-scale study, and §11 says so.

---

## 6. Data, services and persistence

The game has no networked service. Its "service" layer is local persistence with an integrity model,
which is the proportionate choice for a single-player mobile game and was argued in Week 13.

| Data | Where | Why |
|---|---|---|
| Player profile (name, record) | Signed file via `PlayerProfile` | Can carry a signature; `PlayerPrefs` cannot |
| Match settings (team size, difficulty, tag hints) | `PlayerPrefs` via `MatchSettings` | Cosmetic and non-sensitive |
| Map selection | `MapSettings` / `MapDefinition` | Read at load; logged as `[Map] loaded selection: …` |

`PlayerProfile` writes an envelope of payload plus signature over SHA-256(salt + payload JSON), with
a per-install random 16-byte salt (`PlayerProfile.cs:109`, `:627`). Salting per install means a
signature forged on one device does not transfer to another.

---

## 7. Security

**What is protected.** A profile edited by hand fails its signature check and is rejected rather
than trusted: `PlayerProfile.cs:537` computes the expected signature and `:540` produces the
rejection message, after which the file is set aside as `profile.rejected.json` and a fresh profile
starts.

**What is not claimed.** This is not tamper-proofing against an attacker who controls the device. The
salt lives on that device and there is no server to verify against; `PlayerProfile.cs:91–95` states
this limit in the source, and this report does not upgrade it into a security claim it cannot
support.

**Tested.** `PlayerProfileTests` in `Assets/Tests/EditMode/` covers the profile contract, including a
deliberately-bad-signature path via `WriteRawForTests(payloadJson, correctSignature)`
(`PlayerProfile.cs:441`).

**Observed in the wild.** During Week 18 the Windows backup package rejected an editor-written
profile for exactly this reason and preserved it — an unplanned confirmation, recorded in
`Evidence/windows_backup_build.md` §5.

**Secrets.** No credentials are committed. The scan and its raw output are kept in
`Week_17/Evidence/secret_scan.md` and `secret_scan_raw.txt`.

---

## 8. Performance and optimisation

### 8.1 Method

Week 16 profiled the **shipping render path** rather than a convenient one, using
`Assets/Scripts/Diagnostics/PerfCapture.cs` and `PerfScenario.cs`. The condition set used to make a
Windows player exercise the mobile path is documented in the Week 16 records; repeat runs are only
comparable under the same conditions, which is why they are written down.

### 8.2 Measured result

| Metric | Value |
|---|---|
| Main thread | **0.59 – 0.63 ms** |
| GPU | **0.48 ms** |
| Render events | **17** |
| Managed allocation | **~320 B / frame** |
| Budget | **33.3 ms** (30 fps) |
| Render scale | **0.80** |

Main-thread cost is under 2% of the frame budget, so the frame is not CPU-bound; 320 bytes per frame
is small enough that garbage collection is not a plausible source of hitches.

### 8.3 The change that was made

**Prop texture memory fell from 21,849 KB to 1,369 KB — a 20.0 MB reduction.** The low-poly art was
carrying texture memory it never sampled at the resolution it was stored at. This is a memory
reduction, and it is the only optimisation claimed.

### 8.4 What is not claimed

- **The download shrank from 68.30 MB to 31.63 MB between Weeks 16 and 17 because the build changed
  from development to release.** Development builds carry debug symbols and extra runtime checks.
  Nothing was removed from the game. No size optimisation is claimed.
- **No GPU-side win is claimed** beyond the measured 0.48 ms. No shader rewrite was performed.
- **No claim is made that the profile would hold on other hardware.** The numbers are from one
  machine under the documented conditions.

---

## 9. Testing

| Layer | What it covers | Result | Source |
|---|---|---|---|
| EditMode unit tests | Team rules, match settings, profile contract, sprint config | **17 / 17 passed** | `run_tests`, 2026-11-19 |
| Accessibility audit | 516 checks over the release screens | **516 passed, 0 failed, 24 not measurable** | `Week_17/Evidence/readiness_results.md` |
| Balance scenarios | 16 scripted 4v4 scenarios | **16 / 0 failures** | Week 16 records |
| Usability playtest | 5 testers, 25 task attempts | **0 failures**, 23 unaided, 2 facilitated | Week 14 records |
| Release readiness | Four release screens in play mode | `VERDICT: working` | `Week_17/Evidence/readiness_results.md` |
| Demonstration rehearsal | Nine-screen sequence | 9 distinct captures, 14.2 s | `Evidence/demo_rehearsal.txt` |

The accessibility audit was run **twice** — before and after the `DEF-17-07` panel fix — and both
runs are recorded. 24 items are honestly marked *not measurable* rather than passed by omission.

---

## 10. Defects found and what changed

| Id | Defect | Resolution |
|---|---|---|
| `DEF-17-01` | Credits screen printed `0.2` | Credits build line corrected; scene string was baked before the version froze |
| `DEF-17-02` | `productName` carried the version, so `PlayerPrefs` namespace and `persistentDataPath` changed and every save was orphaned | Version removed from `productName`; this is why the app is `AgawanBase`, not `AgawanBase v1.0.0-rc1` |
| `DEF-17-07` | `Panel_HowTo/Body` needed 734.5 px of text inside a 660 px rect, so the tail of the rules rendered behind the BACK button | Body box resized to 1560×600 and `ReleasePanelsBuild.FitText` now **measures** the paragraph and shrinks the font until it fits, logging the result |
| `DEF-15-04` | An Android App Bundle was produced under an `.apk` filename | App Bundle turned off; archive entry layout checked on every release |

`DEF-17-07` is the most instructive: neither the accessibility audit nor the screenshot review
caught text overflowing its own box. It was found by verifying the release build in play mode and
looking at it. Guessing a font size once was enough to stop guessing.

---

## 11. Limitations

1. **No handset installation for this submission.** `adb devices` is empty. The APK is hashed and
   unmodified, but installation on a phone is **BLOCKED**, not demonstrated.
2. **The repository is local only.** `origin/main` is `d08fc9b`, which predates Weeks 15–17, so the
   tag and commit cannot be verified remotely.
3. **AI balance rests on 16 regression scenarios**, not on a study of human players against the AI.
4. **Two Week 14 discoverability findings were never re-tested** after the instructions screen was
   rebuilt for `DEF-17-07`.
5. **The Windows backup package differs from the submission** — Mono instead of IL2CPP, no haptics,
   two post-processing passes stripped (`Evidence/windows_backup_build.md` §6).
6. **The profile signature is device-local integrity, not security against a determined attacker.**

Every one of these is a gap that was found by testing rather than a gap that was assumed.

---

## 12. Reproducing any number in this report

All figures are command outputs or tool logs captured on this machine, not recalled values:

- Build facts: `Week_17/Evidence/build_and_commit.txt`
- Tests: `run_tests` EditMode on this tree, 2026-11-19
- Accessibility and readiness: `Week_17/Evidence/readiness_results.md`
- Defects: `Week_17/Evidence/issue_disposition.md`
- Secrets: `Week_17/Evidence/secret_scan.md`
- Demonstration: `Evidence/demo_rehearsal.txt`
- Build identity: `Evidence/release_record.md`
- Backup package: `Evidence/windows_backup_build.txt`
- Repository state: `Evidence/repo_state.txt`

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
