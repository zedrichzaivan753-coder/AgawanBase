using UnityEngine;

/// <summary>
/// How hard the AI plays. BOTH teams always use the same preset, so a match stays a fair
/// contest - there is no hidden second difficulty for the team-mates.
/// </summary>
public enum MatchDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2
}

/// <summary>
/// Every number a difficulty preset changes, in one bundle. It is a struct, so reading it
/// never allocates - which matters because the AI reads it on every decision.
/// </summary>
public struct DifficultyProfile
{
    /// <summary>Seconds between AI decisions.</summary>
    public float decisionInterval;

    /// <summary>Shortest pause after a decision, in seconds.</summary>
    public float reactionMin;

    /// <summary>Longest pause after a decision, in seconds.</summary>
    public float reactionMax;

    /// <summary>How much fresher than its target an AI must be before it will chase.</summary>
    public float chaseFresherMargin;

    /// <summary>Lowest aggression - scales how far an AI notices an opponent.</summary>
    public float aggressionMin;

    /// <summary>Highest aggression.</summary>
    public float aggressionMax;

    /// <summary>0 = cautious, 1 = takes chances. Biases the flag-stealing score.</summary>
    public float riskTolerance;

    /// <summary>Multiplies how strongly an AI wants the enemy flag.</summary>
    public float stealBias;

    /// <summary>How far an AI can see, in metres.</summary>
    public float visionRadius;

    /// <summary>How long an AI remembers where it last saw an opponent, in seconds.</summary>
    public float memorySeconds;

    /// <summary>Random spread added to every score. This is what makes the AI unpredictable:
    /// two team-mates with the same orders can still choose differently.</summary>
    public float noise;
}

/// <summary>
/// The match the player chose, and the one place PlayerPrefs is read and written.
///
/// WHY A STATIC CLASS AND NOT A SCRIPTABLE OBJECT
/// ----------------------------------------------
/// The whole game is one scene. Restart and Title both reload that scene, so anything that has
/// to survive a restart must live outside it. The project already does this with
/// GameManager.startPlayingOnLoad, and a static field works exactly the same way - with no
/// asset file, no Resources folder and no Inspector reference to keep wired up. The Match Setup
/// screen and the match itself are in the same scene, so there is nothing to carry between
/// scenes either. A ScriptableObject would add an asset, a load path and an instance pass, and
/// buy nothing at this size.
///
/// The values are remembered in PlayerPrefs, so the choice is still there next time the game is
/// opened.
/// </summary>
public static class MatchSettings
{
    public const int MinTeamSize = 1;
    public const int MaxTeamSize = 4;

    const string TeamSizeKey = "match.teamSize";
    const string DifficultyKey = "match.difficulty";

    static int teamSize = MinTeamSize;
    static MatchDifficulty difficulty = MatchDifficulty.Normal;
    static bool loaded;

    /// <summary>Characters on EACH side, including the human on Blue. 1 = 1v1, 4 = 4v4.</summary>
    public static int TeamSize
    {
        get { EnsureLoaded(); return teamSize; }
    }

    /// <summary>The AI skill preset. Both teams use it.</summary>
    public static MatchDifficulty Difficulty
    {
        get { EnsureLoaded(); return difficulty; }
    }

    /// <summary>Number of AI team-mates on Blue. The human is the other Blue character.</summary>
    public static int BlueAiCount
    {
        get { return TeamSize - 1; }
    }

    /// <summary>Number of AI characters on Red. All of them are AI.</summary>
    public static int RedAiCount
    {
        get { return TeamSize; }
    }

    /// <summary>Everyone on the field, the human included. 8 at 4v4.</summary>
    public static int TotalCharacters
    {
        get { return BlueAiCount + RedAiCount + 1; }
    }

    /// <summary>How the match is described on screen, e.g. "3v3".</summary>
    public static string SizeName
    {
        get { return TeamSize + "v" + TeamSize; }
    }

