# Abrera_Zedrich_PT7_User_Guide

**Agawan Base - user guide**
**Version 1.0.0-rc1** - Android 8.0 or newer, ARM64 phone or tablet
**Student:** Zedrich Zaivan B. Abrera - MOBILE DEV 002, Module 5, Week 17

This guide covers installing the game, every screen you will meet, what each control does, the rules in plain language, and what to do when something does not work. It is written for someone who has never seen the game and has two minutes.

## Contents

1. What the game is
2. What you need
3. Installing
4. First launch
5. Starting a match
6. Reading the screen while you play
7. Controls
8. The rules in plain language
9. How a match ends
10. Your profile and record
11. Sound, hints and pause
12. Troubleshooting
13. Removing the game and its data
14. Known limitations

---

## 1. What the game is

Agawan Base is a 3D mobile version of the Filipino playground game *Agawang Base*. Two teams face each other across a walled court. You control one character on the **Blue** team; all the other characters, on both sides, are AI.

You win by stealing the Red team's flag and carrying it into your own base. You also win by catching every Red player, or by leading when the three-minute clock runs out.

The one rule that everything else follows from: **whoever has been out of their base for less time wins a tag.** If you have just left your base and an enemy has been running around for ten seconds, you can catch them by touching them. If it is the other way round, they catch you.

## 2. What you need

| | |
|---|---|
| Device | An Android phone or tablet with a 64-bit ARM processor |
| Android version | 8.0 (API 26) or newer |
| Free storage | about 35 MB |
| Orientation | the game runs in landscape and does not rotate |
| Network | **not required** - the game never connects to anything |

## 3. Installing

1. Copy `Abrera_Zedrich_PT7.apk` to the phone - by USB cable, Bluetooth, or by sending it to yourself.
2. Open the file from your Files app or your notification shade.
3. Android will say the file is from an unknown source. Allow it for the app you are installing from; this is normal for any app that does not come from the Play Store.
4. Tap **Install**, then **Open**.

With a cable and developer options, the same thing from a computer:

```
adb install -r "Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk"
adb shell am start -n com.zedrich.agawanbase/com.unity3d.player.UnityPlayerActivity
```

The app id is `com.zedrich.agawanbase`, so a second copy of an older build will not install beside it - uninstall the old one first.

## 4. First launch

The game opens on the title screen. The top-left corner shows the exact build you are running, for example **`1.0.0-rc1 - built 2026-11-19`**. That string is read from the installed app itself, so if you are ever unsure which build is on the phone, read it there.

![E1 - the title screen of the release candidate. The version caption is top-left; HOW TO PLAY and CREDITS open the two reading screens; EXIT closes the app; START begins a match; PROFILE shows your record.](Evidence/E1_title_release.png)

| Title screen control | What it does |
|---|---|
| **HOW TO PLAY** | opens the instructions screen. BACK returns here |
| **CREDITS** | author, course, theme, engine, licences and the AI-assistance disclosure. BACK returns here |
| **EXIT** | closes the game |
| **START** | goes to map select, then match setup |
| **PROFILE** | your name, matches played, wins, losses, draws, fastest victories and a leaderboard of your recent matches |

## 5. Starting a match

1. **START** on the title opens **MAP SELECT**: three maps - **Classic Open Field**, **Palengke** and **School Campus**. Each has its own layout and its own places to hide.
2. Choose the map, then the **match setup** screen:
   - **Team size:** 1v1, 2v2, 3v3 or 4v4 characters per side. You are always one of the Blue characters; the rest are AI.
   - **AI skill:** EASY, NORMAL or HARD. Both teams always use the same setting, so neither side gets an advantage.
3. Tap **START**. The match begins immediately.

Your choices are remembered: next time you open the game, the same map, size and skill are still selected.

## 6. Reading the screen while you play

![E4 - a match in progress. Top-left: freshness and stamina bars with the safe-at-base read-out. The joystick is bottom-left, SPRINT and the camera arrows bottom-right, and the pause control top-right.](Evidence/E4_match_running.png)

| What you see | What it means |
|---|---|
| **Freshness bar** (top-left) | drains the longer you are away from your base and refills at home. Full bar means you are fresh - you can catch almost anyone |
| **SAFE AT BASE** | you are standing in your own base; nobody can catch you there |
| **Stamina bar** | your sprint energy. Sprint is faster but only lasts about two seconds before it recharges |
| **Team lines** | how many of each team are still free, and where each flag is: at base, carried, or dropped |
| **Ring over an enemy - green with a tick** | you are fresher than them: if you touch them, you catch them |
| **Ring over an enemy - red with a cross** | they are fresher than you: if they touch you, you are caught |
| **Grey ring or no ring** | neutral - the tag would be a tie or hints are switched off |
| **Bobbing arrow and ring** | your own character |
| **Edge-of-screen arrows** | point at off-screen things that matter: the enemy flag, your own flag when it has been stolen, and a team-mate in prison |

Inside a bush, hedge or crate stack your body and bars disappear completely; only your ground ring stays visible, so you can still tell where you are.

## 7. Controls

