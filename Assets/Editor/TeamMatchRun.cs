using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thin driver. It exists only so the plan's build steps can be triggered from outside the
/// editor (the bridge runs one named method on one file). All of the real work lives in
/// TeamMatchBuild - this class holds no layout or wiring of its own, so running it can never
/// change the scene by itself.
/// </summary>
public static class TeamMatchRun
{
    /// <summary>Step 2: build the Match Setup panel and re-point the Title START button.</summary>
    public static void Setup()
    {
        TeamMatchBuild.MatchSetupPanel();

        // The step is only useful if the scene actually holds the result.
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[TeamRun] Match Setup panel step complete.");
    }

    /// <summary>Switches the setup panel on so it can be photographed, and paints it.</summary>
    public static void Preview()
    {
        GameObject panel = GameObject.Find("Game/UI/Panel_MatchSetup");
        if (panel == null) { Debug.LogError("[TeamRun] Panel_MatchSetup is missing."); return; }

        panel.SetActive(true);

        MatchSetupUI ui = panel.GetComponent<MatchSetupUI>();
        if (ui != null) ui.Refresh();

        Debug.Log("[TeamRun] preview ON (remember to call PreviewOff).");
    }

    /// <summary>Puts the panel back the way GameManager expects to find it.</summary>
    public static void PreviewOff()
    {
        GameObject panel = GameObject.Find("Game/UI/Panel_MatchSetup");
        if (panel != null) panel.SetActive(false);

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[TeamRun] preview OFF.");
    }

    /// <summary>Prints every child's WORLD corners next to the Canvas's own world corners. World
    /// space is the only frame that includes every parent's scale, so it is the only way to tell
    /// whether something is actually on screen. Panel_Title is measured the same way as a
    /// known-good reference.</summary>
    public static void Layout()
    {
        GameObject panel = GameObject.Find("Game/UI/Panel_MatchSetup");
        if (panel == null) { Debug.LogError("[TeamRun] Panel_MatchSetup is missing."); return; }

        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        if (canvas == null) { Debug.LogError("[TeamRun] the panel is not under a Canvas."); return; }

        RectTransform canvasRt = canvas.transform as RectTransform;
        Vector3[] cc = new Vector3[4];
        canvasRt.GetWorldCorners(cc);

        Debug.Log("[TeamRun] canvas '" + canvas.name + "' mode=" + canvas.renderMode +
                  " scaleFactor=" + canvas.scaleFactor +
                  " rect=" + canvasRt.rect +
                  " worldX=" + cc[0].x.ToString("0") + ".." + cc[2].x.ToString("0") +
                  " worldY=" + cc[0].y.ToString("0") + ".." + cc[2].y.ToString("0"));

        Report(panel, cc, "setup");
        Report(GameObject.Find("Game/UI/Panel_Title"), cc, "title");

        Debug.Log("[TeamRun] layout complete.");
    }

    static void Report(GameObject root, Vector3[] canvas, string tag)
    {
        if (root == null) { Debug.Log("[TeamRun] " + tag + ": missing."); return; }

        Vector3[] c = new Vector3[4];

        RectTransform rootRt = root.transform as RectTransform;
        if (rootRt != null) Print(rootRt, canvas, tag + "/" + root.name);

        for (int i = 0; i < root.transform.childCount; i++)
        {
            RectTransform rt = root.transform.GetChild(i) as RectTransform;
            if (rt != null) Print(rt, canvas, tag + "/" + rt.name);
        }
    }

