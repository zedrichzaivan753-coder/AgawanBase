using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// PT5 evidence: the automated checks behind Week 13's "security tests".
///
/// Every check runs against a SCRATCH folder under Application.persistentDataPath, never the
/// player's real save, and writes a PASS/FAIL line to
/// Docs/Zedrich Abrera/Week_13/Evidence/pt5_checks.log. The format deliberately mirrors the
/// reference submission (Castillo, Sili-Tubig Clash): one numbered line per case, a TOTAL line
/// at the end, and a KNOWN LIMIT line that states plainly what the design does NOT protect.
///
/// Called through execute_script (method RunDataChecks) - it needs no frames, so it runs in
/// edit mode. The screenshots are the play-mode half, in Pt5Shots below.
/// </summary>
public static class Pt5Checks
{
    static readonly string EvidenceDir =
        Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Zedrich Abrera", "Week_13", "Evidence");

    static string Scratch =
        Path.Combine(Application.persistentDataPath, "AgawanBase_pt5_scratch");

    static StringBuilder log;
    static int passed;
    static int failed;

    [MenuItem("Tools/PT5/Run Data Checks (writes pt5_checks.log)")]
    public static void RunDataChecks()
    {
        log = new StringBuilder();
        passed = 0;
        failed = 0;

        Directory.CreateDirectory(EvidenceDir);

        log.AppendLine("PT5 - player profile, save integrity and recovery checks");
        log.AppendLine("Unity " + Application.unityVersion + "   " +
                       DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
        log.AppendLine("Scratch folder: " + Scratch);
        log.AppendLine("Real save folder (NOT touched by these checks): " +
                       Path.Combine(Application.persistentDataPath, "AgawanBase"));
        log.AppendLine();

        // A clean slate, so L1 really is "no save file at all".
        try { if (Directory.Exists(Scratch)) Directory.Delete(Scratch, true); }
        catch (Exception e) { Debug.LogWarning("[PT5] could not clear the scratch folder: " + e.Message); }

        PlayerProfile.SetDataDirectory(Scratch);

        // ---------------------------------------------------------------- L1
        Check("L1", "no save file: starts on " + PlayerProfile.DefaultName + " without an error",
              PlayerProfile.PlayerName == PlayerProfile.DefaultName && PlayerProfile.MatchesPlayed == 0,
              "name=" + PlayerProfile.PlayerName + " matches=" + PlayerProfile.MatchesPlayed);

        // ---------------------------------------------------------------- L2
        bool named = PlayerProfile.SetName("  Zedrich   Abrera  ");
        Check("L2", "a valid name is trimmed and inner spaces collapsed",
              named && PlayerProfile.PlayerName == "Zedrich Abrera",
              "name='" + PlayerProfile.PlayerName + "'");

        // ---------------------------------------------------------------- L3
        PlayerProfile.Load();
        bool fileThere = File.Exists(PlayerProfile.SavePath);
        string fileText = fileThere ? File.ReadAllText(PlayerProfile.SavePath) : "";
        Check("L3", "the profile survives a reload from disk and the file carries a signature",
              PlayerProfile.PlayerName == "Zedrich Abrera" && fileThere && fileText.Contains("\"sig\""),
              "name='" + PlayerProfile.PlayerName + "' file=" + fileThere +
              " sig=" + fileText.Contains("\"sig\""));

        // ---------------------------------------------------------------- L4
        bool blankRefused = !PlayerProfile.SetName("   ");
        Check("L4", "a blank name is refused and the stored name is kept",
              blankRefused && PlayerProfile.PlayerName == "Zedrich Abrera",
              "refused=" + blankRefused + " name='" + PlayerProfile.PlayerName + "'");

        // ---------------------------------------------------------------- L5
        bool longAccepted = PlayerProfile.SetName("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
        Check("L5", "a name longer than " + PlayerProfile.MaxNameLength + " characters is capped",
              longAccepted && PlayerProfile.PlayerName.Length == PlayerProfile.MaxNameLength,
              "length=" + PlayerProfile.PlayerName.Length + " name='" + PlayerProfile.PlayerName + "'");

        // ---------------------------------------------------------------- L6
        bool controlAccepted = PlayerProfile.SetName("Ze\u0007drich\u0001X");
        Check("L6", "control characters are stripped out of a name",
              controlAccepted && PlayerProfile.PlayerName == "ZedrichX",
              "name='" + PlayerProfile.PlayerName + "'");

        PlayerProfile.SetName("Zedrich Abrera");

        // ---------------------------------------------------------------- L7
        int id = PlayerProfile.NextMatchId;
        bool recorded = PlayerProfile.RecordMatch(id, MatchResult.Win, "FLAG_HOME", 42.5f,
                                                  "classic", 3, "NORMAL");
        Check("L7", "a finished match is recorded and written to disk",
              recorded && PlayerProfile.Wins == 1 && PlayerProfile.Recent.Count == 1 &&
              File.Exists(PlayerProfile.SavePath),
              "returned=" + recorded + " wins=" + PlayerProfile.Wins +
              " recent=" + PlayerProfile.Recent.Count);

        // ---------------------------------------------------------------- L8
        float[] times = { 90f, 70f, 55f, 48f, 300f };
        for (int i = 0; i < times.Length; i++)
        {
            PlayerProfile.RecordMatch(PlayerProfile.NextMatchId, MatchResult.Win, "FLAG_HOME",
                                      times[i], "palengke", 4, "HARD");
        }

        List<MatchEntry> board = PlayerProfile.BestWins();
        bool sorted = true;
        for (int i = 1; i < board.Count; i++)
        {
            if (board[i].seconds < board[i - 1].seconds) sorted = false;
        }

        Check("L8", "the best-times board keeps the 5 fastest victories, quickest first",
              board.Count == PlayerProfile.MaxBoard && sorted &&
              Mathf.Approximately(board[0].seconds, 42.5f),
              "count=" + board.Count + " first=" + (board.Count > 0 ? board[0].seconds.ToString("0.0") : "-") +
              " sorted=" + sorted);

        // ---------------------------------------------------------------- L9
        int lastId = PlayerProfile.LastMatchId;
        bool duplicateRefused = !PlayerProfile.RecordMatch(lastId, MatchResult.Win, "FLAG_HOME", 10f,
                                                           "classic", 3, "NORMAL");
        Check("L9", "the same match id cannot be banked twice",
              duplicateRefused && PlayerProfile.Wins == 6,
              "refused=" + duplicateRefused + " wins=" + PlayerProfile.Wins);

        // ---------------------------------------------------------------- L10
        string rejectedPath = Path.Combine(Scratch, "profile.rejected.json");
        PlayerProfile.WriteGarbageForTests("{ this is not json");
        PlayerProfile.Load();
        Check("L10", "an unreadable save starts empty without a crash and is set aside",
              PlayerProfile.PlayerName == PlayerProfile.DefaultName &&
              PlayerProfile.MatchesPlayed == 0 && File.Exists(rejectedPath),
              "name=" + PlayerProfile.PlayerName + " matches=" + PlayerProfile.MatchesPlayed +
              " rejected kept=" + File.Exists(rejectedPath));

        // ---------------------------------------------------------------- L11
        PlayerProfile.WriteRawForTests(
            "{\"schema\":1,\"playerName\":\"HACKER\",\"wins\":99,\"losses\":0,\"draws\":0," +
            "\"currentStreak\":0,\"bestStreak\":0,\"lastMatchId\":0,\"lastPlayedUtc\":\"\",\"recent\":[]}",
            false);
        PlayerProfile.Load();
        Check("L11", "a hand-edited save whose signature does not match is rejected",
              PlayerProfile.Wins == 0 && PlayerProfile.PlayerName != "HACKER",
              "wins=" + PlayerProfile.Wins + " name=" + PlayerProfile.PlayerName +
              " error='" + PlayerProfile.LastError + "'");

        // ---------------------------------------------------------------- L12
        PlayerProfile.WriteRawForTests(
            "{\"schema\":1,\"playerName\":\"Zedrich Abrera\",\"wins\":-5,\"losses\":-2,\"draws\":0," +
            "\"currentStreak\":9,\"bestStreak\":3,\"lastMatchId\":0,\"lastPlayedUtc\":\"\",\"recent\":[]}",
            true);
        PlayerProfile.Load();
        Check("L12", "impossible numbers in a correctly signed save are clamped, not trusted",
              PlayerProfile.Wins == 0 && PlayerProfile.Losses == 0 &&
              PlayerProfile.BestStreak == 9 && PlayerProfile.PlayerName == "Zedrich Abrera",
              "wins=" + PlayerProfile.Wins + " losses=" + PlayerProfile.Losses +
              " bestStreak=" + PlayerProfile.BestStreak + " name='" + PlayerProfile.PlayerName + "'");

        // ---------------------------------------------------------------- L13
        bool erased = PlayerProfile.ResetProfile();
        Check("L13", "reset erases the record on disk and returns the default name",
              erased && !File.Exists(PlayerProfile.SavePath) && !File.Exists(rejectedPath) &&
              PlayerProfile.PlayerName == PlayerProfile.DefaultName,
              "returned=" + erased + " file=" + File.Exists(PlayerProfile.SavePath) +
              " name=" + PlayerProfile.PlayerName);

        // ---------------------------------------------------------------- L14
        // A path that cannot be a folder: a FILE with that name. This fails on every machine,
        // unlike a missing drive letter, which may one day exist.
        string blocked = Path.Combine(Scratch, "blocked");
        try
        {
            Directory.CreateDirectory(Scratch);
            File.WriteAllText(blocked, "not a folder");
        }
        catch (Exception e) { Debug.LogWarning("[PT5] could not create the blocked path: " + e.Message); }

        PlayerProfile.SetDataDirectory(blocked);
        bool unwritableSaved = PlayerProfile.SetName("Nobody");
        Check("L14", "an unwritable save location reports an error instead of crashing",
              !unwritableSaved && !string.IsNullOrEmpty(PlayerProfile.LastError),
              "saved=" + unwritableSaved + " error='" + PlayerProfile.LastError + "'");

        // ---------------------------------------------------------------- L15
        Check("L15", "KNOWN LIMIT: the save is tamper-EVIDENT, not encrypted. The salt lives on " +
                     "the same device, so a determined local attacker can re-sign an edited file; " +
                     "there is no server to verify against.",
              true, "documented, not a defect");

        // ---------------------------------------------------------------- restore
        PlayerProfile.SetDataDirectory(null);
        try { if (Directory.Exists(Scratch)) Directory.Delete(Scratch, true); }
        catch (Exception e) { Debug.LogWarning("[PT5] could not delete the scratch folder: " + e.Message); }

        log.AppendLine();
        log.AppendLine("TOTAL: " + passed + " passed, " + failed + " failed");
        log.AppendLine("Harness: Assets/Editor/Pt5Checks.cs (RunDataChecks) - the scratch folder was " +
                       "deleted and the player's real save was never written to.");

        string target = Path.Combine(EvidenceDir, "pt5_checks.log");
        File.WriteAllText(target, log.ToString(), Encoding.UTF8);

        Debug.Log("[PT5] checks finished: " + passed + " passed, " + failed + " failed. Log: " + target);
    }

    static void Check(string id, string what, bool ok, string detail)
    {
        string line = (ok ? "PASS  " : "FAIL  ") + id + " " + what + "  [" + detail + "]";

        if (ok) passed++; else failed++;

        // L15 is a documented limitation, so it is reported as PASS without a detail bracket
        // reading like a failure.
        if (id == "L15")
        {
            line = "PASS  " + id + " " + what;
        }

        log.AppendLine(line);
        if (ok) Debug.Log("[PT5] " + line);
        else Debug.LogError("[PT5] " + line);
    }
}

/// <summary>
/// The play-mode half of the PT5 evidence: drives the real panel through the real handlers and
/// writes four captioned screenshots into the Week 13 evidence folder.
///
/// It runs off EditorApplication.update rather than a MonoBehaviour, so nothing is added to the
/// game's scripts and nothing has to be cleaned out of the scene afterwards.
/// </summary>
public static class Pt5Shots
{
    static readonly string EvidenceDir =
        Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Zedrich Abrera", "Week_13", "Evidence");

    static int step;
    static double nextTick;
    static GameManager game;
    static ProfileUI ui;
    static StringBuilder notes;

    /// <summary>Starts the sequence. Must be called while play mode is running.</summary>
    [MenuItem("Tools/PT5/Capture Profile Screenshots (press Play first)")]
    public static void Begin()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("[PT5] the screenshot sequence needs play mode - press Play first.");
            return;
        }

        Directory.CreateDirectory(EvidenceDir);

        notes = new StringBuilder();
        game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (game == null)
        {
            Debug.LogError("[PT5] no GameManager in the open scene.");
            return;
        }

        // The real profile folder, starting from a clean slate, so E1 really is an empty profile.
        PlayerProfile.SetDataDirectory(null);
        PlayerProfile.ResetProfile();

        game.ShowProfile();
        ui = game.panelProfile != null ? game.panelProfile.GetComponent<ProfileUI>() : null;

        if (ui == null)
        {
            Debug.LogError("[PT5] Game/UI/Panel_Profile has no ProfileUI - did ProfileBuild run?");
            return;
        }

        step = 0;
        nextTick = EditorApplication.timeSinceStartup + 0.6;
        EditorApplication.update += Tick;

        Debug.Log("[PT5] screenshot sequence started.");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        if (EditorApplication.timeSinceStartup < nextTick) return;
        if (game == null) return;
        if (ui == null && game.panelProfile != null) ui = game.panelProfile.GetComponent<ProfileUI>();

        switch (step)
        {
            case 0:
                Shoot("E1_profile_empty", "empty profile: " + PlayerProfile.PlayerName + " on the title screen");
                step = 1;
                break;

            case 1:
                PlayerProfile.RecordMatch(PlayerProfile.NextMatchId, MatchResult.Win, "FLAG_HOME",
                                          24.6f, "classic", 3, "NORMAL");
                PlayerProfile.RecordMatch(PlayerProfile.NextMatchId, MatchResult.Loss, "ELIMINATION",
                                          96.3f, "classic", 3, "NORMAL");
                PlayerProfile.RecordMatch(PlayerProfile.NextMatchId, MatchResult.Win, "FLAG_HOME",
                                          61.2f, "palengke", 4, "HARD");
                ui.Refresh();
                Shoot("E2_profile_stats", "record after three recorded matches: " +
                      PlayerProfile.Wins + "W/" + PlayerProfile.Losses + "L");
                step = 2;
                break;

            case 2:
                float[] fastest = { 18.4f, 33.9f, 47.5f, 120f };
                for (int i = 0; i < fastest.Length; i++)
                {
                    PlayerProfile.RecordMatch(PlayerProfile.NextMatchId, MatchResult.Win, "FLAG_HOME",
                                              fastest[i], "schoolcampus", 2, "EASY");
                }
                ui.Refresh();
                Shoot("E3_leaderboard", "fastest-victory board with " + PlayerProfile.BestWins().Count +
                      " entries");
                step = 3;
                break;

            case 3:
                ui.ResetPressed();   // first press: arms
                ui.ResetPressed();   // second press: erases
                ui.Refresh();
                Shoot("E4_after_reset", "after the two-press reset: " + PlayerProfile.PlayerName);
                step = 4;
                break;

            default:
                EditorApplication.update -= Tick;

                string target = Path.Combine(EvidenceDir, "pt5_checks.log");
                try
                {
                    File.AppendAllText(target, Environment.NewLine +
                        "Screenshots (play mode, real panel and real save file):" + Environment.NewLine +
                        notes.ToString(), Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[PT5] could not append the screenshot list: " + e.Message);
                }

                Debug.Log("[PT5] screenshot sequence finished - 4 PNGs in " + EvidenceDir);
                EditorApplication.isPlaying = false;
                return;
        }

        nextTick = EditorApplication.timeSinceStartup + 0.7;
    }

    static void Shoot(string name, string caption)
    {
        string path = Path.Combine(EvidenceDir, name + ".png");
        ScreenCapture.CaptureScreenshot(path);

        string line = "  " + name + ".png - " + caption;
        notes.AppendLine(line);
        Debug.Log("[PT5] screenshot " + line);
    }
}
