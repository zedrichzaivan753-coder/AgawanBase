using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Week 14 accessibility audit: measures the UI instead of asserting things about it.
///
/// Three checks per label and button, plus the debug-flag check that decides whether the build a
/// playtester receives is the real game:
///   C  contrast      - WCAG 2.2 contrast ratio of a label against the panel behind it (>= 4.5:1)
///   L  label fits    - rect height vs fontSize * 1.12 (a legacy Text whose line height outgrows
///                      its rect draws nothing - TeamMatchBuild.cs:212-216)
///   T  touch target  - Button size in canvas px (>= 144x144, i.e. 48 dp at 1080p / xxhdpi)
///   D  debug flags   - the six serialized debugging aids that must be off for anyone who plays
///
/// It runs in EDIT MODE across all three scenes and writes a PASS/FAIL log, so the "before" and
/// the "after" are two files of measured numbers rather than two paragraphs of opinion.
///
/// Called through Tools > Playtest > Run Accessibility Audit, or execute_script (method Run).
/// </summary>
public static class A11yAudit
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Game.unity",
        "Assets/Scenes/Map_Palengke.unity",
        "Assets/Scenes/Map_SchoolCampus.unity"
    };

    const float ContrastMin = 4.5f;      // WCAG 2.2 AA, normal text
    const float MinTouchPx = 144f;       // 48 dp at 1080p / xxhdpi
    const float LabelHeightFactor = 1.12f;

    static string EvidenceDir
    {
        get
        {
            return Path.Combine(Directory.GetCurrentDirectory(),
                                "Docs", "Zedrich Abrera", "Week_14", "Evidence");
        }
    }

    static StringBuilder log;
    static int passed, failed, warned, notMeasured;

    [MenuItem("Tools/Playtest/Run Accessibility Audit")]
    public static void Run()
    {
        log = new StringBuilder();
        passed = failed = warned = notMeasured = 0;

        Directory.CreateDirectory(EvidenceDir);

        log.AppendLine("A11Y AUDIT - AgawanBase (Week 14 usability and accessibility)");
        log.AppendLine("Unity " + Application.unityVersion + "   " +
                       DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC   edit mode");
        log.AppendLine("Canvas Game/UI: CanvasScaler ScaleWithScreenSize, reference 1920x1080, match 0.5");
        log.AppendLine("  -> on a 1080p phone 1 canvas px is ~1 device px, and Android's 48 dp floor is ~144 px.");
        log.AppendLine("Thresholds: contrast >= " + ContrastMin.ToString("0.0") + ":1, touch target >= " +
                       MinTouchPx.ToString("0") + "x" + MinTouchPx.ToString("0") + " canvas px, " +
                       "label rect height >= fontSize x " + LabelHeightFactor.ToString("0.00") + ".");
        log.AppendLine("Contrast is measured against the nearest ancestor Image with alpha >= 0.5, " +
                       "composited over black. That is the most favourable case for the light text this UI " +
                       "uses, so a ratio that fails here fails everywhere. Labels with no such Image are " +
                       "reported N/A rather than guessed.");
        log.AppendLine();

        for (int i = 0; i < Scenes.Length; i++)
        {
            AuditScene(Scenes[i]);
        }

        log.AppendLine();
        log.AppendLine("TOTAL: " + passed + " passed, " + failed + " failed, " +
                       warned + " warnings, " + notMeasured + " not measurable");

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        File.WriteAllText(Path.Combine(EvidenceDir, "a11y_audit.log"), log.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(EvidenceDir, "a11y_audit_" + stamp + ".log"), log.ToString(), Encoding.UTF8);

        Debug.Log("[A11y] audit finished: " + passed + " passed, " + failed + " failed, " +
                  warned + " warnings, " + notMeasured + " not measurable. Log: " +
                  Path.Combine(EvidenceDir, "a11y_audit.log"));

        if (failed > 0) Debug.LogWarning("[A11y] " + failed + " accessibility failures - see a11y_audit.log");

        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
    }

    // ------------------------------------------------------------------ one scene

    static void AuditScene(string scenePath)
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject ui = GameObject.Find("Game/UI");
        log.AppendLine("--- " + scenePath + " ---");

        if (ui == null)
        {
            log.AppendLine("FAIL  X  Game/UI not found in this scene");
            failed++;
            return;
        }

        // ---- T: touch targets ---------------------------------------------------
        Button[] buttons = ui.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            Rect r = b.GetComponent<RectTransform>().rect;
            bool ok = r.width >= MinTouchPx && r.height >= MinTouchPx;

            Line(ok, "T", PathOf(b.transform),
                 "size=" + r.width.ToString("0") + "x" + r.height.ToString("0") + " px" +
                 (ok ? "" : " (needs >= " + MinTouchPx.ToString("0") + "x" + MinTouchPx.ToString("0") + ")"));
        }

        // The two controls that matter most on a phone are not uGUI Buttons: the stick and the
        // sprint pad are Graphics with their own pointer handlers. Measured the same way, or the
        // audit would be quietly reporting only on the easy half of the screen.
        MonoBehaviour[] behaviours = ui.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour m = behaviours[i];
            if (m == null) continue;
            if (!(m is IPointerDownHandler)) continue;
            if (m is Button) continue;                            // already measured above

            Graphic graphic = m.GetComponent<Graphic>();
            if (graphic == null) continue;

            Rect r = graphic.rectTransform.rect;
            bool ok = r.width >= MinTouchPx && r.height >= MinTouchPx;

            Line(ok, "T", PathOf(m.transform) + " (" + m.GetType().Name + ")",
                 "size=" + r.width.ToString("0") + "x" + r.height.ToString("0") + " px" +
                 (ok ? "" : " (needs >= " + MinTouchPx.ToString("0") + "x" + MinTouchPx.ToString("0") + ")"));
        }

        // ---- C and L: every label ----------------------------------------------
        Text[] texts = ui.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text t = texts[i];
            if (t.color.a <= 0.05f) continue;                     // invisible on purpose

            string path = PathOf(t.transform);
            Rect rect = t.GetComponent<RectTransform>().rect;

            // L - does the box have room for one line?
            float lineHeight = t.fontSize * LabelHeightFactor;
            bool fits = rect.height >= lineHeight - 0.5f;

            if (fits)
            {
                Line(true, "L", path, "rect=" + rect.height.ToString("0") +
                     " px for " + t.fontSize + "pt text");
            }
            else if (t.verticalOverflow == VerticalWrapMode.Truncate)
            {
                Line(false, "L", path, "rect=" + rect.height.ToString("0") +
                     " px is under the " + lineHeight.ToString("0") + " px a " + t.fontSize +
                     "pt line needs, and overflow is Truncate (the label can vanish)");
            }
            else
            {
                Warn("L", path, "rect=" + rect.height.ToString("0") + " px is under the " +
                     lineHeight.ToString("0") + " px line height (overflow is allowed, so it draws outside its box)");
            }

            // C - contrast against the panel behind it
            Color backdrop;
            if (!TryBackdrop(t.transform, out backdrop))
            {
                log.AppendLine("  N/A C  " + path + "  drawn over the 3D view (no solid backdrop)");
                notMeasured++;
                continue;
            }

            Color text = Composite(t.color, backdrop);            // text alpha over the backdrop
            float ratio = ContrastRatio(text, backdrop);
            bool okContrast = ratio >= ContrastMin;

            Line(okContrast, "C", path,
                 "contrast=" + ratio.ToString("0.00") + ":1 (" + Hex(text) + " on " + Hex(backdrop) + ")" +
                 (okContrast ? "" : " (needs >= " + ContrastMin.ToString("0.0") + ":1)"));
        }

        // ---- D: the debugging aids a playtester must not see --------------------
        HudController hud = Find<HudController>();
        FlagD("HudController.showAiDebug on Game/UI", hud != null && hud.showAiDebug, hud != null);

        FollowCamera camera = Find<FollowCamera>();
        FlagD("FollowCamera.logView on the camera rig", camera != null && camera.logView, camera != null);

        MatchManager match = Find<MatchManager>();
        FlagD("MatchManager.logRules", match != null && match.logRules, match != null);
        FlagD("MatchManager.allowDebugDrop", match != null && match.allowDebugDrop, match != null);

        CharacterSpawner spawner = Find<CharacterSpawner>();
        FlagD("CharacterSpawner.logRoster", spawner != null && spawner.logRoster, spawner != null);

        Flag[] flags = FindAll<Flag>();
        for (int i = 0; i < flags.Length; i++)
        {
            FlagD("Flag.logFlagEvents on " + PathOf(flags[i].transform), flags[i].logFlagEvents, true);
        }

        // ---- info --------------------------------------------------------------
        SafeAreaFitter safe = Find<SafeAreaFitter>();
        log.AppendLine("INFO  S  SafeAreaFitter " + (safe != null ? "present on " + PathOf(safe.transform) :
                                                              "NOT present - UI may sit under a notch"));
        log.AppendLine();
    }

    // ------------------------------------------------------------------ checks

    static void FlagD(string what, bool on, bool exists)
    {
        if (!exists)
        {
            log.AppendLine("  N/A D  " + what + "  (component not in this scene)");
            notMeasured++;
            return;
        }

        Line(!on, "D", what, on ? "ON - a playtester can see debugging information" : "off");
    }

    static void Line(bool ok, string kind, string what, string detail)
    {
        if (ok) passed++; else failed++;
        log.AppendLine((ok ? "PASS  " : "FAIL  ") + kind + "  " + what + "   [" + detail + "]");
    }

    static void Warn(string kind, string what, string detail)
    {
        warned++;
        log.AppendLine("WARN  " + kind + "  " + what + "   [" + detail + "]");
    }

    // ------------------------------------------------------------------ colour

    static bool TryBackdrop(Transform from, out Color backdrop)
    {
        backdrop = Color.black;

        Transform t = from.parent;
        while (t != null)
        {
            Image image = t.GetComponent<Image>();
            if (image != null && image.color.a >= 0.5f)
            {
                // The panel is translucent over the scene; compositing over black is the darkest
                // plausible backdrop, which is the fair case for the light text this UI uses.
                backdrop = new Color(image.color.r * image.color.a,
                                     image.color.g * image.color.a,
                                     image.color.b * image.color.a, 1f);
                return true;
            }
            t = t.parent;
        }

        return false;
    }

    static Color Composite(Color top, Color bottom)
    {
        float a = top.a;
        return new Color(top.r * a + bottom.r * (1f - a),
                         top.g * a + bottom.g * (1f - a),
                         top.b * a + bottom.b * (1f - a), 1f);
    }

    static float ContrastRatio(Color a, Color b)
    {
        float la = Luminance(a);
        float lb = Luminance(b);
        float hi = Mathf.Max(la, lb);
        float lo = Mathf.Min(la, lb);
        return (hi + 0.05f) / (lo + 0.05f);
    }

    static float Luminance(Color c)
    {
        return 0.2126f * Channel(c.r) + 0.7152f * Channel(c.g) + 0.0722f * Channel(c.b);
    }

    static float Channel(float v)
    {
        v = Mathf.Clamp01(v);
        return v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
    }

    static string Hex(Color c)
    {
        return "#" + Mathf.RoundToInt(c.r * 255f).ToString("X2") +
                     Mathf.RoundToInt(c.g * 255f).ToString("X2") +
                     Mathf.RoundToInt(c.b * 255f).ToString("X2");
    }

    // ------------------------------------------------------------------ scaffolding

    static string PathOf(Transform t)
    {
        string path = t.name;
        Transform p = t.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }

    static T Find<T>() where T : Component
    {
        T[] all = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        return all.Length > 0 ? all[0] : null;
    }

    static T[] FindAll<T>() where T : Component
    {
        return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
    }
}
