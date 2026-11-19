using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Week 17: builds the two screens a finished game needs and this one did not have.
///
/// The module's completeness bar asks for "instructions", "credits" and "exit or restart". The
/// build had restart and nothing else: the title screen's only guidance was the flavour line
/// "Galingan mo boss", no credits panel existed anywhere, and Application.Quit() lived only in the
/// profiling harness. Those three gaps are closed here, and nowhere else in the game is touched.
///
/// Everything is written into ALL THREE scenes for the same reason Panel_MapSelect is: every map IS
/// a scene, so whichever one is loaded must be able to show the screen. The layout is authored once
/// here and applied to each scene in turn, which is what keeps the three copies identical - doing it
/// by hand three times would not.
///
/// Re-running is safe: it deletes what it made last time and builds it again.
/// </summary>
public static class ReleasePanelsBuild
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Game.unity",
        "Assets/Scenes/Map_Palengke.unity",
        "Assets/Scenes/Map_SchoolCampus.unity"
    };

    // The palette the rest of the UI already uses.
    static readonly Color PanelBackdrop = new Color(0f, 0f, 0f, 0.75f);
    static readonly Color ButtonNormal = new Color(0.20f, 0.26f, 0.36f, 1f);
    static readonly Color Warm = new Color(0.95f, 0.72f, 0.25f, 1f);
    static readonly Color SoftText = new Color(0.75f, 0.79f, 0.85f, 1f);
    static readonly Color DimText = new Color(0.62f, 0.66f, 0.72f, 1f);

    // ---------------------------------------------------------------- the rules, written out

    const string HowToBody =
        "THE GOAL\n" +
        "Steal the red team's flag and carry it back into your own blue base. If you cannot steal it, " +
        "lock every enemy in prison, or lead on the clock when the three-minute limit runs out.\n\n" +
        "MOVING\n" +
        "Your left thumb on the stick, bottom left of the screen, walks your character. SPRINT, bottom " +
        "right, makes you run - but it drains the stamina bar above it, and an empty bar leaves you " +
        "slow for a moment. An arrow shows which way your character is facing.\n\n" +
        "TAGGING AND PRISONS\n" +
        "Walk into an opponent to tag them. A tag only catches if you have been out in the open longer " +
        "than they have: the freshness bars above every character show how exposed each one is. A " +
        "caught character is locked in the enemy prison on their side of the field.\n\n" +
        "RESCUING\n" +
        "Walk into your own prison to free every team-mate locked inside. A freed character comes back " +
        "at full strength and can be tagged again.\n\n" +
        "THE FLAG\n" +
        "Walk into the enemy flag to pick it up, then walk into your own base to score. If you are " +
        "caught while carrying, the flag drops where you stood, and it goes home if nobody picks it up.\n\n" +
        "HIDING\n" +
        "Bushes and stacked crates hide whoever stands inside them. A hidden body and its overhead bars " +
        "disappear completely - only your own ground ring stays, so you can always find yourself.\n\n" +
        "PAUSING AND SETTINGS\n" +
        "The pause button, top right of the field, opens RESUME, RESTART, TITLE, TAG HINTS on or off, " +
        "and SOUND on or off. Both settings are remembered the next time you play.";

    public static void Build()
    {
        for (int i = 0; i < Scenes.Length; i++)
        {
            BuildInScene(Scenes[i]);
        }

        Debug.Log("[Release] how-to, credits, version label and title buttons built into " +
                  Scenes.Length + " scenes.");
    }

    // ---------------------------------------------------------------- one scene

    static void BuildInScene(string scenePath)
    {
        EditorSceneManager.SaveOpenScenes();
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Found by type rather than by path, because the three scenes are clones and the root
        // object's name is the one thing a future rebuild is most likely to change.
        GameManager game = Object.FindAnyObjectByType<GameManager>();
        if (game == null || game.panelTitle == null)
        {
            Debug.LogError("[Release] " + scenePath + ": no GameManager or panelTitle, skipped.");
            return;
        }

        Transform title = game.panelTitle.transform;
        Transform ui = title.parent;

        // Rebuild from scratch so a second run cannot leave two panels or two button rows behind.
        RemoveChild(ui, "Panel_HowTo");
        RemoveChild(ui, "Panel_Credits");
        RemoveChild(title, "HowToButton");
        RemoveChild(title, "CreditsButton");
        RemoveChild(title, "ExitButton");
        RemoveChild(title, "VersionLabel");

        GameObject howToPanel = BuildHowTo(ui);
        GameObject creditsPanel = BuildCredits(ui);
        BuildTitleControls(title);

        // GameManager owns the screen switching, so it needs both panels and the caption.
        game.panelHowTo = howToPanel;
        game.panelCredits = creditsPanel;
        game.versionLabel = title.Find("VersionLabel").GetComponent<Text>();

        // The credits build line is filled at runtime for the same reason as the title caption: baked
        // into the scene it freezes at whatever version was current when this builder last ran.
        Transform buildLine = creditsPanel.transform.Find("BuildLine");
        game.creditsVersionLabel = buildLine != null ? buildLine.GetComponent<Text>() : null;
        if (game.creditsVersionLabel == null)
            Debug.LogWarning("[Release] " + scene.name + ": credits BuildLine not found - the credits " +
                             "screen will show the version baked at build time.");

        howToPanel.SetActive(false);
        creditsPanel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("[Release] " + scene.name + ": Panel_HowTo, Panel_Credits, VersionLabel and the " +
                  "three title buttons built, GameManager wired, scene saved.");
    }

    // ---------------------------------------------------------------- the title controls

    static void BuildTitleControls(Transform title)
    {
        GameManager game = Object.FindAnyObjectByType<GameManager>();

        // The whole title screen is flowed from the TOP edge, in one column, in the order the eye
        // reads it. That is the only arrangement that survives every aspect ratio.
        //
        // The canvas is not always 1080 canvas px tall: it is 1031 at the editor's 16:9.4 aspect and
        // about 966 on a 20:9 phone. The screen as it stood used centre-anchored buttons, so the gap
        // between them grew and shrank with the screen - at the editor's aspect RulesText sat under
        // the PROFILE button and was completely invisible, and the first version of this builder put
        // the new CREDITS button through the same button. Anchoring every row to the top keeps the
        // offsets identical everywhere, and the stack now ends at 894 px from the top, which clears
        // the 966 px a 20:9 phone gives it.
        PlaceTop(title, "TitleText", 80f, 1500f, 160f);
        PlaceTop(title, "SubtitleText", 250f, 1500f, 80f);
        PlaceTop(title, "RulesText", 350f, 1500f, 50f);
        PlaceTop(title, "StartButton", 430f, 480f, 144f);
        PlaceTop(title, "ProfileButton", 590f, 480f, 144f);

        // 440x144 stays above the 144 canvas-px touch floor A11yAudit enforces.
        Button howTo = CreateButtonTop(title, "HowToButton", "HOW TO PLAY",
                                       -480f, 750f, 440f, 144f, 32);
        Button credits = CreateButtonTop(title, "CreditsButton", "CREDITS",
                                         0f, 750f, 440f, 144f, 32);
        Button exit = CreateButtonTop(title, "ExitButton", "EXIT",
                                      480f, 750f, 440f, 144f, 32);

        // Top-left corner, where nothing else sits: the running build identifies itself from a
        // screenshot without anyone having to open the APK.
        GameObject versionGo = new GameObject("VersionLabel", typeof(RectTransform),
                                              typeof(CanvasRenderer), typeof(Text));
        versionGo.layer = UiLayer;
        versionGo.transform.SetParent(title, false);

        RectTransform vrect = versionGo.GetComponent<RectTransform>();
        vrect.anchorMin = new Vector2(0f, 1f);
        vrect.anchorMax = new Vector2(0f, 1f);
        vrect.pivot = new Vector2(0f, 1f);
        vrect.anchoredPosition = new Vector2(30f, -30f);
        vrect.sizeDelta = new Vector2(420f, 44f);

        Text version = versionGo.GetComponent<Text>();
        version.text = ReleaseInfo.BuildId;
        version.font = BuiltinFont;
        version.fontSize = 24;
        version.color = DimText;
        version.alignment = TextAnchor.MiddleLeft;
        version.raycastTarget = false;
        version.horizontalOverflow = HorizontalWrapMode.Overflow;
        version.verticalOverflow = VerticalWrapMode.Overflow;

        UnityEventTools.AddVoidPersistentListener(howTo.onClick, new UnityAction(game.ShowHowTo));
        UnityEventTools.AddVoidPersistentListener(credits.onClick, new UnityAction(game.ShowCredits));
        UnityEventTools.AddVoidPersistentListener(exit.onClick, new UnityAction(game.QuitGame));

        Debug.Log("[Release] title buttons wired: HOW TO PLAY -> ShowHowTo, CREDITS -> ShowCredits, " +
                  "EXIT -> QuitGame.");
    }

    // ---------------------------------------------------------------- how to play

    static GameObject BuildHowTo(Transform ui)
    {
        GameObject panel = CreatePanel(ui, "Panel_HowTo");

        CreateText(panel.transform, "Title", "HOW TO PLAY",
                   new Vector2(0f, 430f), new Vector2(1400f, 90f), 56, Color.white);

        // The rules are the longest string in the game and they are drawn top-down from the top
        // edge of their box, so a box that is too short does not clip them - it pushes them out of
        // the bottom, behind the BACK button. The box therefore stops above the button with room to
        // spare on a 20:9 phone, where the canvas is only 966 canvas px tall instead of 1031 and the
        // button's top edge moves up by about 32 px, and the font size is then measured against it.
        Text body = CreateText(panel.transform, "Body", HowToBody,
                               new Vector2(0f, 30f), new Vector2(1560f, 600f), 26, SoftText);
        body.alignment = TextAnchor.UpperLeft;
        FitText(body, 600f, 18);

        Button back = CreateButtonAnchored(panel.transform, "BackButton", "BACK",
                                           new Vector2(0f, 40f), new Vector2(480f, 144f), 40);

        GameManager game = Object.FindAnyObjectByType<GameManager>();
        UnityEventTools.AddVoidPersistentListener(back.onClick, new UnityAction(game.CloseOverlay));

        return panel;
    }

    // ---------------------------------------------------------------- credits

    static GameObject BuildCredits(Transform ui)
    {
        GameObject panel = CreatePanel(ui, "Panel_Credits");

        CreateText(panel.transform, "Title", "CREDITS",
                   new Vector2(0f, 430f), new Vector2(1400f, 90f), 56, Color.white);

        CreateText(panel.transform, "BuildLine",
                   ReleaseInfo.Title + "  -  " + ReleaseInfo.BuildId,
                   new Vector2(0f, 340f), new Vector2(1500f, 48f), 30, Warm);

        CreateText(panel.transform, "AuthorLine",
                   ReleaseInfo.Author + "   |   " + ReleaseInfo.Course,
                   new Vector2(0f, 286f), new Vector2(1500f, 44f), 24, SoftText);

        CreateText(panel.transform, "ThemeLine",
                   "Theme: " + ReleaseInfo.Theme + "   |   " + ReleaseInfo.Engine,
                   new Vector2(0f, 240f), new Vector2(1500f, 44f), 24, DimText);

        Text licences = CreateText(panel.transform, "LicenceText", ReleaseInfo.Licences,
                                   new Vector2(0f, -40f), new Vector2(1500f, 500f), 24, SoftText);
        licences.alignment = TextAnchor.UpperLeft;

        Button back = CreateButtonAnchored(panel.transform, "BackButton", "BACK",
                                           new Vector2(0f, 40f), new Vector2(480f, 144f), 40);

        GameManager game = Object.FindAnyObjectByType<GameManager>();
        UnityEventTools.AddVoidPersistentListener(back.onClick, new UnityAction(game.CloseOverlay));

        return panel;
    }

    // ------------------------------------------------------------------- ui helpers

    static GameObject CreatePanel(Transform ui, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform),
                                          typeof(CanvasRenderer), typeof(Image));
        panel.layer = UiLayer;
        panel.transform.SetParent(ui, false);
        Stretch(panel.GetComponent<RectTransform>());

        Image backdrop = panel.GetComponent<Image>();
        backdrop.color = PanelBackdrop;
        backdrop.raycastTarget = true;

        return panel;
    }

    static void RemoveChild(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) Object.DestroyImmediate(old.gameObject);
    }

    const int UiLayerValue = 5;

    static int UiLayer
    {
        get
        {
            int layer = LayerMask.NameToLayer("UI");
            return layer >= 0 ? layer : UiLayerValue;
        }
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    static RectTransform Place(GameObject go, Vector2 position, Vector2 size)
    {
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    static Text CreateText(Transform parent, string name, string content, Vector2 position,
                           Vector2 size, int fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
                                       typeof(CanvasRenderer), typeof(Text));
        go.layer = UiLayer;
        go.transform.SetParent(parent, false);

        Place(go, position, size);

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = BuiltinFont;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1f;

        return text;
    }

    /// <summary>
    /// Shrinks a paragraph until it fits the box it was given, and says in the console what it did.
    ///
    /// The first build of this panel guessed at 26 px and the measured text turned out to need
    /// 734.5 px in a 660 px box, so the last seventy-odd canvas units of the rules rendered behind
    /// the BACK button. Measuring here instead of guessing means the panel cannot regress that way
    /// if the rule text is ever edited again.
    /// </summary>
    static void FitText(Text text, float maxHeight, int minFontSize)
    {
        int size = text.fontSize;
        while (size > minFontSize && text.preferredHeight > maxHeight)
        {
            size--;
            text.fontSize = size;
        }

        if (text.preferredHeight > maxHeight)
        {
            Debug.LogWarning("[Release] " + text.name + " needs " + text.preferredHeight +
                             " px at font " + size + " but its box is " + maxHeight +
                             " px - it will overflow.");
        }
        else
        {
            Debug.Log("[Release] " + text.name + " fitted at font " + size + ": " +
                      text.preferredHeight + " px inside a " + maxHeight + " px box.");
        }
    }

    static Button CreateButton(Transform parent, string name, string label,
                               Vector2 position, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
                                       typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = UiLayer;
        go.transform.SetParent(parent, false);

        Place(go, position, size);

        Image background = go.GetComponent<Image>();
        background.color = ButtonNormal;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        Text text = CreateText(go.transform, "Label", label, Vector2.zero, size, fontSize, Color.white);
        Stretch(text.GetComponent<RectTransform>());

        return button;
    }

    /// <summary>
    /// Pins an existing element to the top edge at a fixed offset, so the title screen's rows keep
    /// their spacing whatever height the canvas ends up with.
    /// </summary>
    static void PlaceTop(Transform title, string name, float offsetFromTop, float width, float height)
    {
        Transform found = title.Find(name);
        if (found == null)
        {
            Debug.LogWarning("[Release] " + name + " is missing from Panel_Title - skipped.");
            return;
        }

        RectTransform rect = found.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -offsetFromTop);
        rect.sizeDelta = new Vector2(width, height);
    }

    /// <summary>Button pinned to the top edge, used for the row of three new title controls.</summary>
    static Button CreateButtonTop(Transform parent, string name, string label,
                                  float x, float offsetFromTop, float width, float height, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
                                       typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = UiLayer;
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -offsetFromTop);
        rect.sizeDelta = new Vector2(width, height);

        Image background = go.GetComponent<Image>();
        background.color = ButtonNormal;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        Text text = CreateText(go.transform, "Label", label, Vector2.zero,
                               new Vector2(width, height), fontSize, Color.white);
        Stretch(text.GetComponent<RectTransform>());

        return button;
    }

    /// <summary>Same button, but pinned to the bottom edge of its parent with a fixed margin.</summary>
    static Button CreateButtonAnchored(Transform parent, string name, string label,
                                       Vector2 marginFromBottom, Vector2 size, int fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
                                       typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = UiLayer;
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = marginFromBottom;
        rect.sizeDelta = size;

        Image background = go.GetComponent<Image>();
        background.color = ButtonNormal;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        Text text = CreateText(go.transform, "Label", label, Vector2.zero, size, fontSize, Color.white);
        Stretch(text.GetComponent<RectTransform>());

        return button;
    }

    static Font builtinFont;

    static Font BuiltinFont
    {
        get
        {
            // The same built-in font every other screen uses, so the new panels match them.
            if (builtinFont == null) builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtinFont;
        }
    }
}
