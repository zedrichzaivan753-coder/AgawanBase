# PT5 - Secure Player Profile, Integrity-Checked Save, and Local Best-Times Board

**Game:** AgawanBase
**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 Advanced Mobile Application Development, Module 5
**Theme:** Philippine Games and Sports
**Week:** 13 - Secure Player Data and Responsible Development
**Build version:** `AgawanBase v0.2` (Android, ARM64 / IL2CPP, `com.zedrich.agawanbase`)
**Engine:** Unity 6000.6.0f1, Universal Render Pipeline 17.6.0, C#
**Repository:** local Git repository, branch `main`, HEAD `d08fc9b`
**Component under test:** `Assets/Scripts/Core/PlayerProfile.cs`, `Assets/Scripts/UI/ProfileUI.cs`

---

## 1. What was built

The game now remembers the player. A profile screen reachable from the title screen holds a
display name, a win/loss/draw record with the current and best win streak, and a local
best-times board of the five fastest victories. The record is stored in a signed JSON file in
the app's own sandbox, and it survives closing and reopening the game.

| Piece | File | Responsibility |
|---|---|---|
| Profile store | `Assets/Scripts/Core/PlayerProfile.cs` | The only thing that reads, signs, writes, clamps or deletes the save. Static, so it survives the scene reload that Restart and Title both perform |
| Panel controller | `Assets/Scripts/UI/ProfileUI.cs` | Drives `Game/UI/Panel_Profile`: name field, record, board, SAVE / RESET / BACK. Never touches the file and never parses JSON |
| State machine | `Assets/Scripts/UI/GameManager.cs` | New `GameState.Profile`, `ShowProfile()` / `CloseProfile()`, panel visibility in `SetState()`, and one guarded `PlayerProfile.RecordMatch(...)` call from the end-of-match handlers |
| Scene builder | `Assets/Editor/ProfileBuild.cs` | Builds the panel and the title `PROFILE` button into all three scenes and wires every listener. Re-runnable |
| Test harness | `Assets/Editor/Pt5Checks.cs` | `RunDataChecks()` writes the L1-L15 log; `Pt5Shots.Begin()` captures the four screenshots |

### Public surface of `PlayerProfile`

| Member | Purpose |
|---|---|
| `PlayerName`, `Wins`, `Losses`, `Draws`, `MatchesPlayed` | The record as the panel reads it |
| `CurrentStreak`, `BestStreak`, `LastPlayedUtc`, `LastMatchId`, `NextMatchId` | Streak and identity of the last banked match |
| `Recent` | Up to 20 finished matches, newest first |
| `BestWins()` | The best-times board: victories sorted by `seconds` ascending, then by `matchId`, capped at 5 |
| `Load()`, `Save()`, `SetName(string)`, `RecordMatch(...)`, `ResetProfile()` | The five state changes; every one of them is wrapped and returns a bool instead of throwing |
| `SanitizeName(string)`, `ComputeSignature(string)` | The two pure functions the harness proves directly |
| `SetDataDirectory`, `WriteRawForTests`, `WriteGarbageForTests` | Test seams, so the failure cases can be proven without touching the player's real save |
| `DataDirectory`, `SavePath`, `LastError`, `IsLoaded` | Inspection, for the panel's status line and for a bug report |

The panel's on-screen status line is part of the design, not decoration: on open it reads
*"Type a name, then SAVE. Nothing leaves this device."*

---

## 2. Where the player meets it

1. The title screen gains a **PROFILE** button under **START**.
2. Pressing it puts `GameManager` into `GameState.Profile`, which hides every other panel and
   shows `Panel_Profile` (backdrop alpha 0.94).
3. The player types a name (the field itself refuses past 16 characters), presses **SAVE**, and
   the status line confirms. **BACK** returns to the title.
4. Playing a match to an end banks exactly one entry: win, loss or draw, with the ending type,
   the match clock in seconds, the map, the team size and the difficulty.
5. Reopening PROFILE shows the updated record and, for a victory, a line on the fastest-victory
   board.
6. **RESET PROFILE** is deliberately two presses within 4 seconds; the first press renames the
   button to `PRESS AGAIN` and says what will be lost.

Each step is photographed in `Evidence/` (E1 to E4, listed in section 8).

