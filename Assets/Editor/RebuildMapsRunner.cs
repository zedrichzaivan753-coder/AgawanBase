using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs the four map builders in the only order that is safe: the two new maps are cloned from
/// Game.unity (which wipes Game/Environment), the hero props are planted on top of the new
/// environments, and the Classic bushes go last so nothing re-clone deletes them.
/// Called through execute_script (method Run).
/// </summary>
public static class RebuildMapsRunner
{
    public static void Run()
    {
        Debug.Log("[Rebuild] --- School Campus ---");
        SchoolCampusBuild.Build();

        Debug.Log("[Rebuild] --- Palengke ---");
        PalengkeBuild.Build();

        Debug.Log("[Rebuild] --- Hero props ---");
        HeroProps.Apply();

        Debug.Log("[Rebuild] --- Classic hide spots ---");
        ClassicHideSpotBuild.Build();

        AssetDatabase.SaveAssets();
        Debug.Log("[Rebuild] all four builders finished.");
    }
}
