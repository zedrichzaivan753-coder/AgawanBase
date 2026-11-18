using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Week 15 regression net for the profile file - the one thing in this game that can lose a
/// player's data, which makes it the first of the four acceptance conditions (CC-1).
///
/// Every test points PlayerProfile at a throw-away folder with the existing
/// <see cref="PlayerProfile.SetDataDirectory"/> seam, so no test can touch the real save.
/// </summary>
public class PlayerProfileTests
{
    string tempDir;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Application.temporaryCachePath,
                               "agawan_profile_tests_" + System.Guid.NewGuid().ToString("N"));
        PlayerProfile.SetDataDirectory(tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        // Back to the real folder, then remove the proof.
        PlayerProfile.SetDataDirectory(null);
        try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
    }

    /// <summary>TC-PROF-01: a hand-edited or corrupt save must never crash the game, and the
    /// broken file must be kept rather than overwritten.</summary>
    [Test]
    public void CorruptSave_LoadsDefaultProfile_AndKeepsTheBrokenFile()
    {
        Assert.IsTrue(PlayerProfile.WriteGarbageForTests("this is not a profile { \"wins\": "),
            "the seam should be able to plant a corrupt save");

        Assert.DoesNotThrow(() => PlayerProfile.Load(), "a corrupt save must not throw on load");

        Assert.AreEqual(PlayerProfile.DefaultName, PlayerProfile.PlayerName,
            "a rejected save must fall back to the default profile");
        Assert.IsNotEmpty(PlayerProfile.LastError,
            "rejecting a save must leave the reason in LastError, not fail silently");
        Assert.IsTrue(File.Exists(Path.Combine(tempDir, "profile.rejected.json")),
            "the rejected file must be quarantined as evidence, never deleted");
    }

    /// <summary>TC-PROF-02: a zero-byte save (a crash during a write, a truncated copy) is the
    /// same case as corrupt text and must be handled identically.</summary>
    [Test]
    public void EmptySave_LoadsDefaultProfile()
    {
        Assert.IsTrue(PlayerProfile.WriteGarbageForTests(""));
        Assert.DoesNotThrow(() => PlayerProfile.Load());
        Assert.AreEqual(PlayerProfile.DefaultName, PlayerProfile.PlayerName,
            "an empty save is not a profile; the default must be used");
    }

    /// <summary>TC-PROF-03: a first run has no folder and no file. That is not an error.</summary>
    [Test]
    public void MissingSave_LoadsDefaultProfile_WithoutAnError()
    {
        string nested = Path.Combine(tempDir, "nested", "deeper");
        PlayerProfile.SetDataDirectory(nested);
        PlayerProfile.Load();

        Assert.IsTrue(PlayerProfile.IsLoaded, "a missing file still counts as a completed load");
        Assert.AreEqual(PlayerProfile.DefaultName, PlayerProfile.PlayerName);
        Assert.IsEmpty(PlayerProfile.LastError, "a missing save is normal on a first run, not an error");
    }

    /// <summary>TC-PROF-04: the name must survive a restart, which is the whole point of REQ-13.</summary>
    [Test]
    public void SetName_PersistsAcrossAReload()
    {
        Assert.IsTrue(PlayerProfile.SetName("ZEDRICH"), "a legal name must be accepted");
        Assert.AreEqual("ZEDRICH", PlayerProfile.PlayerName);

        PlayerProfile.SetDataDirectory(tempDir);   // same folder, re-read from disk

        Assert.AreEqual("ZEDRICH", PlayerProfile.PlayerName,
            "the name must come back from the file, not from the in-memory copy");
    }

    /// <summary>TC-PROF-05: counters are the reward loop; they must be on disk, not in memory.</summary>
    [Test]
    public void RecordMatch_PersistsCountersAcrossAReload()
    {
        Assert.IsTrue(PlayerProfile.RecordMatch(1, MatchResult.Win, "flag carried home",
                                                42f, "classic", 2, "HARD"));
        Assert.AreEqual(1, PlayerProfile.Wins);

        PlayerProfile.SetDataDirectory(tempDir);

        Assert.AreEqual(1, PlayerProfile.Wins, "the win must survive the reload");
        Assert.AreEqual(1, PlayerProfile.MatchesPlayed);
        Assert.AreEqual(1, PlayerProfile.BestStreak);
    }

    /// <summary>TC-PROF-06: the leaderboard is "fastest victories first", and a match id may only
    /// be banked once - the double-press case the record guard exists for.</summary>
    [Test]
    public void BestWins_OrdersFastestFirst_AndDuplicateMatchIdsAreRefused()
    {
        PlayerProfile.RecordMatch(1, MatchResult.Win, "slow", 60f, "classic", 2, "NORMAL");
        PlayerProfile.RecordMatch(2, MatchResult.Win, "fast", 40f, "school", 2, "HARD");
        PlayerProfile.RecordMatch(3, MatchResult.Win, "middle", 50f, "palengke", 2, "EASY");

        Assert.IsFalse(PlayerProfile.RecordMatch(3, MatchResult.Win, "duplicate", 10f, "classic", 2, "EASY"),
            "a match id that was already recorded must be refused");

        var best = PlayerProfile.BestWins();
        Assert.AreEqual(3, best.Count, "three wins should all fit on a five-slot board");
        Assert.AreEqual(40f, best[0].seconds, 0.001f, "the fastest victory must be first");
        Assert.AreEqual(50f, best[1].seconds, 0.001f);
        Assert.AreEqual(60f, best[2].seconds, 0.001f);
        Assert.AreEqual(3, PlayerProfile.Wins, "the refused duplicate must not bump the win count");
    }

    /// <summary>TC-PROF-07: RESET must leave nothing behind - not the file, not the counters.</summary>
    [Test]
    public void ResetProfile_ClearsTheProfileAndTheFile()
    {
        PlayerProfile.SetName("BEFORE");
        PlayerProfile.RecordMatch(1, MatchResult.Loss, "their flag came home", 30f, "classic", 2, "HARD");

        Assert.IsTrue(PlayerProfile.ResetProfile());

        Assert.AreEqual(PlayerProfile.DefaultName, PlayerProfile.PlayerName, "the name must be reset");
        Assert.AreEqual(0, PlayerProfile.MatchesPlayed, "the counters must be reset");
        Assert.IsFalse(File.Exists(PlayerProfile.SavePath), "the save file must be gone");

        PlayerProfile.SetDataDirectory(tempDir);
        Assert.AreEqual(0, PlayerProfile.MatchesPlayed, "a reload must not resurrect the old profile");
    }

    /// <summary>TC-BND-05 (low-resource / unwritable storage): if the folder cannot be created,
    /// saving must fail cleanly and the in-memory profile must not claim what the disk refused.</summary>
    [Test]
    public void Save_WhenTheFolderCannotBeCreated_FailsWithoutClaimingANewName()
    {
        Directory.CreateDirectory(tempDir);

        // A file where the profile's folder needs to be: Directory.CreateDirectory will fail.
        string blocker = Path.Combine(tempDir, "blocked");
        File.WriteAllText(blocker, "a file standing where a folder should be");

        PlayerProfile.SetDataDirectory(blocker);
        string nameBefore = PlayerProfile.PlayerName;

        Assert.IsFalse(PlayerProfile.SetName("NEWNAME"),
            "the save must report failure rather than pretend it worked");
        Assert.AreEqual(nameBefore, PlayerProfile.PlayerName,
            "a failed save must roll the name back, so memory and disk never disagree");
        Assert.IsNotEmpty(PlayerProfile.LastError, "the failure reason must be recorded");
    }
}
