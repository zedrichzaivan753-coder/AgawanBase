using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 17 readiness captures: the release candidate's full demonstration sequence, driven in one
/// play-mode run and photographed at each step.
///
/// The sequence is the one the module's checkpoint asks for - installs, launches, reaches the core
/// loop, shows instructions and credits, ends a match, restarts - except that "installs" cannot be
/// shown because no handset is attached (see clean_install.md). Everything after that is captured
/// here:
///   E1_title_release.png  - title with the version caption and HOW TO PLAY / CREDITS / EXIT
///   E2_how_to_play.png    - the instructions screen
///   E3_credits.png        - the credits screen with the licence and AI-disclosure block
///   E4_match_running.png  - the live match, i.e. the core loop actually running
///   E5_end_screen.png     - the end screen after a real elimination win
///   E6_restart.png        - the match running again straight after RESTART
///
/// The win in E5 is produced by calling MatchManager.Capture on each free Red character: the same
/// public entry point the tag rule calls, so the elimination rule, the prison placement, the end
/// screen and the profile write are all the real ones. The catches themselves are scripted rather
/// than played, and the documents say so instead of implying a human played it out.
///
/// Waits are real time, and a step is not left until its PNG is on disk and DIFFERENT from the shot
/// before it. That second condition is not paranoia: driven from outside the editor the Game View
/// presents at a crawl even though the player loop keeps running, and ScreenCapture photographs
/// whatever was last presented. The first run of this harness duly produced six files of which
/// E1, E2 and E3 were byte-identical - three title screens, none of them the how-to or the credits.
/// So every capture now forces a repaint and the file is hashed: a repeat of the previous hash is a
/// stale frame, and it is requested again (up to three times) instead of being reported as a shot.
///
/// Menu: Tools > Week17 > Capture Release Screenshots.
/// </summary>
public static class Week17Shots
{
    static string EvidenceDir
    {
        get
        {
            return Path.Combine(Directory.GetCurrentDirectory(),
                                "Docs", "Zedrich Abrera", "Week_17", "Evidence");
        }
    }

    static int step;
    static float stepStartedAt;
    static string notes = "";
    static int captureAttempts;

    // Capture bookkeeping: a shot is only done when its file exists and does not repeat any shot
    // already taken in this run.
    static bool pending;
    static string pendingName;
    static string pendingPath;
    static float requestedAt;
    static int staleTries;
    static readonly List<string> seenHashes = new List<string>();

