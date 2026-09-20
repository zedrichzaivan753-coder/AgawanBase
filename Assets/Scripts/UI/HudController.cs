using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Copies game state into the HUD widgets. Display only: it never changes the game,
/// and it never decides anything. It just reads CharacterStatus, the Flag and TeamManager.
///
/// Every string is BUILT and then COMPARED before it is assigned, because assigning a Text.text
/// every frame rebuilds the canvas mesh every frame - which is real cost on an Android build.
/// </summary>
public class HudController : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("The human player. Freshness, stamina and the capture overlay all read from here.")]
    public CharacterStatus player;

    [Tooltip("The Red flag (the one the player can steal). Drives the carry banner.")]
    public Flag redFlag;

    [Tooltip("The Blue flag (the player's OWN flag). Drives the 'it has been taken' alert.")]
    public Flag blueFlag;

    [Header("HUD widgets")]
    [Tooltip("Image with type = Filled, fillMethod = Horizontal. Full = fresh, empty = stale.")]
    public Image freshnessFill;

    [Tooltip("Small text under the bar, e.g. '4.2 s in the field'.")]
    public Text freshnessLabel;

    [Tooltip("Image with type = Filled, fillMethod = Horizontal. Full = all stamina.")]
    public Image staminaFill;

    [Tooltip("Small text next to the stamina bar, e.g. 'STAMINA  70%'.")]
    public Text staminaLabel;

    [Tooltip("Shouty banner that only appears while the player is carrying the flag.")]
    public Text flagLabel;

    [Tooltip("Top-left line: how many Blue characters are free, and where the Blue flag is.")]
    public Text blueStatusLabel;

    [Tooltip("Top-right line: how many Red characters are free, and where the Red flag is.")]
    public Text redStatusLabel;

    [Tooltip("Centre banner, left switched OFF in the scene. Used for the captured overlay and " +
             "for 'your flag has been taken'. Two lines share it, so only one can show at a time.")]
    public Text alertLabel;

    [Header("AI debug read-out")]
    [Tooltip("Bottom-left panel listing every AI's JOB and its current STATE. Display only - it " +
             "reads the brains, it never steers them.")]
    public Text aiDebugLabel;

    [Tooltip("Switch the AI read-out on. Leave it off for the shipped build: it is a debugging " +
             "aid, and each line names a team-mate the player is not meant to be watching.")]
    public bool showAiDebug = true;

    [Tooltip("How many times a second the read-out is rebuilt. Deliberately slow: building one " +
             "string per character is work the game does not need to do 30 times a second, and " +
             "this only has to be readable, not smooth.")]
    public float aiDebugUpdatesPerSecond = 4f;

    // The player's shared sprint component. Read only - the HUD never changes the game.
    SprintStamina stamina;

    // Counts down to the next AI debug rebuild.
    float aiDebugTimer;

    void Awake()
    {
        // The stamina bar belongs to the player, so grab his sprint component once here.
        if (player != null) stamina = player.GetComponent<SprintStamina>();

        // A Filled Image only draws a PARTIAL bar if it has a sprite. With no sprite Unity
        // ignores fillAmount completely and always draws a full rectangle, so complain loudly
        // here rather than shipping a stamina bar that never moves.
        if (staminaFill != null && staminaFill.sprite == null)
        {
            Debug.LogWarning("HudController: 'staminaFill' has no sprite, so its fillAmount is " +
                             "ignored and the bar will always look full. Give it a sprite, or copy " +
                             "FreshnessFill as the template.");
        }
    }

    void Update()
    {
        UpdateFreshness();
        UpdateStamina();
        UpdateCarryBanner();
        UpdateTeamStatus(Team.Blue, blueStatusLabel, blueFlag);
        UpdateTeamStatus(Team.Red, redStatusLabel, redFlag);
        UpdateAlert();
        UpdateAiDebug();
    }

    // --------------------------------------------------------------- player's own bars

    void UpdateFreshness()
    {
        if (player == null) return;

        if (freshnessFill != null) freshnessFill.fillAmount = player.Freshness01;

        if (freshnessLabel == null) return;

        string wanted;
        if (player.isCaptured) wanted = "CAPTURED";
        else if (player.IsInHomeBase) wanted = "SAFE AT BASE";
        else wanted = player.fieldTime.ToString("F1") + " s in the field";

        if (wanted != freshnessLabel.text) freshnessLabel.text = wanted;
    }

    void UpdateStamina()
    {
        if (stamina == null) return;

        if (staminaFill != null) staminaFill.fillAmount = stamina.Stamina01;
        if (staminaLabel == null) return;

        // The percentage is ALWAYS shown, sprinting or not, so you can see exactly how much
        // stamina is left at the moment you need it. Only the word in front changes.
        string wanted;
        if (player != null && player.isCaptured)
        {
            wanted = "STAMINA  -";
        }
        else
        {
            string percent = Mathf.RoundToInt(stamina.Stamina01 * 100f) + "%";
            wanted = (stamina.IsSprinting ? "SPRINTING  " : "STAMINA  ") + percent;
        }

        if (wanted != staminaLabel.text) staminaLabel.text = wanted;
    }

    // ------------------------------------------------------------------- carry banner

    void UpdateCarryBanner()
    {
        if (flagLabel == null) return;

        // The carry banner only exists while the player actually holds the flag, so the widget
        // is switched off the rest of the time rather than left showing an empty string.
        bool carrying = redFlag != null && redFlag.IsCarried &&
                        player != null && redFlag.carrier == player.transform;

        // The comparison matters: SetActive every frame would rebuild the canvas constantly.
        if (flagLabel.gameObject.activeSelf != carrying) flagLabel.gameObject.SetActive(carrying);

        if (carrying)
        {
            const string wanted = "YOU HAVE THE FLAG!  Get back to your base!";
            if (wanted != flagLabel.text) flagLabel.text = wanted;
        }
    }

    // ------------------------------------------------------------------ team status

    /// <summary>
    /// One line per team: how many of them are still free, and what their OWN flag is doing.
    /// The count comes from TeamManager rather than from a stored number, so splitting a team
    /// into roles, a rescue or a capture is all reflected the moment it happens - there is nothing
    /// to keep in step.
    /// </summary>
    void UpdateTeamStatus(Team team, Text label, Flag flag)
    {
        if (label == null) return;

        string wanted = team.ToString().ToUpperInvariant() +
                        "  " + TeamManager.FreeCount(team) + "/" + TeamManager.Count(team) + " free" +
                        "  |  flag: " + FlagStateText(flag);

        // Strings are only assigned when they have actually changed: a Text assignment rebuilds
        // the canvas, and these two lines would otherwise do it twice every single frame.
        if (wanted != label.text) label.text = wanted;
    }

    static string FlagStateText(Flag flag)
    {
        if (flag == null) return "-";
        if (flag.IsCarried) return "CARRIED";
        if (flag.IsDropped) return "DROPPED";
        return "AT BASE";
    }

    // ------------------------------------------------------------------- centre alert

    /// <summary>
    /// The one centre banner, shared by the two things urgent enough to interrupt the player.
    ///
    /// Being locked in prison wins the slot when both are true: it is the state in which the
    /// player can do least about the flag, and the one he has to act on.
    ///
    /// Note the condition on the captured line. The game only carries on while a team-mate is
    /// free to come and get you - if nobody is, the match has already ended and the end screen
    /// is up, so there is nothing to wait for.
    /// </summary>
    void UpdateAlert()
    {
        if (alertLabel == null) return;

        string wanted = null;

        if (player != null && player.isCaptured && TeamManager.FreeCount(Team.Blue) > 0)
        {
            wanted = "CAPTURED - waiting for a rescue";
        }
        else if (OurFlagHasBeenTaken())
        {
            wanted = "YOUR FLAG HAS BEEN TAKEN!";
        }

        bool show = wanted != null;

        if (alertLabel.gameObject.activeSelf != show) alertLabel.gameObject.SetActive(show);

        if (show && alertLabel.text != wanted) alertLabel.text = wanted;
    }

    /// <summary>True while an ENEMY of the Blue team is carrying the Blue flag.</summary>
    bool OurFlagHasBeenTaken()
    {
        if (blueFlag == null || !blueFlag.IsCarried || blueFlag.carrier == null) return false;

        CharacterStatus carrierStatus = blueFlag.carrier.GetComponent<CharacterStatus>();
        return carrierStatus != null && carrierStatus.team != Team.Blue;
    }

    // --------------------------------------------------------------- AI debug read-out

    /// <summary>
    /// The AI debug panel: one entry per AI, showing its JOB and what it is doing about it.
    ///
    /// This is how the roles are meant to be checked. Two brains with the same job and the same
    /// state, or a whole team stuck in the same state, is a role bug you can see at a glance
    /// rather than having to read the log.
    ///
    /// It is deliberately NOT rebuilt every frame, and it hides itself outside a live match: it
    /// exists to be read, not to be smooth, and every string it builds is an allocation.
    /// </summary>
    void UpdateAiDebug()
    {
        if (aiDebugLabel == null) return;

        bool show = showAiDebug && TeamManager.MatchActive;

        if (aiDebugLabel.gameObject.activeSelf != show) aiDebugLabel.gameObject.SetActive(show);
        if (!show) return;

        aiDebugTimer -= Time.deltaTime;
        if (aiDebugTimer > 0f) return;
        aiDebugTimer = 1f / Mathf.Max(1f, aiDebugUpdatesPerSecond);

        string wanted = BuildAiDebugText();
        if (wanted != aiDebugLabel.text) aiDebugLabel.text = wanted;
    }

    /// <summary>
    /// Builds the read-out. The human is skipped on purpose - it has no AI job and no AI state,
    /// and naming it would only push the line the player actually wants to watch off the screen.
    /// </summary>
    static string BuildAiDebugText()
    {
        string text = "";

        for (int t = 0; t < TeamUtil.All.Length; t++)
        {
            Team team = TeamUtil.All[t];
            List<CharacterStatus> members = TeamManager.Members(team);

            string line = team.ToString().ToUpperInvariant() + ":";

            for (int i = 0; i < members.Count; i++)
            {
                CharacterStatus member = members[i];
                if (member == null) continue;
                if (TeamManager.IsHuman(member)) continue;   // the human drives itself

                string job = TeamManager.RoleName(TeamManager.RoleOf(member));

                // A prisoner is doing the one thing that overrides any state it was in, so it
                // reads as PRISON rather than as whatever it was frozen in the middle of.
                string doing = member.isCaptured ? "PRISON" : StateOf(member);

                line += "   " + member.name + "  " + job + "  " + doing;
            }

            if (text.Length > 0) text += "\n";
            text += line;
        }

        return text;
    }

    static string StateOf(CharacterStatus member)
    {
        EnemyAI ai = member.GetComponent<EnemyAI>();
        return ai != null ? ai.state.ToString().ToUpperInvariant() : "-";
    }
}
