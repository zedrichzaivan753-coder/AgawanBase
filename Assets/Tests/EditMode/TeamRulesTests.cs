using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The first regression net for AgawanBase: three checks over the rules that everything else
/// depends on. They are deliberately small - one value each - so a red test always names exactly
/// which rule moved, instead of pointing at a whole match.
///
/// Runs in Edit mode: no scene, no play mode, no time.
/// </summary>
public class TeamRulesTests
{
    /// <summary>Every rule in the game is written against Team, so the opponent must be a true
    /// pair: switching twice always lands back where it started.</summary>
    [Test]
    public void Opponent_IsItsOwnInverse()
    {
        Assert.AreEqual(Team.Red, Team.Blue.Opponent(), "Blue's opponent should be Red.");
        Assert.AreEqual(Team.Blue, Team.Red.Opponent(), "Red's opponent should be Blue.");

        foreach (Team team in TeamUtil.All)
        {
            Assert.AreEqual(team, team.Opponent().Opponent(),
                "Opponent() applied twice must return the original team.");
        }
    }

    /// <summary>The rules loop over TeamUtil.All every frame, so its contents and order are part
    /// of the contract: Blue first, Red second, nothing else.</summary>
    [Test]
    public void All_IsBlueThenRed()
    {
        Assert.AreEqual(2, TeamUtil.All.Length, "There must be exactly two teams.");
        Assert.AreEqual(Team.Blue, TeamUtil.All[0], "Blue must be first.");
        Assert.AreEqual(Team.Red, TeamUtil.All[1], "Red must be second.");
    }

    /// <summary>Freshness drives the HUD bar, so it must never leave 0..1 no matter how long a
    /// character stands outside its base or how the value gets clamped.</summary>
    [Test]
    public void Freshness_StaysInsideZeroToOne()
    {
        GameObject go = new GameObject("FreshnessProbe");
        CharacterStatus status = go.AddComponent<CharacterStatus>();

        try
        {
            status.fieldTime = 0f;
            Assert.AreEqual(1f, status.Freshness01, 0.0001f, "A character that just left base is fully fresh.");

            status.fieldTime = CharacterStatus.MaxFieldTimeSeconds * 0.5f;
            Assert.AreEqual(0.5f, status.Freshness01, 0.0001f, "Half the reference time is half freshness.");

            status.fieldTime = CharacterStatus.MaxFieldTimeSeconds;
            Assert.AreEqual(0f, status.Freshness01, 0.0001f, "At the reference time the bar is empty.");

            status.fieldTime = CharacterStatus.MaxFieldTimeSeconds * 100f;
            Assert.AreEqual(0f, status.Freshness01, 0.0001f, "Freshness must not go below zero.");

            status.fieldTime = -5f;
            Assert.AreEqual(1f, status.Freshness01, 0.0001f, "Freshness must not go above one.");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
