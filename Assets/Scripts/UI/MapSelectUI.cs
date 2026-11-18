using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the MAP SELECT screen: three cards, one per map, then START MATCH or BACK.
///
/// It follows the same rule as MatchSetupUI: it wires its own buttons in Awake, so nothing has to
/// be hooked up by hand in the Inspector, and it never switches screens itself - it stores the
/// choice in MapSettings and asks GameManager, which stays the only thing that owns the game
/// states. The card list is filled from MapDefinition assets, so a fourth map is one more asset and
/// one more card, with no code change here.
/// </summary>
public class MapSelectUI : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("The screen switcher. Set by the panel builder.")]
    public GameManager game;

    [Header("Maps, in card order")]
    public MapDefinition[] maps = new MapDefinition[3];

    [Header("Cards - one per map, in the same order as Maps")]
    public Button[] cards = new Button[3];

    [Tooltip("The preview picture inside each card.")]
    public Image[] previews = new Image[3];

    [Tooltip("The map name inside each card.")]
    public Text[] nameTexts = new Text[3];

    [Tooltip("The one-line description inside each card.")]
    public Text[] blurbTexts = new Text[3];

    [Header("Buttons")]
    public Button startButton;
    public Button backButton;

    [Header("Text")]
    [Tooltip("The line under the cards naming the chosen map.")]
    public Text statusText;

    [Header("Colours")]
    [Tooltip("The card that is currently chosen.")]
    public Color selectedColor = new Color(0.95f, 0.72f, 0.25f, 1f);

    [Tooltip("Every card that is not chosen.")]
    public Color normalColor = new Color(0.20f, 0.26f, 0.36f, 1f);

    /// <summary>Index into <see cref="maps"/> of the card the player is pointing at.</summary>
    int selected;

    void Awake()
    {
        // One listener per card, each carrying its OWN index. Copying the index into a local
        // first matters: a listener closing over the loop variable would give every card the
        // last index once the loop had finished.
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;

                int index = i;
                cards[i].onClick.AddListener(delegate { Select(index); });
            }
        }

        if (startButton != null) startButton.onClick.AddListener(StartMatch);
        if (backButton != null) backButton.onClick.AddListener(Back);
    }

    void OnEnable()
    {
        // The screen re-reads the remembered map every time it is opened.
        selected = IndexOfStoredMap();
        Refresh();
    }

    // ------------------------------------------------------------------ button handlers

    /// <summary>A card was pressed: choose that map and repaint, without starting anything.</summary>
    public void Select(int index)
    {
        if (maps == null || index < 0 || index >= maps.Length || maps[index] == null) return;

        selected = index;
        Refresh();
    }

    /// <summary>
    /// START MATCH: remember the map, then let GameManager load it. GameManager decides whether
    /// that means reloading this scene or simply moving on to the match setup screen.
    /// </summary>
    public void StartMatch()
    {
        if (game == null)
        {
            Debug.LogWarning("MapSelectUI: 'game' is not assigned, so START MATCH does nothing.");
            return;
        }

        if (maps == null || selected < 0 || selected >= maps.Length || maps[selected] == null)
        {
            Debug.LogWarning("MapSelectUI: no map is chosen, so START MATCH does nothing.");
            return;
        }

        MapDefinition map = maps[selected];

        MapSettings.Select(map);
        game.LoadMap(map);
    }

    /// <summary>BACK: return to the title screen.</summary>
    public void Back()
    {
        if (game != null) game.ShowTitle();
        else Debug.LogWarning("MapSelectUI: 'game' is not assigned, so BACK does nothing.");
    }

    // ------------------------------------------------------------------------- display

    /// <summary>Repaints every card and the status line from the current choice.</summary>
    public void Refresh()
    {
        if (maps == null) return;

        for (int i = 0; i < maps.Length; i++)
        {
            MapDefinition map = maps[i];

            if (cards != null && i < cards.Length) Paint(cards[i], i == selected);

            if (map == null) continue;

            if (nameTexts != null && i < nameTexts.Length && nameTexts[i] != null)
            {
                nameTexts[i].text = map.displayName;
            }

            if (blurbTexts != null && i < blurbTexts.Length && blurbTexts[i] != null)
            {
                blurbTexts[i].text = map.description;
            }

            if (previews != null && i < previews.Length && previews[i] != null)
            {
                // A map with no captured preview still gets a solid card in its own accent colour,
                // so a missing picture can never leave a hole in the screen.
                previews[i].sprite = map.preview;
                previews[i].color = map.preview != null ? Color.white : map.accentColor;
            }
        }

        if (statusText != null)
        {
            MapDefinition chosen = (selected >= 0 && selected < maps.Length) ? maps[selected] : null;

            statusText.text = chosen != null
                ? "MAP: " + chosen.displayName.ToUpperInvariant() + "     press START MATCH"
                : "Choose a map";
        }
    }

    /// <summary>Tints one card so the chosen map is obvious.</summary>
    void Paint(Button card, bool chosen)
    {
        if (card == null) return;

        Image image = card.GetComponent<Image>();
        if (image == null) return;

        // The chosen card is a DARK amber, not a bright one: white names and pale descriptions
        // have to stay readable on it.
        image.color = chosen ? Color.Lerp(Color.black, selectedColor, 0.45f) : normalColor;
    }

    /// <summary>The card that matches the map already remembered, or the first map.</summary>
    int IndexOfStoredMap()
    {
        if (maps == null || maps.Length == 0) return 0;

        string stored = MapSettings.MapId;

        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] != null && maps[i].mapId == stored) return i;
        }

        return 0;
    }
}
