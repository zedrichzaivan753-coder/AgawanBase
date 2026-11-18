using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the MAP SELECT screen into every map scene.
///
/// It is one panel with three cards, and it has to exist in all three scenes for the same reason
/// Panel_Title does: every map IS a scene, so whichever one is loaded must be able to show the
/// screen. The layout is written once here and applied to each scene in turn, which is what keeps
/// the three copies identical - building it by hand three times would not.
///
/// Re-running is safe: it deletes the panel it made last time and builds it again.
/// </summary>
public static class MapSelectBuild
{
    const string ClassicMap = "Assets/Config/Map_Classic.asset";
    const string PalengkeMap = "Assets/Config/Map_Palengke.asset";
    const string SchoolMap = "Assets/Config/Map_SchoolCampus.asset";

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

    public static void Build()
    {
        for (int i = 0; i < Scenes.Length; i++)
        {
            BuildInScene(Scenes[i]);
        }

        Debug.Log("[MapSelect] panel built into " + Scenes.Length + " scenes.");
    }

    static void BuildInScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject ui = GameObject.Find("Game/UI");
        GameObject gameRoot = GameObject.Find("Game");
        GameManager game = gameRoot != null ? gameRoot.GetComponent<GameManager>() : null;

        if (ui == null || game == null)
        {
            Debug.LogError("[MapSelect] " + scenePath + ": no Game/UI or GameManager, skipped.");
            return;
        }

        // Rebuild from scratch so a second run cannot leave two panels behind.
        Transform old = ui.transform.Find("Panel_MapSelect");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        GameObject panel = new GameObject("Panel_MapSelect", typeof(RectTransform),
                                          typeof(CanvasRenderer), typeof(Image));
        panel.layer = UiLayer;
        panel.transform.SetParent(ui.transform, false);

        Stretch(panel.GetComponent<RectTransform>());

        Image backdrop = panel.GetComponent<Image>();
        backdrop.color = PanelBackdrop;
        backdrop.raycastTarget = true;

        Text title = CreateText(panel.transform, "Title", "CHOOSE A MAP",
                                new Vector2(0f, 430f), new Vector2(1400f, 100f), 64,
                                Color.white);

        MapDefinition[] maps =
        {
            AssetDatabase.LoadAssetAtPath<MapDefinition>(ClassicMap),
            AssetDatabase.LoadAssetAtPath<MapDefinition>(PalengkeMap),
            AssetDatabase.LoadAssetAtPath<MapDefinition>(SchoolMap)
        };

        Button[] cards = new Button[maps.Length];
        Image[] previews = new Image[maps.Length];
        Text[] names = new Text[maps.Length];
        Text[] blurbs = new Text[maps.Length];

        float[] cardX = { -520f, 0f, 520f };

        for (int i = 0; i < maps.Length; i++)
        {
            string cardName = "Card_" + (maps[i] != null ? maps[i].mapId : i.ToString());
            cards[i] = CreateCard(panel.transform, cardName, new Vector2(cardX[i], 40f),
                                  out previews[i], out names[i], out blurbs[i]);

            if (maps[i] == null)
            {
                Debug.LogError("[MapSelect] " + scenePath + ": map asset " + i + " is missing.");
                continue;
            }

            // Fill the card in the asset as well as at runtime. A screen that is only correct once
            // its component has run looks broken in the editor, and in the first frame of the
            // build before OnEnable lands.
            names[i].text = maps[i].displayName;
            blurbs[i].text = maps[i].description;
            previews[i].sprite = maps[i].preview;
            previews[i].color = maps[i].preview != null ? Color.white : maps[i].accentColor;

            Image background = cards[i].GetComponent<Image>();
            if (background != null)
            {
                bool chosen = maps[i].mapId == MapSettings.MapId;
                background.color = chosen ? Color.Lerp(Color.black, Warm, 0.45f) : ButtonNormal;
            }
        }

        Text status = CreateText(panel.transform, "Status", "MAP: " + MapSettings.DisplayName.ToUpperInvariant(),
                                 new Vector2(0f, -235f), new Vector2(1400f, 40f), 30, Warm);

        Button backButton = CreateButton(panel.transform, "BackButton", "BACK",
                                         new Vector2(-300f, -360f), new Vector2(480f, 130f), 42);
        Button startButton = CreateButton(panel.transform, "StartButton", "START MATCH",
                                          new Vector2(300f, -360f), new Vector2(480f, 130f), 42);

        // The screen's own component. It wires the buttons in Awake, exactly like MatchSetupUI, so
        // there are no listeners to set up here - only references.
        MapSelectUI screen = panel.AddComponent<MapSelectUI>();
        screen.game = game;
        screen.maps = maps;
        screen.cards = cards;
        screen.previews = previews;
        screen.nameTexts = names;
        screen.blurbTexts = blurbs;
        screen.startButton = startButton;
        screen.backButton = backButton;
        screen.statusText = status;
        screen.selectedColor = Warm;
        screen.normalColor = ButtonNormal;

        // GameManager owns the screen switching, so it needs the panel.
        game.panelMapSelect = panel;

        // The title's START now opens the map list; the match setup screen's BACK goes back to it
        // (MatchSetupUI.Back already does that in code).
        GameObject titleStart = GameObject.Find("Game/UI/Panel_Title/StartButton");
        if (titleStart != null)
        {
            Button start = titleStart.GetComponent<Button>();
            if (start != null)
            {
                for (int i = start.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(start.onClick, i);
                }

                UnityEventTools.AddVoidPersistentListener(
                    start.onClick, new UnityAction(game.ShowMapSelect));

                Debug.Log("[MapSelect] " + scene.name + ": title START -> ShowMapSelect");
            }
        }

        panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("[MapSelect] " + scene.name + ": panel built, GameManager wired, scene saved.");
    }

    // --------------------------------------------------------------- the card itself

    static Button CreateCard(Transform parent, string name, Vector2 position,
                             out Image preview, out Text nameText, out Text blurb)
    {
        GameObject card = new GameObject(name, typeof(RectTransform),
                                         typeof(CanvasRenderer), typeof(Image), typeof(Button));
        card.layer = UiLayer;
        card.transform.SetParent(parent, false);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(460f, 430f);

        Image background = card.GetComponent<Image>();
        background.color = ButtonNormal;

        Button button = card.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;

        preview = CreateImage(card.transform, "Preview", new Vector2(0f, 95f), new Vector2(420f, 236f));
        nameText = CreateText(card.transform, "Name", "MAP", new Vector2(0f, -80f),
                              new Vector2(440f, 56f), 40, Color.white);
        blurb = CreateText(card.transform, "Blurb", "", new Vector2(0f, -150f),
                           new Vector2(440f, 84f), 24, SoftText);
        blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
        blurb.verticalOverflow = VerticalWrapMode.Overflow;

        return button;
    }

    // ------------------------------------------------------------------- ui helpers

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

        return text;
    }

    static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform),
                                       typeof(CanvasRenderer), typeof(Image));
        go.layer = UiLayer;
        go.transform.SetParent(parent, false);

        Place(go, position, size);

        Image image = go.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = false;

        return image;
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

    static Font builtinFont;

    static Font BuiltinFont
    {
        get
        {
            // The same built-in font every other screen uses, so the new panel matches them.
            if (builtinFont == null) builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtinFont;
        }
    }
}
