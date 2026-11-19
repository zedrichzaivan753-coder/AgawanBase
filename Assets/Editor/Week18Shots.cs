using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 18 PA2 demonstration rehearsal.
///
/// This is the sequence the PA2 report claims the student can perform, driven once, end to end, in
/// a single play-mode run so the claim is not just prose: title, instructions, credits, map select,
/// match setup, a live match, the pause/settings screen, an ending, and a restart.
///
///   D1_title.png          - title with the version caption and HOW TO PLAY / CREDITS / EXIT
///   D2_how_to_play.png    - the instructions screen
///   D3_credits.png        - the credits screen with the licence and AI-disclosure block
///   D4_map_select.png     - the map select screen
///   D5_match_setup.png    - team size and difficulty
///   D6_match_running.png  - the core loop actually running
///   D7_pause_settings.png - pause, with SOUND and TAG HINTS
///   D8_end_screen.png     - the end screen after a real elimination win
///   D9_restart.png        - the match running again straight after RESTART
///
/// Alongside the pictures it writes demo_rehearsal.txt: every step with the wall-clock offset at
/// which it happened, so the document can quote real timings instead of estimating them.
///
/// Differences from Week17Shots, which this is cloned from: the output folder is Week_18, the file
/// names are D*, the map-select / match-setup / pause steps are added, and every step is timed.
/// The repaint-forcing and hash-dedup logic is carried over unchanged, because it is what stops a
/// stale Game View frame being filed as evidence - an earlier run produced three identical title
/// screens that were reported as the how-to, credits and match.
///
/// Menu: Tools > Week18 > Rehearse PA2 Demonstration.
/// </summary>
public static class Week18Shots
{
    static string EvidenceDir
    {
        get
        {
            return Path.Combine(Directory.GetCurrentDirectory(),
                                "Docs", "Zedrich Abrera", "Week_18", "Evidence");
        }
    }

    static int step;
    static float stepStartedAt;
    static float runStartedAt;
    static string notes = "";
    static int captureAttempts;

    static readonly StringBuilder timeline = new StringBuilder();

    // Capture bookkeeping: a shot is only done when its file exists and does not repeat any shot
    // already taken in this run.
    static bool pending;
    static string pendingName;
    static string pendingPath;
    static float requestedAt;
    static int staleTries;
    static readonly List<string> seenHashes = new List<string>();

