using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the SCHOOL CAMPUS map: a 64 x 40 m Philippine school ground.
///
/// The shape of it, from above:
///
///   z +19..+16  NORTH ROAD   - clear from the Blue plaza to the Red plaza  (flank route 1)
///   z +16..+10  NORTH CLASSROOM BUILDING - 3 m corridor, enterable from four ways
///   z -5..+10   THE CENTRAL GROUND - school garden to the west, covered court to the east
///   z -10..-16  SOUTH CLASSROOM BUILDING - mirrored, its own corridor
///   z -16..-19  SOUTH ROAD   - clear end to end                             (flank route 2)
///
/// Two gameplay zones, exactly as asked for: the classroom-building area (narrow corridors, walls
/// that block movement, two ways in and out of each corridor) and the covered court and outside
/// area (a big open floor with benches, hoops and trees around it). The two zones meet on the
/// central ground, and three independent routes connect the bases.
///
/// THE COVERED COURT HAS NO ROOF - ON PURPOSE
/// -----------------------------------------
/// The camera looks down from a pitched rig, so a solid 16 x 14 m roof would hide every character
/// underneath it. The court is instead framed by perimeter beams and corner posts at 6.4 m, which
/// reads as a covered court from above while leaving the floor visible and playable.
/// </summary>
public static class SchoolCampusBuild
{
    const string ScenePath = "Assets/Scenes/Map_SchoolCampus.unity";
    const string MapAsset = "Assets/Config/Map_SchoolCampus.asset";

    const float Width = 64f;
    const float Depth = 40f;
    static readonly Vector2 Centre = new Vector2(0f, 0f);

    static readonly Vector3 BlueBase = new Vector3(-26f, 0f, 0f);
    static readonly Vector3 RedBase = new Vector3(26f, 0f, 0f);
    static readonly Vector3 BluePrison = new Vector3(16f, 0f, 15f);    // holds BLUE prisoners, in Red's half
    // The first draft put the red cage at (-16, -15), which is INSIDE the south classroom block (that
    // block runs x -18..-2, z -16.5..-14.1) - so Base_Blue -> PrisonForRed came back PathPartial and
    // Blue could never rescue. The cage now stands on open ground west of the buildings, diagonally
    // opposite the blue cage just like the palengke map does.
    static readonly Vector3 RedPrison = new Vector3(-22f, 0f, -14f);   // holds RED prisoners, in Blue's half

    // The two classroom buildings: corridor z band, and the run of the building along x.
    const float BuildingXFrom = -18f;
    const float BuildingXTo = -2f;
    const float NorthCorridorZ = 12.5f;   // corridor centre
    const float SouthCorridorZ = -12.5f;

    public static void Build()
    {
        if (!MapBuild.CloneScene(ScenePath)) return;
        if (!MapBuild.Open(ScenePath)) return;

        Transform env = MapBuild.RebuildEnvironment();
        if (env == null) return;

        // ---- the frame of the map -------------------------------------------
        MapBuild.CreateGround(env, Width, Depth, Centre);
        MapBuild.CreateBoundary(env, Width, Depth, Centre, 3.5f, 1f);

        // ---- the seven markers every manager points at ----------------------
        MapBuild.CreateBase(env, Team.Blue, BlueBase, 4f);
        MapBuild.CreateBase(env, Team.Red, RedBase, 4f);
        MapBuild.CreateFlag(env, Team.Blue, BlueBase);
        MapBuild.CreateFlag(env, Team.Red, RedBase);
        MapBuild.CreatePrison(env, Team.Blue, BluePrison);
        MapBuild.CreatePrison(env, Team.Red, RedPrison);

        // ---- the campus ------------------------------------------------------
        BuildGrounds(env);
        BuildClassroomBuilding(env, "Building_North", BuildingXFrom, BuildingXTo, NorthCorridorZ, true);
        BuildClassroomBuilding(env, "Building_South", BuildingXFrom, BuildingXTo, SouthCorridorZ, false);
        BuildCoveredCourt(env);
        BuildCanteen(env);
        BuildGarden(env);
        BuildGate(env);
        BuildPerimeter(env);

        MapDefinition map = AssetDatabase.LoadAssetAtPath<MapDefinition>(MapAsset);
        MapBuild.BindAll(map);
        MapBuild.EnsureSurfaceAndBake();
        MapBuild.VerifyRoutes();
        MapBuild.Report("School Campus");
        MapBuild.SaveScene();
    }