    /// <summary>Reads the remembered choice. Called automatically the first time anything asks.</summary>
    public static void Load()
    {
        teamSize = Mathf.Clamp(PlayerPrefs.GetInt(TeamSizeKey, MinTeamSize), MinTeamSize, MaxTeamSize);

        int saved = PlayerPrefs.GetInt(DifficultyKey, (int)MatchDifficulty.Normal);
        difficulty = (MatchDifficulty)Mathf.Clamp(saved, 0, 2);

        loaded = true;

        Debug.Log("[Settings] loaded " + SizeName + " (" + BlueAiCount + " Blue AI + " + RedAiCount +
                  " Red AI) at " + DifficultyName(difficulty) + " skill.");
    }

    /// <summary>Remembers the current choice.</summary>
    public static void Save()
    {
        PlayerPrefs.SetInt(TeamSizeKey, teamSize);
        PlayerPrefs.SetInt(DifficultyKey, (int)difficulty);
        PlayerPrefs.Save();
    }

    /// <summary>Chooses a team size (1..4) and remembers it.</summary>
    public static void SetTeamSize(int size)
    {
        EnsureLoaded();
        teamSize = Mathf.Clamp(size, MinTeamSize, MaxTeamSize);
        Save();

        Debug.Log("[Settings] team size = " + SizeName + " (" + TotalCharacters + " characters on the field)");
    }

    /// <summary>Chooses the AI skill preset and remembers it.</summary>
    public static void SetDifficulty(MatchDifficulty value)
    {
        EnsureLoaded();
        difficulty = value;
        Save();

        Debug.Log("[Settings] difficulty = " + DifficultyName(difficulty));
    }

    /// <summary>The display name of a preset.</summary>
    public static string DifficultyName(MatchDifficulty value)
    {
        if (value == MatchDifficulty.Easy) return "EASY";
        if (value == MatchDifficulty.Hard) return "HARD";
        return "NORMAL";
    }

    /// <summary>The tuning bundle for the chosen preset.</summary>
    public static DifficultyProfile Profile
    {
        get { return ProfileFor(Difficulty); }
    }

    /// <summary>
    /// The three presets. Every number is here so the balance can be read off one table.
    /// Only the SPEED and BOLDNESS of the AI changes - never the rules, and never one team
    /// more than the other.
    /// </summary>
    public static DifficultyProfile ProfileFor(MatchDifficulty value)
    {
        DifficultyProfile p = new DifficultyProfile();

        if (value == MatchDifficulty.Easy)
        {
            p.decisionInterval = 0.35f;   // thinks slowly
            p.reactionMin = 0.30f;
            p.reactionMax = 0.60f;
            p.chaseFresherMargin = 0.60f; // needs a big lead before it commits
            p.aggressionMin = 0.70f;
            p.aggressionMax = 0.95f;
            p.riskTolerance = 0.15f;
            p.stealBias = 0.7f;
            p.visionRadius = 10f;
            p.memorySeconds = 2.0f;
            p.noise = 0.10f;
        }
        else if (value == MatchDifficulty.Hard)
        {
            p.decisionInterval = 0.20f;   // thinks fast
            p.reactionMin = 0.05f;
            p.reactionMax = 0.15f;
            p.chaseFresherMargin = 0.15f; // will take a close call
            p.aggressionMin = 1.00f;
            p.aggressionMax = 1.30f;
            p.riskTolerance = 0.50f;
            p.stealBias = 1.3f;
            p.visionRadius = 14f;
            p.memorySeconds = 4.0f;
            p.noise = 0.18f;
        }
        else
        {
            p.decisionInterval = 0.25f;
            p.reactionMin = 0.10f;
            p.reactionMax = 0.35f;
            p.chaseFresherMargin = 0.30f;
            p.aggressionMin = 0.85f;
            p.aggressionMax = 1.15f;
            p.riskTolerance = 0.30f;
            p.stealBias = 1.0f;
            p.visionRadius = 12f;
            p.memorySeconds = 3.0f;
            p.noise = 0.15f;
        }

        return p;
    }

    static void EnsureLoaded()
    {
        if (!loaded) Load();
    }
}
