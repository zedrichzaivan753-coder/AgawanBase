# SA5 - Theme, Player, and Advanced Game-Integration Analysis

**Game:** AgawanBase
**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Week:** 13 - Secure Player Data and Responsible Development
**Build version:** `AgawanBase v0.2` (Android, ARM64 / IL2CPP, `com.zedrich.agawanbase`)
**Repository:** local Git repository, branch `main`, HEAD `d08fc9b`
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0

---

## 1. Theme-to-Mechanics Justification

### Target player

A Filipino student or young adult with a phone and a few spare minutes: someone who played
*Agawang Base* in a school yard, remembers the rules with their body rather than their head,
and wants that memory back without having to find eight people and a field. The player is
not looking for a story or a progression grind. They are looking for one clean contest
against opponents that behave like classmates.

### Intended experience

The feeling the original game produces is *tension between two decisions*: go for the flag,
or go back for the team-mate who just got caught. That is the experience AgawanBase is
built to reproduce. Every design decision in the game is judged against one question: does
this make the choice between attacking and rescuing harder in a way a player would
recognise from the playground?

### Core gameplay loop

1. Leave the base. Standing still at base is safe, so the match is decided by who dares
   leave and for how long.
2. Close on an opponent. A character who has been in the field longer than the opponent they
   touch is caught, not the other way round.
3. Catch, or be caught. A caught character is locked in the opponent's prison and cannot act.
4. Rescue, or push on. A free team-mate walking up to the prison releases the prisoner, and
   the rescuer gains no protection for doing it.
5. Steal the flag. Carrying the enemy flag into your own base wins the match.
6. Repeat until the flag comes home, one team is entirely locked up, or the three-minute
   clock runs out and the team with more characters free wins.

### How the mechanics communicate the theme

| Element of *Agawang Base* | Mechanic in the game | Why it is not decoration |
|---|---|---|
| Two bases at opposite ends of a field | Two bases, two prisons, two flags, a walled arena 34.5 m long | The arena shape is the rule: the distance between the bases is what creates the risk |
| "Fresh" and "stale" - the player who has been out longer is the one who can be caught | A 20-second freshness timer per character; the staler of two touching characters is the one captured; equal timers capture nobody (`MatchManager.tieEpsilon = 0.01`, `tieIsNoCapture = true`) | This is the rule that makes the game *Agawang Base* and not tag. It inverts the obvious reading of "tag" and it is measured to 0.01 s |
| Rescuing a team-mate ("bawi") | `rescueDistance = 1.5 m`, `immunitySeconds = 2`, `rescueCooldown = 3`, and the rescuer gets no immunity | The asymmetry - the freed player is briefly safe, the rescuer is not - is exactly the playground's risk |
| Hiding in bushes and behind the market stalls | `HideSpot` volumes: a character standing inside one loses its body and its overhead bars, keeping only the ground ring of the player's own character | Hiding is the theme's answer to being watched; it is a mechanic with a cost, not a cosmetic effect |
| Everyone plays every role | One preset difficulty drives both teams; the human is always Blue with AI team-mates, and Red is all AI | No side is secretly stronger, which is how a playground game stays fair |
| Playing on your own ground - the school, the market, the open field | Three separate scenes: `Game` (classic field), `Map_Palengke`, `Map_SchoolCampus`, each with its own hiding places and NavMesh | The maps are Filipino places, and their geometry changes the hiding routes |

A generic top-down tag game with a Filipino skin would not have the freshness inversion, the
no-immunity-for-the-rescuer rule, or the bush hiding that punishes standing still in the
open. Those three together are the theme expressed as rules.

---

## 2. Player and stakeholder analysis

| Stakeholder | What they need from this build | How the build answers it |
|---|---|---|
| The player | Understand the goal without reading a manual; control with one thumb; know instantly what just happened | Title screen, a rules line, one map-select screen with a blurb per map, a match-setup screen with a summary line, a live HUD with freshness, stamina, flag and team counters, and an end screen that names which of the three endings occurred |
| The player, as the owner of their own data | Know that nothing about them leaves the phone | The game contains no networking code, forces no network permission and makes no network calls. The profile is one JSON file in the app's own sandbox, and the profile screen says so on screen: "Type a name, then SAVE. Nothing leaves this device." |
| The instructor | Verifiable evidence that the work is the student's own | Repository with commits, the PT5 check log with real measured results, and a disclosure record in PT5 |
| The developer (me) | Be able to see why the AI did something | A serialised AI read-out on the HUD, which is a debugging aid that must be off in a shipped build - carried into Week 15 as a defect |
| Future playtesters (Week 14) | A build that behaves the same way twice | Fixed camera, fixed rules, no randomness in the rules themselves; only the AI's scores carry noise (`DifficultyProfile.noise`) |

---

## 3. Gameplay and integration requirements