---

## 3. The save file

### Location

| Platform | Path |
|---|---|
| Android | `/data/data/com.zedrich.agawanbase/files/AgawanBase/profile.json` (i.e. `Application.persistentDataPath/AgawanBase/profile.json`) |
| Windows editor | `C:/Users/<user>/AppData/LocalLow/<company>/AgawanBase v0_2/AgawanBase/profile.json` |

The folder is the app's private sandbox. No other app on the phone can read it, and uninstalling
the game deletes it. Nothing is written anywhere else.

### Format

One UTF-8 JSON envelope: an `int schema` for future migrations, a `string sig` holding the
signature, and the `payload` that the signature covers.

```json
{
    "schema": 1,
    "sig": "6b1f0d5a91c7e4a2b8d3f6071c2e9a4b5d6c7e8f90a1b2c3d4e5f60718293a4b",
    "payload": {
        "schema": 1,
        "playerName": "Zedrich Abrera",
        "wins": 2,
        "losses": 1,
        "draws": 0,
        "currentStreak": 1,
        "bestStreak": 2,
        "lastMatchId": 3,
        "lastPlayedUtc": "2026-11-18T11:42:07Z",
        "recent": [
            {
                "matchId": 3,
                "result": "WIN",
                "outcome": "FLAG_CARRIED_HOME",
                "seconds": 47.3,
                "mapId": "palengke",
                "teamSize": 2,
                "difficulty": "NORMAL",
                "whenUtc": "2026-11-18T11:42:07Z"
            }
        ]
    }
}
```

| Field | Type | Rule enforced by `PlayerProfile` |
|---|---|---|
| `schema` | int | Refused and quarantined if it is newer than `SchemaVersion` (1) |
| `sig` | string | Lowercase hex SHA-256; compared with `StringComparison.Ordinal` |
| `payload.playerName` | string | Trimmed, control characters stripped, runs of spaces collapsed, capped at 16, blank becomes `PLAYER` |
| `payload.wins` / `losses` / `draws` | int | Floored at 0 after the signature passes |
| `payload.currentStreak` | int | Floored at 0; reset to 0 by a loss, kept by a draw |
| `payload.bestStreak` | int | Raised to at least `currentStreak`, so an edited save cannot show a streak better than the record |
| `payload.lastMatchId` | int | Floored at 0. `RecordMatch` refuses any id at or below it, which is what makes a double-bank impossible |
| `payload.recent` | list | Null entries removed; trimmed to 20 newest |

`*.apk` is listed in `.gitignore`, so the build under `Week_13/` is deliberately outside version
control. The source that produced it is not.

---

## 4. Integrity design

Six decisions, in the order they protect the file.

**4.1 The save is signed, and the signature is over the canonical payload.**
On write, the payload is serialised with `JsonUtility.ToJson(state)` and that exact string is
signed. On read, the file is parsed and the payload is re-serialised with the same call before
the signature is recomputed. Signing the raw text of the file instead would have made the check
depend on whitespace and key order, so a cosmetic difference would have looked like tampering.
The canonical form is the contract.

**4.2 The key is per install and never shipped.**
`ComputeSignature` prepends a 16-byte salt generated by `RandomNumberGenerator` on first save and
kept in `PlayerPrefs["profile.salt"]` as base64. There is no constant in the source, so a compile
of the game does not contain the key, and two installs sign differently.

**4.3 The write is atomic.**
The JSON goes to `profile.json.tmp`, the old file is deleted, and the temp file is moved into
place. A process killed mid-write leaves a `.tmp` file behind, never a half-written `profile.json`
where a complete one used to be.

**4.4 A rejected file is quarantined, not destroyed.**
When parsing or the signature check fails, the file is moved to `profile.rejected.json` and a
fresh default profile starts. A player's only copy is never overwritten by a game that did not
understand it, and if this ever happens for real, the rejected file is the evidence.

**4.5 Numbers are clamped even when the signature passes.**
A correctly signed file is still a file, and a correctly signed file is what an attacker with the
salt produces. So after the signature passes, negatives are floored, `bestStreak` is raised to at
least `currentStreak`, the name is re-sanitised and the recent list is cleaned. Conservative
clamping means an edited save degrades to something plausible instead of something impossible.

