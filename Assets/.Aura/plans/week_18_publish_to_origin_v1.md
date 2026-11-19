# Week 18 - Publish the Repository to origin

## Objective
Push the ten unpushed commits (Weeks 16-18) and the three tags to `https://github.com/zedrichzaivan753-coder/AgawanBase.git` over Git LFS reusing the stored Windows credential, verify the remote matches, then correct the "local only" statements the PA2 documents currently make.

## Changes
- `README.md` - lines 11, 12, 279, 281: drop "not pushed" / "local only", name the pushed tag and the resolved remote head.
- `Docs/Zedrich Abrera/README.md` - lines 11, 141: repository is now the public origin at the pushed commit.
- `Docs/Zedrich Abrera/Week_18/Evidence/repo_state.txt` - add a `=== PUBLISHED TO ORIGIN ===` block after the existing "REMOTE: STILL UNTOUCHED" section; leave that section intact as the pre-push record rather than rewriting history.
- `Docs/Zedrich Abrera/Week_18/Evidence/final_pre_submission_checklist.md` - line 77 PASS row and lines 105-106: local-only caveat replaced with the verified remote read.
- `Docs/Zedrich Abrera/Week_18/Evidence/release_record.md` (line 125), `Evidence/defense_prep.md` (line 110) - same correction.
- `Docs/Zedrich Abrera/Week_18/others/Abrera_Zedrich_PA2.md` (27, 271-272), `_PA2_Technical_Report.md` (258), `_PA2_Evidence_Portfolio.md` (211), `_PA2_Originality_Declaration.md` (114) - corrected, then re-converted and re-printed to PDF.
- No change to `ProjectSettings.asset`, no APK rebuild, no new tag on the PA2 commit.

## Relevant Assets and Quirks
- **Git LFS is active and will carry this push.** `.gitattributes` marks `*.pdf`, `*.png`, `*.zip`, `*.apk`, `*.mp3`, `*.exe`, `*.dll` as `lfs`. `.git/config` has `filter.lfs.required = true` and an `lfs "https://github.com/zedrichzaivan753-coder/AgawanBase.git/info/lfs"` block with `access = basic`. `.git/lfs/objects/` is populated, so git-lfs is installed - do not reinstall it.
- **Quota is the real risk, not the commit count.** GitHub free LFS is 1 GB storage / 1 GB month bandwidth. Weeks 16-18 carry dozens of PDFs and evidence PNGs, all LFS. This must be measured *before* pushing; a quota failure aborts mid-push.
- `origin/main` = `d08fc9b`, last pushed **2026-09-21 21:31 +0800** (that is the "2 months ago" the user sees). Local `main` = `5e63b1c`, ahead by 10.
- The ten commits to publish: `55b043a`, `b4cff53` (tag `week16-final`), `ae4aaf2`, `aa1ec43`, `5f37bf4`, `13e6fc6`, `84c0f2e`, `07057bd` (tag `v1.0.0-rc1`), `983d758`, `5e63b1c` (tag `v1.0.0-pa2`).
- **Never amend or rebase `5e63b1c`.** `v1.0.0-pa2` points at it and `repo_state.txt` derives its evidence from it. Corrections go in a *new* commit on top.
- `.gitignore` keeps `*.apk`, `*.aab` and `/Docs/Zedrich Abrera/Week_18/*.zip` out. `git ls-files "*.apk" "*.aab" "*.zip"` must still return only `Docs/REFERENCE/CASTILLO_.../Week_14/Castillo_Janelle_Ann_PT6_Windows.zip`.
- **The repo also carries `Docs/REFERENCE/CASTILLO_Janelle_Ann_23-10260_SiliTubigClash/`** - another student's full Week 13-18 submission including APKs and PDFs. Confirm the repository's visibility before pushing; if it is public, this publishes third-party coursework.
- `execute_command` dies at 120 s and has no interactive prompt. The credential must already be cached in Windows Credential Manager, and a push that exceeds the cap is resumable by re-running the same `git push`.
- PDF re-export: every headless Edge print needs its own `--user-data-dir` plus a sleep, or only the last document is written. Converter is `Docs/tools/MdToHtml.cs`, `Weeks` already includes `"Week_18"`; output `.html` lands in the week folder then moves to `others/`.

## Steps
- [x] 1. Pre-flight audit with `execute_command` (all read-only): `git status --porcelain` (expect empty), `git log --oneline origin/main..main` (expect exactly the ten commits above), `git rev-list --left-right --count origin/main...main` (expect `0 10`), `git ls-files "*.apk" "*.aab" "*.zip"`, `git lfs ls-files --size` summed, and a scan of all reachable blobs for anything over 100 MB. Also record the repository's visibility from GitHub so the CASTILLO reference set is an informed inclusion, not an accident. [depends: none]
- [x] 2. Prove the credential before committing to a long push: `git ls-remote origin` must return `d08fc9b` for `refs/heads/main` without prompting. If it hangs or 401s, stop and have the token cached in Windows Credential Manager (`git credential-manager configure` / Control Panel > Credential Manager > Windows Credentials > `git:https://github.com`), then re-test. [depends: 1]
- [x] 3. Push the branch: `git push origin main`, with LFS uploading alongside. If the 120 s cap truncates it, re-run the same command - an interrupted LFS push resumes rather than restarting, and a later run of an already-received ref reports "Everything up-to-date". [depends: 2]
- [x] 4. Push the three tags: `git push origin v1.0.0-pa2 v1.0.0-rc1 week16-final` - annotated objects, so `git ls-remote --tags origin` must show the peeled `^{}` entries matching `5e63b1c`, `07057bd`, `b4cff53`. [depends: 3]
- [x] 5. Verify the remote, then correct the statements. Verification: `git ls-remote origin` head equals `5e63b1c4fc52f1d2a5115a04a02dda21c03e35a0`, `git rev-list --left-right --count origin/main...main` returns `0 0`, `git status --porcelain` stays empty. Only then edit the eleven statement sites listed under Changes, and in `repo_state.txt` append the published block instead of deleting the old one. [depends: 4]
- [x] 6. Re-convert and re-print the four corrected documents: run `MdToHtml` for `Abrera_Zedrich_PA2`, `_PA2_Technical_Report`, `_PA2_Evidence_Portfolio`, `_PA2_Originality_Declaration`, confirm each `.html` still contains its `<img>` count (9 / 1 / 4 / 0) and each PDF's page and `/Subtype /Image` counts are unchanged from 11/9, 7/1, 8/4, 3/0. [depends: 5]
- [x] 7. Commit the correction as a new commit on top of `v1.0.0-pa2` (never amending it), push it, and re-confirm `git rev-parse "v1.0.0-pa2^{}"` still equals `5e63b1c`. [depends: 6]

