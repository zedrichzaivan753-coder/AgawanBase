using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Week 16: the build-time half of "profile the mobile render path on the desktop".
///
/// ProjectSettings/QualitySettings.asset excludes Standalone from the "Mobile" quality level, so a Windows
/// player is built with exactly one level - "PC" - and that level points at PC_RPAsset: render scale 1.0 with
/// 50 m main-light shadows. The Android build the assignment is about renders through Mobile_RPAsset: render
/// scale 0.8, no main-light shadows. Two consequences:
///
///   1. Profiling the untouched Windows player measures a path that is heavier than the one shipping, so any
///      optimisation measured against it would be measured against the wrong workload.
///   2. Mobile_RPAsset is not referenced by any surviving quality level, so it is stripped out of the Windows
///      build and a running player cannot load it. It has to be referenced before the build.
///
/// So: point "PC" at Mobile_RPAsset, build, and point it straight back. The order matters - leaving the project
/// on the mobile pipeline would silently change how the game looks on desktop.
///
/// Menu: Tools &gt; Week16 &gt; Perf Build.
/// </summary>
public static class PerfBuildPipeline
{
    const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";
    const string MobileAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
    const string PcAssetPath = "Assets/Settings/PC_RPAsset.asset";
    const string LevelName = "PC";

    [MenuItem("Tools/Week16/Perf Build/1 - Point PC level at Mobile_RPAsset (before building)")]
    public static void UseMobilePipeline()
    {
        RenderPipelineAsset mobile = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(MobileAssetPath);
        if (mobile == null)
        {
            Debug.LogError("[PerfBuild] " + MobileAssetPath + " not found - nothing changed.");
            return;
        }

        if (Point(LevelName, mobile))
            Debug.Log("[PerfBuild] '" + LevelName + "' level now uses Mobile_RPAsset. Build the perf player now, " +
                      "then run step 2 to put the project back.");
    }

    [MenuItem("Tools/Week16/Perf Build/2 - Restore PC_RPAsset (after building)")]
    public static void RestoreDesktopPipeline()
    {
        RenderPipelineAsset pc = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PcAssetPath);
        if (pc == null)
        {
            Debug.LogError("[PerfBuild] " + PcAssetPath + " not found - nothing changed.");
            return;
        }

        if (Point(LevelName, pc))
            Debug.Log("[PerfBuild] '" + LevelName + "' level restored to PC_RPAsset.");
    }

    [MenuItem("Tools/Week16/Perf Build/Show which pipeline each level uses")]
    public static void Status()
    {
        Object[] objects = AssetDatabase.LoadAllAssetsAtPath(QualitySettingsPath);
        if (objects == null || objects.Length == 0)
        {
            Debug.LogError("[PerfBuild] " + QualitySettingsPath + " produced no objects.");
            return;
        }

        SerializedObject serialised = new SerializedObject(objects[0]);
        SerializedProperty levels = serialised.FindProperty("m_QualitySettings");
        for (int i = 0; i < levels.arraySize; i++)
        {
            SerializedProperty level = levels.GetArrayElementAtIndex(i);
            RenderPipelineAsset pipeline =
                level.FindPropertyRelative("customRenderPipeline").objectReferenceValue as RenderPipelineAsset;
            Debug.Log("[PerfBuild] level " + i + " '" + level.FindPropertyRelative("name").stringValue + "' -> " +
                      (pipeline != null ? pipeline.name : "(none)"));
        }
    }

    /// <summary>Returns true when the level actually had to change, so the caller can say so instead of
    /// claiming a swap that was already in place.</summary>
    static bool Point(string levelName, RenderPipelineAsset asset)
    {
        Object[] objects = AssetDatabase.LoadAllAssetsAtPath(QualitySettingsPath);
        if (objects == null || objects.Length == 0)
        {
            Debug.LogError("[PerfBuild] " + QualitySettingsPath + " produced no objects.");
            return false;
        }

        SerializedObject serialised = new SerializedObject(objects[0]);
        SerializedProperty levels = serialised.FindProperty("m_QualitySettings");
        if (levels == null || !levels.isArray)
        {
            Debug.LogError("[PerfBuild] m_QualitySettings not found on " + objects[0].GetType().Name + ".");
            return false;
        }

        bool changed = false;
        for (int i = 0; i < levels.arraySize; i++)
        {
            SerializedProperty level = levels.GetArrayElementAtIndex(i);
            if (!string.Equals(level.FindPropertyRelative("name").stringValue, levelName)) continue;

            SerializedProperty pipeline = level.FindPropertyRelative("customRenderPipeline");
            if (pipeline.objectReferenceValue != asset)
            {
                pipeline.objectReferenceValue = asset;
                changed = true;
            }
        }

        if (!changed) return false;

        serialised.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return true;
    }
}