    // ================================================================ grounds

    static void BuildGrounds(Transform env)
    {
        GameObject group = Group(env, "Grounds");
        Transform t = group.transform;

        // Paths: the three routes between the bases, all walk-through floor markings.
        MapBuild.Flat(t, "Path_Central", new Vector3(0f, 0f, 1f), new Vector2(Width - 4f, 5f), "Mat_Asphalt");
        MapBuild.Flat(t, "Path_North", new Vector3(0f, 0f, 17.5f), new Vector2(Width - 4f, 3.5f), "Mat_Asphalt");
        MapBuild.Flat(t, "Path_South", new Vector3(0f, 0f, -17.5f), new Vector2(Width - 4f, 3.5f), "Mat_Asphalt");

        // The two end plazas, kept clear so bases, spawns and cages are never boxed in.
        MapBuild.Flat(t, "Plaza_Blue", new Vector3(-26f, 0f, 0f), new Vector2(12f, 22f), "Mat_Ground");
        MapBuild.Flat(t, "Plaza_Red", new Vector3(26f, 0f, 0f), new Vector2(12f, 22f), "Mat_Ground");

        // Lawn either side of the central path.
        MapBuild.Flat(t, "Lawn_North", new Vector3(0f, 0f, 7.2f), new Vector2(Width - 6f, 3.4f), "Mat_Foliage");
        MapBuild.Flat(t, "Lawn_South", new Vector3(0f, 0f, -7.2f), new Vector2(Width - 6f, 3.4f), "Mat_Foliage");
    }

    // ====================================================== classroom buildings

    /// <summary>
    /// One classroom building: a 3 m corridor with a solid classroom block on the outside and a
    /// wall with two doorways on the inside, open at both ends. That is four ways in or out, so a
    /// corridor is a route and never a trap.
    /// </summary>
    static void BuildClassroomBuilding(Transform env, string name, float xFrom, float xTo,
                                       float corridorZ, bool classroomsToNorth)
    {
        GameObject group = Group(env, name);
        Transform t = group.transform;

        float length = xTo - xFrom;
        float centreX = (xFrom + xTo) * 0.5f;
        float inner = classroomsToNorth ? corridorZ - 1.6f : corridorZ + 1.6f;   // wall facing the grounds
        float outer = classroomsToNorth ? corridorZ + 1.6f : corridorZ - 1.6f;   // wall behind the rooms
        float roomDirection = classroomsToNorth ? 1f : -1f;

        // Corridor floor - a floor marking, walkable.
        MapBuild.Flat(t, "Corridor", new Vector3(centreX, 0f, corridorZ), new Vector2(length, 3.2f), "Mat_Chalk");

        // The classroom block itself: solid, 2.4 m deep, with a roof so it reads as a building.
        MapBuild.Solid(t, "Classrooms",
                       new Vector3(centreX, 1.6f, outer + roomDirection * 1.2f),
                       new Vector3(length, 3.2f, 2.4f), "Mat_HouseWall");
        MapBuild.Decor(t, "Roof",
                       new Vector3(centreX, 3.35f, outer + roomDirection * 1.4f),
                       new Vector3(length + 0.8f, 0.3f, 3.4f), "Mat_TinRoof");
        MapBuild.Decor(t, "Roof_Ridge",
                       new Vector3(centreX, 3.6f, outer + roomDirection * 1.4f),
                       new Vector3(length + 0.8f, 0.2f, 1.2f), "Mat_TinRoof");

        // Windows along the classroom face, purely decorative.
        for (int i = 0; i < 5; i++)
        {
            float x = Mathf.Lerp(xFrom + 2f, xTo - 2f, i / 4f);
            MapBuild.Decor(t, "Window_" + i,
                           new Vector3(x, 2.0f, outer - roomDirection * 0.05f),
                           new Vector3(1.4f, 0.9f, 0.06f), "Mat_Dark");
        }

        // The wall facing the grounds, with two 3 m doorways in it.
        float[] doors = { xFrom + length * 0.32f, xFrom + length * 0.68f };
        WallAlongX(t, "Wall_Inner", xFrom, xTo, inner, 2.5f, 0.4f, doors, 3f);

        // End walls, so the corridor is a room and not an open shed - but with a 3 m gap each end.
        MapBuild.Solid(t, "EndCap_West", new Vector3(xFrom, 1.25f, corridorZ), new Vector3(0.4f, 2.5f, 0.8f), "Mat_Wall");
        MapBuild.Solid(t, "EndCap_East", new Vector3(xTo, 1.25f, corridorZ), new Vector3(0.4f, 2.5f, 0.8f), "Mat_Wall");
    }

