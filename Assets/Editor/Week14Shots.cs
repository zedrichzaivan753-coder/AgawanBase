using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 14 evidence captures, in the same shape as the Week 13 Pt5Shots harness: enter play mode,
/// drive the game into the state the screenshot needs, save the PNG, leave play mode.
///
/// Three pictures, one run:
///   E2_hud_contrast.png   - the in-match HUD with the AI debug read-out gone
///   E3_catch_feedback.png - right after a capture, so the [Feedback] catch line exists in the
///                           console. The capture is triggered by THIS harness calling
///                           MatchManager.Capture on a Red character: the same public entry point
///                           the tag rule uses, so the event and the sound path are the real ones,
///                           but the catch itself is scripted rather than played. The documents
///                           say so.
///   E1_pause_sound.png    - the pause screen with the new SOUND button
///
/// Waits are real time, and a capture is not stepped over until its PNG is actually on disk:
/// ScreenCapture writes at the end of a frame, and the first version of this harness walked past
/// its own captures, so E3 came out showing the pause screen.
///
/// Menu: Tools > Week14 > Capture Evidence Screenshots.
/// </summary>
public static class Week14Shots
{
    static string EvidenceDir
    {
        get
        {
            return Path.Combine(Directory.GetCurrentDirectory(),
                                "Docs", "Zedrich Abrera", "Week_14", "Evidence");
        }
    }

    static int step;
    static float stepStartedAt;
    static string notes;
    static string pendingFile;

    [MenuItem("Tools/Week14/Capture Evidence Screenshots")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Week14Shots] already in play mode - stop it first.");
            return;
        }

        Directory.CreateDirectory(EvidenceDir);

        step = 0;
        stepStartedAt = 0f;
        notes = "";
        pendingFile = null;

        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Debug.Log("[Week14Shots] entering play mode for E1 / E2 / E3.");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;

        // A capture is only finished once its file is on disk.
        if (pendingFile != null)
        {
            if (!File.Exists(pendingFile) && Elapsed < 5f) return;
            pendingFile = null;
        }

        GameManager game = Object.FindAnyObjectByType<GameManager>();
        MatchManager match = Object.FindAnyObjectByType<MatchManager>();

        switch (step)
        {
            case 0:   // wait for the scene, then press START the way the title button does
                if (Elapsed < 2.5f) return;

                if (game != null && CharacterSpawner.HasSpawned) game.StartGame();
                else if (game != null) game.StartMatchAndReload();

                notes += "started the match from the title screen; ";
                Next();
                return;

            case 1:   // the match is running
                if (game == null || match == null) return;
                if (game.State != GameManager.GameState.Playing) return;
                if (Elapsed < 1.5f) return;

                Capture("E2_hud_contrast.png");
                Next();
                return;

            case 2:   // a capture, through the real rule entry point
            {
                CharacterStatus victim = FirstFreeRed();

                if (victim != null)
                {
                    match.Capture(victim);
                    notes += "captured " + victim.name + " through MatchManager.Capture; ";
                }
                else
                {
                    notes += "no free Red character to capture; ";
                }

                Next();
                return;
            }

            case 3:   // the moment after the catch
                if (Elapsed < 1.0f) return;

                Capture("E3_catch_feedback.png");
                Next();
                return;

            case 4:   // pause screen
                if (game != null) game.Pause();
                Next();
                return;

            case 5:
                if (Elapsed < 1.0f) return;

                Capture("E1_pause_sound.png");
                notes += "pause screen captured with the SOUND button; ";
                Next();
                return;

            case 6:
                if (game != null) game.Resume();
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
        System.Collections.Generic.List<CharacterStatus> reds = TeamManager.Members(Team.Red);
        for (int i = 0; i < reds.Count; i++)
        {
            if (reds[i] != null && !reds[i].isCaptured) return reds[i];
        }
        return null;
    }

    static void Capture(string fileName)
    {
        string path = Path.Combine(EvidenceDir, fileName);

        // Delete the previous run's file first: otherwise File.Exists() would say "done" for a
        // picture this run has not taken yet.
        if (File.Exists(path)) File.Delete(path);

        ScreenCapture.CaptureScreenshot(path);

        pendingFile = path;

        Debug.Log("[Week14Shots] captured " + fileName);
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;

        File.WriteAllText(Path.Combine(EvidenceDir, "week14_shots.txt"),
                          "Week 14 evidence captures (Tools > Week14 > Capture Evidence Screenshots)\n" +
                          "Unity " + Application.unityVersion + "\n" +
                          "Finished: " + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC\n" +
                          "E2_hud_contrast.png, E3_catch_feedback.png, E1_pause_sound.png\n" +
                          "Notes: " + notes + "\n");

        Debug.Log("[Week14Shots] finished. " + notes);
    }
}
