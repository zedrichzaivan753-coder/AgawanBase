# Week 18 - PA2 Final Demonstration Package and Individual Defense

## Objective
Produce the complete Week 18 PA2 submission - the same approved `1.0.0-rc1` binary re-identified as `Abrera_Zedrich_PA2.apk`, its technical report, user guide, evidence portfolio, originality declaration, presentation deck and Week 18 reflection PDF - plus a rehearsed demonstration, the final tagged commit, and an honest record of the two items this machine cannot produce (handset demo, supervised E3).

## Changes
- `Assets/Editor/Week18Shots.cs` - (create) demonstration harness cloned from `Week17Shots` (repaint-forcing + MD5 dedupe); writes `D1`-`D6` PNGs and `demo_rehearsal.txt` into `Week_18/Evidence/`, never overwriting the PT7 captures.
- `Assets/Editor/Week18Build.cs` - (create) `BuildWindowsBackup()`: switch to `StandaloneWindows64` (Mono), build to `Builds/PA2_Windows/`, zip into `Week_18/`, switch back to `Android`, log the seven build facts.
- `Docs/tools/MdToHtml.cs` - add `"Week_18"` to `Weeks` and a six-document row.
- `.gitignore` - add `*.zip` beside `*.apk` (backup package stays on disk, hash-identified, unversioned).
- `Docs/Zedrich Abrera/Week_18/` - (create) `Abrera_Zedrich_PA2.apk`, `Abrera_Zedrich_PA2_Windows.zip`, 6 PDFs, `Abrera_Zedrich_PA2_Presentation.pptx`, `Evidence/`, `others/`.
- `Docs/Zedrich Abrera/Week_18/Evidence/` - (create) `release_record.md`, `final_pre_submission_checklist.md`, `defense_prep.md`, `e3_record.md`, `windows_backup_build.md`, `repo_state.txt`, `demo_rehearsal.txt`, `D1`-`D6`, `architecture.svg`.
- `README.md` + `Docs/Zedrich Abrera/README.md` - repository URL, final tag/commit, PA2 rows, install/controls, limitations, local-only statement.

## Relevant Assets and Quirks
- Module source: `Docs/MAD2-Laboratory-Module-Week-13-18-...docx`, Week 18 Laboratory + Detailed Student Guide. Reference set to mirror exactly: `Docs/REFERENCE/CASTILLO_Janelle_Ann_23-10260_SiliTubigClash/Week_18/` - `PA2.apk`, `PA2.pdf`, `PA2_Technical_Report.pdf`, `PA2_User_Guide.pdf`, `PA2_Evidence_Portfolio.pdf`, `PA2_Originality_Declaration.pdf`, `PA2_Presentation.pptx`, `Week18_Reflection.pdf`. Author is **Zedrich Zaivan B. Abrera**, never Castillo.
- **Do not rebuild the APK.** Verified today: `Week_17/Abrera_Zedrich_PT7.apk` = **33,170,107 B, md5 `ef38de8dd65100109791002a67738da1`**, built from `84c0f2e`, `v1.0.0-rc1` tag at `07057bd`. PA2 ships that byte copy; a differing md5 means the wrong file was copied.
- Binary self-identifies as `1.0.0-rc1`; final tag is `v1.0.0-pa2` on the PA2 commit. Rebuilding to print "1.0.0" would ship an undemonstrated build, so the decision is documented instead.
- Nothing is pushed (user decision): `origin` = `https://github.com/zedrichzaivan753-coder/AgawanBase.git`, `origin/main` still `d08fc9b`; both tags are local. PA2 must say the instructor cannot verify the commit from the remote; `repo_state.txt` proves the state.
- No handset (`adb devices` empty). Windows backup justified as the module's "backup installation package"; it runs Mono and `GameFeedback.cs:174` guards haptics behind `#if UNITY_ANDROID || UNITY_IOS`, so no catch buzz - disclose both.
- E3 is sat under supervision and reading generated answers is a listed mistake: `e3_record.md` is a record with fields; `defense_prep.md` gives questions with exact file/commit/test/number pointers, never scripted answers.
- Numbers to cite, re-verified on this tree in Week 17: EditMode **17/17**, accessibility **516 passed / 0 failed / 24 not measurable**, quality scenarios **16/0/0**.
- Carried gaps stay visible in PA2: the two Week 14 S2 discoverability findings were never re-tested against the new instructions screen; AI balance has only the 16 regression scenarios.
- Images must be **embedded** with captions (Week 13 complaint). Tooling quirks: `execute_script` is broken here - drive editor hooks via `execute_code` reflection; `MdToHtml`'s internal `Convert` shadows `System.Convert`; `.md`/`.html` sit in the week folder during conversion then move to `others/`; Edge needs an absolute `--print-to-pdf` path with `%20` URLs. `.pptx`: `python-pptx` is absent - `pip install python-pptx`, else PowerPoint COM at `C:\Program Files (x86)\Microsoft Office\root\Office16\POWERPNT.EXE` (confirmed present).