    static void Print(RectTransform rt, Vector3[] canvas, string label)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);

        bool fits = c[0].x >= canvas[0].x - 0.5f && c[2].x <= canvas[2].x + 0.5f &&
                    c[0].y >= canvas[0].y - 0.5f && c[2].y <= canvas[2].y + 0.5f;

        Text t = rt.GetComponent<Text>();

        Debug.Log("[TeamRun] " + label +
                  " worldX=" + c[0].x.ToString("0") + ".." + c[2].x.ToString("0") +
                  " worldY=" + c[0].y.ToString("0") + ".." + c[2].y.ToString("0") +
                  (fits ? "  ON SCREEN" : "  ** OFF SCREEN **") +
                  (t != null ? " text=\"" + t.text + "\"" : ""));
    }

    /// <summary>Step 2 proof: reads back what the builder claims it wired and prints it.
    /// Read-only - it changes nothing.</summary>
    public static void Check()
    {
        GameObject panel = GameObject.Find("Game/UI/Panel_MatchSetup");
        if (panel == null) { Debug.LogError("[TeamRun] Panel_MatchSetup is missing."); return; }

        Debug.Log("[TeamRun] panel active=" + panel.activeSelf +
                  "  siblingIndex=" + panel.transform.GetSiblingIndex() +
                  "  of " + (panel.transform.parent.childCount - 1));

        RectTransform prt = panel.GetComponent<RectTransform>();
        Debug.Log("[TeamRun] panel rect anchors=" + prt.anchorMin + ".." + prt.anchorMax +
                  " sizeDelta=" + prt.sizeDelta);

        Image bg = panel.GetComponent<Image>();
        Debug.Log("[TeamRun] panel image colour=" + (bg != null ? bg.color.ToString() : "MISSING") +
                  " raycastTarget=" + (bg != null && bg.raycastTarget));

        MatchSetupUI ui = panel.GetComponent<MatchSetupUI>();
        if (ui == null) { Debug.LogError("[TeamRun] Panel_MatchSetup has no MatchSetupUI."); return; }

        Debug.Log("[TeamRun] MatchSetupUI game=" + (ui.game != null ? ui.game.name : "NULL") +
                  "  sizeButtons=" + Count(ui.sizeButtons) + "  sizeLabels=" + Count(ui.sizeLabels) +
                  "  diffButtons=" + Count(ui.difficultyButtons) + "  diffLabels=" + Count(ui.difficultyLabels) +
                  "  summary=" + (ui.summaryText != null ? "ok" : "NULL"));

        for (int i = 0; i < ui.sizeButtons.Length; i++)
        {
            Debug.Log("[TeamRun] size[" + i + "] = " + Name(ui.sizeButtons[i]) +
                      " size=" + Size(ui.sizeButtons[i]));
        }

        for (int i = 0; i < ui.difficultyButtons.Length; i++)
        {
            Debug.Log("[TeamRun] diff[" + i + "] = " + Name(ui.difficultyButtons[i]) +
                      " size=" + Size(ui.difficultyButtons[i]));
        }

        Debug.Log("[TeamRun] BACK  listeners=" + Listeners(FindButton(panel, "BackButton")) +
                  " target=" + ListenerTarget(FindButton(panel, "BackButton")));
        Debug.Log("[TeamRun] START listeners=" + Listeners(FindButton(panel, "StartButton")) +
                  " target=" + ListenerTarget(FindButton(panel, "StartButton")));

        GameManager gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        Debug.Log("[TeamRun] GameManager.panelMatchSetup=" +
                  (gm != null && gm.panelMatchSetup != null ? gm.panelMatchSetup.name : "NULL") +
                  "  state=" + (gm != null ? gm.State.ToString() : "-"));

        Button tb = FindButton(GameObject.Find("Game/UI/Panel_Title"), "StartButton");
        Debug.Log("[TeamRun] Title START listeners=" + Listeners(tb) +
                  " target=" + ListenerTarget(tb) +
                  "  (expected: exactly 1, on GameManager)");

        Debug.Log("[TeamRun] check complete.");
    }

    static Button FindButton(GameObject panel, string name)
    {
        if (panel == null) return null;
        Transform t = panel.transform.Find(name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    static string Name(Button b) { return b != null ? b.name : "NULL"; }

    static string Size(Button b)
    {
        return b != null ? b.GetComponent<RectTransform>().sizeDelta.ToString() : "-";
    }

    static int Count(Button[] array) { return array != null ? array.Length : -1; }

    static int Count(Text[] array) { return array != null ? array.Length : -1; }

    static int Listeners(Button b)
    {
        return b != null ? b.onClick.GetPersistentEventCount() : -1;
    }

    static string ListenerTarget(Button b)
    {
        if (b == null) return "-";
        if (b.onClick.GetPersistentEventCount() == 0) return "none";

        string s = "";
        for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
        {
            Object target = b.onClick.GetPersistentTarget(i);
            s += (i > 0 ? ", " : "") + b.onClick.GetPersistentMethodName(i) +
                 " on " + (target != null ? target.GetType().Name : "NULL");
        }
        return s;
    }
}
