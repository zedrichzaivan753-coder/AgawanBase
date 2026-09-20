using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Play-mode driver for the plan's build steps. The bridge runs one named method on one file, and
/// this class holds no layout or scene wiring of its own.
///
/// Presses the real buttons through the EventSystem rather than calling the handlers directly, so
/// what it tests is exactly what a player's finger does.
/// </summary>
public static class TeamMatchPlayTest
{
    static PointerEventData _pointer;

    /// <summary>The whole Step 2 acceptance walk: Title -> setup -> pick 4v4 + Hard -> BACK ->
    /// Title -> setup again -> START.</summary>
    public static void Run()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[PlayTest] not in play mode.");
            return;
        }

        GameManager gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        GameObject setupPanel = GameObject.Find("Game/UI/Panel_MatchSetup");
        GameObject titlePanel = GameObject.Find("Game/UI/Panel_Title");

        if (gm == null || setupPanel == null || titlePanel == null)
        {
            Debug.LogError("[PlayTest] missing GameManager / Panel_MatchSetup / Panel_Title.");
            return;
        }

        LogCanvas(setupPanel);

        Debug.Log("[PlayTest] --- start: state=" + gm.State +
                  "  titleActive=" + titlePanel.activeSelf +
                  "  setupActive=" + setupPanel.activeSelf);

        // 1. Title screen START should open the setup screen, not start the match.
        Click(find: "Game/UI/Panel_Title/StartButton");
        Debug.Log("[PlayTest] after title START: state=" + gm.State +
                  "  setupActive=" + setupPanel.activeSelf +
                  "  titleActive=" + titlePanel.activeSelf +
                  "   (expect MatchSetup / True / False)");

        MatchSetupUI ui = setupPanel.GetComponent<MatchSetupUI>();

        // 2. Pick the biggest match and the hardest skill.
        Click("Game/UI/Panel_MatchSetup/SizeButton4");
        Click("Game/UI/Panel_MatchSetup/HardButton");

        Debug.Log("[PlayTest] after 4v4 + HARD: teamSize=" + MatchSettings.TeamSize +
                  " difficulty=" + MatchSettings.Difficulty +
                  " summary=\"" + (ui.summaryText != null ? ui.summaryText.text : "NULL") + "\"");

        ReportPaint(ui);

        // 3. BACK should return to the title and the choice should be remembered.
        Click("Game/UI/Panel_MatchSetup/BackButton");
        Debug.Log("[PlayTest] after BACK: state=" + gm.State +
                  "  setupActive=" + setupPanel.activeSelf +
                  "  titleActive=" + titlePanel.activeSelf +
                  "   (expect Title / False / True)");

        // 4. Straight back in - the setup screen must remember 4v4 + HARD.
        Click("Game/UI/Panel_Title/StartButton");
        Debug.Log("[PlayTest] reopened: state=" + gm.State +
                  " summary=\"" + (ui.summaryText != null ? ui.summaryText.text : "NULL") + "\"" +
                  "   (expect the 4v4 / HARD line)");

        ReportPaint(ui);

        // 5. START begins the match. If the choice differs from the roster already on the field,
        // this reloads the scene, so the state can only be read on a later call: use AfterStart().
        Debug.Log("[PlayTest] roster on the field was built for teamSize=" +
                  CharacterSpawner.SpawnedTeamSize + " " + CharacterSpawner.SpawnedDifficulty +
                  ", the setup screen now asks for " + MatchSettings.TeamSize + " " +
                  MatchSettings.Difficulty + "  (different -> the scene will reload)");

        Click("Game/UI/Panel_MatchSetup/StartButton");
        Debug.Log("[PlayTest] after START: state=" + gm.State +
                  "  setupActive=" + setupPanel.activeSelf +
                  "   (Playing if the roster already matched, otherwise a reload is in flight)");

        Debug.Log("[PlayTest] PlayerPrefs match.teamSize=" + PlayerPrefs.GetInt("match.teamSize", -1) +
                  " match.difficulty=" + PlayerPrefs.GetInt("match.difficulty", -1));
    }

    /// <summary>Read this AFTER the setup START reload has happened: proves the reload produced a
    /// Playing match built at the chosen size.</summary>
    public static void AfterStart()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        GameObject setupPanel = GameObject.Find("Game/UI/Panel_MatchSetup");

        Debug.Log("[PlayTest] after the reload: state=" +
                  (gm != null ? gm.State.ToString() : "NULL") +
                  "  setupActive=" + (setupPanel != null && setupPanel.activeSelf) +
                  "  builtFor=" + CharacterSpawner.SpawnedTeamSize + " " +
                  CharacterSpawner.SpawnedDifficulty +
                  "   (expect Playing / False / the size chosen in the setup screen)");

        SpawnCheck();
    }

    /// <summary>Step 4 proof: prints the roster that was actually spawned, with positions, and the
    /// closest pair of bodies, so "no overlapping bodies" is a number rather than an opinion.</summary>
    public static void SpawnCheck()
    {
        if (!Application.isPlaying) { Debug.LogError("[PlayTest] not in play mode."); return; }

        Debug.Log("[PlayTest] teamSize=" + MatchSettings.TeamSize + " (" + MatchSettings.SizeName +
                  ")  expected Blue AI=" + MatchSettings.BlueAiCount +
                  " Red AI=" + MatchSettings.RedAiCount +
                  " total=" + MatchSettings.TotalCharacters);

        ReportTeam(Team.Blue);
        ReportTeam(Team.Red);

        float closest = ClosestPair(out string pair);
        Debug.Log("[PlayTest] closest pair of any two characters: " + closest.ToString("F2") +
                  " m (" + pair + ")  - bodies are 1.00 m wide, so anything above 1.00 is clear");

        Debug.Log("[PlayTest] spawn check complete.");
    }

    /// <summary>Proves every spawned body really wears its own team's jersey: prints the rig's
    /// teamJersey and the material actually on the two jersey renderers.</summary>
    public static void JerseyCheck()
    {
        if (!Application.isPlaying) { Debug.LogError("[PlayTest] not in play mode."); return; }

        Jersey(Team.Blue);
        Jersey(Team.Red);

        Debug.Log("[PlayTest] jersey check complete.");
    }

    static void Jersey(Team team)
    {
        var members = TeamManager.Members(team);

        for (int i = 0; i < members.Count; i++)
        {
            CharacterStatus s = members[i];
            if (s == null) continue;

            CharacterRigAnimator rig = s.GetComponent<CharacterRigAnimator>();
            if (rig == null) { Debug.Log("[PlayTest] " + s.name + " has no rig."); continue; }

            Debug.Log("[PlayTest] " + s.name + " team=" + s.team +
                      " teamJersey=" + Name(rig.teamJersey) +
                      " flag=" + (rig.flag != null ? rig.flag.name : "NULL") +
                      " jerseyA=" + Shared(rig.jerseyA) +
                      " jerseyB=" + Shared(rig.jerseyB));
        }
    }

    static string Name(Object o) { return o != null ? o.name : "NULL"; }

    static string Shared(Renderer r)
    {
        return (r != null && r.sharedMaterial != null) ? r.sharedMaterial.name : "none";
    }

    /// <summary>Changes the remembered team size, then reloads the scene so the spawner runs again
    /// at the new size - the same path Restart uses.</summary>
    public static void SetSizeAndReload(int size)
    {
        if (!Application.isPlaying) { Debug.LogError("[PlayTest] not in play mode."); return; }

        MatchSettings.SetTeamSize(size);
        MatchSettings.Save();
        Debug.Log("[PlayTest] team size set to " + size + ", reloading the scene.");

        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    static void ReportTeam(Team team)
    {
        var members = TeamManager.Members(team);
        Debug.Log("[PlayTest] " + team + ": " + members.Count + " member(s)");

        for (int i = 0; i < members.Count; i++)
        {
            CharacterStatus s = members[i];
            if (s == null) continue;

            EnemyAI ai = s.GetComponent<EnemyAI>();
            Debug.Log("[PlayTest]   " + s.name +
                      " pos=" + s.transform.position.ToString("F2") +
                      " spawnIndex=" + s.spawnIndex +
                      " fieldTime=" + s.fieldTime.ToString("F2") +
                      " inBase=" + s.IsInHomeBase +
                      " ai=" + (ai != null ? ai.state.ToString() : "none"));
        }
    }

    /// <summary>Smallest distance between any two living characters, and who they are.</summary>
    static float ClosestPair(out string who)
    {
        who = "-";
        float best = float.MaxValue;

        var all = new System.Collections.Generic.List<CharacterStatus>();
        all.AddRange(TeamManager.Members(Team.Blue));
        all.AddRange(TeamManager.Members(Team.Red));

        for (int i = 0; i < all.Count; i++)
        {
            for (int j = i + 1; j < all.Count; j++)
            {
                float d = Vector3.Distance(all[i].transform.position, all[j].transform.position);
                if (d < best) { best = d; who = all[i].name + " / " + all[j].name; }
            }
        }

        return best == float.MaxValue ? 0f : best;
    }

    /// <summary>Puts the setup screen back on top so it can be photographed.</summary>
    public static void ShowSetup()
    {
        GameObject panel = GameObject.Find("Game/UI/Panel_MatchSetup");
        GameManager gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);

        if (panel == null || gm == null) { Debug.LogError("[PlayTest] missing panel / GameManager."); return; }

        gm.ShowMatchSetup();
        Debug.Log("[PlayTest] showing the setup screen for a screenshot. state=" + gm.State +
                  " active=" + panel.activeSelf);
    }

    /// <summary>Dumps everything about one child of the setup panel, so a missing label can be
    /// diagnosed from numbers instead of guessing.</summary>
    public static void Probe()
    {
        GameObject panel = GameObject.Find("Game/UI/Panel_MatchSetup");
        if (panel == null) { Debug.LogError("[PlayTest] no Panel_MatchSetup."); return; }

        for (int i = 0; i < panel.transform.childCount; i++)
        {
            Transform child = panel.transform.GetChild(i);
            Text t = child.GetComponent<Text>();
            if (t == null) continue;

            CanvasRenderer cr = child.GetComponent<CanvasRenderer>();
            RectTransform rt = child as RectTransform;

            Debug.Log("[PlayTest] PROBE " + child.name +
                      " active=" + child.gameObject.activeSelf +
                      " enabled=" + t.enabled +
                      " font=" + (t.font != null ? t.font.name : "NULL") +
                      " fontSize=" + t.fontSize +
                      " colour=" + t.color +
                      " align=" + t.alignment +
                      " hOverflow=" + t.horizontalOverflow +
                      " vOverflow=" + t.verticalOverflow +
                      " rect=" + rt.rect +
                      " scale=" + rt.localScale +
                      " visibleChars=" + t.cachedTextGenerator.characterCountVisible +
                      " canvasAlpha=" + (cr != null ? cr.GetAlpha().ToString() : "-"));
        }
    }

    /// <summary>Reads the live canvas so the layout can be judged in the space it really renders in.
    /// In edit mode the CanvasScaler has not been applied yet, so a panel measured there looks ~2x
    /// too big; play mode is the only honest frame.</summary>
    public static void Frame()
    {
        GameObject panel = GameObject.Find("Game/UI/Panel_MatchSetup");
        if (panel != null) LogCanvas(panel);
    }

    // ----------------------------------------------------------------------------- internals

    static void LogCanvas(GameObject panel)
    {
        Canvas canvas = panel.GetComponentInParent<Canvas>(true);
        RectTransform crt = canvas.transform as RectTransform;
        Vector3[] c = new Vector3[4];
        crt.GetWorldCorners(c);

        Debug.Log("[PlayTest] canvas scaleFactor=" + canvas.scaleFactor +
                  " rect=" + crt.rect +
                  " screen=" + Screen.width + "x" + Screen.height +
                  " worldX=" + c[0].x.ToString("0") + ".." + c[2].x.ToString("0") +
                  " worldY=" + c[0].y.ToString("0") + ".." + c[2].y.ToString("0"));

        Vector3[] p = new Vector3[4];
        for (int i = 0; i < panel.transform.childCount; i++)
        {
            RectTransform rt = panel.transform.GetChild(i) as RectTransform;
            if (rt == null) continue;

            rt.GetWorldCorners(p);
            bool fits = p[0].x >= c[0].x - 0.5f && p[2].x <= c[2].x + 0.5f &&
                        p[0].y >= c[0].y - 0.5f && p[2].y <= c[2].y + 0.5f;

            Debug.Log("[PlayTest] " + rt.name +
                      " worldX=" + p[0].x.ToString("0") + ".." + p[2].x.ToString("0") +
                      " worldY=" + p[0].y.ToString("0") + ".." + p[2].y.ToString("0") +
                      (fits ? "  ON SCREEN" : "  ** OFF SCREEN **"));
        }
    }

    /// <summary>Prints which button is painted as "chosen" on each row.</summary>
    static void ReportPaint(MatchSetupUI ui)
    {
        string sizes = "";
        for (int i = 0; i < ui.sizeButtons.Length; i++)
        {
            if (ui.sizeButtons[i] == null) continue;
            Image img = ui.sizeButtons[i].GetComponent<Image>();
            bool chosen = img.color == ui.selectedColor;
            sizes += (chosen ? "[" : " ") + ui.sizeButtons[i].name + (chosen ? "]" : " ");
        }

        string diffs = "";
        for (int i = 0; i < ui.difficultyButtons.Length; i++)
        {
            if (ui.difficultyButtons[i] == null) continue;
            Image img = ui.difficultyButtons[i].GetComponent<Image>();
            bool chosen = img.color == ui.selectedColor;
            diffs += (chosen ? "[" : " ") + ui.difficultyButtons[i].name + (chosen ? "]" : " ");
        }

        Debug.Log("[PlayTest] highlighted sizes:" + sizes);
        Debug.Log("[PlayTest] highlighted skill:" + diffs);
    }

    /// <summary>Presses a button the way the EventSystem does.</summary>
    static void Click(string find)
    {
        GameObject go = GameObject.Find(find);
        if (go == null) { Debug.LogError("[PlayTest] button not found: " + find); return; }

        Button button = go.GetComponent<Button>();
        if (button == null) { Debug.LogError("[PlayTest] no Button on " + find); return; }
        if (!button.IsActive() || !button.interactable)
        {
            Debug.LogError("[PlayTest] button not pressable: " + find);
            return;
        }

        if (_pointer == null) _pointer = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(button.gameObject, _pointer, ExecuteEvents.pointerClickHandler);
    }
}
