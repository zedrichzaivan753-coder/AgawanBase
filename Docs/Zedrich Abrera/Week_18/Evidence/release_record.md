# Release Record — PA2 Submitted Binary

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Assessment:** Week 18 PA2 — Final Demonstration Package
**Recorded:** 2026-11-19

---

## 1. The submitted binary

| Field | Value |
|---|---|
| File | `Docs/Zedrich Abrera/Week_18/Abrera_Zedrich_PA2.apk` |
| Size on disk | **33,170,107 bytes** (31.63 MB) |
| MD5 | **`ef38de8dd65100109791002a67738da1`** |
| Where it came from | Byte copy of `Docs/Zedrich Abrera/Week_17/Abrera_Zedrich_PT7.apk` |
| Copy verified | Both source and copy hashed with `certutil -hashfile`; identical MD5 and identical size |

The copy command and both hashes:

```
copy /Y "Docs\Zedrich Abrera\Week_17\Abrera_Zedrich_PT7.apk" ^
        "Docs\Zedrich Abrera\Week_18\Abrera_Zedrich_PA2.apk"

Week_17\Abrera_Zedrich_PT7.apk   ef38de8dd65100109791002a67738da1   33170107
Week_18\Abrera_Zedrich_PA2.apk   ef38de8dd65100109791002a67738da1   33170107
```

**Nothing was rebuilt.** The file was renamed by copying it, not by compiling it again. If the
submitted file's hash does not match `ef38de8d…`, the wrong file was submitted.

---

## 2. Why it was not rebuilt for PA2

This is the single most important decision in the Week 18 submission, so it is argued rather than
asserted.

The obvious move at the end of a module is to rebuild with the version bumped from `1.0.0-rc1` to
`1.0.0`, so the app finally looks "released". That was rejected:

1. **The module's central PA2 warning is demonstrating a different build from the one submitted.**
   The Week 17 release candidate has been verified against, screenshotted, put through a 516-check
   accessibility audit, a 16-scenario balance run and a profiling pass. Those results belong to the
   bytes with MD5 `ef38de8d…`. A rebuild — however small the source diff — produces different bytes
   and inherits none of that evidence.
2. **Bumping the version would change the app's identity, not just its label.** Week 17 already
   found this the hard way: `DEF-17-02` recorded that `productName` fed both the `PlayerPrefs`
   namespace and `persistentDataPath`, so embedding the version there silently orphaned every saved
   setting and the player record. Version strings in this project are not cosmetic.
3. **A `1.0.0` build would be unverified in exactly the way the module penalises.** The honest
   alternative is not to relabel the binary but to label the *commit*: the tag `v1.0.0-pa2` marks
   the PA2 submission commit, while the binary keeps reporting the version string it was built with
   and every artefact and test names that same hash.

The consequence is disclosed prominently — the app says `1.0.0-rc1` on its title screen, and the
PA2 documents say why.

---

## 3. Build provenance

| Field | Value |
|---|---|
| Built | 2026-11-19, 15:20 local (build started 15:20:27, took 24.8 s) |
| Engine | Unity **6000.6.0f1**, URP 17.6.0 |
| Platform | Android, **release** build — Development Build **off**, Build App Bundle **off** |
| Scripting backend | IL2CPP |
| Architecture | ARM64 only |
| Minimum Android | API 26 (Android 8.0) |
| Managed stripping | Minimal |
| Orientation | Landscape left, locked |
| `bundleVersion` | `1.0.0-rc1` |
| `AndroidBundleVersionCode` | **2** |
| `productName` | `AgawanBase` |
| Application id | `com.zedrich.agawanbase` |
| Scenes | `Game` (index 0), `Map_Palengke`, `Map_SchoolCampus` |
| Source commit | **`84c0f2e`** — the tree the APK was compiled from |

Full build detail is in `Week_17/Evidence/build_and_commit.txt`.

### Package sanity