| ID | Requirement | Where it lives |
|---|---|---|
| R1 | Choose a map, see its name and a one-line description | `MapSelectUI`, `MapDefinition` assets in `Assets/Config` |
| R2 | Choose 1v1 to 4v4 and Easy / Normal / Hard; the same preset applies to both teams | `MatchSettings`, `MatchSetupUI` |
| R3 | Start, pause, resume, restart, return to title | `GameManager` state machine |
| R4 | Catch the staler character on contact; equal freshness captures nobody | `MatchManager` |
| R5 | Rescue a prisoner from prison; the rescuer gains no immunity | `MatchManager` |
| R6 | Carry the enemy flag home to win; concede a flag carried home by the enemy | `Flag`, `MatchManager` |
| R7 | Hide inside a bush, hedge or crate stack | `HideSpot`, `CharacterBushHide`, `CharacterBars` |
| R8 | Remember the player's settings and map between launches | `MatchSettings`, `MapSettings` (PlayerPrefs) |
| R9 | Remember who the player is and what they have achieved, across restarts | **Week 13: `PlayerProfile` (signed JSON save)** |
| R10 | Show that record on a screen the player can open from the title | **Week 13: `ProfileUI`, `Game/UI/Panel_Profile`** |
| R11 | Survive a corrupt or hand-edited save without crashing or trusting it | **Week 13: signature + quarantine + clamps** |

---

## 4. Data inventory

Everything the build touches, in one table. "Sandbox" means the app's private folder, which
on Android is not readable by other apps.

| Data item | Where it comes from | Where it is stored | Who can read it | Lifetime and deletion |
|---|---|---|---|---|
| Display name (up to 16 characters) | Typed by the player on the profile screen | `profile.json` in `Application.persistentDataPath/AgawanBase/` | The app; the same device user via a file manager | Until the player presses RESET, or uninstalls the app |
| Match results (result, ending type, match seconds, map id, team size, difficulty, UTC timestamp) | Recorded at the end screen from `MatchManager.outcome` and `MatchManager.MatchClock` | `profile.json`, `recent` list, newest first | The app | Oldest entries are dropped past 20; RESET erases all |
| Aggregate record (wins, losses, draws, current streak, best streak, last match id) | Derived while recording a result | `profile.json` | The app | Same as above |
| Match settings (team size, difficulty, tag hints) | Match Setup screen and the pause screen | PlayerPrefs keys `match.*` | The app | Until the player changes them |
| Map choice (map id, scene name, display name) | Map Select screen | PlayerPrefs keys `map.*` | The app | Until the player changes the map |
| Signature salt (16 random bytes, base64) | Generated by the app on first save | PlayerPrefs key `profile.salt` | The app | Survives RESET, so a reset profile is signed with the same install salt |
| **Not collected:** account, e-mail, phone number, contacts, location, advertising id, device identifiers, analytics, telemetry, crash reports | - | - | - | - |

The list of things *not* collected is the important half. The game has no login, no
multiplayer, no analytics SDK and no `UnityWebRequest` call; the project keeps no custom
Android manifest and does not force the `INTERNET` permission (the Player Setting
`forceInternetPermission` is not present, so it is off). There is nothing to transmit and
nowhere to transmit it. The scan behind that sentence is
`Week_13/Evidence/secrets_and_gitignore.txt`.

---

## 5. Data-flow diagram

<!--INLINE:SA5_data_flow.svg-->

Reading the diagram: the player's typed name and the RESET command enter through the panel;
the profile class is the only thing that signs or writes; the only two stores are one file
and PlayerPrefs; the only other writer is the end-of-match callback, and it goes through the
same `RecordMatch` gate that refuses an id it has already banked. Nothing crosses the dashed
line.

---

## 6. Identity model decision

The module allows no account, local profile, anonymous authentication or e-mail
authentication, and asks for the choice to be proportionate.

**Decision: a local profile, with no account of any kind.**

Why:

- The game is single-player and offline. An account would exist only to identify a player to
  a server that does not exist; there is no leaderboard to protect and no second device to
  sync to.
- A login screen on a playground game adds a failure state (no signal, forgotten password)
  in exchange for no gameplay.
- Collecting an e-mail address from a game aimed at students would create a data-protection
  obligation that the project cannot service - there would be no deletion process, no
  retention policy and no breach process.
- `Packages/manifest.json` contains no Firebase, PlayFab or Unity Services package, so the
  choice also matches the build: adding a cloud service would mean installing a package,
  creating a cloud project and holding a key, which is a much larger surface than this
  build needs.

What the player gets instead: their name and their record on the screen they open from the
title, remembered between launches, and the ability to erase it themselves in two presses.

The honest cost of this decision, and the reason it is defensible rather than lazy: a local
record can be edited by whoever owns the phone. The signature makes that *detectable*, not
impossible. See PT5 L15.

---

## 7. Secure configuration and secret handling

