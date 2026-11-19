using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Week 18 PA2 Windows backup package.
///
/// PA2 ships the Android APK that Week 17 already released: the exact 33,170,107-byte binary whose
/// md5 is ef38de8dd65100109791002a67738da1. That file is copied, never rebuilt, because rebuilding
/// throws away the identity the instructor would otherwise be able to check.
///
/// This script exists for the module's "backup installation package" requirement instead. There is
/// no handset attached to this machine, so a Windows player is the one runnable artefact that can be
/// demonstrated without hardware. It is a *backup*, not the submission: it runs Mono rather than
/// IL2CPP, and GameFeedback guards the catch haptics behind UNITY_ANDROID || UNITY_IOS, so the buzz
/// is absent on desktop. Both facts are logged so the evidence file can disclose them.
///
/// The build is split into three stages rather than one call, because switching the active build
/// target reimports the whole project for the new platform, and doing that twice plus the build in a
/// single editor call is long enough to time out:
///
///   StagePrepare  - log the Android facts, set the Standalone backend, switch to StandaloneWindows64
///   StageBuild    - build Builds/PA2_Windows/AgawanBase.exe and zip it into Week_18
///   StageRestore  - switch back to Android, re-assert every Android fact, log both sides
///
/// Originals are captured in StagePrepare and restored in StageRestore, so ProjectSettings.asset
/// comes back the way it went in; the restore is logged so a diff can be seen rather than assumed.
///
/// Menu: Tools > Week18 > Build Windows Backup Package (runs all three stages).
/// </summary>
public static class Week18Build
{
    const string BuildDirName = "PA2_Windows";
    const string ExeName = "AgawanBase.exe";
    const string ZipName = "Abrera_Zedrich_PA2_Windows.zip";

    // Captured in StagePrepare, restored in StageRestore. Held in EditorPrefs because a domain
    // reload between stages would drop a static field.
    const string KeyBackend = "Week18Build.standaloneBackend";

    static string ProjectRoot { get { return Directory.GetCurrentDirectory(); } }

    static string BuildDir { get { return Path.Combine(ProjectRoot, "Builds", BuildDirName); } }

    static string ExePath { get { return Path.Combine(BuildDir, ExeName); } }

    static string WeekDir
    {
        get { return Path.Combine(ProjectRoot, "Docs", "Zedrich Abrera", "Week_18"); }
    }

    static string ZipPath { get { return Path.Combine(WeekDir, ZipName); } }

    static string RecordPath
    {
        get { return Path.Combine(WeekDir, "Evidence", "windows_build.txt"); }
    }

    // ------------------------------------------------------------------ logging

    static readonly List<string> Lines = new List<string>();

    static void Say(string s)
    {
        Debug.Log("[Week18Build] " + s);
        Lines.Add(s);
    }

    /// <summary>Editor. Prints the seven Android build facts the plan's verify step checks.</summary>
    public static void ReportAndroidFacts(string tag)
    {
        Say("-- Android build facts (" + tag + ") --");
        Say("  activeTarget        = " + EditorUserBuildSettings.activeBuildTarget);
        Say("  buildAppBundle      = " + EditorUserBuildSettings.buildAppBundle + "  (must be False)");
        Say("  development         = " + EditorUserBuildSettings.development + "  (must be False)");
        Say("  scriptingBackend    = " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) + "  (must be IL2CPP)");
        Say("  targetArchitectures = " + PlayerSettings.Android.targetArchitectures + "  (must be ARM64)");
        Say("  minSdkVersion       = " + PlayerSettings.Android.minSdkVersion + "  (must be 26)");
        Say("  versionCode         = " + PlayerSettings.Android.bundleVersionCode + "  (must be 2)");
        Say("  bundleVersion       = " + PlayerSettings.bundleVersion + "  (must be 1.0.0-rc1)");
        Say("  applicationId       = " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
        Say("  orientation         = " + PlayerSettings.defaultInterfaceOrientation);
    }

    // ------------------------------------------------------------------ stage 1