**4.6 There is exactly one gate, and it is a match id.**
`RecordMatch` returns early when `matchId <= lastMatchId`. `GameManager` also keeps its own
`recordedMatchId` field, so a duplicated end-of-match callback cannot bank a match twice even
before it reaches the store. Two independent guards for the same failure, because an inflated
record is the failure mode that actually matters to a player.

**Rejected alternative:** encrypting the file. There is no key the attacker does not have, so
encryption would add code, a decryption failure path, and the false impression of a guarantee,
without changing who can produce a valid file. It is not in the build and section 5 says so on
the record.

---

## 5. What the signature does not do (the honest limit)

The save is **tamper-evident, not tamper-proof.**

- It is not encrypted. Anyone who can read the file can read the name and the record.
- The salt lives on the same device as the file. A determined local attacker can extract it and
  re-sign an edited save, and the game will accept it.
- There is no server, no account and no online leaderboard, so there is nothing to verify the
  record against. For an offline single-player playground game this is the true ceiling, and it
  is stated rather than hidden.

This is recorded in the check log as **L15 KNOWN LIMIT** rather than passed off as a pass, and it
is repeated in `README.md` under known limitations.

---

## 6. Security check results

Every check below was executed by `Assets/Editor/Pt5Checks.cs` in the Unity editor on
2026-11-18 11:49:27 UTC, against a scratch folder
(`.../AgawanBase_pt5_scratch`) so the player's real save was never written to. The scratch folder
was deleted afterwards. The measured value in the right-hand column is copied from
`Evidence/pt5_checks.log`.

| ID | What is being proven | Result | Measured value |
|---|---|---|---|
| L1 | No save file at all is not an error | PASS | `name=PLAYER matches=0` |
| L2 | A valid name is trimmed and inner spaces collapsed | PASS | `name='Zedrich Abrera'` |
| L3 | The profile survives a reload from disk, and the file carries a signature | PASS | `name='Zedrich Abrera' file=True sig=True` |
| L4 | A blank name is refused and the stored name is kept | PASS | `refused=True name='Zedrich Abrera'` |
| L5 | A name longer than 16 characters is capped | PASS | `length=16 name='ABCDEFGHIJKLMNOP'` |
| L6 | Control characters are stripped out of a name | PASS | `name='ZedrichX'` |
| L7 | A finished match is recorded and written to disk | PASS | `returned=True wins=1 recent=1` |
| L8 | The best-times board keeps the 5 fastest victories, quickest first | PASS | `count=5 first=42.5 sorted=True` |
| L9 | The same match id cannot be banked twice | PASS | `refused=True wins=6` |
| L10 | An unreadable save starts empty without a crash, and is set aside | PASS | `name=PLAYER matches=0 rejected kept=True` |
| L11 | A hand-edited save whose signature does not match is rejected | PASS | `wins=0 name=PLAYER error='the signature does not match the contents (the file was edited by hand)'` |
| L12 | Impossible numbers in a correctly signed save are clamped, not trusted | PASS | `wins=0 losses=0 bestStreak=9 name='Zedrich Abrera'` |
| L13 | Reset erases the record on disk and returns the default name | PASS | `returned=True file=False name=PLAYER` |
| L14 | An unwritable save location reports an error instead of crashing | PASS | `saved=False error='could not write ...\blocked\profile.json: ...'` |
| L15 | **Known limit:** the save is tamper-evident, not encrypted | documented | Not asserted as a pass; printed as a limitation |

```
TOTAL: 15 passed, 0 failed
```

Two details worth stating, because they are what the checks are actually worth:

- L12 plants a save whose numbers are impossible but whose signature is **correct**. It passes the
  signature gate and is then clamped. This proves that the signature is not being used as the only
  defence.
- L14 does not merely fail to crash: `PlayerProfile.LastError` carries the operating system's own
  message, and `ProfileUI` prints it, so a real player would see *"Could not save."* followed by
  the reason rather than a silent no-op.

---

## 7. Reproducing every check

`Evidence/HOW_TO_RERUN.txt` holds the exact procedure: which editor menu command runs the harness,
where the log lands, how the scratch folder is isolated from the real save, and how the four
screenshots are re-captured. Nothing in that procedure needs a phone.

---

## 8. Evidence

