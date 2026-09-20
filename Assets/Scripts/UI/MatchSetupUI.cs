using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Match Setup screen: pick a team size, pick an AI skill, then press START.
///
/// It wires its OWN buttons in Awake, so there is nothing to hook up by hand in the Inspector -
/// adding a button to the scene is enough. It never starts a match itself: it stores the choice
/// in MatchSettings and asks GameManager to switch screens, so GameManager stays the only thing
/// that owns the game states.
/// </summary>
public class MatchSetupUI : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("The screen switcher. Set by the panel builder.")]
    public GameManager game;

    [Header("Team size buttons - index 0..3 means 1v1, 2v2, 3v3, 4v4")]
    public Button[] sizeButtons = new Button[4];

    [Tooltip("The text inside each team size button.")]
    public Text[] sizeLabels = new Text[4];

    [Header("Difficulty buttons - index 0..2 means Easy, Normal, Hard")]
    public Button[] difficultyButtons = new Button[3];

    [Tooltip("The text inside each difficulty button.")]
    public Text[] difficultyLabels = new Text[3];

    [Header("Text")]
    [Tooltip("The line under the buttons describing the chosen match.")]
    public Text summaryText;

    [Header("Colours")]
    [Tooltip("The button that is currently chosen.")]
    public Color selectedColor = new Color(0.95f, 0.72f, 0.25f, 1f);

    [Tooltip("Every button that is not chosen.")]
    public Color normalColor = new Color(0.20f, 0.26f, 0.36f, 1f);

    void Awake()
    {
        // Buttons are wired here rather than in the Inspector, so the panel is self-contained.
        Hook(sizeButtons, OnSizeClicked);
        Hook(difficultyButtons, OnDifficultyClicked);
    }

    void OnEnable()
    {
        // The screen re-reads the remembered choice every time it is opened.
        Refresh();
    }

    // ------------------------------------------------------------------ button wiring

    /// <summary>Adds one click listener per button, carrying that button's own index.</summary>
    void Hook(Button[] buttons, System.Action<int> handler)
    {
        if (buttons == null) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            // Copy the index into a local variable FIRST. A listener that closed over the loop
            // variable itself would give every button the last index once the loop ended.
            int index = i;
            buttons[i].onClick.AddListener(delegate { handler(index); });
        }
    }

    void OnSizeClicked(int index)
    {
        MatchSettings.SetTeamSize(index + 1);
        Refresh();
    }

    void OnDifficultyClicked(int index)
    {
        MatchSettings.SetDifficulty((MatchDifficulty)index);
        Refresh();
    }

    // ------------------------------------------------------------------------ actions

    /// <summary>
    /// START button: leave the setup screen and begin the match.
    ///
    /// The roster is built while the scene loads, so a size or skill that differs from the one
    /// already on the field can only take effect after a reload. Pressing START on the choice the
    /// field was built with just starts the match - no pointless flash of a reloading screen.
    /// GameManager owns both routes, so it stays the only thing that changes state.
    /// </summary>
    public void StartMatch()
    {
        if (game == null)
        {
            Debug.LogWarning("MatchSetupUI: 'game' is not assigned, so START does nothing.");
            return;
        }

        MatchSettings.Save();

        bool fieldMatchesTheChoice =
            CharacterSpawner.HasSpawned &&
            MatchSettings.TeamSize == CharacterSpawner.SpawnedTeamSize &&
            MatchSettings.Difficulty == CharacterSpawner.SpawnedDifficulty;

        if (fieldMatchesTheChoice) game.StartGame();
        else game.StartMatchAndReload();
    }

    /// <summary>BACK button: return to the title screen.</summary>
    public void Back()
    {
        if (game != null) game.ShowTitle();
        else Debug.LogWarning("MatchSetupUI: 'game' is not assigned, so BACK does nothing.");
    }

    // ------------------------------------------------------------------------- display

    /// <summary>Repaints every button and the summary line from the current settings.</summary>
    public void Refresh()
    {
        for (int i = 0; i < sizeButtons.Length; i++)
        {
            int size = i + 1;
            bool chosen = (size == MatchSettings.TeamSize);

            Paint(sizeButtons[i], chosen);
            if (sizeLabels[i] != null) sizeLabels[i].text = size + "v" + size;
        }

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            bool chosen = (i == (int)MatchSettings.Difficulty);

            Paint(difficultyButtons[i], chosen);
            if (difficultyLabels[i] != null)
            {
                difficultyLabels[i].text = MatchSettings.DifficultyName((MatchDifficulty)i);
            }
        }

        if (summaryText != null)
        {
            summaryText.text = MatchSettings.SizeName +
                               "     Blue: you + " + MatchSettings.BlueAiCount + " AI" +
                               "     Red: " + MatchSettings.RedAiCount + " AI" +
                               "     Skill: " + MatchSettings.DifficultyName(MatchSettings.Difficulty);
        }
    }

    /// <summary>Tints one button so the chosen option is obvious.</summary>
    void Paint(Button button, bool chosen)
    {
        if (button == null) return;

        Image image = button.GetComponent<Image>();
        if (image != null) image.color = chosen ? selectedColor : normalColor;
    }
}
