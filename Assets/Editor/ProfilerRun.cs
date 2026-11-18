using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 16: the editor side of the profiling pipeline.
///
/// Two menu items, both of them just a request: they arm <see cref="PerfCapture"/> and enter play
/// mode, and the capture itself is the same code that runs in a player build. That is deliberate -
/// a capture path that only exists in the editor would measure the editor.
///
/// Menu: Tools &gt; Week16.
/// Output: Docs/Zedrich Abrera/Week_16/Evidence/raw_csv/perf_&lt;label&gt;_&lt;stamp&gt;.csv
/// </summary>
public static class ProfilerRun
{
    const string EvidenceFolder = "Docs/Zedrich Abrera/Week_16/Evidence";

    /// <summary>Ten seconds: enough to prove the pipeline writes real rows without spending a minute.</summary>
    [MenuItem("Tools/Week16/Run Profiling Scenario (editor smoke, 10 s)")]
    public static void RunSmoke()
    {
        RunEditorRun("editor_smoke", 10f);
    }

    /// <summary>The full length the module requires, for an editor-side look at the same scenario.</summary>
    [MenuItem("Tools/Week16/Run Profiling Scenario (editor, 60 s)")]
    public static void RunFull()
    {
        RunEditorRun("editor_60s", 60f);
    }

    [MenuItem("Tools/Week16/Show Scenario Definition")]
    public static void ShowScenario()
    {
        Debug.Log("[Perf] scenario: " + PerfScenario.TeamSize + "v" + PerfScenario.TeamSize + " at " +
                  PerfScenario.Difficulty + ", AI random seed " + PerfScenario.Seed +
                  ", human on a fixed 4-waypoint loop between the two flags at walking speed, no sprint. " +
                  "Captures run uncapped; the shipped 30 fps cap is restored afterwards.");
    }

    [MenuItem("Tools/Week16/Open Evidence Folder")]
    public static void OpenEvidenceFolder()
    {
        string full = System.IO.Path.Combine(PerfCaptureProjectRoot(), EvidenceFolder);
        System.IO.Directory.CreateDirectory(full);
        EditorUtility.RevealInFinder(full);
    }

    /// <summary>
    /// Arms a capture and enters play mode. Callable from a script as well as a menu, so an automated
    /// run uses the exact same entry point a human does.
    /// </summary>
    public static void RunEditorRun(string label, float seconds)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Perf] already in play mode - stop it before starting a capture.");
            return;
        }

        PerfCapture.pendingRun = true;
        PerfCapture.pendingLabel = string.IsNullOrEmpty(label) ? "editor_run" : label;
        PerfCapture.pendingDuration = seconds > 1f ? seconds : 60f;

        EditorApplication.EnterPlaymode();

        Debug.Log("[Perf] editor capture requested: label '" + PerfCapture.pendingLabel + "', " +
                  PerfCapture.pendingDuration + " s. The CSV lands in " + EvidenceFolder + "/raw_csv.");
    }

    static string PerfCaptureProjectRoot()
    {
        System.IO.DirectoryInfo assets = new System.IO.DirectoryInfo(Application.dataPath);
        return assets.Parent != null ? assets.Parent.FullName : Application.dataPath;
    }
}
