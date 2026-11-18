# Sili-Tubig Clash

| | |
|---|---|
| Student | Janelle Ann F. Castillo, BSIT 4A |
| Course | MOBILE DEV 002 Advanced Mobile Application Development |
| Theme | Philippine Games and Sports |
| Platform | Android (primary), Windows |
| Engine | Godot 4.7.2, GDScript, GL Compatibility renderer |
| Repository | https://github.com/nncast/godot-sili-tubig-clash (private) |
| Tag | v0.9.1 |
| Current commit ID | 40c57e03bacdce67c6ce175898f7d72c3dabb3dc |
| Last updated | November 7, 2026 |

## The game

A local-network party game based on the Filipino playground game Sili-Sili
Maanghang, Tubig-Tubig Malamig. One Sili hunts four Tubig. A tag burns a Tubig
in place, and a teammate must reach them and hold to rescue before the burn runs
out. A series is five rounds, and everyone is the Sili once. It can also be played
alone against bots.

The game began as the F.I.R.E.S DEV team entry for CODE & CONQUER Cagayan Valley
(RSTW 2026). Contributors and AI assistance are disclosed in PT5 and in the
technical documentation.

## Installation

**Android:** copy any of the weekly `.apk` files to the phone (they are the same
build), allow installing from this source when asked, and open the file. Uninstall
any older version first. The build is debug-signed and needs a 64-bit ARM phone.

**Windows:** unzip `Week_14/Castillo_Janelle_Ann_PT6_Windows.zip`, keep the two
files together, and run `SiliTubigClash.exe`.

**From source:** clone the repository, check out tag `v0.9.1`, and open
`project.godot` in Godot 4.7.

To play together, all devices must be on the same network. One device chooses
Play, then Host. Others choose Join and type the 4-digit lobby code, or the host's
IP address if the code is not found. To play alone, choose Play vs Bots.

## Controls

| Action | Touch | Keyboard |
|---|---|---|
| Move | Stick, bottom left | WASD or arrow keys |
| Sprint (uses stamina) | RUN | Shift |
| Tag, rescue (hold), drink, tunnel | Round button | E or Space |
| Controls guide | ? button | F1 or H |
| Settings and volume | Gear button | Gear button |

## Known limitations

- Phone performance has not been measured. The student reports that the game works on
  a physical phone and that other people have tested it.
- Rounds cannot be paused, because a shared real-time round cannot stop for one
  player. Settings opens over the running round.
- LAN only. Lobby codes can fail on hotspots and some routers; Join by IP is the
  fallback.
- The APK is debug-signed and arm64 only.
- Three Filipino voice callouts are not recorded, so those cues are silent.
- A bot Sili overwhelms bot Tubig.
- One map, and the cultural art pass is not done.
- The in-match settings and guide buttons are small.
- Web export cannot host or join. iOS is untested on a device. macOS and iOS were
  last built at v0.8.0.
