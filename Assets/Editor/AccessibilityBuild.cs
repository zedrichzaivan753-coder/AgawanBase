using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Week 14: applies what the accessibility audit (A11yAudit) measured as failing, to all three
/// scenes at once.
///
/// It fixes four things and nothing else:
///   1. touch targets   - every Button at least 144 canvas px tall (48 dp at 1080p / xxhdpi), with
///                        the column it sits in re-spaced so growing it cannot overlap its
///                        neighbour, and the profile name field raised to the same floor.
///   2. contrast        - the chosen setup button becomes a DARK amber (the MapSelectUI rule) so
///                        its white label goes from 1.80:1 to 7.3:1; the map blurbs and the name
///                        field placeholder are brightened past 4.5:1.
///   3. debug flags     - the six serialized debugging aids are switched off, so the build a
///                        playtester receives is the real game.
///   4. HUD read-out    - handled in code (HudController.showAiDebug is editor-only now); the
///                        scene value is switched off here as well.
///
/// Re-running is safe: every fix is written from a fixed rule, so a second run produces the same
/// scene. Called through Tools > Playtest > Apply Accessibility Fixes, or execute_script (Build).
/// </summary>
public static class AccessibilityBuild
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Game.unity",
        "Assets/Scenes/Map_Palengke.unity",
        "Assets/Scenes/Map_SchoolCampus.unity"
    };

    const float MinTouch = 144f;
    const float ColumnGap = 24f;

    static readonly Color SelectedAmber = new Color(0.95f, 0.72f, 0.25f, 1f);
    static readonly Color NormalBlue = new Color(0.20f, 0.26f, 0.36f, 1f);
    static readonly Color BrightBlurb = new Color(0.93f, 0.95f, 0.98f, 1f);
    static readonly Color BrightPlaceholder = new Color(0.82f, 0.85f, 0.90f, 1f);

    // Every control this run made taller. Only columns that contain one of these get re-spaced,
    // so a column that was already the right size is left exactly where the artist put it.
    static readonly List<RectTransform> resized = new List<RectTransform>();

    [MenuItem("Tools/Playtest/Apply Accessibility Fixes")]
    public static void Build()
    {
        for (int i = 0; i < Scenes.Length; i++)
        {
            ApplyScene(Scenes[i]);
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        AssetDatabase.SaveAssets();

        Debug.Log("[A11y] accessibility fixes applied to " + Scenes.Length + " scenes; " +
                  "editor restored to Game.unity.");
    }

    static void ApplyScene(string scenePath)
    {
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject ui = GameObject.Find("Game/UI");
        if (ui == null)
        {
            Debug.LogError("[A11y] " + scenePath + ": no Game/UI, skipped.");
            return;
        }

        int grown = GrowTouchTargets(ui);
        int restored = RestoreMatchSetupNav(ui);
        int moved = RespaceColumns(ui);
        int colours = FixColours(ui);
        int flags = SilenceDebugFlags(ui);
        int feedback = BuildFeedbackLayer(ui);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[A11y] " + scenePath + ": " + grown + " controls raised to " + MinTouch +
                  " px, " + restored + " nav buttons restored, " + moved +
                  " buttons re-spaced, " + colours + " colour fixes, " +
                  flags + " debug flags switched off, " + feedback + " feedback objects.");
    }

    // ---------------------------------------------------------------- touch targets

    static int GrowTouchTargets(GameObject ui)
    {
        resized.Clear();
        Button[] buttons = ui.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform rt = buttons[i].GetComponent<RectTransform>();
            if (rt.rect.height >= MinTouch - 0.5f) continue;

            // sizeDelta is the authored box: every button in this UI is centre- or corner-anchored
            // with a fixed size, so this is the number the layout inspector shows.
            Vector2 size = rt.sizeDelta;
            rt.sizeDelta = new Vector2(size.x, MinTouch);
            resized.Add(rt);

            Debug.Log("[A11y] touch target " + rt.name + ": " + size.y.ToString("0") +
                      " -> " + MinTouch.ToString("0") + " px tall");
        }

        // The profile name field is the one input on the touch path and was 100 px tall.
        GameObject field = GameObject.Find("Game/UI/Panel_Profile/NameField");
        if (field != null)
        {
            RectTransform rt = field.GetComponent<RectTransform>();
            if (rt != null && rt.sizeDelta.y < MinTouch)
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, MinTouch);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 205f);
                resized.Add(rt);

                // The record below it moves down to keep the 24 px rhythm, not to touch the field.
                GameObject stats = GameObject.Find("Game/UI/Panel_Profile/StatsText");
                if (stats != null)
                {
                    RectTransform srt = stats.GetComponent<RectTransform>();
                    if (srt != null) srt.anchoredPosition = new Vector2(srt.anchoredPosition.x, 30f);
                }

                Debug.Log("[A11y] profile name field raised to " + MinTouch +
                          " px tall; the record moved down to keep its gap.");
            }
        }

        return resized.Count;
    }

    /// <summary>
    /// The setup screen's BACK and START sit in their own row under the difficulty buttons, at the
    /// same x as the Easy button. The first run of this script treated them as a column with it and
    /// pulled them up to y -288, which put them under the difficulty row. They are not touch-target
    /// failures (they are 144 px tall), so their authored y is put back here.
    /// </summary>
    static int RestoreMatchSetupNav(GameObject ui)
    {
        int restored = 0;
        string[] names = { "BackButton", "StartButton" };

        for (int i = 0; i < names.Length; i++)
        {
            GameObject nav = GameObject.Find("Game/UI/Panel_MatchSetup/" + names[i]);
            if (nav == null) continue;

            RectTransform rt = nav.GetComponent<RectTransform>();
            if (rt == null || Mathf.Abs(rt.anchoredPosition.y + 330f) < 0.5f) continue;

            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -330f);
            restored++;

            Debug.Log("[A11y] restored " + names[i] + " to its authored row at y -330");
        }

        return restored;
    }

    /// <summary>
    /// Growing a button can make it overlap the one above or below it. For each column of buttons
    /// that actually changed, the column is re-stacked from its top button downwards with a fixed
    /// 24 px gap - the gap the title screen already used.
    /// </summary>
    static int RespaceColumns(GameObject ui)
    {
        // group by parent, then by x
        Dictionary<Transform, List<RectTransform>> byParent = new Dictionary<Transform, List<RectTransform>>();
        Button[] buttons = ui.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform rt = buttons[i].GetComponent<RectTransform>();
            Transform parent = rt.parent;
            if (parent == null) continue;

            if (!byParent.ContainsKey(parent)) byParent[parent] = new List<RectTransform>();
            byParent[parent].Add(rt);
        }

        int moved = 0;

        foreach (KeyValuePair<Transform, List<RectTransform>> pair in byParent)
        {
            List<RectTransform> list = pair.Value;
            if (list.Count < 2) continue;

            Dictionary<float, List<RectTransform>> columns = new Dictionary<float, List<RectTransform>>();
            for (int i = 0; i < list.Count; i++)
            {
                float x = Mathf.Round(list[i].anchoredPosition.x);
                if (!columns.ContainsKey(x)) columns[x] = new List<RectTransform>();
                columns[x].Add(list[i]);
            }

            foreach (KeyValuePair<float, List<RectTransform>> column in columns)
            {
                List<RectTransform> stack = column.Value;
                if (stack.Count < 2) continue;

                // Only a column that contains a control this run made taller is re-spaced. A column
                // that was always the right size keeps the spacing it was authored with.
                bool touched = false;
                for (int i = 0; i < stack.Count; i++)
                {
                    if (resized.Contains(stack[i])) { touched = true; break; }
                }
                if (!touched) continue;

                stack.Sort(delegate (RectTransform a, RectTransform b)
                {
                    return b.anchoredPosition.y.CompareTo(a.anchoredPosition.y);
                });

                for (int i = 1; i < stack.Count; i++)
                {
                    RectTransform above = stack[i - 1];
                    RectTransform below = stack[i];

                    float wanted = above.anchoredPosition.y -
                                   (above.sizeDelta.y * 0.5f) - ColumnGap - (below.sizeDelta.y * 0.5f);

                    if (Mathf.Abs(below.anchoredPosition.y - wanted) < 0.5f) continue;

                    float was = below.anchoredPosition.y;
                    below.anchoredPosition = new Vector2(below.anchoredPosition.x, wanted);
                    moved++;

                    Debug.Log("[A11y] re-spaced " + below.name + " under " + above.name +
                              ": y " + was.ToString("0") + " -> " + wanted.ToString("0"));
                }
            }
        }

        return moved;
    }

    // ---------------------------------------------------------------- colour

    static int FixColours(GameObject ui)
    {
        int fixes = 0;

        // 1. Match Setup: the chosen option, painted with the same rule MatchSetupUI.Paint uses.
        //    The component sits on the panel, not on Game/UI, so it has to be searched for.
        MatchSetupUI setup = ui.GetComponentInChildren<MatchSetupUI>(true);
        if (setup != null)
        {
            Color chosen = Color.Lerp(Color.black, SelectedAmber, 0.45f);

            if (setup.sizeButtons != null)
            {
                for (int i = 0; i < setup.sizeButtons.Length; i++)
                {
                    if (setup.sizeButtons[i] == null) continue;
                    bool isChosen = (i + 1) == MatchSettings.TeamSize;
                    fixes += Paint(setup.sizeButtons[i], isChosen ? chosen : NormalBlue);
                }
            }

            if (setup.difficultyButtons != null)
            {
                for (int i = 0; i < setup.difficultyButtons.Length; i++)
                {
                    if (setup.difficultyButtons[i] == null) continue;
                    bool isChosen = i == (int)MatchSettings.Difficulty;
                    fixes += Paint(setup.difficultyButtons[i], isChosen ? chosen : NormalBlue);
                }
            }
        }

        // 2. Map Select: the blurbs sit on a translucent amber card and measured 4.36:1.
        MapSelectUI select = ui.GetComponentInChildren<MapSelectUI>(true);
        if (select != null && select.blurbTexts != null)
        {
            for (int i = 0; i < select.blurbTexts.Length; i++)
            {
                if (select.blurbTexts[i] == null) continue;
                if (select.blurbTexts[i].color == BrightBlurb) continue;
                select.blurbTexts[i].color = BrightBlurb;
                EditorUtility.SetDirty(select.blurbTexts[i]);
                fixes++;
            }
        }

        // 3. Profile: the name field's placeholder was 40% white on a dark field (3.66:1).
        GameObject placeholder = GameObject.Find("Game/UI/Panel_Profile/NameField/Placeholder");
        if (placeholder != null)
        {
            Text text = placeholder.GetComponent<Text>();
            if (text != null && text.color != BrightPlaceholder)
            {
                text.color = BrightPlaceholder;
                EditorUtility.SetDirty(text);
                fixes++;
            }
        }

        return fixes;
    }

    static int Paint(Button button, Color colour)
    {
        Image image = button.GetComponent<Image>();
        if (image == null || image.color == colour) return 0;

        image.color = colour;
        EditorUtility.SetDirty(image);
        return 1;
    }

    // ---------------------------------------------------------------- debug flags

    static int SilenceDebugFlags(GameObject ui)
    {
        int off = 0;

        HudController hud = ui.GetComponentInChildren<HudController>(true);
        if (hud != null && hud.showAiDebug)
        {
            hud.showAiDebug = false;
            EditorUtility.SetDirty(hud);
            off++;
        }

        FollowCamera[] cameras = Object.FindObjectsByType<FollowCamera>(FindObjectsInactive.Include);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (!cameras[i].logView) continue;
            cameras[i].logView = false;
            EditorUtility.SetDirty(cameras[i]);
            off++;
        }

        MatchManager[] matches = Object.FindObjectsByType<MatchManager>(FindObjectsInactive.Include);
        for (int i = 0; i < matches.Length; i++)
        {
            bool changed = false;
            if (matches[i].logRules) { matches[i].logRules = false; changed = true; }
            if (matches[i].allowDebugDrop) { matches[i].allowDebugDrop = false; changed = true; }
            if (changed) { EditorUtility.SetDirty(matches[i]); off++; }
        }

        CharacterSpawner[] spawners = Object.FindObjectsByType<CharacterSpawner>(FindObjectsInactive.Include);
        for (int i = 0; i < spawners.Length; i++)
        {
            if (!spawners[i].logRoster) continue;
            spawners[i].logRoster = false;
            EditorUtility.SetDirty(spawners[i]);
            off++;
        }

        Flag[] flags = Object.FindObjectsByType<Flag>(FindObjectsInactive.Include);
        for (int i = 0; i < flags.Length; i++)
        {
            if (!flags[i].logFlagEvents) continue;
            flags[i].logFlagEvents = false;
            EditorUtility.SetDirty(flags[i]);
            off++;
        }

        return off;
    }

    // ---------------------------------------------------------------- feedback layer

    /// <summary>
    /// Adds the one thing the game had none of before Week 14: feedback you can hear.
    ///
    /// It creates <c>Game/Audio</c> (AudioSource + GameFeedback, the five clips assigned) and the
    /// SOUND button on the pause panel, and wires the button to GameFeedback.ToggleSound. Both are
    /// deleted first if a previous run made them, so this stays re-runnable.
    /// </summary>
    static int BuildFeedbackLayer(GameObject ui)
    {
        int actions = 0;

        GameObject previousAudio = GameObject.Find("Game/Audio");
        if (previousAudio != null) { Object.DestroyImmediate(previousAudio); actions++; }

        GameObject gameRoot = GameObject.Find("Game");
        if (gameRoot == null)
        {
            Debug.LogError("[A11y] no Game root - feedback layer skipped.");
            return actions;
        }

        GameObject audio = new GameObject("Audio", typeof(AudioSource), typeof(GameFeedback));
        audio.transform.SetParent(gameRoot.transform, false);

        GameFeedback feedback = audio.GetComponent<GameFeedback>();
        feedback.catchClip = Clip("Catch");
        feedback.rescueClip = Clip("Rescue");
        feedback.flagPickupClip = Clip("FlagPickup");
        feedback.victoryClip = Clip("Victory");
        feedback.defeatClip = Clip("Defeat");
        EditorUtility.SetDirty(feedback);
        actions++;

        GameObject pause = GameObject.Find("Game/UI/Panel_Pause");
        if (pause == null)
        {
            Debug.LogWarning("[A11y] no Panel_Pause - the SOUND button was not placed.");
            return actions;
        }

        GameObject previousButton = GameObject.Find("Game/UI/Panel_Pause/SoundButton");
        if (previousButton != null) { Object.DestroyImmediate(previousButton); actions++; }

        Button button = MakeButton(pause.transform, "SoundButton", 26, NormalBlue);

        // Top-right corner of the pause panel: out of the way of the four-row column, and still
        // inside the 144 px touch-target floor the audit measures.
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.one;
        rt.anchorMax = Vector2.one;
        rt.pivot = Vector2.one;
        rt.anchoredPosition = new Vector2(-40f, -40f);
        rt.sizeDelta = new Vector2(240f, 144f);

        UnityEventTools.AddPersistentListener(button.onClick, new UnityAction(feedback.ToggleSound));

        Text label = button.GetComponentInChildren<Text>(true);
        feedback.soundButtonLabel = label;
        feedback.RefreshLabel();

        EditorUtility.SetDirty(feedback);
        EditorUtility.SetDirty(button);
        if (label != null) EditorUtility.SetDirty(label);

        Debug.Log("[A11y] feedback layer: Game/Audio + SOUND button on the pause panel " +
                  "(label reads \"" + (label != null ? label.text : "?") + "\")");

        return actions;
    }

    static AudioClip Clip(string name)
    {
        string path = "Assets/Audio/Generated/" + name + ".mp3";
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogError("[A11y] missing clip: " + path);
        return clip;
    }

    static Font HouseFont()
    {
        GameObject titleText = GameObject.Find("Game/UI/Panel_Title/TitleText");
        if (titleText != null)
        {
            Text t = titleText.GetComponent<Text>();
            if (t != null && t.font != null) return t.font;
        }

        return null;
    }

    static Button MakeButton(Transform parent, string name, int fontSize, Color background)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = background;
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(go.transform, false);

        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        Text label = labelGo.GetComponent<Text>();
        label.text = "SOUND";
        label.font = HouseFont();
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        return button;
    }
}
