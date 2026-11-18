using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Week 16 one-off: lists the render-related profiler markers this Unity version actually exposes.
///
/// Why: PerfCapture asks for "Draw Calls Count" and "Batches Count" and gets a valid recorder that
/// only ever reports zero, so those columns are written as -1 and the bottleneck table has no draw-call
/// numbers at all. Guessing at the spelling again is not an option, so this prints every marker whose
/// name contains Draw or Batch, with its category, stat type and current value.
///
/// Menu: Tools &gt; Week16 &gt; Dump Render Markers. Delete this file once the names are settled.
/// </summary>
public static class PerfMarkerProbe
{
    [MenuItem("Tools/Week16/Dump Render Markers")]
    public static void Dump()
    {
        List<ProfilerRecorderHandle> handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);

        StringBuilder text = new StringBuilder();
        text.AppendLine("[PerfMarkers] available markers: " + handles.Count);

        StringBuilder values = new StringBuilder();
        int matched = 0;

        foreach (ProfilerRecorderHandle handle in handles)
        {
            ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
            string name = description.Name.ToString();

            bool wanted = name.IndexOf("draw", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                          name.IndexOf("batch", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (!wanted) continue;

            matched++;
            text.AppendLine("[PerfMarkers] " + description.Category + " | '" + name + "' | " +
                            description.DataType + " | " + description.UnitType);
        }

        text.AppendLine("[PerfMarkers] matched " + matched + ".");
        Debug.Log(text.ToString());
        if (values.Length > 0) Debug.Log(values.ToString());
    }
}
