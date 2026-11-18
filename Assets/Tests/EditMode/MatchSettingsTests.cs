using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Week 15 negative and boundary tests for the two match choices the player can make.
///
/// MatchSettings is the only class in the project that talks to PlayerPrefs, so these tests
/// snapshot the real remembered values and put them back afterwards - a test must not change
/// what the next session starts with.
/// </summary>
public class MatchSettingsTests
{
    const string TeamSizeKey = "match.teamSize";
    const string DifficultyKey = "match.difficulty";
    const string TagHintsKey = "match.tagHints";

    int oldTeamSize, oldDifficulty, oldTagHints;
    bool hadTeamSize, hadDifficulty, hadTagHints;

    [SetUp]
    public void SetUp()
    {
        hadTeamSize = PlayerPrefs.HasKey(TeamSizeKey);
        hadDifficulty = PlayerPrefs.HasKey(DifficultyKey);
        hadTagHints = PlayerPrefs.HasKey(TagHintsKey);
        oldTeamSize = PlayerPrefs.GetInt(TeamSizeKey, MatchSettings.MinTeamSize);
        oldDifficulty = PlayerPrefs.GetInt(DifficultyKey, (int)MatchDifficulty.Normal);
        oldTagHints = PlayerPrefs.GetInt(TagHintsKey, 1);
    }

    [TearDown]
    public void TearDown()
    {
        Restore(TeamSizeKey, oldTeamSize, hadTeamSize);
        Restore(DifficultyKey, oldDifficulty, hadDifficulty);
        Restore(TagHintsKey, oldTagHints, hadTagHints);
        PlayerPrefs.Save();
    }

    static void Restore(string key, int value, bool had)
    {
        if (had) PlayerPrefs.SetInt(key, value);
        else PlayerPrefs.DeleteKey(key);
    }

    /// <summary>TC-SET-01: team size is clamped on the way in, because the match setup screen is
    /// not the only caller - a saved value or a script can put anything there.</summary>
    [Test]
    public void SetTeamSize_ClampsToTheLegalRange()
    {
        MatchSettings.SetTeamSize(0);
        Assert.AreEqual(MatchSettings.MinTeamSize, MatchSettings.TeamSize, "0 must clamp up to the minimum");

        MatchSettings.SetTeamSize(-7);
        Assert.AreEqual(MatchSettings.MinTeamSize, MatchSettings.TeamSize, "a negative size must clamp");

        MatchSettings.SetTeamSize(99);
        Assert.AreEqual(MatchSettings.MaxTeamSize, MatchSettings.TeamSize, "an oversized team must clamp down");

        MatchSettings.SetTeamSize(3);
        Assert.AreEqual(3, MatchSettings.TeamSize, "a legal size must be kept exactly");
    }

    /// <summary>TC-SET-02: difficulty must be validated the same way Load() already validates it.
    /// This test is expected to FAIL against the current build - that failure is the defect
    /// record for the unclamped setter, and it is what the fix in step 5 has to turn green.</summary>
    [Test]
    public void SetDifficulty_ClampsAnOutOfRangeValue()
    {
        MatchSettings.SetDifficulty((MatchDifficulty)99);

        Assert.GreaterOrEqual((int)MatchSettings.Difficulty, 0,
            "difficulty must never hold a value below Easy");
        Assert.LessOrEqual((int)MatchSettings.Difficulty, 2,
            "difficulty must never hold a value above Hard, even if a bad value is passed in");
    }

    /// <summary>TC-SET-03: the three remembered choices must survive being saved and re-read.</summary>
    [Test]
    public void RememberedChoices_RoundTripThroughPlayerPrefs()
    {
        MatchSettings.SetTeamSize(3);
        MatchSettings.SetDifficulty(MatchDifficulty.Hard);
        MatchSettings.SetTagHints(false);

        MatchSettings.Load();   // read them back the way a new session would

        Assert.AreEqual(3, MatchSettings.TeamSize, "team size must round-trip");
        Assert.AreEqual(MatchDifficulty.Hard, MatchSettings.Difficulty, "difficulty must round-trip");
        Assert.IsFalse(MatchSettings.TagHints, "tag hints must round-trip as OFF");
    }

    /// <summary>TC-BND-04: the derived counts the spawner reads must stay consistent with the
    /// chosen size at both ends of the range.</summary>
    [Test]
    public void DerivedCounts_FollowTheTeamSize()
    {
        MatchSettings.SetTeamSize(4);
        Assert.AreEqual(3, MatchSettings.BlueAiCount, "4v4 means three Blue AI beside the human");
        Assert.AreEqual(4, MatchSettings.RedAiCount, "4v4 means four Red AI");
        Assert.AreEqual(8, MatchSettings.TotalCharacters, "4v4 puts eight characters on the field");

        MatchSettings.SetTeamSize(1);
        Assert.AreEqual(0, MatchSettings.BlueAiCount, "1v1 means no Blue AI");
        Assert.AreEqual(1, MatchSettings.RedAiCount);
        Assert.AreEqual(2, MatchSettings.TotalCharacters);
    }
}
