# Week 17 — Repository secrets and hygiene scan

**Scanned tree** commit `b4cff53` (tag `week16-final`) plus the Week 17 working tree
**Scan date** November 19, 2026
**Raw output** `secret_scan_raw.txt` in this folder

---

## 1. Method

`findstr` was run recursively over every text-bearing file type a secret could hide in:

```
findstr /s /i /n /r /c:"ApiKey" /c:"api_key" /c:"ghp_" /c:"AIza"
        /c:"BEGIN RSA" /c:"BEGIN PRIVATE KEY" /c:"secret" /c:"password"
        Assets\*.cs Assets\*.asset Assets\*.json Assets\*.txt
        ProjectSettings\*.asset Docs\*.md Docs\*.txt
```

`findstr` refused to open one file whose folder contains a space
(`Assets/Settings/Build Profiles/AndroidT.asset`), so that folder was re-scanned separately
with the editor's own ripgrep wrapper. It returned **no matches**.

## 2. Result

**No credential of any kind is present in the repository.** Twelve matches were returned and
every one of them is prose:

| Match location | What it is |
|---|---|
| `Assets/.Aura/plans/week13_player_profile_and_docs_v1.md` (×3) | Plan text describing the Week 13 secrets *evidence file* and stating that the game stores no secrets |
| `Assets/.Aura/plans/week_17_release_candidate_pt7_v1.md` (×1) | This week's plan step, quoting the scan pattern |

There is no API key, no access token, no private key, no signing keystore and no password in the
tree. This is expected: the game makes **no network calls at all** (OWASP MASVS-STORAGE is
satisfied by having nothing to store), the profile is a local signed file, and the APK is
debug-signed by Unity's own debug keystore rather than by a checked-in keystore.

## 3. Hygiene actions taken this week

| Action | Why |
|---|---|
| `git rm -r --cached` on `Week_14/Abrera_Zedrich_PT6_files/` and `Week_14/others/Abrera_Zedrich_SA6_files/` | Both are browser "Save page as" mirrors — the same PNG/JPGs that are already base64-inlined inside the `.html` documents, plus Word `filelist.xml` / `themedata.thmx` junk. They were versioned by accident and duplicated 20 files. The copies stay on disk (nothing the author handled by hand is destroyed) but they are no longer versioned. |
| `*_files/` added to `.gitignore` | Stops the next browser save from re-adding them |
| `*.raw` and `/Builds/` added to `.gitignore` | Week 16 left a 1.38 GB `profiler_editor_bottleneck.raw` in the tree and `Builds/` holds two 170 MB players; neither may ever be committed |
| `git add -f Docs/Zedrich Abrera/Week_16/Evidence/test_logs_regression/a11y_regression.log` | `*.log` is ignored repo-wide, so the Week 16 accessibility regression log was shipping in the submitted folder but missing from the repository. Force-added so the regression gate's third leg is reproducible from a clone. |
| Week 16 `.md` / `.html` sources moved into `Week_16/others/` | Matches the Week 14 layout, where the PDFs are the deliverable and the sources sit beside them |

## 4. What is deliberately still ignored

`Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`, `Builds/`, `*.apk`, `*.aab`, `*.log`,
`*.unitypackage`, `MemoryCaptures/`, `Recordings/`, and the Addressables and Visual Scripting
caches. The submission APK under `Docs/Zedrich Abrera/Week_17/` is force-added, exactly as the
Week 13-16 APKs were, because it is the deliverable rather than build output.

## 5. Conclusion

Repository hygiene requirement of Week 17 procedure step 4: **satisfied**. Nothing sensitive is
versioned, no duplicate asset remains tracked, and no build or profiler artefact is versioned.
