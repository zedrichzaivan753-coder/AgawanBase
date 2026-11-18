using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the PLAYER PROFILE screen into every map scene, and puts the PROFILE button under
/// START on the title screen.
///
/// It has to exist in all three scenes for the same reason Panel_Title and Panel_MapSelect do:
/// every map IS a scene, so whichever one is loaded must be able to show the screen. The layout
/// is written once here and applied to each scene in turn, which is what keeps the three copies
/// identical - building it by hand three times would not.
///
/// Re-running is safe: it deletes the panel and the button it made last time and builds them again.
/// Called through execute_script (method Build).
/// </summary>
public static class ProfileBuild
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/Game.unity",
        "Assets/Scenes/Map_Palengke.unity",
        "Assets/Scenes/Map_SchoolCampus.unity"
    };

    const string PanelPath = "Game/UI/Panel_Profile";
    const string TitlePanelPath = "Game/UI/Panel_Title";

    // The palette the rest of the UI already uses (see MapSelectBuild / TeamMatchBuild).
    static readonly Color ButtonNormal = new Color(0.20f, 0.26f, 0.36f, 1f);
    static readonly Color Warm = new Color(0.95f, 0.72f, 0.25f, 1f);
    static readonly Color Label = new Color(0.85f, 0.87f, 0.90f, 1f);
    static readonly Color SoftText = new Color(0.75f, 0.79f, 0.85f, 1f);
    static readonly Color FieldBack = new Color(0.12f, 0.15f, 0.20f, 1f);

    public static void Build()
    {
        for (int i = 0; i < Scenes.Length; i++)
        {
            BuildInScene(Scenes[i]);
        }

        // Leave the editor on the scene the project treats as its home.
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        AssetDatabase.SaveAssets();

        Debug.Log("[ProfileBuild] profile screen built into " + Scenes.Length + " scenes; " +
                  "editor restored to Game.unity.");
    }

    static void BuildInScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject ui = GameObject.Find("Game/UI");
        GameObject gameRoot = GameObject.Find("Game");
        GameManager game = gameRoot != null ? gameRoot.GetComponent<GameManager>() : null;

        if (ui == null || game == null)
        {
            Debug.LogError("[ProfileBuild] " + scenePath + ": no Game/UI or GameManager, skipped.");
            return;
        }

        Font font = HouseFont();
        if (font == null)
        {
            Debug.LogError("[ProfileBuild] " + scenePath + ": no font to copy from the existing UI, skipped.");
            return;
        }

        // ---------------------------------------------------------------- start clean
        GameObject previous = GameObject.Find(PanelPath);
        if (previous != null) Object.DestroyImmediate(previous);

        GameObject previousButton = GameObject.Find(TitlePanelPath + "/ProfileButton");
        if (previousButton != null) Object.DestroyImmediate(previousButton);

        // ---------------------------------------------------------------- the panel
        GameObject panel = new GameObject("Panel_Profile", typeof(RectTransform), typeof(Image),
                                          typeof(ProfileUI));
        panel.transform.SetParent(ui.transform, false);

        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = Vector2.zero;

        Image bg = panel.GetComponent<Image>();
        // Darker than the other panels on purpose: this screen is mostly small text, and at 0.75
        // the title screen behind it read through the record lines.
        bg.color = new Color(0f, 0f, 0f, 0.94f);
        bg.raycastTarget = true;   // swallows clicks, exactly like Panel_MatchSetup

        ProfileUI script = panel.GetComponent<ProfileUI>();

        // ---------------------------------------------------------------- heading and name row
        MakeText(panel.transform, "Title", "PLAYER PROFILE",
                 new Vector2(0f, 430f), new Vector2(1400f, 100f), 64, Color.white, font);

        MakeText(panel.transform, "NameLabel", "NAME  (up to " + PlayerProfile.MaxNameLength + " characters)",
                 new Vector2(0f, 305f), new Vector2(1100f, 40f), 32, Label, font);

        InputField nameField = MakeInputField(panel.transform, "NameField",
                                              new Vector2(0f, 205f), new Vector2(760f, 144f),
                                              40, font, Color.white, FieldBack);

        // The record is four short lines, so the box is sized for four: a legacy Text whose line
        // height outgrows its rect silently renders nothing (TeamMatchBuild.cs:212-216).
        Text stats = MakeMultilineText(panel.transform, "StatsText", "",
                                       new Vector2(0f, 30f), new Vector2(1400f, 170f), 30, Label, font);

        Text board = MakeMultilineText(panel.transform, "BoardText", "",
                                       new Vector2(0f, -115f), new Vector2(1400f, 210f), 28, Warm, font);

        Text status = MakeText(panel.transform, "StatusText", "",
                               new Vector2(0f, -250f), new Vector2(1400f, 40f), 26, SoftText, font);

        // ---------------------------------------------------------------- buttons
        Button save = MakeButton(panel.transform, "SaveButton", "SAVE",
                                 new Vector2(-480f, -350f), new Vector2(420f, 160f), 34,
                                 ButtonNormal, font);

        Button reset = MakeButton(panel.transform, "ResetButton", "RESET PROFILE",
                                  new Vector2(0f, -350f), new Vector2(420f, 160f), 34,
                                  new Color(0.42f, 0.22f, 0.22f, 1f), font);

        Button back = MakeButton(panel.transform, "BackButton", "BACK",
                                 new Vector2(480f, -350f), new Vector2(420f, 160f), 34,
                                 ButtonNormal, font);

        // ---------------------------------------------------------------- wiring
        script.nameField = nameField;
        script.statsText = stats;
        script.boardText = board;
        script.statusText = status;
        script.saveButton = save;
        script.resetButton = reset;
        script.backButton = back;
        script.game = game;
        EditorUtility.SetDirty(script);

        UnityEventTools.AddPersistentListener(save.onClick, new UnityAction(script.SaveName));
        UnityEventTools.AddPersistentListener(reset.onClick, new UnityAction(script.ResetPressed));
        UnityEventTools.AddPersistentListener(back.onClick, new UnityAction(script.Back));

        game.panelProfile = panel;
        game.profileUI = script;
        EditorUtility.SetDirty(game);

        // ---------------------------------------------------------------- title screen button
        BuildTitleButton(game, font);

        // Drawn on top of every other panel, so it can never be hidden behind the title.
        panel.transform.SetAsLastSibling();

        // GameManager owns visibility; the inspector should not carry a half-on panel.
        panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[ProfileBuild] " + scenePath + ": Panel_Profile built (name field, record, " +
                  "fastest-victory board, SAVE / RESET / BACK) and wired to GameManager.");
    }

    static void BuildTitleButton(GameManager game, Font font)
    {
        GameObject titlePanel = GameObject.Find(TitlePanelPath);
        if (titlePanel == null)
        {
            Debug.LogWarning("[ProfileBuild] " + TitlePanelPath + " not found - no PROFILE button.");
            return;
        }

        // Placed from the START button's own rect, so it follows whatever layout the title
        // screen happens to have instead of hard-coding coordinates that could drift.
        GameObject startGo = GameObject.Find(TitlePanelPath + "/StartButton");
        Vector2 position = new Vector2(0f, -300f);
        Vector2 size = new Vector2(480f, 140f);

        if (startGo != null)
        {
            RectTransform startRt = startGo.GetComponent<RectTransform>();
            if (startRt != null)
            {
                position = startRt.anchoredPosition;
                size = startRt.sizeDelta;
                position.y -= size.y + 24f;
            }
        }

        Button button = MakeButton(titlePanel.transform, "ProfileButton", "PROFILE",
                                   position, size, 34, ButtonNormal, font);

        UnityEventTools.AddPersistentListener(button.onClick, new UnityAction(game.ShowProfile));
        button.transform.SetAsLastSibling();

        Debug.Log("[ProfileBuild] title PROFILE button placed at y=" + position.y.ToString("0") +
                  " (under START at y=" + (position.y + size.y + 24f).ToString("0") + ").");
    }

    // ==================================================================== helpers

    static Font HouseFont()
    {
        GameObject titleText = GameObject.Find("Game/UI/Panel_Title/TitleText");
        if (titleText != null)
        {
            Text t = titleText.GetComponent<Text>();
            if (t != null && t.font != null) return t.font;
        }

        GameObject any = GameObject.Find("Game/UI/Panel_MatchSetup/Title");
        if (any != null)
        {
            Text t = any.GetComponent<Text>();
            if (t != null && t.font != null) return t.font;
        }

        return null;
    }

    static void Centre(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
    }

    static Text NewText(Transform parent, string name, string content, int fontSize, Color colour, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.color = colour;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    static Text MakeText(Transform parent, string name, string content, Vector2 position, Vector2 size,
                         int fontSize, Color colour, Font font)
    {
        Text text = NewText(parent, name, content, fontSize, colour, font);
        Centre(text.rectTransform, position, size);

        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        EditorUtility.SetDirty(text);
        return text;
    }

    static Text MakeMultilineText(Transform parent, string name, string content, Vector2 position,
                                  Vector2 size, int fontSize, Color colour, Font font)
    {
        Text text = MakeText(parent, name, content, position, size, fontSize, colour, font);
        text.alignment = TextAnchor.UpperCenter;
        return text;
    }

    static InputField MakeInputField(Transform parent, string name, Vector2 position, Vector2 size,
                                     int fontSize, Font font, Color textColour, Color backdrop)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        Centre(go.GetComponent<RectTransform>(), position, size);

        Image background = go.GetComponent<Image>();
        background.color = backdrop;
        background.raycastTarget = true;

        Text text = NewText(go.transform, "Text", "", fontSize, textColour, font);
        RectTransform trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(20f, 8f);
        trt.offsetMax = new Vector2(-20f, -8f);
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;   // single line, scrolls inside the box
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = false;

        // Full opacity, not 0.40: at 40% white on the field's #1F2633 backdrop the placeholder
        // measured 3.66:1 in the Week 14 accessibility audit. It is grey enough to read as a hint
        // at this value while still passing 4.5:1.
        Text placeholder = NewText(go.transform, "Placeholder", "YOUR NAME", fontSize,
                                   new Color(0.82f, 0.85f, 0.90f, 1f), font);
        RectTransform prt = placeholder.rectTransform;
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(20f, 8f);
        prt.offsetMax = new Vector2(-20f, -8f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.horizontalOverflow = HorizontalWrapMode.Overflow;
        placeholder.supportRichText = false;

        InputField field = go.GetComponent<InputField>();
        field.targetGraphic = background;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.lineType = InputField.LineType.SingleLine;
        field.contentType = InputField.ContentType.Standard;
        field.characterLimit = PlayerProfile.MaxNameLength;
        field.caretColor = Color.white;
        field.selectionColor = new Color(0.95f, 0.72f, 0.25f, 0.45f);

        EditorUtility.SetDirty(field);
        EditorUtility.SetDirty(background);
        return field;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 position, Vector2 size,
                             int fontSize, Color background, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Centre(go.GetComponent<RectTransform>(), position, size);

        Image image = go.GetComponent<Image>();
        image.color = background;
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        Text text = NewText(go.transform, "Label", label, fontSize, Color.white, font);
        RectTransform trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(text);
        return button;
    }
}
