using UnityEngine;

/// <summary>
/// Which side a character, base, prison or flag belongs to.
/// Used by CharacterStatus, BaseZone, Flag, TeamManager and MatchManager.
///
/// Nothing in the project may assume "Blue is the human" or "Red is the enemy". Every rule is
/// written against the Team value, so the SAME AI drives both sides and the same tag, flag and
/// prison rules cover a 1v1 and a 4v4.
/// </summary>
public enum Team
{
    /// <summary>The team the human player is on.</summary>
    Blue,

    /// <summary>The other team.</summary>
    Red
}

/// <summary>
/// Tiny helper for working with <see cref="Team"/>.
/// </summary>
public static class TeamUtil
{
    /// <summary>
    /// Both teams, in a fixed order. Iterating this array allocates nothing, which matters
    /// because the rules loop over the teams every frame.
    /// </summary>
    public static readonly Team[] All = { Team.Blue, Team.Red };

    /// <summary>Returns the opposing team.</summary>
    public static Team Opponent(this Team team)
    {
        return team == Team.Blue ? Team.Red : Team.Blue;
    }

    /// <summary>The colour this team is drawn in, for gizmos and debug labels.</summary>
    public static Color ToColor(this Team team)
    {
        return team == Team.Blue
            ? new Color(0.25f, 0.55f, 0.95f)
            : new Color(0.90f, 0.30f, 0.25f);
    }
}
