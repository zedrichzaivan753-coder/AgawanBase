"""Build the PA2 presentation deck.

Twelve slides, 16:9, carrying the demonstration captures D1-D6 and the architecture diagram.
Text is deliberately sparse: the deck supports the demonstration, it does not replace it.

Run:
    python Docs/tools/build_pa2_deck.py
"""

import os
from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WEEK = os.path.join(ROOT, "Docs", "Zedrich Abrera", "Week_18")
EVID = os.path.join(WEEK, "Evidence")
OUT = os.path.join(WEEK, "Abrera_Zedrich_PA2_Presentation.pptx")

NAVY = RGBColor(0x12, 0x24, 0x3A)
AMBER = RGBColor(0xE6, 0xA8, 0x3C)
GREY = RGBColor(0x55, 0x5F, 0x6D)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
INK = RGBColor(0x1C, 0x1F, 0x24)
FONT = "Segoe UI"

SW, SH = 13.333, 7.5


def new_deck():
    prs = Presentation()
    prs.slide_width = Inches(SW)
    prs.slide_height = Inches(SH)
    return prs


def blank(prs):
    return prs.slides.add_slide(prs.slide_layouts[6])


def rect(slide, x, y, w, h, fill):
    from pptx.enum.shapes import MSO_SHAPE
    sh = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(x), Inches(y), Inches(w), Inches(h))
    sh.fill.solid()
    sh.fill.fore_color.rgb = fill
    sh.line.fill.background()
    sh.shadow.inherit = False
    return sh


def text(slide, x, y, w, h, body, size=18, bold=False, color=INK, align=PP_ALIGN.LEFT,
         anchor=MSO_ANCHOR.TOP, spacing=6):
    tb = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    tf = tb.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = anchor
    lines = body if isinstance(body, list) else [body]
    for i, line in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = align
        p.space_after = Pt(spacing)
        run = p.add_run()
        run.text = line
        run.font.size = Pt(size)
        run.font.name = FONT
        run.font.bold = bold
        run.font.color.rgb = color
    return tb


def header(slide, title, kicker=None):
    rect(slide, 0, 0, SW, 0.92, NAVY)
    rect(slide, 0, 0.92, SW, 0.06, AMBER)
    text(slide, 0.5, 0.16, SW - 1.0, 0.6, title, size=28, bold=True, color=WHITE)
    if kicker:
        text(slide, 0.5, SH - 0.55, SW - 1.0, 0.35, kicker, size=11, color=GREY)


def picture(slide, path, box, caption):
    """Fit the image inside box, centred, keeping aspect, with a caption underneath."""
    x, y, w, h = box
    with Image.open(path) as im:
        iw, ih = im.size
    ar = iw / ih
    cap_h = 0.5
    max_h = h - cap_h
    if w / ar <= max_h:
        pw, ph = w, w / ar
    else:
        ph, pw = max_h, max_h * ar
    px = x + (w - pw) / 2
    slide.shapes.add_picture(path, Inches(px), Inches(y), Inches(pw), Inches(ph))
    text(slide, x, y + ph + 0.08, w, cap_h, caption, size=12, color=GREY, align=PP_ALIGN.CENTER)