    [MenuItem("Tools/Week17/Capture Release Screenshots")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Week17Shots] already in play mode - stop it first.");
            return;
        }

        Directory.CreateDirectory(EvidenceDir);

        step = 0;
        stepStartedAt = 0f;
        notes = "";
        pending = false;
        pendingName = null;
        pendingPath = null;
        staleTries = 0;
        seenHashes.Clear();
        captureAttempts = 0;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Debug.Log("[Week17Shots] entering play mode for the readiness sequence.");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;

        GameManager game = Object.FindAnyObjectByType<GameManager>();
        MatchManager match = Object.FindAnyObjectByType<MatchManager>();
        if (game == null) return;

        switch (step)
        {
            case 0:     // the title, before anything is pressed
                if (Elapsed < 2.5f) return;

                // Shot() is retried on every tick until the file is there, so this is said once.
                if (!pending)
                {
                    Debug.Log("[Week17Shots] title state=" + game.State +
                              " versionLabel=" + (game.versionLabel != null ? game.versionLabel.text : "MISSING"));
                    notes += "title shows version '" +
                             (game.versionLabel != null ? game.versionLabel.text : "MISSING") + "'; ";
                }
                if (!Shot("E1_title_release.png")) return;
                Next();
                return;

            case 1:     // instructions
                game.ShowHowTo();
                if (game.State != GameManager.GameState.HowTo) notes += "HOW TO PLAY did not open; ";
                Next();
                return;

            case 2:
                if (Elapsed < 0.8f) return;
                if (!Shot("E2_how_to_play.png")) return;
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
                if (!Shot("E3_credits.png")) return;
                game.CloseOverlay();
                Next();
                return;

            case 5:     // into the match
                if (game.State != GameManager.GameState.Title) return;
                if (CharacterSpawner.HasSpawned) game.StartGame();
                else game.StartMatchAndReload();
                notes += "match started; ";
                Next();
                return;

            case 6:
                if (game.State != GameManager.GameState.Playing) return;
                if (Elapsed < 1.5f) return;
                if (!Shot("E4_match_running.png")) return;
                Next();
                return;

            case 7:     // win by elimination, through the rule entry point
                if (game.State != GameManager.GameState.Playing)
                {
                    notes += "match ended as " + game.State + " after " + captureAttempts + " scripted capture(s); ";
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

                // A free Red can be absent for a frame or two after a map load while the roster is
                // rebuilt, so this waits instead of declaring the rule broken on the first look - but
                // it does not wait forever either.
                if (victim == null)
                {
                    if (Elapsed > 20f)
                    {
                        Debug.LogWarning("[Week17Shots] could not force an elimination ending - " +
                                         "no free Red after 20s. The end screen capture will show " +
                                         "whatever state the match is in.");
                        notes += "elimination win not reached (" + TeamManager.Count(Team.Red) +
                                 " Red(s), " + TeamManager.FreeCount(Team.Red) + " free); ";
                        Next();
                    }
                    return;
                }

                match.Capture(victim);
                captureAttempts++;
                notes += "captured " + victim.name + "; ";

                if (captureAttempts > 12)
                {
                    notes += "elimination stopped after 12 captures without an ending; ";
                    Next();
                }
                return;

            case 8:
                if (Elapsed < 1.5f) return;
                if (!Shot("E5_end_screen.png")) return;
                Next();
                return;

            case 9:     // restart, which reloads the scene
                game.Restart();
                notes += "restart pressed; ";
                Next();
                return;

            case 10:
                // Elapsed here is time since RESTART was pressed. A tick count is not a wait: the
                // editor's update callback fires far faster than a scene can load, so 900 ticks ran
                // out in under a second and gave up on a reload that was still in progress.
                if (game.State != GameManager.GameState.Playing || !CharacterSpawner.HasSpawned)
                {
                    if (Elapsed > 25f) { notes += "restart never reached a live match; "; Next(); }
                    return;
                }
                if (Elapsed < 3.5f) return;
                if (!Shot("E6_restart.png")) return;

                // Time.timeSinceLevelLoad restarts at the reload, so quoting it is what distinguishes
                // this from the E4 frame: it is a picture of a scene that is only seconds old.
                notes += "restart reached a live match again " + Time.timeSinceLevelLoad.ToString("F1") +
                         "s into the reloaded scene; ";
                Next();
                return;

            case 11:    // the exit control, which only logs inside the editor
                game.QuitGame();
                Next();
                return;

            case 12:
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
    /// Takes one evidence shot and reports whether it is safely on disk.
    ///
    /// Returns false while the shot is still being produced, so the caller stays in its step. A file
    /// that repeats the previous shot's hash is a frame the Game View never re-presented: the harness
    /// forces a repaint and asks again, up to three times, rather than reporting a picture of the
    /// wrong screen as evidence.
    /// </summary>
    static bool Shot(string fileName)
    {
        // Forcing the repaint is what actually fixes the staleness: with the editor driven from
        // outside, the Game View presents rarely even though the player loop runs at full rate.
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

        if (!pending) Begin(fileName);

        if (File.Exists(pendingPath) && LooksLikePng(pendingPath))
        {
            string hash = HashOf(pendingPath);

            // Compared against every shot so far, not just the one before: a repeat of any earlier
            // screen means the Game View handed back a frame from before this step.
            if (!seenHashes.Contains(hash) || staleTries >= 3)
            {
                int tries = staleTries;
                staleTries = 0;
                seenHashes.Add(hash);
                pending = false;
                Debug.Log("[Week17Shots] captured " + pendingName + " (md5 " + hash.Substring(0, 8) + ")");
                if (tries >= 3) notes += pendingName + " repeats an earlier screen even after forced repaints; ";
                return true;
            }

            // Same bytes as the last shot: the Game View is showing a frame from before this step.
            staleTries++;
            Debug.Log("[Week17Shots] " + pendingName + " was a repeat of the previous shot - asking again.");
            Begin(pendingName);
            return false;
        }

        if (Time.realtimeSinceStartup - requestedAt > 8f)
        {
            Debug.LogWarning("[Week17Shots] capture timed out: " + pendingName);
            notes += pendingName + " MISSING; ";
            pending = false;
            return true;   // never hang the walk on one picture
        }

        return false;
    }

    static void Begin(string fileName)
    {
        pendingName = fileName;
        pendingPath = Path.Combine(EvidenceDir, fileName);

        // Delete the previous run's file first: otherwise File.Exists() would say "done" for a
        // picture this run has not taken yet.
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
            System.Text.StringBuilder text = new System.Text.StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++) text.Append(hash[i].ToString("x2"));
            return text.ToString();
        }
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;

        File.WriteAllText(Path.Combine(EvidenceDir, "week17_shots.txt"),
                          "Week 17 readiness captures (Tools > Week17 > Capture Release Screenshots)\n" +
                          "Unity " + Application.unityVersion + "\n" +
                          "Finished: " + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC\n" +
                          "E1_title_release.png, E2_how_to_play.png, E3_credits.png, " +
                          "E4_match_running.png, E5_end_screen.png, E6_restart.png\n" +
                          "Each shot was hashed when written and re-taken if it repeated the previous " +
                          "shot, so an identical pair here would mean the Game View never repainted.\n" +
                          "Notes: " + notes + "\n");

        Debug.Log("[Week17Shots] finished. " + notes);
    }
}
