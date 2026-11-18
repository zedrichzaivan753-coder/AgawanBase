using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 14: records the feedback layer being USED, rather than merely compiled.
///
/// It starts a real match, runs the AI at 3x speed for up to two real minutes, and copies every
/// [Feedback] line the game writes into Evidence/feedback_lines.txt. That file is what the report
/// cites for "a catch, a rescue, a flag pick-up and a match end each produce one [Feedback] line
/// naming the clip" - no hand-written claim, no invoked shortcut: this is the AI playing.
///
/// It stops early once all five kinds have been seen, or when the match ends, and it always
/// restores Time.timeScale and leaves play mode before it writes the summary.
///
/// Menu: Tools > Week14 > Record AI Match Feedback.
/// </summary>
public static class Week14FeedbackRun
{
    static readonly string[] Kinds =
    {
        "catch - ",
        "rescue - ",
        "flag picked up by ",
        "victory - ",
        "defeat - "
    };

    static readonly HashSet<string> seen = new HashSet<string>();
    static readonly List<string> lines = new List<string>();

    static float startedAt;
    static bool startedMatch;
    static bool matchSeenPlaying;
    static string stopReason;

    static string EvidenceDir
    {
        get
        {
            return Path.Combine(Directory.GetCurrentDirectory(),
                                "Docs", "Zedrich Abrera", "Week_14", "Evidence");
        }
    }

    [MenuItem("Tools/Week14/Record AI Match Feedback")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[FeedbackRun] already in play mode - stop it first.");
            return;
        }

        seen.Clear();
        lines.Clear();
        startedMatch = false;
        matchSeenPlaying = false;
        stopReason = "still running";

        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();

        Debug.Log("[FeedbackRun] starting an AI match at 3x speed to record the feedback layer.");
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (!message.StartsWith("[Feedback]")) return;

        lines.Add(message.Replace("[Feedback] ", ""));

        for (int i = 0; i < Kinds.Length; i++)
        {
            if (message.Contains(Kinds[i])) seen.Add(Kinds[i].Trim(' ', '-'));
        }
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;

        if (startedAt <= 0f) startedAt = Time.realtimeSinceStartup;

        GameManager game = Object.FindAnyObjectByType<GameManager>();
        if (game == null) return;

        if (!startedMatch)
        {
            if (Time.realtimeSinceStartup - startedAt < 2.5f) return;

            startedMatch = true;
            if (CharacterSpawner.HasSpawned) game.StartGame();
            else game.StartMatchAndReload();

            Debug.Log("[FeedbackRun] match started");
            return;
        }

        if (game.State == GameManager.GameState.Playing)
        {
            matchSeenPlaying = true;

            // 3x so a full match fits in the time a linter would allow; re-asserted because the
            // pause/resume paths own this value.
            if (Mathf.Approximately(Time.timeScale, 1f)) Time.timeScale = 3f;
        }
        else if (matchSeenPlaying)
        {
            stopReason = "the match ended (" + game.State + ")";
            Finish();
            EditorApplication.ExitPlaymode();
            return;
        }

        if (seen.Count >= Kinds.Length)
        {
            stopReason = "all five feedback kinds were seen";
            Finish();
            EditorApplication.ExitPlaymode();
            return;
        }

        if (Time.realtimeSinceStartup - startedAt > 130f)
        {
            stopReason = "stopped after 130 s of play";
            Finish();
            EditorApplication.ExitPlaymode();
        }
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;

        Time.timeScale = 1f;

        Directory.CreateDirectory(EvidenceDir);

        string summary = "";
        for (int i = 0; i < Kinds.Length; i++)
        {
            string kind = Kinds[i].Trim(' ', '-');
            summary += (seen.Contains(kind) ? "SEEN     " : "NOT SEEN ") + kind + "\n";
        }

        File.WriteAllText(Path.Combine(EvidenceDir, "feedback_lines.txt"),
                          "Week 14 - feedback layer, recorded from a real AI match\n" +
                          "Unity " + Application.unityVersion + "\n" +
                          "Finished: " + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC\n" +
                          "Stopped because: " + stopReason + "\n" +
                          "Lines recorded: " + lines.Count + "\n\n" +
                          summary + "\n" +
                          "--- every [Feedback] line, in order ---\n" +
                          string.Join("\n", lines.ToArray()) + "\n");

        Debug.Log("[FeedbackRun] " + stopReason + ". " + seen.Count + "/" + Kinds.Length +
                  " kinds seen, " + lines.Count + " lines -> Evidence/feedback_lines.txt");
    }
}
