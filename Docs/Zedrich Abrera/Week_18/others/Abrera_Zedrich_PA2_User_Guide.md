# PA2 — User Guide

**Game:** Agawan Base 3D
**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Week:** 18 · **Assessment:** PA2
**Version:** `1.0.0-rc1` · built 2026-11-19

---

## 1. What this game is

Agawan Base 3D is a three-dimensional, top-down mobile game built from the Filipino street game
*Agawang Base*. Two teams defend a base each. You tag opponents to catch them, free your own
caught teammates, and steal the other team's flag and carry it home. A match lasts at most three
minutes, which makes it a game you can finish in a class break.

It is played **in landscape**, held in two hands, with both thumbs on the screen.

---

## 2. Installing the game

### On an Android phone

| Requirement | Value |
|---|---|
| Android version | **8.0 (API 26)** or newer |
| Architecture | **ARM64** — every phone from roughly 2017 onward |
| File | `Abrera_Zedrich_PA2.apk` |
| Size | 31.63 MB (33,170,107 bytes) |

1. Copy `Abrera_Zedrich_PA2.apk` to the phone, by cable, cloud storage or messaging app.
2. Open the file from the phone's file manager or the notification that appears.
3. Android will warn that the app comes from an unknown source, because it is not distributed
   through the Play Store. Allow installation for the app that is opening the file — usually
   **Files** or **Chrome** — then tap **Install** and confirm.
4. Open **Agawan Base** from the app drawer. Hold the phone sideways.

The app installs as `com.zedrich.agawanbase`. Because it is not from the Play Store, Android shows a
generic icon in some launchers; that is expected.

### Checking you have the right build

The title screen prints its own identity in the top-left corner. It must read:

```
1.0.0-rc1  -  built 2026-11-19
```

If it reads anything else, you have a different file. The submitted build's MD5 is
`ef38de8dd65100109791002a67738da1`.

### If you have no Android phone

`Abrera_Zedrich_PA2_Windows.zip` contains a Windows desktop build you can unzip and run. It is a
**backup**, not the submitted game, and it differs: no vibration, and two post-processing effects
are missing. It also shares its save folder with the Unity editor, so it may reject a profile the
editor wrote. See `Evidence/windows_backup_build.md`.

---

## 3. Controls

| Control | What it does |
|---|---|
| **Left thumb stick** | Move. The stick appears where your thumb lands, so you do not have to find it |
| **Right-hand button** | Sprint, draining the stamina bar and recovering when released |
| **On-screen buttons** | Everything in the menus — large targets, at least 48 dp tall |

There are no touch targets smaller than a fingertip. Getting close to an opponent shows a ring
indicator before contact, so you can judge a tag without guessing.

![Figure 1. The title screen. The version caption in the top-left identifies the build you are running; the four large buttons are the whole of the main menu.](Evidence/D1_title.png)

---

## 4. Getting into a match

From the title screen:

1. **START** — goes to map selection.
2. Pick a map: **Classic Open Field**, **Palengke**, or **School Campus**. Each has different
   cover, which changes how much hiding matters.
3. Set **team size** (1v1 up to 4v4) and **difficulty** (Easy, Normal, Hard).
4. Press **START** to begin the match.

![Figure 2. Map selection. Three maps with different cover; the blurb under each explains what the map rewards.](Evidence/D4_map_select.png)

![Figure 3. Match setup. Team size and difficulty. Difficulty is applied to both teams equally — Easy is an easier match, not a rigged one.](Evidence/D5_match_setup.png)

---

## 5. How to play

### The goal

Win by **capturing the red flag** and carrying it back to your base, or by **eliminating the
opposing team** — catching every opponent so none are left free.

### Tagging and catching

Touch a free opponent to tag them. A tagged player is taken to their opponent's base and held.

### Freeing your teammates

Walk up to a teammate held at the enemy base to free them. A freed player:

- is **immune for 2 seconds**, so they can get clear instead of being tagged again instantly;
- **cannot be freed again for 3 seconds**, so rescuers cannot bounce the same prisoner forever.

### The flag

Stand near the enemy flag to pick it up; carry it to your own base to win. If the carrier is tagged,
the flag drops where they fell and **returns itself after 10 seconds** if nobody picks it up, so a
match cannot stall over an unreachable flag.

### Hiding

Bushes hide you from the other team. The game remains fair about it: your own overhead bars and body
disappear too, but a marker stays on your screen so you are never lost inside your own hiding place.

### The clock

A match ends after **180 seconds** at most, with the win awarded on the state at that moment. Three
minutes keeps a match inside a class break.

### The overhead bars

- **Freshness** — how close you are to being catchable. It recovers over time.
- **Stamina** — spent by sprinting, recovered by not sprinting.

---

## 6. Screens

| Screen | What it is for |
|---|---|
| **Title** | Start, profile, instructions, credits, exit |
| **How to play** | The rules, written out, in the same terms the game uses |
| **Credits** | Who made what, licences, and the AI-disclosure statement |
| **Map select** | Three maps |
| **Match setup** | Team size and difficulty |
| **Match** | The game, with the HUD, stick, sprint and off-screen markers |
| **Pause** | Resume, restart, tag hints, sound, back to title |
| **End** | The result, with restart or return |

![Figure 4. The core loop: top-down chase, the on-screen stick, overhead freshness and stamina bars, and the base ring.](Evidence/D6_match_running.png)

![Figure 5. The pause screen during a match. SOUND and TAG HINTS both remember their state between matches.](Evidence/D7_pause_settings.png)

![Figure 6. The end screen. From here, restart goes straight into a fresh match on the same map and settings.](Evidence/D8_end_screen.png)

---

## 7. Settings and what they do

| Setting | Options | Notes |
|---|---|---|
| **Team size** | 1v1 to 4v4 | Applies to both teams |
| **Difficulty** | Easy, Normal, Hard | Applies to both teams equally |
| **Tag hints** | On / Off | The ring indicator that warns you before contact. Persists |
| **Sound** | On / Off | Five one-shot sounds; the choice persists |

Settings and your profile survive closing the app. Your profile is stored in a signed file, and if
that file is edited by hand the game will refuse it, keep the original as `profile.rejected.json`,
and start a fresh profile rather than load corrupted data.

---

## 8. Troubleshooting

| Problem | Cause and fix |
|---|---|
| "App not installed" during install | An older copy is installed. Uninstall the existing *Agawan Base*, then install again |
| Install blocked | Allow unknown sources for the app you are installing from, not for the game |
| Screen stays portrait | The game locks landscape; if the phone is rotation-locked, unlock it in quick settings |
| Title shows a different version than `1.0.0-rc1` | You have the wrong APK. Re-copy the submitted file and check its MD5 |
| Profile reset unexpectedly | The profile file failed its integrity check and was set aside as `profile.rejected.json`. This is intended behaviour when the file is modified |
| Controls feel too big or too small | The interface scales with screen size and respects notches; if controls sit under a notch, report the device |
| No vibration on a catch | Sound is always present; vibration requires a device with a haptic motor. The Windows backup never vibrates |

---

## 9. Uninstalling and saved data

Uninstalling removes the app and its saved profile and settings. Nothing is stored anywhere else —
the game has no account and no server, so there is no cloud copy to lose.

---

## 10. Credits and licences

The **CREDITS** screen in the game lists authorship, third-party assets and licences, and the
required AI-disclosure statement. It is the authoritative list for what is in the build; this guide
does not duplicate it.

![Figure 7. The credits screen, which carries the licence and AI-disclosure block required by the module.](Evidence/D3_credits.png)

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