Everything is done with the left thumb on the stick and the right thumb on the buttons.

| Action | How |
|---|---|
| Move | drag the **joystick**, bottom-left |
| Sprint | hold **SPRINT**, bottom-right |
| Catch an enemy | walk into them while you are fresher - nothing to press |
| Rescue a team-mate | walk into the enemy prison while you are free - nothing to press |
| Pick up the flag | walk onto the enemy flag |
| Pause | the pause control, top-right |
| Look around | the camera arrows, bottom-right |
| Play again / leave | **RESTART** or **TITLE** on the pause or end screen |

## 8. The rules in plain language

- **Freshness.** Every character has a timer that counts up while they are away from their base and resets when they step back in. You do not see the number; the freshness bar and the coloured rings show you the result.
- **Tagging.** When two opponents touch, the one with the **smaller** timer - the fresher one - wins. The loser is pulled into the enemy **prison** and cannot move. A perfect tie catches nobody.
- **Bases.** Inside your own base you are safe: nobody can catch you there.
- **Rescue.** Stand in the enemy prison while free and your imprisoned team-mate is released. They get two seconds of protection, and prison cannot be re-entered instantly - the rescue itself has a short cooldown.
- **The flag.** Walk onto the enemy flag to pick it up. If the carrier is caught, the flag falls where they were caught and returns to its base after ten seconds.
- **Hiding.** Bushes, hedges and crate stacks hide you. Use them to cross the middle of the map without being seen.
- **Stamina.** Sprint is faster but finite. Do not arrive at the enemy flag with an empty bar.

## 9. How a match ends

| Ending | Condition |
|---|---|
| **Victory by flag** | you carry the Red flag into your own base |
| **Defeat by flag** | a Red carries your flag into their base |
| **Victory by elimination** | every Red character is in prison at the same moment |
| **Defeat by elimination** | all Blue characters are in prison |
| **Win / loss on time** | at the 180-second limit, the team with more members free wins |
| **Draw** | the clock expires with an equal number of free members and equal freshness |

Every ending shows a screen with the reason in words, and offers **RESTART** (a new match with the same map, size and skill) and **TITLE**.

## 10. Your profile and record

The **PROFILE** screen holds one local profile: a display name you choose, the number of matches played, wins, losses, draws, your fastest victories, and a leaderboard of your most recent matches.

Everything is stored on this phone. There is no account and nothing is uploaded. The save file refuses a record that has been edited outside the game - it is tamper-evident, not encrypted. One profile exists per install, so two people sharing a phone share one record. **Reset profile** clears the name and the record and leaves a usable, empty profile.

## 11. Sound, hints and pause

The pause screen (top-right of the HUD) offers:

| Control | Effect |
|---|---|
| **RESUME** | continue where you left off |
| **RESTART** | start the same match over |
| **TITLE** | back to the title screen; the match is abandoned |
| **TAG HINTS** | switch the coloured rings over opponents on or off - a display choice only, it changes no rule |
| **SOUND: ON / OFF** | mute or restore the five feedback sounds. The choice is remembered |

Sending the app to the background (Home, or a call) pauses the match automatically. Coming back offers **RESUME**.

## 12. Troubleshooting

| Symptom | Cause and what to do |
|---|---|
| "App not installed" | an older version with the same app id is on the phone. Uninstall it, then install again |
| "There was a problem parsing the package" | the file was truncated in transit - copy it again, and check it is the full 31.63 MB |
| The installer refuses for a 32-bit phone | this build is ARM64 only; it will not run on 32-bit ARM devices |
| No sound | check **SOUND** on the pause screen is ON, then the phone's media volume |
| The game paused by itself | the app pauses when it loses focus; that is deliberate |
| The rings over enemies disappeared | **TAG HINTS** was switched off on the pause screen |
| My record was reset | **Reset profile** was used, or the app was cleared from Android settings, or the app id changed between builds - the record lives inside the app's own data folder |
| Everything feels too small / too large | the layout scales with the screen and is measured against a 1920 x 1080 reference; report the device and the version caption if a label is unreadable |

## 13. Removing the game and its data

Uninstalling the app removes the game and its profile together, because the profile lives in the app's private storage. There is nothing else to clean up - no account to close, no cloud copy to delete.

## 14. Known limitations

These are stated here rather than discovered by a player later:

- **Not tested on a phone by its author.** The release candidate was built and verified in the Unity editor; no Android handset was available when it was frozen. Everything described above is what the build is designed and measured to do, and a device run is the outstanding check.
- **No music and no ambience** - five one-shot sounds only: catch, rescue, flag pick-up, victory, defeat.
- **One profile per install**, stored locally.
- **The AI is competent, not a person.** EASY is slower and more cautious; HARD notices you sooner, remembers longer and takes risks.
- **The instructions screen is recent.** Two of the five Week 14 playtesters needed a nudge to find the rescue and catch rules; the screen was added to answer that, and nobody has been re-tested on it yet.

---

*Agawan Base - Zedrich Zaivan B. Abrera - MOBILE DEV 002, Module 5, Week 17.*
