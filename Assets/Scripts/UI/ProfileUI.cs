using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives Game/UI/Panel_Profile: the player's display name, their win/loss record, the local
/// best-times board of their fastest victories, and the two-press RESET.
///
/// The panel is built by <c>Assets/Editor/ProfileBuild.cs</c> into all three map scenes and is
/// only ever shown while <see cref="GameManager.GameState.Profile"/> is the current state -
/// GameManager owns visibility, exactly as it does for every other panel.
///
/// Everything the panel shows comes from <see cref="PlayerProfile"/>, which owns the save file.
/// The panel never touches the file itself, never parses JSON, and never signs anything.
/// </summary>
public class ProfileUI : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("The name the player types. Capped at PlayerProfile.MaxNameLength characters.")]
    public InputField nameField;

    [Tooltip("The record line: matches, wins, losses, draws, streak.")]
    public Text statsText;

    [Tooltip("The local best-times board.")]
    public Text boardText;

    [Tooltip("One-line feedback under the buttons.")]
    public Text statusText;

    public Button saveButton;
    public Button resetButton;
    public Button backButton;
    public GameManager game;

    [Header("Behaviour")]
    [Tooltip("How long the armed RESET stays armed before it disarms itself, in seconds.")]
    public float resetArmSeconds = 4f;

    bool resetArmed;
    float resetArmedAt;

    void Awake()
    {
        if (nameField == null) Debug.LogWarning("[ProfileUI] 'nameField' is not assigned - the name cannot be typed.");
        if (statsText == null) Debug.LogWarning("[ProfileUI] 'statsText' is not assigned - the record cannot be shown.");
        if (boardText == null) Debug.LogWarning("[ProfileUI] 'boardText' is not assigned - the board cannot be shown.");
        if (game == null) Debug.LogWarning("[ProfileUI] 'game' is not assigned - BACK cannot leave the panel.");

        if (nameField != null)
        {
            // Enforced in the component as well as in PlayerProfile: the player cannot even type
            // past the limit, so the panel and the save agree on what a name is.
            nameField.characterLimit = PlayerProfile.MaxNameLength;
            nameField.lineType = InputField.LineType.SingleLine;
            nameField.contentType = InputField.ContentType.Standard;
            nameField.onEndEdit.RemoveListener(HandleNameFieldFinished);
            nameField.onEndEdit.AddListener(HandleNameFieldFinished);
        }
    }

    void OnEnable()
    {
        DisarmReset();
        Refresh();
    }

    void Update()
    {
        // An armed RESET must not stay armed forever behind a player who walked away.
        if (resetArmed && Time.unscaledTime - resetArmedAt > resetArmSeconds) DisarmReset();
    }

    /// <summary>Called by GameManager when the panel comes up.</summary>
    public void Open()
    {
        DisarmReset();
        Refresh();
        SetStatus("Type a name, then SAVE. Nothing leaves this device.");

        Debug.Log("[ProfileUI] opened - " + PlayerProfile.PlayerName + ", " + PlayerProfile.MatchesPlayed +
                  " match(es) on record.");
    }

    /// <summary>Redraws every text from the stored profile.</summary>
    public void Refresh()
    {
        if (nameField != null) nameField.text = PlayerProfile.PlayerName;
        if (statsText != null) statsText.text = BuildRecord();
        if (boardText != null) boardText.text = BuildBoard();
    }

    /// <summary>SAVE button. Validates through the profile, which is the only place a name is accepted.</summary>
    public void SaveName()
    {
        DisarmReset();

        string typed = nameField != null ? nameField.text : "";
        string clean = PlayerProfile.SanitizeName(typed);

        if (string.IsNullOrEmpty(clean))
        {
            SetStatus("A name cannot be only spaces. The record keeps " + PlayerProfile.PlayerName + ".");
            Debug.LogWarning("[ProfileUI] SAVE refused - the typed name was empty after trimming.");
            if (nameField != null) nameField.text = PlayerProfile.PlayerName;
            return;
        }

        bool saved = PlayerProfile.SetName(typed);
        Refresh();

        if (saved)
        {
            SetStatus("Saved as " + PlayerProfile.PlayerName + ".");
            Debug.Log("[ProfileUI] SAVE ok - name is now '" + PlayerProfile.PlayerName + "'.");
        }
        else
        {
            SetStatus("Could not save. " + PlayerProfile.LastError);
            Debug.LogError("[ProfileUI] SAVE failed - " + PlayerProfile.LastError);
        }
    }

    /// <summary>
    /// RESET button. Deliberately two presses: the first arms it and says what will be lost, the
    /// second erases. A single tap that wipes a record is a defect waiting to happen on a phone.
    /// </summary>
    public void ResetPressed()
    {
        if (!resetArmed)
        {
            resetArmed = true;
            resetArmedAt = Time.unscaledTime;

            if (resetButton != null)
            {
                Text label = resetButton.GetComponentInChildren<Text>();
                if (label != null) label.text = "PRESS AGAIN";
            }

            SetStatus("Press RESET again to erase the name and the whole record.");
            Debug.Log("[ProfileUI] RESET armed - waiting for the second press (" + resetArmSeconds + " s).");
            return;
        }

        DisarmReset();

        bool removed = PlayerProfile.ResetProfile();
        Refresh();

        SetStatus(removed
            ? "Profile erased. The name is back to " + PlayerProfile.DefaultName + "."
            : "The record was cleared but the file could not be deleted. " + PlayerProfile.LastError);

        Debug.Log("[ProfileUI] RESET confirmed - " + (removed ? "file deleted." : "file kept, record cleared."));
    }

    /// <summary>BACK button. Returns to the title screen through GameManager.</summary>
    public void Back()
    {
        DisarmReset();

        if (game != null)
        {
            Debug.Log("[ProfileUI] BACK - returning to the title screen.");
            game.CloseProfile();
        }
        else
        {
            Debug.LogWarning("[ProfileUI] BACK pressed with no GameManager to return to.");
            gameObject.SetActive(false);
        }
    }

    void HandleNameFieldFinished(string value)
    {
        // Trims what is left in the box, so the player sees exactly what will be saved.
        if (nameField == null) return;

        string clean = PlayerProfile.SanitizeName(value);
        if (clean.Length == 0) clean = PlayerProfile.PlayerName;
        if (clean != value) nameField.text = clean;
    }

    void DisarmReset()
    {
        if (!resetArmed)
        {
            if (resetButton != null) SetResetLabel("RESET PROFILE");
            return;
        }

        resetArmed = false;
        if (resetButton != null) SetResetLabel("RESET PROFILE");
    }

    void SetResetLabel(string text)
    {
        Text label = resetButton.GetComponentInChildren<Text>();
        if (label != null) label.text = text;
    }

    void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    string BuildRecord()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("NAME     ").Append(PlayerProfile.PlayerName).Append('\n');
        sb.Append("MATCHES  ").Append(PlayerProfile.MatchesPlayed)
          .Append("    WINS ").Append(PlayerProfile.Wins)
          .Append("    LOSSES ").Append(PlayerProfile.Losses)
          .Append("    DRAWS ").Append(PlayerProfile.Draws);
        sb.Append('\n');
        sb.Append("STREAK   ").Append(PlayerProfile.CurrentStreak)
          .Append("    BEST ").Append(PlayerProfile.BestStreak);

        if (!string.IsNullOrEmpty(PlayerProfile.LastPlayedUtc))
        {
            sb.Append('\n').Append("LAST MATCH   ").Append(PlayerProfile.LastPlayedUtc);
        }

        return sb.ToString();
    }

    string BuildBoard()
    {
        System.Collections.Generic.List<MatchEntry> best = PlayerProfile.BestWins();

        if (best.Count == 0) return "FASTEST VICTORIES\nNo victories yet.";

        StringBuilder sb = new StringBuilder("FASTEST VICTORIES");
        for (int i = 0; i < best.Count; i++)
        {
            MatchEntry e = best[i];
            sb.Append('\n').Append(i + 1).Append(".  ")
              .Append(e.seconds.ToString("0.0")).Append(" s   ")
              .Append(e.mapId).Append("   ")
              .Append(e.difficulty).Append(' ')
              .Append(e.teamSize).Append('v').Append(e.teamSize);
        }

        return sb.ToString();
    }
}