## Steps
- [x] 1. Baseline and rehearsal: record `git rev-parse HEAD` / `git tag -l` / `git status` into `Week_18/Evidence/repo_state.txt`, run `run_tests` EditMode (expect 17/17) and `check_compile_errors`, then create `Assets/Editor/Week18Shots.cs` and run it via `execute_code` to write `D1`-`D6` (all hashes distinct) and `demo_rehearsal.txt` with per-step timings for title → how-to → credits → map select → match → capture → end → restart. [depends: none]
- [x] 2. Backup package: create `Assets/Editor/Week18Build.cs` and invoke `BuildWindowsBackup()` via `execute_code` - build `StandaloneWindows64` (Mono) to `Builds/PA2_Windows/`, zip to `Week_18/Abrera_Zedrich_PA2_Windows.zip`, switch back to `Android`, log and verify `activeTarget=Android`, App Bundle off, Development off, IL2CPP, ARM64, API 26, versionCode 2, `bundleVersion=1.0.0-rc1`; write `Evidence/windows_backup_build.md`; add `*.zip` to `.gitignore`. [depends: 1]
- [x] 3. Binary identity and integrity evidence: copy the PT7 APK to `Week_18/Abrera_Zedrich_PA2.apk` and confirm md5/size with `execute_command` + `certutil`; write `Evidence/release_record.md`, `final_pre_submission_checklist.md` (every PA2/E3 requirement marked PASS/BLOCKED/PENDING with its proof), `defense_prep.md`, `e3_record.md`. [depends: 2]
- [x] 4. Write the six documents in `Week_18/others/` with embedded captioned screenshots: `Abrera_Zedrich_PA2.md`, `_PA2_Technical_Report.md` (consolidated from Weeks 15-17, no new claims), `_PA2_User_Guide.md`, `_PA2_Evidence_Portfolio.md` (every artefact Weeks 13-18 plus the unproven list), `_PA2_Originality_Declaration.md` (authorship, third-party and AI disclosure, signature line), `_Week18_Reflection.md` (four required prompts, one captioned figure, exact closing declaration, 1-2 pages). [depends: 3]
- [x] 5. Register `Week_18` in `Docs/tools/MdToHtml.cs`, run the conversion cycle, headless-print the six PDFs and confirm page/figure counts; then build `Abrera_Zedrich_PA2_Presentation.pptx` (twelve slides, title slide carrying `1.0.0-rc1` + source commit + final tag, `D1`-`D6` and the architecture diagram embedded) via `pip install python-pptx` or PowerPoint COM, and verify slide and media counts. [depends: 4]
- [x] 6. Commit locally, create annotated tag `v1.0.0-pa2`, update both READMEs and refresh `repo_state.txt`; no push; then confirm with `unity_verification_agent` that the shipped build still reports `1.0.0-rc1 - built 2026-11-19` and the loop title → how-to → match → end → restart works. [depends: 5]

## Verify
- `check_compile_errors` clean after each new editor script; `run_tests` EditMode still **17/17**.
- `certutil -hashfile "Week_18/Abrera_Zedrich_PA2.apk" MD5` equals `ef38de8dd65100109791002a67738da1` at 33,170,107 B.
- The seven Android build facts read back correct after the Windows detour and `git status` shows `ProjectSettings.asset` unchanged.
- `D1`-`D6` all have distinct MD5s, none matching a PT7 hash; `demo_rehearsal.txt` matches the ordered steps the deck and PA2 claim, with real timings.
- Each `.html` contains `<img` and each PDF's page and `/Subtype /Image` counts match what `PA2.md` states.
- The `.pptx` opens with twelve slides and embedded media; `git tag -l` shows `week16-final`, `v1.0.0-rc1`, `v1.0.0-pa2`; `origin/main` untouched.