| Risk | Position in this build | Evidence |
|---|---|---|
| Credentials in source code | There are none. No API key, token, service account file or password exists, because no service is used | `Week_13/Evidence/secrets_and_gitignore.txt` (scan output) |
| Shipped secrets | The only secret-like value is the signature salt, and it is **generated at runtime** into PlayerPrefs, never committed | `PlayerProfile.Salt()` |
| Build artefacts in the repository | `.gitignore` excludes `*.apk`, `*.aab`, `*.unitypackage`, `Library/`, `Temp/` and `Builds/` | `.gitignore` lines 81-88 |
| Player records readable by other apps | The save lives in the app's own sandbox under `Application.persistentDataPath` | `PlayerProfile.DataDirectory` |
| Trusting untrusted input | The save file is treated as hostile: parsed defensively, signature-checked, clamped, and quarantined rather than overwritten | `PlayerProfile.TryParse`, `Clamp`, `Quarantine` |

What is *not* claimed: the save is not encrypted, and the salt is on the same device as the
file. This is stated on screen and in the documentation rather than glossed over.

---

## 8. Failure and recovery design

An offline game has no network to fail, so the failure cases that matter are the disk and the
file itself:

| Failure | What the build does | Check |
|---|---|---|
| No save file at all | Starts on `PLAYER` with an empty record; writes nothing until the player saves something | L1 |
| Save file is not JSON | Parsing is wrapped; the file is renamed to `profile.rejected.json` and a fresh profile starts. The bad file is kept as evidence | L10 |
| Save file was edited by hand | Signature mismatch; same quarantine path as above. The edited values never reach the UI | L11 |
| Save holds impossible numbers | Clamped to zero (or raised to the current streak) after the signature passes; an edited save cannot produce a negative record | L12 |
| Save folder cannot be written | The write is wrapped, `LastError` is set, the UI reports it, and the in-memory record is rolled back so memory and disk never disagree | L14 |
| App killed mid-write | The file is written to `profile.json.tmp` first and then moved over the old one, so a half file never replaces a whole one | design, see `PlayerProfile.Save` |
| The same match reported twice | `RecordMatch` refuses an id at or below `lastMatchId`, and `GameManager` guards with its own `recordedMatchId` field | L9 |
| The player wipes the record by accident | RESET is two presses within 4 s, and the first press says what will be lost | `ProfileUI.ResetPressed` |

---

## 9. Integration plan, Weeks 13 to 18

| Week | What this profile work becomes the input for |
|---|---|
| 13 | The profile, the save, the panel and the fifteen security checks - this submission |
| 14 | The panel is one of the screens the usability test covers: is a 16-character name field with a visible limit understandable on a phone; is the fastest-victory board legible at arm's length; is the two-press RESET discoverable |
| 15 | The record path is a test target: a match ended by all three endings must be banked exactly once, and the RESET path must not leave a half-erased state. The AI debug read-out defect already recorded in `Docs/Part_D_Test_Results.md` is fixed here |
| 16 | Profiling: the save is a synchronous file write at the end of a match, so its cost is measured and, if needed, moved off the end-screen frame |
| 17 | README, technical documentation, user guide and evidence index all describe this component rather than being written from scratch |
| 18 | The profile is part of the demonstration script: name it, win a match, show the record and the board, then show the two-press reset |

---

## 10. Risk register

| Risk | Likelihood | Impact | Mitigation in this build |
|---|---|---|---|
| A hand-edited save is trusted | Medium | The player's record becomes meaningless | Signature check; rejected files quarantined, never merged |
| A corrupt save crashes the game on launch | Low | The game will not open | Every read and write is wrapped; a failure starts a fresh profile |
| The player loses a long record by accident | Medium | Trust in the game | Two-press reset with a warning, 4-second disarm |
| Personal data collected without a process to delete it | Very low | Data-protection exposure | No personal data is collected at all; name is 16 characters, player-chosen, player-deleted |
| The profile work breaks the match flow | Medium | The submission does not run | The end-of-match call is the only coupling; guarded by match id and verified by playing a match end to end |
| A secret ends up in the repository later | Low | Credential exposure | No service to hold a key for; `.gitignore` excludes builds; scan recorded in the evidence folder |

---

## 11. Evidence index

| File | What it shows |
|---|---|
| `Week_13/Evidence/pt5_checks.log` | Fifteen checks, fifteen passes, with the measured value for each |
| `Week_13/Evidence/E1_profile_empty.png` | The panel on a fresh install: `PLAYER`, no matches |
| `Week_13/Evidence/E2_profile_stats.png` | The record after three recorded matches |
| `Week_13/Evidence/E3_leaderboard.png` | The fastest-victory board with five entries, quickest first |
| `Week_13/Evidence/E4_after_reset.png` | The panel after the two-press reset |
| `Week_13/Evidence/SA5_data_flow.svg` | The data-flow diagram in this document |
| `Week_13/Evidence/build_and_commit.txt` | The APK, its size, the build settings and the repository commit |
| `Week_13/Evidence/secrets_and_gitignore.txt` | The secret scan and the ignore rules |
| `Week_13/Evidence/HOW_TO_RERUN.txt` | How to reproduce every check and screenshot |