def build():
    prs = new_deck()

    # 1 ------------------------------------------------------------------ title
    s = blank(prs)
    rect(s, 0, 0, SW, SH, NAVY)
    rect(s, 0, 3.02, SW, 0.07, AMBER)
    text(s, 0.9, 1.05, SW - 1.8, 1.0, "AGAWAN BASE 3D", size=54, bold=True, color=WHITE)
    text(s, 0.9, 2.05, SW - 1.8, 0.6, "A 3D capture-the-flag mobile game from the Filipino street game",
         size=20, color=AMBER)
    text(s, 0.9, 3.35, SW - 1.8, 2.2, [
        "PA2 - Complete Thematic Mobile-Game Demonstration and Defense",
        "Zedrich Zaivan B. Abrera  |  MOBILE DEV 002  |  Module 5",
        "Theme: Philippine Games and Sports",
        "Build 1.0.0-rc1   |   source commit 84c0f2e   |   tag v1.0.0-pa2",
    ], size=17, color=WHITE, spacing=10)
    text(s, 0.9, SH - 1.15, SW - 1.8, 0.6,
         "Unity 6000.6.0f1  |  URP 17.6.0  |  IL2CPP / ARM64  |  Android API 26+",
         size=13, color=RGBColor(0x9A, 0xA6, 0xB4))

    # 2 ------------------------------------------------------------------ theme
    s = blank(prs)
    header(s, "The theme, and why the mechanics had to follow it",
           "Agawan Base 3D  |  PA2  |  Zedrich Zaivan B. Abrera")
    text(s, 0.6, 1.25, 6.1, 5.4, [
        "Agawang base is a Filipino street game of two bases and two teams.",
        "The tension is leaving your own base undefended.",
        "",
        "That tension is not a skin. The mechanics that make the street game recognisable are the ones the code implements:",
        "   -  a base you can be tagged near",
        "   -  a prisoner you can free",
        "   -  a flag that decides the match",
        "   -  short immunity after a rescue",
        "",
        "Design constraint: a match must fit a class break, so the clock stops at 180 s.",
    ], size=16, spacing=9)
    picture(s, os.path.join(EVID, "D4_map_select.png"), (6.9, 1.25, 5.9, 4.6),
            "D4 - three maps with different cover")
    text(s, 6.9, 6.25, 5.9, 0.6,
         "Classic Open Field  /  Palengke  /  School Campus",
         size=12, color=GREY, align=PP_ALIGN.CENTER)

    # 3 ------------------------------------------------------------------ target player
    s = blank(prs)
    header(s, "Target player, and the decisions that followed",
           "Agawan Base 3D  |  PA2  |  Zedrich Zaivan B. Abrera")
    text(s, 0.6, 1.3, 12.1, 1.0,
         "Someone who played agawan base as a child, has a mid-range Android phone, and ten minutes.",
         size=20, bold=True)
    rows = [
        ("Landscape, two thumbs", "Controls at the bottom corners; every button at least 144 canvas px (~48 dp) tall."),
        ("30 fps, not 60", "A steady 30 suits a top-down chase and roughly halves the thermal cost."),
        ("The rules teach themselves", "The HOW TO PLAY screen explains tagging, rescuing and the flag in the code's own words."),
        ("Difficulty is symmetric", "Easy / Normal / Hard applies identically to both teams - an easier match, not a rigged one."),
    ]
    y = 2.55
    for label, detail in rows:
        rect(s, 0.6, y, 0.08, 0.85, AMBER)
        text(s, 0.85, y + 0.02, 3.4, 0.5, label, size=17, bold=True)
        text(s, 4.3, y + 0.04, 8.4, 0.8, detail, size=15, color=GREY)
        y += 1.05

    # 4 ------------------------------------------------------------------ rules
    s = blank(prs)
    header(s, "The rules, as implemented", "Constants in code, not design intentions")
    rules = [
        ("Tag reach", "1.2 m", "MatchManager.cs:88"),
        ("Flag steal reach", "1.5 m", "MatchManager.cs:91"),
        ("Rescue reach", "1.5 m", "MatchManager.cs:110"),
        ("Immunity after rescue", "2 s", "MatchManager.cs:119"),
        ("Rescue cooldown", "3 s", "MatchManager.cs:123"),
        ("Match time limit", "180 s", "MatchManager.cs:128"),
        ("Dropped flag returns", "10 s", "Flag.cs:44"),
        ("Team sizes", "1v1 - 4v4", "MatchSettings"),
    ]
    y = 1.35
    for name, val, where in rules:
        text(s, 0.7, y, 4.3, 0.4, name, size=16)
        text(s, 5.0, y, 1.6, 0.4, val, size=16, bold=True, color=NAVY)
        text(s, 6.8, y, 5.6, 0.4, where, size=13, color=GREY)
        y += 0.62
    rect(s, 0.7, 1.28, 11.7, 0.02, RGBColor(0xD8, 0xDE, 0xE6))
    text(s, 0.7, 6.45, 11.9, 0.6,
         "Two anti-stall guards: a just-freed player ignores rescuers for the cooldown, and a dropped flag returns itself.",
         size=13, color=GREY)

    # 5 ------------------------------------------------------------------ D1
    s = blank(prs)
    header(s, "Onboarding - the title screen identifies its own build",
           "D1_title.png  |  captured 2.33 s into the rehearsal")
    picture(s, os.path.join(EVID, "D1_title.png"), (0.6, 1.25, 12.1, 5.3),
            "D1 - version caption '1.0.0-rc1 - built 2026-11-19', and the four entry points")
    text(s, 0.6, 6.6, 12.1, 0.5,
         "The build in front of an examiner can be checked against the document without any tooling.",
         size=14, color=GREY, align=PP_ALIGN.CENTER)

    # 6 ------------------------------------------------------------------ D2
    s = blank(prs)
    header(s, "Onboarding - the instructions screen", "D2_how_to_play.png")
    picture(s, os.path.join(EVID, "D2_how_to_play.png"), (0.6, 1.25, 12.1, 5.3),
            "D2 - the rules in the same terms the code uses")
    text(s, 0.6, 6.6, 12.1, 0.5,
         "DEF-17-07 was found here: 734.5 px of text in a 660 px box, hidden behind the BACK button. FitText now measures it.",
         size=13, color=GREY, align=PP_ALIGN.CENTER)

    # 7 ------------------------------------------------------------------ D3
    s = blank(prs)
    header(s, "Credits, licences and AI disclosure", "D3_credits.png")
    picture(s, os.path.join(EVID, "D3_credits.png"), (0.6, 1.25, 12.1, 5.3),
            "D3 - authorship, Unity licences, and the AI-disclosure block")
    text(s, 0.6, 6.6, 12.1, 0.5,
         "Disclosure travels with the binary, not only in the documents.",
         size=14, color=GREY, align=PP_ALIGN.CENTER)

    # 8 ------------------------------------------------------------------ D5
    s = blank(prs)
    header(s, "Settings - team size and difficulty", "D5_match_setup.png")
    picture(s, os.path.join(EVID, "D5_match_setup.png"), (0.6, 1.25, 12.1, 5.3),
            "D5 - 1v1 to 4v4, Easy / Normal / Hard, applied to both teams")
    text(s, 0.6, 6.6, 12.1, 0.5,
         "Difficulty changes how demanding the match is, not which side the game favours.",
         size=14, color=GREY, align=PP_ALIGN.CENTER)

    # 9 ------------------------------------------------------------------ D6
    s = blank(prs)
    header(s, "The core loop", "D6_match_running.png  |  captured 7.55 s into the rehearsal")
    picture(s, os.path.join(EVID, "D6_match_running.png"), (0.6, 1.25, 12.1, 5.3),
            "D6 - top-down chase, on-screen stick, overhead freshness and stamina bars, base ring")
    text(s, 0.6, 6.6, 12.1, 0.5,
         "Nine screens were driven in 14.2 s; every capture has a distinct hash, so no frame repeats another.",
         size=13, color=GREY, align=PP_ALIGN.CENTER)

    # 10 ----------------------------------------------------------------- architecture
    s = blank(prs)
    header(s, "Architecture", "Three assemblies, five script folders")
    picture(s, os.path.join(EVID, "fig_architecture.png"), (0.5, 1.2, 12.3, 4.7),
            "Scenes, runtime scripts, and the data kept outside the scene")
    text(s, 0.6, 6.15, 12.1, 1.0, [
        "AgawanBase.Runtime  /  AgawanBase.Editor  /  AgawanBase.Tests.EditMode",
        "Visual reads match state and never writes it - IndicatorController.cs:24 states the contract.",
    ], size=14, color=GREY)

    # 11 ----------------------------------------------------------------- security + testing
    s = blank(prs)
    header(s, "Security, and the evidence it works", "PlayerProfile.cs")
    text(s, 0.6, 1.3, 6.2, 2.4, [
        "Profile is a FILE, not PlayerPrefs, so it can carry a signature.",
        "SHA-256 over a per-install salt + payload JSON (PlayerProfile.cs:627),",
        "checked at :537. An edited file is refused and kept as",
        "profile.rejected.json.",
    ], size=15, spacing=8)
    text(s, 0.6, 3.9, 6.2, 2.4, [
        "Its limit, stated in the source: the salt is on the device and there is no server,",
        "so this is device-local integrity, not security against a determined attacker.",
        "",
        "Observed for real: the Windows backup package rejected an editor-written profile.",
    ], size=14, color=GREY, spacing=8)
    text(s, 7.1, 1.3, 5.6, 0.5, "Evidence", size=18, bold=True)
    evidence = [
        "17 / 17  EditMode tests pass",
        "516 passed, 0 failed, 24 not measurable  (accessibility)",
        "16 scenarios, 0 failures  (balance, 4v4)",
        "5 testers, 25 tasks, 0 failures  (playtest)",
        "9 captures, 9 distinct hashes  (demonstration)",
    ]
    y = 1.95
    for line in evidence:
        rect(s, 7.1, y + 0.10, 0.07, 0.32, AMBER)
        text(s, 7.35, y, 5.3, 0.5, line, size=14)
        y += 0.62

    # 12 ----------------------------------------------------------------- optimisation + limits
    s = blank(prs)
    header(s, "Optimisation, and what I do not claim", "Week 16 profiling")
    text(s, 0.6, 1.3, 6.0, 0.5, "Measured", size=18, bold=True)
    text(s, 0.6, 1.9, 6.0, 2.6, [
        "Main thread  0.59 - 0.63 ms",
        "GPU  0.48 ms",
        "17 render events",
        "~320 B / frame",
        "Budget 33.3 ms at render scale 0.80",
        "Prop texture memory  21,849 KB  ->  1,369 KB   (-20.0 MB)",
    ], size=15, spacing=8)
    text(s, 0.6, 4.6, 6.0, 0.5, "Not claimed", size=18, bold=True, color=RGBColor(0xB0, 0x3A, 0x2E))
    text(s, 0.6, 5.15, 6.0, 1.6, [
        "68.30 MB -> 31.63 MB is development -> release, not optimisation.",
        "No GPU win beyond the measured 0.48 ms.",
    ], size=14, color=GREY, spacing=8)

    text(s, 7.0, 1.3, 5.8, 0.5, "Blocked, and stated", size=18, bold=True, color=RGBColor(0xB0, 0x3A, 0x2E))
    text(s, 7.0, 1.9, 5.8, 4.6, [
        "-  No handset: the APK is hashed and unmodified, but installation is BLOCKED.",
        "-  Repository is local only: origin/main is d08fc9b, so the tag cannot be verified remotely.",
        "-  E3 is sat under supervision: the record is blank until then.",
        "-  Two Week 14 findings were never re-tested.",
        "-  AI balance rests on 16 regression scenarios.",
        "-  The Windows backup differs: Mono, no haptics, two post-processing passes stripped.",
    ], size=14, spacing=12)

    prs.save(OUT)

    # ---------------------------------------------------------------- report
    media = 0
    for slide in prs.slides:
        for sh in slide.shapes:
            if sh.shape_type == 13:
                media += 1
    print("saved:", OUT)
    print("slides:", len(prs.slides._sldIdLst))
    print("pictures:", media)
    print("bytes:", os.path.getsize(OUT))


if __name__ == "__main__":
    build()