    /// <summary>Builds a solid wall along X at a fixed z, leaving a gap at each doorway centre.</summary>
    static void WallAlongX(Transform parent, string name, float xFrom, float xTo, float z,
                           float height, float thickness, float[] doors, float doorWidth)
    {
        System.Array.Sort(doors);

        float cursor = xFrom;
        int index = 0;

        for (int d = 0; d < doors.Length; d++)
        {
            float gapStart = doors[d] - doorWidth * 0.5f;
            float gapEnd = doors[d] + doorWidth * 0.5f;

            if (gapStart > cursor)
            {
                AddWallSegment(parent, name + "_" + index++, cursor, gapStart, z, height, thickness);
            }

            cursor = Mathf.Max(cursor, gapEnd);
        }

        if (cursor < xTo)
        {
            AddWallSegment(parent, name + "_" + index, cursor, xTo, z, height, thickness);
        }
    }

    static void AddWallSegment(Transform parent, string name, float xFrom, float xTo, float z,
                               float height, float thickness)
    {
        float length = xTo - xFrom;
        if (length < 0.2f) return;

        MapBuild.Solid(parent, name,
                       new Vector3((xFrom + xTo) * 0.5f, height * 0.5f, z),
                       new Vector3(length, height, thickness), "Mat_Wall");
    }

    // ============================================================ covered court

    /// <summary>The covered basketball court: a big open floor, framed by beams instead of a roof.</summary>
    static void BuildCoveredCourt(Transform env)
    {
        GameObject group = Group(env, "CoveredCourt");
        Transform t = group.transform;

        const float cx = 10f;
        const float cz = 0.5f;
        const float halfW = 8f;
        const float halfD = 7f;

        MapBuild.Flat(t, "Court", new Vector3(cx, 0f, cz), new Vector2(halfW * 2f, halfD * 2f), "Mat_Asphalt");

        // Court markings - chalk, no collider.
        MapBuild.Flat(t, "Line_Sideline_N", new Vector3(cx, 0f, cz + halfD - 0.4f), new Vector2(halfW * 2f - 1f, 0.14f), "Mat_Chalk");
        MapBuild.Flat(t, "Line_Sideline_S", new Vector3(cx, 0f, cz - halfD + 0.4f), new Vector2(halfW * 2f - 1f, 0.14f), "Mat_Chalk");
        MapBuild.Flat(t, "Line_Baseline_W", new Vector3(cx - halfW + 0.4f, 0f, cz), new Vector2(0.14f, halfD * 2f - 1f), "Mat_Chalk");
        MapBuild.Flat(t, "Line_Baseline_E", new Vector3(cx + halfW - 0.4f, 0f, cz), new Vector2(0.14f, halfD * 2f - 1f), "Mat_Chalk");
        MapBuild.Flat(t, "Line_Halfway", new Vector3(cx, 0f, cz), new Vector2(0.14f, halfD * 2f - 1f), "Mat_Chalk");
        MapBuild.Flat(t, "Circle_Centre", new Vector3(cx, 0f, cz), new Vector2(3.6f, 3.6f), "Mat_Chalk");

        // The frame that makes it a COVERED court without hiding the floor from the camera.
        MapBuild.Decor(t, "Beam_N", new Vector3(cx, 6.4f, cz + halfD), new Vector3(halfW * 2f + 1f, 0.35f, 0.35f), "Mat_TinRoof");
        MapBuild.Decor(t, "Beam_S", new Vector3(cx, 6.4f, cz - halfD), new Vector3(halfW * 2f + 1f, 0.35f, 0.35f), "Mat_TinRoof");
        MapBuild.Decor(t, "Beam_W", new Vector3(cx - halfW, 6.4f, cz), new Vector3(0.35f, 0.35f, halfD * 2f), "Mat_TinRoof");
        MapBuild.Decor(t, "Beam_E", new Vector3(cx + halfW, 6.4f, cz), new Vector3(0.35f, 0.35f, halfD * 2f), "Mat_TinRoof");

        float[] postX = { cx - halfW, cx + halfW };
        float[] postZ = { cz - halfD, cz + halfD };

        for (int ix = 0; ix < postX.Length; ix++)
        {
            for (int iz = 0; iz < postZ.Length; iz++)
            {
                MapBuild.Decor(t, "Post", new Vector3(postX[ix], 3.2f, postZ[iz]), new Vector3(0.35f, 6.4f, 0.35f), "Mat_Dark");
            }
        }

        // The two hoops, at the ends of the court.
        Hoop(t, "Hoop_West", new Vector3(cx - halfW + 1.2f, 0f, cz), 1f);
        Hoop(t, "Hoop_East", new Vector3(cx + halfW - 1.2f, 0f, cz), -1f);
    }