    /// <summary>
    /// Editor. Stage 1: record what Android looks like now, remember the Standalone scripting
    /// backend, then switch the active target to StandaloneWindows64. Syncs the platform, so it can
    /// take a few minutes on the first run.
    /// </summary>
    public static void StagePrepare()
    {
        Lines.Clear();
        Say("=== Stage 1: prepare for the Windows backup build ===");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            Say("WARNING: expected to start on Android, found " + EditorUserBuildSettings.activeBuildTarget);

        ReportAndroidFacts("before the Windows detour");

        ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        EditorPrefs.SetInt(KeyBackend, (int)backend);
        Say("saved Standalone scripting backend = " + backend + " (restored in stage 3)");

        // A backup package is a desktop demo: the fast backend keeps the download small, and the
        // difference to the shipped Android build is disclosed rather than hidden.
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        EditorUserBuildSettings.development = false;
        Say("Standalone backend set to Mono2x, development build off");

        if (File.Exists(ExePath)) File.Delete(ExePath);
        if (Directory.Exists(BuildDir)) Directory.Delete(BuildDir, true);
        Say("cleared " + BuildDir);

        bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        Say("SwitchActiveBuildTarget(StandaloneWindows64) -> " + switched +
            ", activeTarget now " + EditorUserBuildSettings.activeBuildTarget);

        if (!switched && EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            Say("ERROR: could not switch to StandaloneWindows64. Is the platform module installed?");

        Flush("stage 1");
    }

    // ------------------------------------------------------------------ stage 2

    /// <summary>
    /// Editor. Stage 2: build the player and zip it into the Week_18 folder. Refuses to run unless
    /// the active target is already StandaloneWindows64, so a half-finished stage 1 cannot quietly
    /// produce an Android build with a .exe name.
    /// </summary>
    public static void StageBuild()
    {
        Lines.Clear();
        Say("=== Stage 2: build and package the Windows player ===");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            Say("ERROR: active target is " + EditorUserBuildSettings.activeBuildTarget +
                ", not StandaloneWindows64. Run StagePrepare first.");
            Flush("stage 2 FAILED");
            return;
        }

        var scenes = new List<string>();
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            if (EditorBuildSettings.scenes[i].enabled) scenes.Add(EditorBuildSettings.scenes[i].path);

        if (scenes.Count == 0)
        {
            Say("ERROR: no enabled scenes in the build settings.");
            Flush("stage 2 FAILED");
            return;
        }

        Say("scenes: " + string.Join(", ", scenes.ToArray()));
        Say("output: " + ExePath);
        Say("backend for this build: " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));

        Directory.CreateDirectory(BuildDir);

        var opts = new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = ExePath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(opts);
        UnityEditor.Build.Reporting.BuildSummary s = report.summary;

        Say("result=" + s.result +
            " errors=" + s.totalErrors +
            " warnings=" + s.totalWarnings +
            " sizeBytes=" + s.totalSize +
            " sizeMB=" + (s.totalSize / 1048576.0).ToString("F2") +
            " buildSeconds=" + s.totalTime.TotalSeconds.ToString("F1"));

        if (s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Say("ERROR: the Windows build did not succeed, so nothing was packaged.");
            Flush("stage 2 FAILED");
            return;
        }

        if (!File.Exists(ExePath))
        {
            Say("ERROR: the build reported success but " + ExePath + " is missing.");
            Flush("stage 2 FAILED");
            return;
        }

        // Fold the whole player folder into one file so the backup package is a single artefact with
        // a single hash, the same way the APK is shipped.
        Directory.CreateDirectory(WeekDir);
        if (File.Exists(ZipPath)) File.Delete(ZipPath);

        // Fully qualified: UnityEngine also defines a CompressionLevel and the bare name is ambiguous.
        ZipFile.CreateFromDirectory(BuildDir, ZipPath,
            System.IO.Compression.CompressionLevel.Optimal, false);

