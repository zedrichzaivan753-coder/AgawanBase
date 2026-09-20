using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the parts of the TEAM PLAY upgrade that are SCENE STRUCTURE rather than code:
/// the Game/Systems object that carries TeamManager, and (in later steps) the Match Setup
/// panel and the new HUD labels.
///
/// Same idea as BarangayBuild: every object is found by name and updated in place, so running
/// it twice never duplicates anything. It creates objects and sets serialized fields only - it
/// never touches gameplay logic.
/// </summary>
public static class TeamMatchBuild
{
    // ==================================================================== entry points

    /// <summary>Prints both team rosters. Play mode or edit mode.</summary>
    public static void Roster()
    {
        TeamManager.LogRoster();
    }

    /// <summary>Play mode only. Dismisses the title screen (calls the existing public
    /// GameManager.StartGame - it changes no logic at all).</summary>
    public static void StartMatch()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null) { Debug.LogWarning("[TeamBuild] no GameManager (are we in play mode?)."); return; }
        gm.StartGame();
        Debug.Log("[TeamBuild] match started, state = " + gm.State);
    }

    /// <summary>Play mode only. Prints everything the rules and the AI are currently doing:
    /// the game state, both rosters with position / fieldTime / prison state, every AI's state,
    /// and both flags. This is the read-out used to prove a build does not regress.</summary>
    public static void Report()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        MatchManager m = Object.FindAnyObjectByType<MatchManager>();

        Debug.Log("[TeamBuild] clock: playing=" + EditorApplication.isPlaying +
                  " paused=" + EditorApplication.isPaused +
                  " timeScale=" + Time.timeScale +
                  " frame=" + Time.frameCount);

        if (gm == null || m == null)
        {
            Debug.LogWarning("[TeamBuild] no GameManager/MatchManager.");
            return;
        }

        Debug.Log("[TeamBuild] GameManager state=" + gm.State +
                  "  matchActive=" + m.matchActive +
                  "  TeamManager.MatchActive=" + TeamManager.MatchActive +
                  "  redsCaptured=" + m.redsCaptured + "/" + m.TotalEnemies);

        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            Team team = TeamUtil.All[t];
            var list = TeamManager.Members(team);

            for (int i = 0; i < list.Count; i++)
            {
                CharacterStatus c = list[i];
                if (c == null) { Debug.Log("[TeamBuild] " + team + "[" + i + "] DESTROYED"); continue; }

                EnemyAI ai = c.GetComponent<EnemyAI>();
                Debug.Log("[TeamBuild] " + team + "[" + i + "] " + c.name +
                          " pos=" + c.transform.position.ToString("F2") +
                          " fieldTime=" + c.fieldTime.ToString("F2") +
                          " inHomeBase=" + c.IsInHomeBase +
                          " captured=" + c.isCaptured +
                          " ai=" + (ai != null ? ai.state.ToString() : "-"));
            }
        }

        Flag a = m.FlagOf(Team.Blue);
        Flag b = m.FlagOf(Team.Red);
        Debug.Log("[TeamBuild] Flag_Blue state=" + (a != null ? a.state.ToString() : "none") +
                  " carrier=" + (a != null && a.carrier != null ? a.carrier.name : "none") +
                  "   Flag_Red state=" + (b != null ? b.state.ToString() : "none") +
                  " carrier=" + (b != null && b.carrier != null ? b.carrier.name : "none"));
    }

    /// <summary>Play mode only. Parks the human mid-field with a stale fieldTime, so the Red AI
    /// should come out and capture him. The quickest way to prove the team-based tag rule.</summary>
    public static void BaitTag(float x, float z, float fieldTimeSeconds)
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null || gm.match == null || gm.match.player == null)
        {
            Debug.LogWarning("[TeamBuild] bait: no GameManager/MatchManager/player.");
            return;
        }

        CharacterStatus p = gm.match.player;
        CharacterMotor motor = p.GetComponent<CharacterMotor>();
        if (motor != null) motor.TeleportTo(new Vector3(x, 0f, z));
        p.fieldTime = fieldTimeSeconds;

        Debug.Log("[TeamBuild] baited the player to (" + x.ToString("F1") + ", " + z.ToString("F1") +
                  ") with fieldTime=" + fieldTimeSeconds.ToString("F2") +
                  " - the Red AI should now come out and capture him.");
    }

    /// <summary>Play mode only. Reloads the scene through the existing GameManager.Restart, so a
    /// fresh 1v2 can be set up without leaving play mode.</summary>
    public static void RestartMatch()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null) { Debug.LogWarning("[TeamBuild] restart: no GameManager."); return; }
        gm.Restart();
        Debug.Log("[TeamBuild] restarted the scene.");
    }

    /// <summary>Play mode only. Puts the human at a ground spot with fieldTime reset to 0.</summary>
    public static void PlacePlayer(float x, float z)
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null || gm.match == null || gm.match.player == null)
        {
            Debug.LogWarning("[TeamBuild] place: no GameManager/MatchManager/player.");
            return;
        }

        CharacterStatus p = gm.match.player;
        CharacterMotor motor = p.GetComponent<CharacterMotor>();
        if (motor != null) motor.TeleportTo(new Vector3(x, 0f, z));
        p.fieldTime = 0f;

        Debug.Log("[TeamBuild] placed the player at (" + x.ToString("F1") + ", " + z.ToString("F1") + ")");
    }

    /// <summary>Play mode only. Switches every AI brain off (and back on), so a flag test cannot be
    /// interrupted by a capture. The AI components stay in the scene and their rosters stay intact.</summary>
    public static void FreezeAI(bool freeze)
    {
        int changed = 0;

        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            var list = TeamManager.Members(TeamUtil.All[t]);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                EnemyAI ai = list[i].GetComponent<EnemyAI>();
                if (ai == null) continue;
                ai.enabled = !freeze;
                changed++;
            }
        }

        Debug.Log("[TeamBuild] freezeAI=" + freeze + " -> " + changed + " brain(s) " + (freeze ? "off" : "on"));
    }

    /// <summary>Creates Game/Systems with the TeamManager registry on it.</summary>
    public static void Systems()
    {
        GameObject go = FindOrCreate("Game", "Systems");

        TeamManager tm = go.GetComponent<TeamManager>();
        if (tm == null) tm = go.AddComponent<TeamManager>();

        EditorUtility.SetDirty(tm);
        Save();

        Debug.Log("[TeamBuild] Game/Systems ready. TeamManager visionRadius=" + tm.visionRadius +
                  " memorySeconds=" + tm.memorySeconds + " logRoster=" + tm.logRoster);
    }

    /// <summary>
    /// Builds Game/UI/Panel_MatchSetup: the team size row, the AI skill row, the summary line,
    /// and the START / BACK buttons. Safe to run more than once - everything is found by name
    /// and updated in place.
    /// </summary>
    public static void MatchSetupPanel()
    {
        GameObject ui = GameObject.Find("Game/UI");
        if (ui == null) { Debug.LogError("[TeamBuild] Game/UI not found."); return; }

        Font font = HouseFont();
        if (font == null) { Debug.LogError("[TeamBuild] no font to copy from the existing UI."); return; }

        Color buttonColour = new Color(0.20f, 0.26f, 0.36f, 1f);
        Color labelColour = new Color(0.85f, 0.87f, 0.90f, 1f);
        Color summaryColour = new Color(0.95f, 0.72f, 0.25f, 1f);

        // ---------------------------------------------------------------- the panel itself
        GameObject panel = EnsureUi(ui.transform, "Panel_MatchSetup");

        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        if (bg == null) bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        bg.raycastTarget = true;   // swallows clicks, exactly like Panel_Pause

        MatchSetupUI script = panel.GetComponent<MatchSetupUI>();
        if (script == null) script = panel.AddComponent<MatchSetupUI>();

        // ------------------------------------------------------------------- text
        // The rect is 100 tall, not 70, and that is deliberate: a legacy Text whose line height
        // (fontSize x ~1.12) is taller than its rect is silently dropped - it renders nothing at
        // all. 64pt needs ~72 units, so a 70-unit box would have shown an empty screen.
        MakeText(panel.transform, "Title", "MATCH SETUP",
                 new Vector2(0f, 430f), new Vector2(1400f, 100f), 64, Color.white, font);

        MakeText(panel.transform, "SizeLabel", "TEAM SIZE",
                 new Vector2(0f, 322f), new Vector2(900f, 40f), 34, labelColour, font);

        // Naming the row this way is deliberate: one preset drives BOTH teams, so neither side
        // ever gets a hidden advantage.
        MakeText(panel.transform, "DifficultyLabel", "AI SKILL  (BOTH TEAMS USE THE SAME)",
                 new Vector2(0f, 62f), new Vector2(1100f, 40f), 34, labelColour, font);

        Text summary = MakeText(panel.transform, "Summary", "",
                 new Vector2(0f, -196f), new Vector2(1300f, 36f), 28, summaryColour, font);

        // Without this the screen would run perfectly and simply never show a summary line.
        script.summaryText = summary;
        EditorUtility.SetDirty(script);

        // ------------------------------------------------------- team size buttons (1..4)
        script.sizeButtons = new Button[4];
        script.sizeLabels = new Text[4];

        float[] sizeX = { -435f, -145f, 145f, 435f };
        for (int i = 0; i < 4; i++)
        {
            Button b = MakeButton(panel.transform, "SizeButton" + (i + 1), (i + 1) + "v" + (i + 1),
                                  new Vector2(sizeX[i], 196f), new Vector2(260f, 200f), 40,
                                  buttonColour, font);

            script.sizeButtons[i] = b;
            script.sizeLabels[i] = LabelOf(b);
        }

        // ---------------------------------------------------- difficulty buttons (0..2)
        script.difficultyButtons = new Button[3];
        script.difficultyLabels = new Text[3];

        string[] diffNames = { "EasyButton", "NormalButton", "HardButton" };
        string[] diffText = { "EASY", "NORMAL", "HARD" };
        float[] diffX = { -300f, 0f, 300f };

        for (int i = 0; i < 3; i++)
        {
            Button b = MakeButton(panel.transform, diffNames[i], diffText[i],
                                  new Vector2(diffX[i], -64f), new Vector2(260f, 200f), 34,
                                  buttonColour, font);

            script.difficultyButtons[i] = b;
            script.difficultyLabels[i] = LabelOf(b);
        }

        // ------------------------------------------------------------- bottom row
        Button back = MakeButton(panel.transform, "BackButton", "BACK",
                                 new Vector2(-300f, -330f), new Vector2(480f, 200f), 40,
                                 buttonColour, font);

        Button start = MakeButton(panel.transform, "StartButton", "START",
                                  new Vector2(300f, -330f), new Vector2(480f, 200f), 40,
                                  buttonColour, font);

        // ------------------------------------------------------------------- wiring
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[TeamBuild] no GameManager, so the setup screen cannot be wired.");
        }
        else
        {
            gm.panelMatchSetup = panel;
            EditorUtility.SetDirty(gm);

            // The title screen's START now opens the setup screen, so its old listener has to go -
            // otherwise the button would both show the setup screen AND start the match.
            Button titleStart = FreshButton("Game/UI/Panel_Title/StartButton");
            if (titleStart != null)
            {
                UnityEventTools.AddPersistentListener(titleStart.onClick, new UnityAction(gm.ShowMatchSetup));
            }
            else
            {
                Debug.LogWarning("[TeamBuild] Game/UI/Panel_Title/StartButton not found - " +
                                 "the title screen still starts the match directly.");
            }
        }

        script.game = gm;
        EditorUtility.SetDirty(script);

        // Rebuilt rather than added-to, so running this builder twice still leaves exactly one
        // listener on each button.
        Button freshBack = FreshButton(back.gameObject);
        UnityEventTools.AddPersistentListener(freshBack.onClick, new UnityAction(script.Back));

        Button freshStart = FreshButton(start.gameObject);
        UnityEventTools.AddPersistentListener(freshStart.onClick, new UnityAction(script.StartMatch));

        // Drawn on top of every other panel, so it can never be hidden behind the title.
        panel.transform.SetAsLastSibling();

        // GameManager owns visibility; the inspector should not carry a half-on panel.
        panel.SetActive(false);

        Save();

        Debug.Log("[TeamBuild] Panel_MatchSetup built: 4 team size buttons (260x200), " +
                  "3 difficulty buttons (260x200), BACK/START (480x200). " +
                  "Title START now opens it.");
    }

    // ======================================================================= spawner

    /// <summary>
    /// Step 4: puts the CharacterSpawner on Game/Systems, wires every reference it needs, and
    /// removes the two hand-placed Red enemies.
    ///
    /// After this the roster is built from MatchSettings when the scene loads, so choosing 4v4 on
    /// the setup screen really does put eight characters on the field - and choosing 1v1 puts
    /// exactly one AI per team opposite the human.
    /// </summary>
    public static void Spawner()
    {
        GameObject systems = GameObject.Find("Game/Systems");
        if (systems == null)
        {
            Debug.LogError("[TeamBuild] Game/Systems missing - run Systems() first.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RedEnemy.prefab");
        if (prefab == null)
        {
            Debug.LogError("[TeamBuild] Assets/Prefabs/RedEnemy.prefab not found.");
            return;
        }

        Material blueJersey = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Blue.mat");
        Material redJersey = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Red.mat");
        if (blueJersey == null || redJersey == null)
        {
            Debug.LogError("[TeamBuild] Mat_Blue / Mat_Red missing, so spawned AI would wear the " +
                           "wrong colour.");
            return;
        }

        GameObject rosterRoot = GameObject.Find("Game/Characters");
        if (rosterRoot == null)
        {
            Debug.LogError("[TeamBuild] Game/Characters missing.");
            return;
        }

        GameObject host = EnsureChild(systems.transform, "CharacterSpawner");
        CharacterSpawner spawner = host.GetComponent<CharacterSpawner>();
        if (spawner == null) spawner = host.AddComponent<CharacterSpawner>();

        spawner.aiPrefab = prefab;
        spawner.rosterRoot = rosterRoot.transform;
        spawner.blueJersey = blueJersey;
        spawner.redJersey = redJersey;

        spawner.baseBlue = ComponentOn<BaseZone>("Game/Environment/Base_Blue");
        spawner.baseRed = ComponentOn<BaseZone>("Game/Environment/Base_Red");
        spawner.flagBlue = ComponentOn<Flag>("Game/Environment/Flag_Blue");
        spawner.flagRed = ComponentOn<Flag>("Game/Environment/Flag_Red");

        EditorUtility.SetDirty(spawner);

        // The two hand-placed enemies are replaced by spawned ones. Leaving them in would put
        // three Red AI on the field at 1v1 and give the scene two more bodies than the roster.
        string[] leftovers = { "Game/Characters/Enemy_Red_1", "Game/Characters/Enemy_Red_2" };
        int removed = 0;
        for (int i = 0; i < leftovers.Length; i++)
        {
            GameObject old = GameObject.Find(leftovers[i]);
            if (old == null) continue;
            Object.DestroyImmediate(old);
            removed++;
        }

        Save();

        Debug.Log("[TeamBuild] CharacterSpawner wired on Game/Systems (prefab RedEnemy, " +
                  "Mat_Blue/Mat_Red jerseys, both bases and both flags) and " + removed +
                  " hand-placed Red enemies removed. The roster is now built at load from " +
                  "MatchSettings.");
    }

    /// <summary>A plain child GameObject, find-or-create by name.</summary>
    static GameObject EnsureChild(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found.gameObject;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    /// <summary>The component of that type on a scene object, or null with a warning.</summary>
    static T ComponentOn<T>(string path) where T : Component
    {
        GameObject go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning("[TeamBuild] missing object: " + path); return null; }

        T found = go.GetComponent<T>();
        if (found == null) Debug.LogWarning("[TeamBuild] " + path + " has no " + typeof(T).Name + ".");
        return found;
    }

    // ======================================================================= helpers

    /// <summary>Rebuilds a button's Button component from scratch and returns the new one, so the
    /// component carries EXACTLY the listeners this builder adds and nothing else.
    /// This is done instead of clearing the listeners: unregistering a persistent call leaves an
    /// empty entry behind that the event still counts and still tries to invoke, so the button
    /// would keep a dead listener. The old colour tint, transition, navigation and target graphic
    /// are copied across, so the button looks and behaves exactly as it did.</summary>
    static Button FreshButton(GameObject go)
    {
        if (go == null) return null;

        Button old = go.GetComponent<Button>();

        ColorBlock colours = old != null ? old.colors : ColorBlock.defaultColorBlock;
        Navigation navigation = old != null ? old.navigation : new Navigation();
        Selectable.Transition transition = old != null ? old.transition : Selectable.Transition.ColorTint;
        Graphic target = (old != null && old.targetGraphic != null) ? old.targetGraphic : go.GetComponent<Graphic>();

        if (old != null) Object.DestroyImmediate(old);

        Button fresh = go.AddComponent<Button>();
        fresh.targetGraphic = target;
        fresh.transition = transition;
        fresh.colors = colours;
        if (old != null) fresh.navigation = navigation;
        fresh.interactable = true;

        EditorUtility.SetDirty(fresh);
        return fresh;
    }

    /// <summary>Same, found by hierarchy path.</summary>
    static Button FreshButton(string hierarchyPath)
    {
        GameObject go = GameObject.Find(hierarchyPath);
        return FreshButton(go);
    }

    /// <summary>An existing Text component's font, so the new panel matches the rest of the UI
    /// exactly without hard-coding a font asset that may not be in the project.</summary>
    static Font HouseFont()
    {
        GameObject go = GameObject.Find("Game/UI/Panel_Title/TitleText");
        if (go != null)
        {
            Text t = go.GetComponent<Text>();
            if (t != null && t.font != null) return t.font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>Find-or-create a UI object (one that carries a RectTransform) under a parent.</summary>
    static GameObject EnsureUi(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>Centres a RectTransform: middle anchors, middle pivot, explicit size.</summary>
    static void Centre(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    /// <summary>A centred label. Returns the Text so the caller can bind it.</summary>
    static Text MakeText(Transform parent, string name, string text, Vector2 position, Vector2 size,
                         int fontSize, Color colour, Font font)
    {
        GameObject go = EnsureUi(parent, name);
        RectTransform rt = go.GetComponent<RectTransform>();
        Centre(rt, position, size);

        Text t = go.GetComponent<Text>();
        if (t == null) t = go.AddComponent<Text>();

        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = colour;
        t.raycastTarget = false;   // labels must never eat a button press

        EditorUtility.SetDirty(t);
        return t;
    }

    /// <summary>A centred button with a stretch-filled "Label" child, matching the house style
    /// (flat Image + Button with a ColourTint transition).</summary>
    static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size,
                             int fontSize, Color colour, Font font)
    {
        GameObject go = EnsureUi(parent, name);
        RectTransform rt = go.GetComponent<RectTransform>();
        Centre(rt, position, size);

        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.color = colour;
        image.raycastTarget = true;   // the button has to receive presses

        Button button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        button.targetGraphic = image;                       // so the tint actually shows
        button.transition = Selectable.Transition.ColorTint;

        // The label stretches to fill the button, so it stays centred at any button size.
        GameObject labelGo = EnsureUi(go.transform, "Label");
        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.pivot = new Vector2(0.5f, 0.5f);
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta = Vector2.zero;

        Text t = labelGo.GetComponent<Text>();
        if (t == null) t = labelGo.AddComponent<Text>();
        t.text = label;
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;

        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(t);
        return button;
    }

    /// <summary>The Text inside a button built by MakeButton.</summary>
    static Text LabelOf(Button button)
    {
        if (button == null) return null;
        Transform label = button.transform.Find("Label");
        return label != null ? label.GetComponent<Text>() : null;
    }

    /// <summary>Find childName under parentPath; create it (at the origin, unparented scale)
    /// when it is not there yet.</summary>
    static GameObject FindOrCreate(string parentPath, string childName)
    {
        GameObject parent = GameObject.Find(parentPath);
        if (parent == null) { Debug.LogError("[TeamBuild] parent not found: " + parentPath); return null; }

        Transform existing = parent.transform.Find(childName);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    static void Save()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }
}
