using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the PALENGKE map: a 64 x 40 m Filipino public market.
///
/// The shape of it, from above:
///
///   NORTH EDGE  - one long delivery road, clear from wall to wall   (flank route 1)
///   z +9..+11   - sari-sari and dry-goods shop row
///   z +4..+6    - market stall row
///   z -4..+4    - the main market avenue, 8 m wide, crossing the whole map (main route)
///   z -6..-4    - market stall row
///   z -11..-9   - wet market (fish and meat) stall row
///   SOUTH EDGE  - one long delivery road, clear from wall to wall   (flank route 2)
///
/// Blue's base is at the WEST end, Red's at the EAST end, and there are three independent ways
/// between them - north road, main avenue, south road - plus the 3 m aisles between the stalls,
/// so neither team ever has to walk a single corridor.
///
/// Everything a character must not walk through is a MapBuild.Solid (BoxCollider + Navigation
/// Static). Everything a character may brush past - awnings, umbrellas, tarpaulins, baskets,
/// hanging signs - is a MapBuild.Decor: no collider, no navmesh hole, one renderer.
/// </summary>
public static class PalengkeBuild
{
    const string ScenePath = "Assets/Scenes/Map_Palengke.unity";
    const string MapAsset = "Assets/Config/Map_Palengke.asset";

    const float Width = 64f;
    const float Depth = 40f;
    static readonly Vector2 Centre = new Vector2(0f, 0f);

    static readonly Vector3 BlueBase = new Vector3(-26f, 0f, 0f);
    static readonly Vector3 RedBase = new Vector3(26f, 0f, 0f);
    static readonly Vector3 BluePrison = new Vector3(19f, 0f, -12f);   // holds BLUE prisoners, in Red's half
    static readonly Vector3 RedPrison = new Vector3(-19f, 0f, 12f);    // holds RED prisoners, in Blue's half

    /// <summary>Stall row centres in z, and the x of every stall in a row (6 m pitch, 3 m stalls).</summary>
    static readonly float[] RowZ = { -10f, -5f, 5f, 10f };
    static readonly float[] StallX = { -15f, -9f, -3f, 3f, 9f, 15f };

    static readonly string[] RoofMaterials = { "Mat_TinRoof", "Mat_WarmAccent", "Mat_TinRoof", "Mat_Blue" };
    static readonly string[] CounterMaterials = { "Mat_HouseWall", "Mat_Wall", "Mat_HouseWall", "Mat_WarmAccent" };

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

        // ---- the market itself ----------------------------------------------
        BuildRoads(env);
        BuildMarketRows(env);
        BuildShopRow(env, "Shops_North", 14f, "Mat_TinRoof");
        BuildShopRow(env, "Shops_South", -14f, "Mat_TinRoof");
        BuildPlazas(env);
        BuildProps(env);