| File | What it shows |
|---|---|
| `Evidence/pt5_checks.log` | The fifteen checks with their measured values, and the four screenshot descriptions |
| `Evidence/E1_profile_empty.png` | The panel on a fresh install: `PLAYER`, no matches, empty board |
| `Evidence/E2_profile_stats.png` | The record after three recorded matches: 2W/1L, streak and best streak |
| `Evidence/E3_leaderboard.png` | The fastest-victory board with five entries, quickest first |
| `Evidence/E4_after_reset.png` | After the two-press reset: back to `PLAYER`, record empty |
| `Evidence/build_and_commit.txt` | Unity version, pipeline, backend, APK bytes, compile result, commit |
| `Evidence/secrets_and_gitignore.txt` | The credential scan and the ignore rules |
| `Abrera_Zedrich_PT5.apk` | The installable build, 40,876,464 bytes |

The screenshots were captured in play mode from the real panel driving the real save file. After
the first capture the backdrop was judged too light at alpha 0.75 against the record text, so it
was darkened to 0.94 and the four are the re-shot set; the log says so at the head of its
screenshot section rather than presenting them as if nothing changed.

---

## 9. Disclosure record

Stated plainly, because the module asks for it and because the alternative is worse.

| Activity | How it was done |
|---|---|
| SA5 analysis, PT5 write-up, reflection drafting | Written by me, with AI assistance for structure and phrasing, then reviewed and corrected against the code |
| `PlayerProfile.cs`, `ProfileUI.cs`, `ProfileBuild.cs`, `Pt5Checks.cs` | Written with AI assistance, read line by line, and every constant in this document was taken from the code rather than from the plan |
| The fifteen checks | Executed by the harness in the editor; the values in section 6 are copied from the generated log, not typed from memory |
| The four screenshots | Captured by the harness in play mode; they are the real panel |
| The APK | Built by Unity 6000.6.0f1 through the project's Android build profile from this source |
| The design decision in section 4 (sign, do not encrypt) | Mine, and the reasoning in section 5 is the reason I am not claiming more than it does |

Nothing in the project was fabricated for the write-up. Where the build falls short - no audio,
no on-device profiling, a debug-signed APK, the AI debug read-out still enabled - `README.md` and
section 5 list it as a limitation instead of leaving it out.

---

## 10. Build and repository state

| Item | Value |
|---|---|
| APK | `Week_13/Abrera_Zedrich_PT5.apk`, 40,876,464 bytes (38.98 MB) |
| Build target | Android, ARM64 only, IL2CPP, minimum API 26 |
| Build duration | 258 s, 0 errors, 20 warnings |
| Package | `com.zedrich.agawanbase`, product `AgawanBase v0.2` |
| Scenes in build | `Game.unity`, `Map_Palengke.unity`, `Map_SchoolCampus.unity` |
| Signing | Debug keystore - not suitable for store distribution |
| Repository HEAD | `d08fc9b` "Update README", 2026-09-21 |
| Commit for this work | pending; see the note below |

The profile work is present in the working tree but was not committed when this document was
written. `Evidence/build_and_commit.txt` records the HEAD at build time and the state of the
working tree. The commit id must be pasted into this row and into `README.md` once the commit
exists - the row is left explicit rather than filled with a guess.

---

## 11. How this meets the Week 13 laboratory requirements

| Module requirement | Where it is satisfied |
|---|---|
| Secure and responsible handling of player data | A profile file in the app sandbox; no identifier, no account, no networking code, no forced network permission, no analytics SDK |
| An identity model proportionate to the game | Local profile only, argued in SA5 section 6 and implemented as `PlayerProfile` |
| An integrity check on the saved data | SHA-256 over the canonical payload, salted per install (section 4.1, 4.2) |
| Graceful behaviour on corrupt or tampered data | Quarantine plus clamps (L10, L11, L12) |
| A recoverable, provable reset | Two-press reset with a 4-second disarm (L13, `ProfileUI.ResetPressed`) |
| Evidence rather than assertion | `pt5_checks.log` with measured values, four screenshots, a real APK |
| Responsible development and disclosure | Section 9, plus SA5 section 7 on secret handling |
| A reflective account of the work | `Abrera_Zedrich_Week13_Reflection.md` |
