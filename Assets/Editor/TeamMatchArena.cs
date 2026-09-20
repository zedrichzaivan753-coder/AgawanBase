using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Step 3 read-out and build driver for the arena resize. Report() is read-only: it prints the
/// current transform and collider of every object the resize touches, so the numbers can be
/// checked against what is really in the scene.
/// </summary>
public static class TeamMatchArena
{
    static readonly string[] Paths =
    {
        "Game/CameraRig/Main Camera",
        "Game/Environment/Ground",
        "Game/Environment/Wall_N",
        "Game/Environment/Wall_S",
        "Game/Environment/Wall_E",
        "Game/Environment/Wall_W",
        "Game/Environment/Base_Blue",
        "Game/Environment/Base_Red",
        "Game/Environment/PrisonForBlue",
        "Game/Environment/PrisonForRed",
        "Game/Environment/PrisonForBlue/Plate",
        "Game/Environment/PrisonForRed/Plate",
        "Game/Environment/Decor_Court/Floor_Outside",
        "Game/Environment/Decor_Court/Court_Slab",
        "Game/Environment/Decor_Court/Line_Sideline_W",
        "Game/Environment/Decor_Court/Line_Sideline_E",
        "Game/Environment/Decor_Court/Line_Baseline_N",
        "Game/Environment/Decor_Court/Line_Baseline_S",
        "Game/Environment/Decor_Court/Line_Halfway",
        "Game/Environment/Decor_Court/Line_CentreCircle_0",
        "Game/Environment/Decor_Court/Ring_Blue_0",
        "Game/Environment/Decor_Court/Ring_Red_0",
        "Game/Environment/Decor_Backdrop/BasePost_Blue",
        "Game/Environment/Decor_Backdrop/BasePost_Red",
        "Game/Environment/Flag_Blue",
        "Game/Environment/Flag_Red",
        "Game/Characters/BluePlayer",
        "Game/Characters/Enemy_Red_1",
        "Game/Characters/Enemy_Red_2"
    };

    public static void Report()
    {
        // Collider.bounds is cached until the physics engine syncs, so a resize made in the same
        // editor frame would otherwise read back the OLD box and look like a broken collider.
        Physics.SyncTransforms();

        foreach (string path in Paths)
        {
            GameObject go = GameObject.Find(path);
            if (go == null) { Debug.Log("[Arena] " + path + "  ** MISSING **"); continue; }

            string extra = "";

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Bounds b = col.bounds;
                extra += "  collider=" + col.GetType().Name +
                         " boundsCentre=" + b.center.ToString("F2") + " boundsSize=" + b.size.ToString("F2");
            }

            BaseZone zone = go.GetComponent<BaseZone>();
            if (zone != null) extra += "  BaseZone.radius=" + zone.radius + " team=" + zone.team;

            Camera cam = go.GetComponent<Camera>();
            if (cam != null) extra += "  orthoSize=" + cam.orthographicSize + " ortho=" + cam.orthographic;

            Debug.Log("[Arena] " + path + "  pos=" + go.transform.position.ToString("F3") +
                      "  localScale=" + go.transform.localScale.ToString("F3") +
                      "  lossyScale=" + go.transform.lossyScale.ToString("F3") + extra);
        }

        Debug.Log("[Arena] report complete.");
    }

    /// <summary>Applies the Step 3 arena resize through the normal scene builder, then saves.</summary>
    public static void Build()
    {
        BarangayBuild.All();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Arena] arena rebuild done and the scene saved.");
    }
}