The archive holds **419 entries**, with `classes.dex` and `AndroidManifest.xml` at the **root** of
the archive — no `BundleConfig.pb`, no `base/dex/` prefix. This is the `DEF-15-04` guard: a Week 14
file was an Android App Bundle wearing an `.apk` filename, which no handset will install. App Bundle
is off in the project and the entry listing is the proof for this build.

---

## 4. Commit and tag identity

Two different ids are in play, and conflating them is a common mistake:

| Id | What it is |
|---|---|
| `84c0f2e` | The **source commit** the APK was compiled from |
| `07057bd` | The commit **tagged** `v1.0.0-rc1` — the commit that ships the APK together with the documentation written against its hash |
| `983d758` | Repository **HEAD** at the start of Week 18 |
| `v1.0.0-pa2` → PA2 commit | The **final tag**, created locally in step 6 of the Week 18 plan |

Neither a commit id nor a tag identifies the binary on its own — **the MD5 does**. A commit is a
tree of source; the APK is a compiled artefact. `ef38de8d…` is the identifier every test record,
screenshot and audit result refers to.

### Superseded builds — do not quote these

Three builds were produced on 2026-11-19. Only the third is the release candidate:

| # | Time | Size | MD5 | Why superseded |
|---|---|---|---|---|
| 1 | 14:17 | 33,170,243 B | `bb36569a7a1387997c556a56aeaf0344` | `productName` carried the version; credits printed `0.2` (`DEF-17-02`, `DEF-17-01`) |
| 2 | 14:51 | 33,170,059 B | `4b1152fe203ba6f679404d955fdaa59d` | how-to text overflowed its box behind the BACK button (`DEF-17-07`) |
| 3 | **15:20** | **33,170,107 B** | **`ef38de8dd65100109791002a67738da1`** | **current — this is PA2** |

The first two are recorded rather than erased because each was a real state of the release
candidate that some artefact was produced against. A reader who encounters one of those hashes
elsewhere should know what it was, and that it is not the submission.

---

## 5. Repository visibility

The repository was **local only** when this record was written. Both tags and every commit from
Weeks 15–18 existed solely on this machine. That changed on 2026-11-19: the commits and all three
tags were published to `origin`, and the table below now carries both the pre-push state and the
published state.

| Field | Value |
|---|---|
| Remote | `https://github.com/zedrichzaivan753-coder/AgawanBase.git` |
| Branch | `main` |
| Branch state at capture | `main...origin/main [ahead 9]` |
| `origin/main` | `d08fc9b2c9e28876f337cfcad009b88802d2b534` — predates Weeks 15–17 |
| **Published** | 2026-11-19 (GitHub reports `pushed_at` `2026-11-19T08:52:15Z`) |
| `origin/main` after publish | `5e63b1c4fc52f1d2a5115a04a02dda21c03e35a0` |
| Tag `v1.0.0-pa2` remotely | `5e63b1c` |
| Tag `v1.0.0-rc1` remotely | `07057bd` |
| Tag `week16-final` remotely | `b4cff53` |
| Branch state after publish | `git rev-list --left-right --count origin/main...main` = `0 / 0` |
| LFS | 128 objects, 80 MB uploaded, no quota error |

The module asks for repository access so the instructor can verify the final tag and commit. **That
can now be done from the remote**: `origin/main` is `5e63b1c` and each tag resolves to the commit it
names. The local evidence is retained as well — `repo_state.txt` (the raw `git` output at capture,
followed by the publication block) and this record — so the pre-push state is documented rather than
quietly overwritten.

---

## 6. What can be concluded from this record

- The submitted APK is the Week 17 release candidate, unmodified, byte for byte.
- Every verification result from Weeks 15–17 therefore applies to the submitted binary — it is not
  a new, untested build.
- The binary reports `1.0.0-rc1`; the final tag is `v1.0.0-pa2` on the submission commit. These are
  different kinds of label and both are correct.
- No APK is versioned in git. `git ls-files "*.apk" "*.aab" "*.zip"` returns exactly one path, and
  it is instructor reference material under `Docs/REFERENCE/`, not this student's build.

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
