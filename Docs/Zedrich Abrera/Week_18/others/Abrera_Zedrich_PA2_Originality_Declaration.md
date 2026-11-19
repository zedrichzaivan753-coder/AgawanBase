# PA2 — Originality and Authorship Declaration

**Student:** Zedrich Zaivan B. Abrera
**Course:** MOBILE DEV 002 — Advanced Mobile Application Development, Module 5
**Week:** 18 · **Assessment:** PA2
**Game:** Agawan Base 3D
**Submitted build:** `Abrera_Zedrich_PA2.apk` · MD5 `ef38de8dd65100109791002a67738da1` · version `1.0.0-rc1`

---

## 1. Declaration of authorship

I declare that **Agawan Base 3D** is my own work, produced for this module. I designed the game, I
wrote and reviewed the code, I built the scenes and the art direction, and I wrote the documentation.
Where that work was assisted — by tools, by third-party software, or by AI — the assistance is
declared in §3 and §4 of this document, and it is also disclosed on the in-game CREDITS screen.

Specifically, I declare that:

1. The game design, the rules, and the decisions about scope are mine. The rules constants in
   `MatchManager.cs` and `Flag.cs` are values I chose and tuned, not defaults I accepted.
2. The source code under `Assets/Scripts/` and `Assets/Editor/` was written by me, with AI assistance
   as declared in §4. I am able to explain every file I submitted; `Evidence/defense_prep.md` is an
   index of where each answer lives in the repository, not a substitute for explaining it.
3. The commits and tags cited throughout this submission were produced by me on this machine, in this
   repository. `Evidence/repo_state.txt` records the state of that repository.
4. The evidence in `Evidence/` and in the earlier weeks' evidence folders was produced by running
   this project, not assembled to look like results. Where a result is weaker than it appears, the
   document says so — for example the Week 14 catch screenshots, which were scripted rather than
   played.

---

## 2. What the game is adapted from

*Agawang Base* is a **traditional Filipino playground game**. Its rules and its name belong to that
tradition and to no individual. This adaptation claims **no rights over the game itself** — the
originality claimed here is in the digital implementation: the three-dimensional presentation, the
code, the AI, the interface and the documentation.

This is why the theme is described as *Philippine Games and Sports* and not as an original property.

---

## 3. Third-party components and licences

The CREDITS screen in the shipped build is the authoritative list, because it travels with the
binary. It reads:

- **Built with Unity 6000.6.0f1 and the Universal Render Pipeline 17.6.0**, used under the **Unity
  Personal licence**. Unity and the Unity logo are trademarks of Unity Technologies.
- **Interface text uses Unity's built-in uGUI legacy font**, redistributed with the editor under the
  **Unity Companion License**.
- **All geometry, textures, sounds and interface art in this build were produced for this project.
  No third-party art, music or sound effects are included.**

Beyond those, the project uses the following packages from the Unity registry, each under its own
Unity licence: Universal RP 17.6.0, Input System 1.20.0, and the Test Framework used by
`Assets/Tests/EditMode/`.

No asset pack, marketplace content, scraped texture, stock music or recorded sound effect from a
third party is present in the build.

---

## 4. AI assistance disclosure

AI assistance **was** used on this project. It is declared here in the detail the module's responsible
AI use requirement asks for, rather than in a single vague sentence.

### 4.1 Where AI assistance was used

| Area | Nature of the assistance |
|---|---|
| **Documentation drafting** | Drafting and structuring of the weekly Markdown documents. Every factual claim, number and file reference was checked against the actual project before submission, and the numbers are command outputs rather than model recollections |
| **Code drafting** | Drafting and revision of C# across the module — gameplay, UI, and the editor tooling that produces evidence (`Week18Shots.cs`, `Week18Build.cs`, the Week 17 screenshot harness) |
| **Art and audio generation** | **Part of** the visual and audio assets were generated with AI tooling and then imported into this project |
| **Code review and diagnosis** | Reading compiler errors, logs and profiler output to locate causes |

### 4.2 Where it was not used, and what remains mine

- **The examination is mine.** E3 is sat under supervision and `Evidence/e3_record.md` is deliberately
  blank until then. `Evidence/defense_prep.md` contains pointers to files and commits and **no drafted
  answers**, because the module treats reading a generated answer during a defense as a violation, and
  I agree with it.
- **The design decisions are mine.** Scope, the rules, what was cut for time, and the arguments in
  `release_record.md` §2 about not rebuilding the binary are positions I took and can defend.
- **The verification is real.** Tests were run, profiling was measured, screenshots were captured from
  a running build. AI did not produce the numbers; it helped write the code that produced them.
- **Every file I submit, I can explain.** Where I cannot yet explain something, it is on the unproven
  list in `Abrera_Zedrich_PA2_Evidence_Portfolio.pdf` §9 rather than presented as finished.

### 4.3 What this means honestly

A reader should understand that AI materially assisted this project, and that the module permits
assisted work when it is disclosed. The disclosure is not a legal formality: it is on the credits
screen of the submitted build, in the project README, and here. What the assistance does **not**
excuse is being unable to explain the code — which is precisely what the individual defense exists to
test, and why no answers are pre-written for it.

---

## 5. Authorship trail

The work is traceable to commits in this repository, each with a message describing what changed:

| Commit | What it did |
|---|---|
| `84c0f2e` | The source commit the submitted APK was built from |
| `07057bd` | Tagged `v1.0.0-rc1` — the commit that ships the binary with its documentation |
| `983d758` | Repository HEAD at the start of Week 18 |
| `v1.0.0-pa2` | Final annotated tag on the PA2 submission commit |

The repository was **local only** while this declaration was written; `origin/main` was at
`d08fc9b` and predated Weeks 15–18. It was published on 2026-11-19, so this trail is now verifiable
remotely: `v1.0.0-pa2` peels to `5e63b1c`, and `v1.0.0-rc1` and `week16-final` resolve on the remote
too. `Evidence/repo_state.txt` keeps the pre-push state as a record.

---

## 6. Statement on the evidence

I declare that the evidence submitted with this assessment was produced from this project as it was
built, and that where a claim cannot be supported — a handset installation, the supervised
examination, two un-retested usability findings — it is marked as unproven rather than dressed up.
The unproven list in the evidence portfolio is part of the submission, not an omission from it.

---

## 7. Signature

I confirm that this declaration accurately describes the authorship of the submitted game and the
assistance I received, and that all assistance, including AI assistance, has been disclosed in §4.

| | |
|---|---|
| **Name** | Zedrich Zaivan B. Abrera |
| **Student / course** | MOBILE DEV 002 — Advanced Mobile Application Development, Module 5 |
| **Assessment** | PA2 — Complete Thematic Mobile-Game Demonstration and Defense |
| **Signature** | `_________________________________` |
| **Date** | `_________________________________` |

*AgawanBase · Zedrich Zaivan B. Abrera · MOBILE DEV 002, Module 5, Week 18*
