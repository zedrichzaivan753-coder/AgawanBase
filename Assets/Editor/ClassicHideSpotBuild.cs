using UnityEditor;
using UnityEngine;

/// <summary>
/// Plants the hide-spot bushes in the ORIGINAL Classic Open Field map.
///
/// The Classic map has no builder of its own - it was hand-made - so this is the one editor pass
/// that touches it, and it only ADDS a "Bushes" group under Game/Environment. It never touches
/// Ground, Base_Blue, Base_Red, Flag_*, PrisonFor*, the walls or the court markings, and it always
/// plants through MapBuild.CreateBush so Classic hides exactly the same way as the two new maps.
///
/// The four bushes sit off the base-to-base lane (z = 0) and are mirrored about the centre, so
/// neither team is the only one with cover.
/// </summary>
public static class ClassicHideSpotBuild
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string GroupName = "Bushes";

    /// <summary>Radius handed to MapBuild.CreateBush. The hide circle inside it is 0.85 of this.</summary>
    const float BushRadius = 1.1f;

    static readonly Vector2[] Spots =
    {
        new Vector2(-5f,  4.5f),
        new Vector2( 5f,  4.5f),
        new Vector2(-5f, -4.5f),
        new Vector2( 5f, -4.5f)
    };

    public static void Build()
    {
        if (!MapBuild.Open(ScenePath)) return;

        GameObject envObject = GameObject.Find("Game/Environment");
        if (envObject == null)
        {
            Debug.LogError("[Bushes] Game/Environment is missing, so Classic gets no hide spots.");
            return;
        }

        // Idempotent: a previous run's group is removed first, so re-running never doubles up.
        Transform old = envObject.transform.Find(GroupName);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
            Debug.Log("[Bushes] removed the previous " + GroupName + " group.");
        }

        GameObject group = new GameObject(GroupName);
        group.transform.SetParent(envObject.transform, false);

        for (int i = 0; i < Spots.Length; i++)
        {
            MapBuild.CreateBush(group.transform, "Bush_Classic_" + i,
                                new Vector3(Spots[i].x, 0f, Spots[i].y), BushRadius);
        }

        // The beds are new static geometry, so the NavMesh has to see them again.
        MapBuild.EnsureSurfaceAndBake();
        MapBuild.VerifyRoutes();
        MapBuild.Report("Classic");
        MapBuild.SaveScene();

        Debug.Log("[Bushes] Classic: planted " + Spots.Length + " hide-spot bushes at " +
                  Spots[0] + ", " + Spots[1] + ", " + Spots[2] + ", " + Spots[3] + ".");
    }
}
