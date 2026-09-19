/// <summary>
/// Which side a character or base belongs to.
/// Used by BaseZone, CharacterStatus and MatchManager.
/// </summary>
public enum Team
{
    /// <summary>The human player's team.</summary>
    Blue,

    /// <summary>The AI enemies' team.</summary>
    Red
}

/// <summary>
/// Tiny helper for working with <see cref="Team"/>.
/// </summary>
public static class TeamUtil
{
    /// <summary>Returns the opposing team.</summary>
    public static Team Opponent(this Team team)
    {
        return team == Team.Blue ? Team.Red : Team.Blue;
    }
}