    static void Hoop(Transform parent, string name, Vector3 position, float facing)
    {
        GameObject group = Group(parent, name);
        Transform t = group.transform;

        MapBuild.Solid(t, "Post", position + new Vector3(0f, 1.7f, 0f), new Vector3(0.22f, 3.4f, 0.22f), "Mat_Dark");
        MapBuild.Decor(t, "Backboard", position + new Vector3(facing * 0.35f, 3.3f, 0f), new Vector3(0.08f, 1.0f, 1.7f), "Mat_Chalk");
        MapBuild.Decor(t, "Rim", position + new Vector3(facing * 0.75f, 3.0f, 0f), new Vector3(0.5f, 0.06f, 0.5f), "Mat_WarmAccent");
    }

    // ================================================================= canteen

    static void BuildCanteen(Transform env)
    {
        GameObject group = Group(env, "Canteen");
        Transform t = group.transform;

        const float cx = 8.5f;
        const float cz = 13f;

        // The serving counter: solid.
        MapBuild.Solid(t, "Counter", new Vector3(cx - 2f, 0.55f, cz), new Vector3(6f, 1.1f, 1.2f), "Mat_HouseWall");
        MapBuild.Decor(t, "Canopy", new Vector3(cx, 2.8f, cz), new Vector3(10f, 0.14f, 5.4f), "Mat_TinRoof");
        MapBuild.Decor(t, "Sign", new Vector3(cx, 2.1f, cz - 1.4f), new Vector3(3.4f, 0.6f, 0.08f), "Mat_WarmAccent");

        // Tables and chairs, spaced so people can walk between them.
        float[] tableX = { cx - 2.4f, cx + 0.4f, cx + 3.2f };

        for (int i = 0; i < tableX.Length; i++)
        {
            MapBuild.Solid(t, "Table_" + i, new Vector3(tableX[i], 0.72f, cz - 3.2f), new Vector3(1.5f, 0.12f, 1.5f), "Mat_WarmAccent");
            MapBuild.Decor(t, "Leg_" + i, new Vector3(tableX[i], 0.36f, cz - 3.2f), new Vector3(0.2f, 0.72f, 0.2f), "Mat_Dark");

            MapBuild.Decor(t, "Chair_A_" + i, new Vector3(tableX[i] - 1.2f, 0.3f, cz - 3.2f), new Vector3(0.5f, 0.6f, 0.5f), "Mat_Blue");
            MapBuild.Decor(t, "Chair_B_" + i, new Vector3(tableX[i] + 1.2f, 0.3f, cz - 3.2f), new Vector3(0.5f, 0.6f, 0.5f), "Mat_Red");
        }

        // Student benches along the walkway.
        Bench(t, "Bench_A", new Vector3(cx - 4.5f, 0f, cz - 5.4f), 0f);
        Bench(t, "Bench_B", new Vector3(cx + 4.5f, 0f, cz - 5.4f), 0f);
    }