## Verify
- `git ls-remote origin` returns `5e63b1c4fc52f1d2a5115a04a02dda21c03e35a0` for `refs/heads/main`; `git ls-remote --tags origin` lists `v1.0.0-pa2`, `v1.0.0-rc1` and `week16-final` with peeled ids `5e63b1c`, `07057bd`, `b4cff53`.
- `git rev-list --left-right --count origin/main...main` returns `0	0`, and a fresh `git fetch` changes nothing.
- The GitHub commit page for `5e63b1c` shows the PA2 tag and the ten-commit history, and no `*.apk`, `*.aab` or Week 18 zip appears in the tree.
- The LFS objects uploaded without a quota error, and `git lfs ls-files` on a fresh clone of the remote resolves the PDFs and PNGs to real bytes rather than pointer files.
- Both READMEs and the five PA2 documents no longer claim the repository is local-only, and their PDF page/figure counts are unchanged from the pre-push builds.
- `v1.0.0-pa2` still resolves to `5e63b1c` after the correction commit, and `git status` is clean at the end.

## Notes
- **Step 1 surfaced a decision the plan did not anticipate.** The repository is public, and at `d08fc9b`
  `Docs/` held only `Evidence/` and `Part_D_Test_Results.md`. Both `Docs/REFERENCE/CASTILLO_.../`
  (another student's full Week 13-18 submission) and the instructor's module `.docx` were introduced
  by the first unpushed commit, `55b043a`, so no commit on top could keep them off the remote. The
  user was shown the consequence and chose to push as-is to the public repository rather than make it
  private or rewrite the ten commits.
- **Push payload:** 487 changed files, 215 MB on disk - 118 LFS files (74.47 MB) and 140.43 MB of
  plain blobs, almost all Week 16 profiler CSVs. Largest blob in `HEAD` is 8.15 MB, so no GitHub
  file-size limit was near.
- **The LFS pre-push hook exceeds the 120 s command budget.** The branch push itself uploaded 128
  LFS objects / 80 MB and completed. Both the first tag push and the first attempt at the correction
  commit's push timed out in the hook. Fix: `git lfs push --all origin` (146 objects, 85 MB) followed
  by `git push --no-verify`. The final push is `5e63b1c..328853b`.
- **Converter is not compiled into the project.** `Docs/tools/MdToHtml.cs` sits outside `Assets/`, so
  it was copied to `Assets/Editor/MdToHtmlTool.cs`, invoked by reflection, and deleted again; compile
  is clean and the working tree holds no stray copy. The four sources were temporarily copied into
  `Week_18/` for conversion (paths resolve relative to the week folder) and moved back to `others/`.
- **Counts after re-export are identical to pre-push:** PA2 11 pp / 9 figures, Technical Report 7 / 1,
  Evidence Portfolio 8 / 4, Originality Declaration 3 / 0. User Guide and Reflection were not touched.
- The plan's Verify bullet expecting `origin/main` at `5e63b1c` now describes an intermediate state:
  `origin/main` is `328853b` and `v1.0.0-pa2` still peels to `5e63b1c`.
- **Verification came back `broken`, and it was right.** Bullet 1 failed because `origin/main` had
  advanced past `5e63b1c` with the correction commit. The agent also caught the real defect behind
  it: the notes above and eleven document sites named `origin/main` as `5e63b1c`, which was true only
  until the next commit landed - the same mistake `repo_state.txt` already knew to avoid. Every site
  now names the durable fact instead (`v1.0.0-pa2` peels to `5e63b1c`; all three tags resolve;
  `origin/main` carries the PA2 commit plus the documentation commits after it). The four documents
  were re-exported from their Markdown with page and figure counts unchanged, and `repo_state.txt`
  gained a section explaining why it pins no tip id and why the annotated tag message is left as a
  historical artefact rather than re-tagged. Bullet 1 as literally worded cannot hold without
  rewriting history, which would move the tag and destroy the evidence it exists to provide. Final
  state: `origin/main` = `aa687ed`, `v1.0.0-pa2` -> `5e63b1c`, working tree clean.
- **Afterwards the user asked for the reference set to go.** `Docs/REFERENCE/` - the other student's
  Week 13-18 submission that this note calls out above as published by explicit decision - was deleted
  from the tree and the index (62 tracked files), every document that named it was corrected, the
  three affected documents were re-exported and re-printed with unchanged page and figure counts, and
  the removal went in as an ordinary commit with no new tag. The commits before it still carry the
  files: deleting a path cleans the tip, not the history. `v1.0.0-pa2` was not moved and `5e63b1c`
  was not amended.