        var zip = new FileInfo(ZipPath);
        Say("zip: " + ZipPath);
        Say("zipBytes=" + zip.Length + " zipMB=" + (zip.Length / 1048576.0).ToString("F2"));
        Say("zipMd5=" + Md5(ZipPath));
        Say("exeMd5=" + Md5(ExePath));

        // What the package does and does not contain, so the document can quote it rather than guess.
        Say("haptics in this package: absent (GameFeedback guards the catch buzz behind UNITY_ANDROID || UNITY_IOS)");
        Say("this is a backup/demo package, NOT the submitted binary; the submitted binary is the APK copied from Week_17");

        Flush("stage 2 succeeded");
    }

    // ------------------------------------------------------------------ stage 3

    /// <summary>
    /// Editor. Stage 3: switch back to Android, put the Standalone backend back the way it was, and
    /// re-assert every Android build fact so the project is left exactly as the APK was built for.
    /// </summary>
    public static void StageRestore()
    {
        Lines.Clear();
        Say("=== Stage 3: restore the Android configuration ===");

        bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Android, BuildTarget.Android);

        Say("SwitchActiveBuildTarget(Android) -> " + switched +
            ", activeTarget now " + EditorUserBuildSettings.activeBuildTarget);

        int saved = EditorPrefs.GetInt(KeyBackend, (int)ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, (ScriptingImplementation)saved);
        Say("Standalone scripting backend restored to " + (ScriptingImplementation)saved);
        EditorPrefs.DeleteKey(KeyBackend);

        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.buildAppBundle = false;

        ReportAndroidFacts("after the Windows detour");

        bool ok =
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
            !EditorUserBuildSettings.buildAppBundle &&
            !EditorUserBuildSettings.development &&
            PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP &&
            PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64 &&
            PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel26 &&
            PlayerSettings.Android.bundleVersionCode == 2 &&
            PlayerSettings.bundleVersion == "1.0.0-rc1";

        Say(ok ? "all Android build facts read back correct" : "ERROR: at least one Android build fact drifted - see above");

        Flush(ok ? "stage 3 clean" : "stage 3 DRIFTED");
    }

    // ------------------------------------------------------------------ all three

    /// <summary>Editor. Runs the three stages in order. Menu entry point.</summary>
    public static void BuildWindowsBackup()
    {
        StagePrepare();
        StageBuild();
        StageRestore();
        Debug.Log("[Week18Build] BuildWindowsBackup finished. See " + RecordPath);
    }

    // ------------------------------------------------------------------ helpers

    static string Md5(string path)
    {
        using (MD5 md5 = MD5.Create())
        using (FileStream fs = File.OpenRead(path))
            return BitConverter.ToString(md5.ComputeHash(fs)).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>Appends this stage's lines to windows_build.txt, newest stage last.</summary>
    static void Flush(string headline)
    {
        Directory.CreateDirectory(Path.Combine(WeekDir, "Evidence"));

        var sb = new StringBuilder();
        if (!File.Exists(RecordPath))
        {
            sb.AppendLine("Week 18 PA2 Windows backup package");
            sb.AppendLine("Unity " + Application.unityVersion + " - editor script Assets/Editor/Week18Build.cs");
            sb.AppendLine();
        }

        sb.AppendLine("[" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC] " + headline);
        for (int i = 0; i < Lines.Count; i++) sb.AppendLine("  " + Lines[i]);
        sb.AppendLine();

        File.AppendAllText(RecordPath, sb.ToString());
        Lines.Clear();
    }

    // ------------------------------------------------------------------ menu

    [MenuItem("Tools/Week18/Build Windows Backup Package")]
    static void MenuBuildAll() { BuildWindowsBackup(); }

    [MenuItem("Tools/Week18/Build Windows Backup - stage 1 prepare")]
    static void MenuStage1() { StagePrepare(); }

    [MenuItem("Tools/Week18/Build Windows Backup - stage 2 build")]
    static void MenuStage2() { StageBuild(); }

    [MenuItem("Tools/Week18/Build Windows Backup - stage 3 restore Android")]
    static void MenuStage3() { StageRestore(); }
}