    [MenuItem("Tools/Week18/Rehearse PA2 Demonstration")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Week18Shots] already in play mode - stop it first.");
            return;
        }

        Directory.CreateDirectory(EvidenceDir);

        step = 0;
        stepStartedAt = 0f;
        runStartedAt = 0f;
        notes = "";
        pending = false;
        pendingName = null;
        pendingPath = null;
        staleTries = 0;
        seenHashes.Clear();
        captureAttempts = 0;
        timeline.Length = 0;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Debug.Log("[Week18Shots] entering play mode for the PA2 demonstration rehearsal.");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;

        if (runStartedAt <= 0f)
        {
            runStartedAt = Time.realtimeSinceStartup;
            Mark("run started - play mode live");
        }

        GameManager game = Object.FindAnyObjectByType<GameManager>();
        MatchManager match = Object.FindAnyObjectByType<MatchManager>();
        if (game == null) return;

        switch (step)
        {
            case 0:     // the title, before anything is pressed
                if (Elapsed < 2.5f) return;
                if (!pending)
                {
                    string caption = game.versionLabel != null ? game.versionLabel.text : "MISSING";
                    Debug.Log("[Week18Shots] title state=" + game.State + " versionLabel=" + caption);
                    Mark("title reached, state=" + game.State + ", version caption '" + caption + "'");
                    notes += "title shows version '" + caption + "'; ";
                }
                if (!Shot("D1_title.png")) return;
                Next();
                return;

            case 1:     // instructions
                game.ShowHowTo();
                if (game.State != GameManager.GameState.HowTo) notes += "HOW TO PLAY did not open; ";
                Next();
                return;

            case 2:
                if (Elapsed < 0.8f) return;
                if (!Shot("D2_how_to_play.png")) return;
                game.CloseOverlay();
                Next();
                return;

            case 3:     // credits
                game.ShowCredits();
                if (game.State != GameManager.GameState.Credits) notes += "CREDITS did not open; ";
                Next();
                return;

            case 4:
                if (Elapsed < 0.8f) return;
                if (!Shot("D3_credits.png")) return;
                game.CloseOverlay();
                Next();
                return;

            case 5:     // map select
                if (game.State != GameManager.GameState.Title) return;
                game.ShowMapSelect();
                if (game.State != GameManager.GameState.MapSelect) notes += "MAP SELECT did not open; ";
                Next();
                return;

            case 6:
                if (Elapsed < 0.9f) return;
                if (!Shot("D4_map_select.png")) return;
                game.ShowMatchSetup();
                Next();
                return;

            case 7:     // match setup
                if (Elapsed < 0.9f) return;
                if (!Shot("D5_match_setup.png")) return;
                Mark("match setup reached, state=" + game.State);
                Next();
                return;

            case 8:     // start the match
                if (CharacterSpawner.HasSpawned) game.StartGame();
                else game.StartMatchAndReload();
                Mark("START pressed from match setup");
                notes += "match started; ";
                Next();
                return;

            case 9:
                if (game.State != GameManager.GameState.Playing) return;
                if (Elapsed < 1.5f) return;
                if (!Shot("D6_match_running.png")) return;
                Next();
                return;

            case 10:    // pause and settings
                game.Pause();
                if (game.State != GameManager.GameState.Paused) notes += "PAUSE did not open; ";
                Next();
                return;

            case 11:
                if (Elapsed < 0.9f) return;
                if (!Shot("D7_pause_settings.png")) return;
                game.Resume();
                Mark("pause screen shown and resumed, state=" + game.State);
                Next();
                return;

            case 12:    // win by elimination, through the rule entry point
                if (game.State != GameManager.GameState.Playing)
                {
                    Mark("match ended as " + game.State + " after " + captureAttempts + " scripted capture(s)");
                    notes += "match ended as " + game.State + " after " + captureAttempts +
                             " scripted capture(s); ";
                    Next();
                    return;
                }

                if (match == null)
                {
                    notes += "no MatchManager in the running map; ";
                    Next();
                    return;
                }

                CharacterStatus victim = FirstFreeRed();
                if (victim == null)
                {
                    if (Elapsed > 20f)
                    {
                        Debug.LogWarning("[Week18Shots] could not force an elimination ending - " +
                                         "no free Red after 20s.");
                        notes += "elimination win not reached (" + TeamManager.Count(Team.Red) +
                                 " Red(s), " + TeamManager.FreeCount(Team.Red) + " free); ";
                        Next();
                    }
                    return;
                }

                match.Capture(victim);
                captureAttempts++;
                notes += "captured " + victim.name + "; ";
                return;

            case 13:
                if (Elapsed < 1.5f) return;
                if (!Shot("D8_end_screen.png")) return;
                Mark("end screen captured, state=" + game.State);
                Next();
                return;

            case 14:    // restart, which reloads the scene
                game.Restart();
                Mark("RESTART pressed");
                notes += "restart pressed; ";
                Next();
                return;

            case 15:
                if (game.State != GameManager.GameState.Playing || !CharacterSpawner.HasSpawned)
                {
                    if (Elapsed > 25f) { notes += "restart never reached a live match; "; Next(); }
                    return;
                }
                if (Elapsed < 3.5f) return;
                if (!Shot("D9_restart.png")) return;
                notes += "restart reached a live match again " +
                         Time.timeSinceLevelLoad.ToString("F1") + "s into the reloaded scene; ";
                Mark("live match again " + Time.timeSinceLevelLoad.ToString("F1") + "s into the reloaded scene");
                Next();
                return;

            case 16:    // the exit control, which only logs inside the editor
                game.QuitGame();
                Mark("EXIT pressed (editor: logs only)");
                Next();
                return;

            case 17:
                if (Elapsed < 0.5f) return;
                Finish();
                EditorApplication.ExitPlaymode();
                return;
        }
    }

    static float Elapsed
    {
        get { return Time.realtimeSinceStartup - stepStartedAt; }
    }

    static void Next()
    {
        step++;
        stepStartedAt = Time.realtimeSinceStartup;
    }

    static void Mark(string what)
    {
        float at = runStartedAt > 0f ? Time.realtimeSinceStartup - runStartedAt : 0f;
        timeline.AppendLine(string.Format("{0,7:F2}s  {1}", at, what));
        Debug.Log("[Week18Shots] " + at.ToString("F2") + "s " + what);
    }

    static CharacterStatus FirstFreeRed()
    {
        List<CharacterStatus> reds = TeamManager.Members(Team.Red);
        for (int i = 0; i < reds.Count; i++)
        {
            if (reds[i] != null && !reds[i].isCaptured) return reds[i];
        }
        return null;
    }

    /// <summary>
    /// Takes one evidence shot and reports whether it is safely on disk. A file that repeats any
    /// earlier shot's hash is a frame the Game View never re-presented: the harness forces a
    /// repaint and asks again, up to three times, rather than filing a picture of the wrong screen.
    /// </summary>
    static bool Shot(string fileName)
    {
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

        if (!pending) Begin(fileName);

        if (File.Exists(pendingPath) && LooksLikePng(pendingPath))
        {
            string hash = HashOf(pendingPath);

            if (!seenHashes.Contains(hash) || staleTries >= 3)
            {
                int tries = staleTries;
                staleTries = 0;
                seenHashes.Add(hash);
                pending = false;
                Debug.Log("[Week18Shots] captured " + pendingName + " (md5 " + hash.Substring(0, 8) + ")");
                if (tries >= 3) notes += pendingName + " repeats an earlier screen even after forced repaints; ";
                Mark("captured " + pendingName + " md5 " + hash.Substring(0, 8));
                return true;
            }

            staleTries++;
            Debug.Log("[Week18Shots] " + pendingName + " was a repeat - asking again.");
            Begin(pendingName);
            return false;
        }

        if (Time.realtimeSinceStartup - requestedAt > 8f)
        {
            Debug.LogWarning("[Week18Shots] capture timed out: " + pendingName);
            notes += pendingName + " MISSING; ";
            Mark("MISSING " + pendingName);
            pending = false;
            return true;   // never hang the rehearsal on one picture
        }

        return false;
    }

    static void Begin(string fileName)
    {
        pendingName = fileName;
        pendingPath = Path.Combine(EvidenceDir, fileName);

        if (File.Exists(pendingPath)) File.Delete(pendingPath);

        ScreenCapture.CaptureScreenshot(pendingPath);

        requestedAt = Time.realtimeSinceStartup;
        pending = true;
    }

    static bool LooksLikePng(string path)
    {
        try
        {
            byte[] head = new byte[4];
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Read(head, 0, 4) < 4) return false;
            }
            return head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47;
        }
        catch (IOException)
        {
            return false;   // still being written
        }
    }

    static string HashOf(string path)
    {
        using (MD5 md5 = MD5.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            byte[] hash = md5.ComputeHash(stream);
            StringBuilder text = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) text.Append(hash[i].ToString("x2"));
            return text.ToString();
        }
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;

        float total = runStartedAt > 0f ? Time.realtimeSinceStartup - runStartedAt : 0f;

        File.WriteAllText(Path.Combine(EvidenceDir, "demo_rehearsal.txt"),
                          "Week 18 PA2 demonstration rehearsal (Tools > Week18 > Rehearse PA2 Demonstration)\n" +
                          "Unity " + Application.unityVersion + " - editor play mode, no handset attached\n" +
                          "Finished: " + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC\n" +
                          "Total rehearsal time: " + total.ToString("F1") + "s\n" +
                          "Shots: D1_title, D2_how_to_play, D3_credits, D4_map_select, D5_match_setup, " +
                          "D6_match_running, D7_pause_settings, D8_end_screen, D9_restart\n" +
                          "Each shot was hashed when written and re-taken if it repeated any earlier shot, " +
                          "so an identical pair would mean the Game View never repainted.\n" +
                          "Notes: " + notes + "\n\n" +
                          "Timeline (wall clock from the first frame of the run):\n" +
                          timeline.ToString());

        Debug.Log("[Week18Shots] finished in " + total.ToString("F1") + "s. " + notes);
    }
}