    static void Bench(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject group = Group(parent, name);
        Transform t = group.transform;

        MapBuild.Solid(t, "Seat", position + new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.12f, 0.5f), "Mat_WarmAccent", yaw);
        MapBuild.Decor(t, "Back", position + new Vector3(0f, 0.75f, -0.24f), new Vector3(1.8f, 0.5f, 0.1f), "Mat_WarmAccent", yaw);
        MapBuild.Decor(t, "Legs", position + new Vector3(0f, 0.2f, 0f), new Vector3(1.6f, 0.4f, 0.4f), "Mat_Dark", yaw);
    }

    // ================================================================== garden

    static void BuildGarden(Transform env)
    {
        GameObject group = Group(env, "Garden");
        Transform t = group.transform;

        MapBuild.Flat(t, "Path", new Vector3(-10f, 0f, 3f), new Vector2(9f, 1.6f), "Mat_Chalk");
        MapBuild.Flat(t, "Path_B", new Vector3(-10f, 0f, -1f), new Vector2(1.6f, 7f), "Mat_Chalk");

        float[] planterX = { -14f, -11f, -8f, -5f };
        float[] planterZ = { 5f, -3f };

        for (int ix = 0; ix < planterX.Length; ix++)
        {
            for (int iz = 0; iz < planterZ.Length; iz++)
            {
                // A garden bed used to be a 0.5 m planter the characters could not step into, with
                // a shrub painted on top of it. It is now a walk-in bush: a 0.2 m kerb (under the
                // 0.3 m stepOffset), 1.5 m of foliage, and a HideSpot that makes it real cover.
                MapBuild.CreateBush(t, "Bush_Garden_" + ix + "_" + iz,
                                    new Vector3(planterX[ix], 0f, planterZ[iz]), 1.1f);
            }
        }

        Tree(t, "Tree_A", new Vector3(-16f, 0f, 0.5f));
        Tree(t, "Tree_B", new Vector3(-3f, 0f, 4f));
        Tree(t, "Tree_C", new Vector3(-3f, 0f, -4f));
    }

    static void Tree(Transform parent, string name, Vector3 position)
    {
        GameObject group = Group(parent, name);
        Transform t = group.transform;

        MapBuild.Solid(t, "Trunk", position + new Vector3(0f, 1.5f, 0f), new Vector3(0.5f, 3f, 0.5f), "Mat_HouseWall");
        MapBuild.Decor(t, "Canopy", position + new Vector3(0f, 3.3f, 0f), new Vector3(3f, 0.9f, 3f), "Mat_Foliage");
    }

    // ============================================================ gate + walls

    static void BuildGate(Transform env)
    {
        GameObject group = Group(env, "Gate");
        Transform t = group.transform;

        // The school gate stands in the west plaza, out of every route.
        MapBuild.Solid(t, "GatePillar_N", new Vector3(-24f, 1.6f, 18.4f), new Vector3(0.8f, 3.2f, 0.8f), "Mat_Wall");
        MapBuild.Solid(t, "GatePillar_S", new Vector3(-24f, 1.6f, 15.6f), new Vector3(0.8f, 3.2f, 0.8f), "Mat_Wall");
        MapBuild.Decor(t, "GateArch", new Vector3(-24f, 3.4f, 17f), new Vector3(0.8f, 0.6f, 3.6f), "Mat_WarmAccent");
        MapBuild.Decor(t, "Signboard", new Vector3(-24f, 2.6f, 17f), new Vector3(0.2f, 0.9f, 3f), "Mat_Chalk");

        MapBuild.Solid(t, "Guardhouse", new Vector3(-28.5f, 1.3f, 13.5f), new Vector3(3f, 2.6f, 3f), "Mat_HouseWall");
        MapBuild.Decor(t, "GuardhouseRoof", new Vector3(-28.5f, 2.75f, 13.5f), new Vector3(3.6f, 0.3f, 3.6f), "Mat_TinRoof");
    }

    static void BuildPerimeter(Transform env)
    {
        GameObject group = Group(env, "Perimeter");
        Transform t = group.transform;

        // A low fence line just inside the north and south walls, so the edge reads as a campus.
        for (int i = 0; i < 8; i++)
        {
            float x = -26f + i * 7f;
            MapBuild.Decor(t, "FenceN_" + i, new Vector3(x, 0.6f, 19f), new Vector3(0.08f, 1.2f, 0.08f), "Mat_Dark");
            MapBuild.Decor(t, "FenceS_" + i, new Vector3(x, 0.6f, -19f), new Vector3(0.08f, 1.2f, 0.08f), "Mat_Dark");
        }

        // Roadside trees, all on the plaza edges so none of them stands in a route.
        Tree(t, "Tree_N1", new Vector3(-30f, 0f, 12f));
        Tree(t, "Tree_N2", new Vector3(30f, 0f, 12f));
        Tree(t, "Tree_S1", new Vector3(-30f, 0f, -12f));
        Tree(t, "Tree_S2", new Vector3(30f, 0f, -12f));
        Tree(t, "Tree_E1", new Vector3(30f, 0f, 6f));
        Tree(t, "Tree_E2", new Vector3(30f, 0f, -6f));
        Tree(t, "Tree_W1", new Vector3(-30f, 0f, 6f));
        Tree(t, "Tree_W2", new Vector3(-30f, 0f, -6f));
    }

    // ================================================================= helpers

    static GameObject Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }
}