        MapDefinition map = AssetDatabase.LoadAssetAtPath<MapDefinition>(MapAsset);
        MapBuild.BindAll(map);
        MapBuild.EnsureSurfaceAndBake();
        MapBuild.VerifyRoutes();
        MapBuild.Report("Palengke");
        MapBuild.SaveScene();
    }

    // ================================================================== roads

    /// <summary>The concrete network everything else sits on. Walk-through: floor markings only.</summary>
    static void BuildRoads(Transform env)
    {
        GameObject group = Group(env, "Roads");
        Transform t = group.transform;

        MapBuild.Flat(t, "Road_Main", new Vector3(0f, 0f, 0f), new Vector2(Width - 2f, 6f), "Mat_Asphalt");
        MapBuild.Flat(t, "Road_North", new Vector3(0f, 0f, 17.5f), new Vector2(Width - 2f, 4f), "Mat_Asphalt");
        MapBuild.Flat(t, "Road_South", new Vector3(0f, 0f, -17.5f), new Vector2(Width - 2f, 4f), "Mat_Asphalt");

        // Three cross-aisles through the stall block, so the rows are not walls.
        MapBuild.Flat(t, "Cross_W", new Vector3(-18f, 0f, 0f), new Vector2(3f, 34f), "Mat_Asphalt");
        MapBuild.Flat(t, "Cross_Centre", new Vector3(0f, 0f, 0f), new Vector2(4f, 34f), "Mat_Asphalt");
        MapBuild.Flat(t, "Cross_E", new Vector3(18f, 0f, 0f), new Vector2(3f, 34f), "Mat_Asphalt");
    }

    // ============================================================ market stalls

    /// <summary>Four rows of stalls, each stall 3 m wide with a 3 m aisle on either side.</summary>
    static void BuildMarketRows(Transform env)
    {
        GameObject group = Group(env, "MarketRows");

        for (int r = 0; r < RowZ.Length; r++)
        {
            GameObject row = Group(group.transform, "Row_" + r + "_z" + RowZ[r].ToString("F0"));

            for (int i = 0; i < StallX.Length; i++)
            {
                BuildStall(row.transform, "Stall_" + r + "_" + i, StallX[i], RowZ[r],
                           CounterMaterials[r], RoofMaterials[r], i % 3 == 0);
            }
        }
    }

    /// <summary>One vendor stall: a solid counter with a canopy over it and produce on top.</summary>
    static void BuildStall(Transform parent, string name, float x, float z,
                           string counterMaterial, string roofMaterial, bool withCrates)
    {
        GameObject group = Group(parent, name);
        Transform t = group.transform;

        // The counter is the only SOLID part: 3 x 0.9 x 1.8 m. That is what makes the stall an
        // obstacle and what fills the NavMesh around the aisle.
        MapBuild.Solid(t, "Counter", new Vector3(x, 0.45f, z), new Vector3(3f, 0.9f, 1.8f), counterMaterial);

        // Canopy, posts and goods: decoration. A post thick enough to carve the NavMesh would make
        // 2 m of aisle unusable, so none of this may collide.
        MapBuild.Decor(t, "Awning", new Vector3(x, 2.25f, z), new Vector3(3.5f, 0.12f, 2.5f), roofMaterial);
        MapBuild.Decor(t, "Post_W", new Vector3(x - 1.65f, 1.15f, z - 1.15f), new Vector3(0.12f, 2.3f, 0.12f), "Mat_Dark");
        MapBuild.Decor(t, "Post_E", new Vector3(x + 1.65f, 1.15f, z - 1.15f), new Vector3(0.12f, 2.3f, 0.12f), "Mat_Dark");

        // Produce laid out on the counter.
        MapBuild.Decor(t, "Goods_A", new Vector3(x - 0.85f, 1.0f, z), new Vector3(1.1f, 0.22f, 1.4f), "Mat_Foliage");
        MapBuild.Decor(t, "Goods_B", new Vector3(x + 0.85f, 1.0f, z), new Vector3(1.1f, 0.22f, 1.4f), "Mat_WarmAccent");

        // Hanging price board, above head height so it reads without blocking the view.
        MapBuild.Decor(t, "Sign", new Vector3(x, 1.75f, z - 0.95f), new Vector3(1.6f, 0.35f, 0.06f), "Mat_Chalk");

        if (withCrates)
        {
            MapBuild.Solid(t, "Crate_A", new Vector3(x + 1.9f, 0.4f, z + 1.6f), new Vector3(0.8f, 0.8f, 0.8f), "Mat_WarmAccent");
            MapBuild.Solid(t, "Crate_B", new Vector3(x + 1.9f, 1.2f, z + 1.6f), new Vector3(0.7f, 0.7f, 0.7f), "Mat_TinRoof");
        }
    }

    // ================================================================= shops

    /// <summary>A row of small sari-sari style shops, 5 m wide with 3 m gaps between them.</summary>
    static void BuildShopRow(Transform env, string groupName, float z, string roofMaterial)
    {
        GameObject group = Group(env, groupName);

        float[] xs = { -14f, -7f, 0f, 7f, 14f };

        for (int i = 0; i < xs.Length; i++)
        {
            GameObject shop = Group(group.transform, "Shop_" + i);
            Transform t = shop.transform;
            float x = xs[i];

            MapBuild.Solid(t, "Body", new Vector3(x, 1.6f, z), new Vector3(5f, 3.2f, 3f), "Mat_HouseWall");
            MapBuild.Decor(t, "Roof", new Vector3(x, 3.35f, z), new Vector3(5.6f, 0.3f, 3.6f), roofMaterial);
            MapBuild.Decor(t, "Awning", new Vector3(x, 2.3f, z - Mathf.Sign(z) * 1.9f), new Vector3(5.2f, 0.12f, 1.6f), "Mat_TinRoof");
            MapBuild.Decor(t, "Counter", new Vector3(x, 1.0f, z - Mathf.Sign(z) * 1.55f), new Vector3(3.6f, 0.18f, 0.7f), "Mat_Chalk");
            MapBuild.Decor(t, "Signboard", new Vector3(x, 2.75f, z - Mathf.Sign(z) * 1.62f), new Vector3(3.2f, 0.7f, 0.08f), "Mat_WarmAccent");
        }
    }

    // ================================================================= plazas

    /// <summary>The two end plazas, kept clear so the bases, spawns and cages are never boxed in.</summary>
    static void BuildPlazas(Transform env)
    {
        GameObject group = Group(env, "Plazas");

        MapBuild.Flat(group.transform, "Plaza_Blue", new Vector3(-26f, 0f, 0f), new Vector2(12f, 20f), "Mat_Ground");
        MapBuild.Flat(group.transform, "Plaza_Red", new Vector3(26f, 0f, 0f), new Vector2(12f, 20f), "Mat_Ground");

        // A few decorative palms around each plaza, kept well off the base plate.
        Palm(group.transform, "Palm_Blue_N", new Vector3(-30f, 0f, 8f));
        Palm(group.transform, "Palm_Blue_S", new Vector3(-30f, 0f, -8f));
        Palm(group.transform, "Palm_Red_N", new Vector3(30f, 0f, 8f));
        Palm(group.transform, "Palm_Red_S", new Vector3(30f, 0f, -8f));
    }

    static void Palm(Transform parent, string name, Vector3 position)
    {
        GameObject group = Group(parent, name);
        Transform t = group.transform;

        MapBuild.Solid(t, "Trunk", position + new Vector3(0f, 1.4f, 0f), new Vector3(0.34f, 2.8f, 0.34f), "Mat_HouseWall");
        MapBuild.Decor(t, "Canopy", position + new Vector3(0f, 3.1f, 0f), new Vector3(2.4f, 0.35f, 2.4f), "Mat_Foliage");
    }

    // ================================================================== props

    /// <summary>Loose market clutter: carts, tricycles, bins, umbrellas, tarpaulins, deliveries.</summary>
    static void BuildProps(Transform env)
    {
        GameObject group = Group(env, "Props");
        Transform t = group.transform;

        // --- the delivery area at the east end, next to Red's base ---
        Jeepney(t, "Jeepney_Delivery", new Vector3(24f, 0f, -16f), 90f);
        Tricycle(t, "Tricycle_A", new Vector3(17f, 0f, 15.5f), 0f);
        Tricycle(t, "Tricycle_B", new Vector3(14f, 0f, 16.5f), 0f);

        // --- the west end, next to Blue's base ---
        Jeepney(t, "Jeepney_Market", new Vector3(-24f, 0f, 16f), 0f);
        Tricycle(t, "Tricycle_C", new Vector3(-17f, 0f, -16.5f), 180f);

        // --- clutter through the market, never in an aisle centre ---
        CrateStack(t, "Crates_A", new Vector3(-12f, 0f, -12.6f));
        CrateStack(t, "Crates_B", new Vector3(12f, 0f, 12.6f));
        CrateStack(t, "Crates_C", new Vector3(-3f, 0f, -12.6f));
        CrateStack(t, "Crates_D", new Vector3(6f, 0f, 12.6f));

        Bin(t, "Bin_A", new Vector3(-20f, 0f, -3f));
        Bin(t, "Bin_B", new Vector3(20f, 0f, 3f));

        Umbrella(t, "Umb_A", new Vector3(-21f, 0f, 5f));
        Umbrella(t, "Umb_B", new Vector3(21f, 0f, -5f));

        Tarp(t, "Tarp_A", new Vector3(-6f, 0f, -7.5f));
        Tarp(t, "Tarp_B", new Vector3(6f, 0f, 7.5f));
    }

    static void CrateStack(Transform parent, string name, Vector3 position)
    {
        GameObject group = Group(parent, name);
        Transform t = group.transform;

        // The group sits ON the stack so its hide volume is centred on it. Children are placed in
        // world coordinates below, so the offset is unaffected by this.
        t.position = position;

        MapBuild.Solid(t, "Crate_A", position + new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f), "Mat_WarmAccent");
        MapBuild.Solid(t, "Crate_B", position + new Vector3(0.9f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f), "Mat_WarmAccent");
        MapBuild.Solid(t, "Crate_C", position + new Vector3(0.45f, 1.1f, 0f), new Vector3(0.7f, 0.7f, 0.7f), "Mat_TinRoof");
        MapBuild.Decor(t, "Basket", position + new Vector3(1.7f, 0.25f, 0f), new Vector3(0.6f, 0.5f, 0.6f), "Mat_Foliage");

        // Chest-high crates are the market's cover: crouch behind them and the enemy loses you.
        // Same HideSpot the open-field bushes use, so there is only one hiding rule in the game.
        HideSpot spot = group.AddComponent<HideSpot>();
        spot.radius = 1.15f;
    }

    static void Bin(Transform parent, string name, Vector3 position)
    {
        MapBuild.Solid(parent, name, position + new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.6f), "Mat_Dark");
    }

    static void Umbrella(Transform parent, string name, Vector3 position)
    {
        GameObject group = Group(parent, name);
        Transform t = group.transform;

        MapBuild.Decor(t, "Pole", position + new Vector3(0f, 1.1f, 0f), new Vector3(0.08f, 2.2f, 0.08f), "Mat_Dark");
        MapBuild.Decor(t, "Canopy", position + new Vector3(0f, 2.25f, 0f), new Vector3(2.2f, 0.16f, 2.2f), "Mat_WarmAccent");
    }

    static void Tarp(Transform parent, string name, Vector3 position)
    {
        MapBuild.Decor(parent, name, position + new Vector3(0f, 2.6f, 0f), new Vector3(4f, 0.08f, 2.5f), "Mat_Blue");
    }

    /// <summary>A parked jeepney: solid, because a delivery vehicle blocks the road it is on.</summary>
    static void Jeepney(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject group = Group(parent, name);
        group.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Transform t = group.transform;

        MapBuild.Solid(t, "Body", position + new Vector3(0f, 0.95f, 0f), new Vector3(4.6f, 1.5f, 1.8f), "Mat_WarmAccent", yaw);
        MapBuild.Decor(t, "Roof", position + new Vector3(0f, 1.85f, 0f), new Vector3(4.4f, 0.14f, 1.75f), "Mat_TinRoof", yaw);
        MapBuild.Decor(t, "Windshield", position + new Vector3(2.15f, 1.35f, 0f), new Vector3(0.12f, 0.7f, 1.6f), "Mat_Dark", yaw);

        float[] wheelX = { -1.5f, 1.5f };
        float[] wheelZ = { -0.95f, 0.95f };

        for (int ix = 0; ix < wheelX.Length; ix++)
        {
            for (int iz = 0; iz < wheelZ.Length; iz++)
            {
                MapBuild.Decor(t, "Wheel", position + new Vector3(wheelX[ix], 0.38f, wheelZ[iz] * 1f),
                               new Vector3(0.7f, 0.7f, 0.24f), "Mat_Dark", yaw);
            }
        }
    }

    /// <summary>A parked tricycle, the small sidecar taxi of every Philippine street.</summary>
    static void Tricycle(Transform parent, string name, Vector3 position, float yaw)
    {
        GameObject group = Group(parent, name);
        Transform t = group.transform;

        MapBuild.Solid(t, "Body", position + new Vector3(0f, 0.62f, 0f), new Vector3(2.1f, 0.9f, 1.2f), "Mat_Blue", yaw);
        MapBuild.Decor(t, "Roof", position + new Vector3(0f, 1.5f, 0f), new Vector3(2.2f, 0.12f, 1.35f), "Mat_Dark", yaw);
        MapBuild.Decor(t, "Wheel_F", position + new Vector3(1.0f, 0.3f, 0f), new Vector3(0.55f, 0.55f, 0.16f), "Mat_Dark", yaw);
        MapBuild.Decor(t, "Wheel_B", position + new Vector3(-1.0f, 0.3f, 0f), new Vector3(0.55f, 0.55f, 0.16f), "Mat_Dark", yaw);
    }

    // ================================================================= helpers

    static GameObject Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }
}
