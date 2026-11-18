using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// The shared map factory. Every map scene is built as a COPY of the one working gameplay scene,
/// so the HUD, the managers, the on-screen controls and the character roster are literally the
/// same objects - never re-created by hand, never re-wired by hand, never able to drift.
///
/// WHAT A MAP SCENE IS
/// -------------------
/// Only Game/Environment differs between maps. Everything else (MatchManager, GameManager, the
/// HUD, the joystick, the camera rig, the human player) stays exactly as it is, which is why
/// adding a map cannot break a rule of the game.
///
/// THE ONE DANGEROUS PART
/// ----------------------
/// Deleting Game/Environment also deletes every object the managers point at, so their serialized
/// references are left null. BindAll() puts them back by NAME, from the markers the map builder
/// just created. Every map must therefore end up with these exact objects:
///
///   Game/Environment/Ground            - the floor
///   Game/Environment/Base_Blue         - BaseZone, team Blue
///   Game/Environment/Base_Red          - BaseZone, team Red
///   Game/Environment/Flag_Blue         - Flag, owner Blue
///   Game/Environment/Flag_Red          - Flag, owner Red
///   Game/Environment/PrisonForBlue     - where BLUE prisoners are held (Red's half)
///   Game/Environment/PrisonForRed      - where RED prisoners are held (Blue's half)
///
/// Blue is always WEST and Red always EAST. That is not decoration: MatchManager.RescueExit and
/// PrisonSpot work out which way "home" is from the sign of x, and the AI's Intercept walks the
/// road home on the same assumption.
/// </summary>
public static class MapBuild
{
    /// <summary>The scene every map is cloned from. It is never modified by a map build.</summary>
    public const string SourceScenePath = "Assets/Scenes/Game.unity";

    /// <summary>Layer every solid piece of scenery goes on. Never a collider layer for triggers.</summary>
    public const string EnvironmentLayer = "Environment";

    // =============================================================== scene factory

    /// <summary>Copies the gameplay scene to a new map scene. Returns false if the copy failed.</summary>
    public static bool CloneScene(string targetPath)
    {
        EditorSceneManager.SaveOpenScenes();

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) != null)
        {
            AssetDatabase.DeleteAsset(targetPath);
            Debug.Log("[MapBuild] replaced the existing " + targetPath);
        }

        if (!AssetDatabase.CopyAsset(SourceScenePath, targetPath))
        {
            Debug.LogError("[MapBuild] could not copy " + SourceScenePath + " -> " + targetPath);
            return false;
        }

        AssetDatabase.Refresh();
        Debug.Log("[MapBuild] cloned " + SourceScenePath + " -> " + targetPath);
        return true;
    }

    /// <summary>Opens a map scene on its own, saving whatever was open first.</summary>
    public static bool Open(string scenePath)
    {
        EditorSceneManager.SaveOpenScenes();

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError("[MapBuild] could not open " + scenePath);
            return false;
        }

        return true;
    }

    /// <summary>Marks the open scene dirty and saves it to its own path.</summary>
    public static void SaveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[MapBuild] saved " + scene.path);
    }

    // ============================================================ environment root

    /// <summary>Deletes the old Game/Environment and returns a fresh empty one under Game.</summary>
    public static Transform RebuildEnvironment()
    {
        GameObject game = GameObject.Find("Game");
        if (game == null)
        {
            Debug.LogError("[MapBuild] there is no 'Game' object in this scene - it is not a map scene.");
            return null;
        }

        Transform old = game.transform.Find("Environment");
        if (old != null)
        {
            int removed = old.GetComponentsInChildren<Transform>(true).Length;
            Object.DestroyImmediate(old.gameObject);
            Debug.Log("[MapBuild] removed the old Environment (" + removed + " objects)");
        }

        flatLayer = 0;   // the floor-marking depth stack restarts with every rebuild

        GameObject root = new GameObject("Environment");
        root.transform.SetParent(game.transform, false);
        return root.transform;
    }

    // ================================================================== markers
    // These recreate the seven objects every manager points at.

    public static GameObject CreateGround(Transform env, float width, float depth, Vector2 centre)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.transform.SetParent(env, false);
        ground.transform.position = new Vector3(centre.x, -0.1f, centre.y);
        ground.transform.localScale = new Vector3(width, 0.2f, depth);

        Paint(ground, "Mat_Ground");
        MarkStatic(ground, true);   // this IS the walkable surface, so it must be navigable

        return ground;
    }

    public static BaseZone CreateBase(Transform env, Team team, Vector3 position, float radius)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = team == Team.Blue ? "Base_Blue" : "Base_Red";
        go.layer = LayerMask.NameToLayer("Interactable");
        go.transform.SetParent(env, false);
        go.transform.position = new Vector3(position.x, 0.04f, position.z);
        go.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);

        // The plate is a floor marking, not an obstacle: it must never push a character around.
        Object.DestroyImmediate(go.GetComponent<Collider>());

        Paint(go, "Mat_Zone");

        BaseZone zone = go.AddComponent<BaseZone>();
        zone.team = team;
        zone.radius = radius;
        return zone;
    }

    /// <summary>
    /// A walk-in bush a character can hide inside. Every map plants its bushes through this one
    /// method, so hiding is the same mechanic everywhere instead of three lookalike props.
    ///
    /// Two parts, on purpose:
    ///   Bed     - a 0.2 m kerb, Solid. Deliberately lower than the characters' 0.3 stepOffset, so
    ///             nobody is stopped at the edge of a bush; the NavMesh still reads it.
    ///   Foliage - Decor (no collider), 1.5 m tall, so it covers a standing body without ever
    ///             trapping one or carving the NavMesh.
    /// The HideSpot on the group is what actually hides somebody - a plain XZ distance test.
    /// </summary>
    public static HideSpot CreateBush(Transform parent, string name, Vector3 position, float radius)
    {
        GameObject go = new GameObject(name);
        go.layer = LayerMask.NameToLayer(EnvironmentLayer);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(position.x, 0f, position.z);

        Solid(go.transform, "Bed", new Vector3(position.x, 0.1f, position.z),
              new Vector3(radius * 2f, 0.2f, radius * 2f), "Mat_HouseWall");

        Decor(go.transform, "Foliage", new Vector3(position.x, 0.85f, position.z),
              new Vector3(radius * 1.9f, 1.5f, radius * 1.9f), "Mat_Foliage");

        HideSpot spot = go.AddComponent<HideSpot>();
        spot.radius = radius * 0.85f;
        return spot;
    }

    public static Flag CreateFlag(Transform env, Team owner, Vector3 position)
    {
        GameObject go = new GameObject(owner == Team.Blue ? "Flag_Blue" : "Flag_Red");
        go.layer = LayerMask.NameToLayer("Interactable");
        go.transform.SetParent(env, false);
        go.transform.position = new Vector3(position.x, 0f, position.z);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(go.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        pole.transform.localScale = new Vector3(0.09f, 1.2f, 0.09f);
        Object.DestroyImmediate(pole.GetComponent<Collider>());
        Paint(pole, "Mat_Dark");

        GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banner.name = "Banner";
        banner.transform.SetParent(go.transform, false);
        banner.transform.localPosition = new Vector3(0.32f, 2.0f, 0f);
        banner.transform.localScale = new Vector3(0.62f, 0.72f, 0.05f);
        Object.DestroyImmediate(banner.GetComponent<Collider>());
        Paint(banner, owner == Team.Blue ? "Mat_Blue" : "Mat_Red");

        Flag flag = go.AddComponent<Flag>();
        flag.ownerTeam = owner;
        return flag;
    }

    /// <summary>
    /// The cage a team's PRISONERS are held in. Named for the team it holds, exactly like the
    /// original: PrisonForBlue sits in Red's half.
    /// </summary>
    public static Transform CreatePrison(Transform env, Team prisonerTeam, Vector3 position)
    {
        GameObject go = new GameObject(prisonerTeam == Team.Blue ? "PrisonForBlue" : "PrisonForRed");
        go.layer = LayerMask.NameToLayer("Interactable");
        go.transform.SetParent(env, false);
        go.transform.position = new Vector3(position.x, 0f, position.z);

        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Plate";
        plate.transform.SetParent(go.transform, false);
        plate.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        plate.transform.localScale = new Vector3(3f, 0.05f, 3f);
        Object.DestroyImmediate(plate.GetComponent<Collider>());
        Paint(plate, "Mat_Chalk");

        // Four corner posts, so the cage reads as a cage from above without blocking the way in.
        float[][] corners =
        {
            new float[] { -1.4f, -1.4f },
            new float[] {  1.4f, -1.4f },
            new float[] { -1.4f,  1.4f },
            new float[] {  1.4f,  1.4f }
        };

        for (int i = 0; i < corners.Length; i++)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Post_" + (i + 1);
            post.transform.SetParent(go.transform, false);
            post.transform.localPosition = new Vector3(corners[i][0], 0.6f, corners[i][1]);
            post.transform.localScale = new Vector3(0.14f, 1.2f, 0.14f);
            Object.DestroyImmediate(post.GetComponent<Collider>());
            Paint(post, prisonerTeam == Team.Blue ? "Mat_Red" : "Mat_Blue");
        }

        return go.transform;
    }

    /// <summary>Four walls around the playing area. Nothing may leave the map.</summary>
    public static void CreateBoundary(Transform env, float width, float depth, Vector2 centre,
                                      float height, float thickness)
    {
        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;

        Solid(env, "Wall_N", new Vector3(centre.x, height * 0.5f, centre.y + halfD - thickness * 0.5f),
              new Vector3(width, height, thickness), "Mat_Wall");

        Solid(env, "Wall_S", new Vector3(centre.x, height * 0.5f, centre.y - halfD + thickness * 0.5f),
              new Vector3(width, height, thickness), "Mat_Wall");

        Solid(env, "Wall_E", new Vector3(centre.x + halfW - thickness * 0.5f, height * 0.5f, centre.y),
              new Vector3(thickness, height, depth), "Mat_Wall");

        Solid(env, "Wall_W", new Vector3(centre.x - halfW + thickness * 0.5f, height * 0.5f, centre.y),
              new Vector3(thickness, height, depth), "Mat_Wall");
    }

    // ==================================================================== props

    /// <summary>
    /// A solid object: gets its BoxCollider, goes on the Environment layer, and is baked into the
    /// NavMesh. Use this for anything a character must not walk through.
    /// </summary>
    public static GameObject Solid(Transform parent, string name, Vector3 centre, Vector3 size,
                                   string materialName, float yawDegrees = 0f)
    {
        GameObject go = Block(parent, name, centre, size, materialName, yawDegrees);
        MarkStatic(go, true);
        return go;
    }

    /// <summary>
    /// A decoration: no collider, so it can never trap a character or carve the NavMesh - and it
    /// is cheap. Use this for anything a character may walk through (or over).
    /// </summary>
    public static GameObject Decor(Transform parent, string name, Vector3 centre, Vector3 size,
                                   string materialName, float yawDegrees = 0f)
    {
        GameObject go = Block(parent, name, centre, size, materialName, yawDegrees);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        MarkStatic(go, false);
        return go;
    }

    // The ground's top face sits at exactly y = 0 (the ground cube is 0.2 tall, centred on -0.1).
    // Every floor marking used to share one plane 0.000 - 0.024 above it, so a road crossing the
    // court, or one path crossing another, z-fought and flashed. Each marking now gets its own thin
    // layer: 6 mm of clearance over the ground, then 1.5 mm per marking, so no two ever share a plane.
    const float FlatGroundGap = 0.006f;
    const float FlatLayerStep = 0.0015f;
    static int flatLayer;

    /// <summary>A flat slice lying on the ground, e.g. a road, a court or a chalk line.</summary>
    public static GameObject Flat(Transform parent, string name, Vector3 centre, Vector2 size,
                                  string materialName, float yawDegrees = 0f)
    {
        float y = FlatGroundGap + 0.012f + flatLayer++ * FlatLayerStep;

        GameObject go = Decor(parent, name, new Vector3(centre.x, y, centre.z),
                              new Vector3(size.x, 0.024f, size.y), materialName, yawDegrees);

        // Paint does not cast shadows: a 2.4 cm slab throwing its own shadow on the ground directly
        // under it is shadow acne waiting to happen, and it reads as a second kind of flicker.
        MeshRenderer markings = go.GetComponent<MeshRenderer>();
        if (markings != null)
        {
            markings.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            markings.receiveShadows = false;
        }

        return go;
    }

    static GameObject Block(Transform parent, string name, Vector3 centre, Vector3 size,
                            string materialName, float yawDegrees)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.layer = LayerMask.NameToLayer(EnvironmentLayer);
        go.transform.SetParent(parent, false);
        go.transform.position = centre;
        go.transform.localScale = size;

        if (Mathf.Abs(yawDegrees) > 0.001f)
        {
            go.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        Paint(go, materialName);
        return go;
    }

    // ================================================================== binding

    /// <summary>
    /// Puts every deleted reference back by looking the markers up by NAME, sets the map's camera
    /// framing, and points the (untouched) human player at this map's base and enemy flag.
    /// </summary>
    public static void BindAll(MapDefinition map)
    {
        Transform env = GameObject.Find("Game/Environment") != null
            ? GameObject.Find("Game/Environment").transform
            : null;

        if (env == null)
        {
            Debug.LogError("[MapBuild] no Game/Environment, so nothing can be bound.");
            return;
        }

        BaseZone blueBase = MarkerComponent<BaseZone>(env, "Base_Blue");
        BaseZone redBase = MarkerComponent<BaseZone>(env, "Base_Red");
        Flag blueFlag = MarkerComponent<Flag>(env, "Flag_Blue");
        Flag redFlag = MarkerComponent<Flag>(env, "Flag_Red");
        Transform prisonForBlue = Marker(env, "PrisonForBlue");
        Transform prisonForRed = Marker(env, "PrisonForRed");

        if (blueBase == null || redBase == null || blueFlag == null || redFlag == null ||
            prisonForBlue == null || prisonForRed == null)
        {
            Debug.LogError("[MapBuild] the map is missing a base, flag or prison marker. " +
                           "MatchManager cannot start with an unbound map.");
            return;
        }

        // --- the rules ---
        MatchManager match = Object.FindAnyObjectByType<MatchManager>(FindObjectsInactive.Include);
        if (match != null)
        {
            match.blueBase = blueBase;
            match.redBase = redBase;
            match.blueFlag = blueFlag;
            match.redFlag = redFlag;
            match.prisonForBlue = prisonForBlue;
            match.prisonForRed = prisonForRed;
        }
        else Debug.LogError("[MapBuild] no MatchManager in the scene.");

        // --- the spawner ---
        CharacterSpawner spawner = Object.FindAnyObjectByType<CharacterSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            spawner.baseBlue = blueBase;
            spawner.baseRed = redBase;
            spawner.flagBlue = blueFlag;
            spawner.flagRed = redFlag;
        }
        else Debug.LogError("[MapBuild] no CharacterSpawner in the scene.");

        // --- the screens ---
        GameManager game = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (game != null)
        {
            game.blueFlag = blueFlag;
            game.redFlag = redFlag;

            // Restart and Title reload THIS scene, so the name has to follow the map.
            game.sceneName = SceneManager.GetActiveScene().name;
        }
        else Debug.LogError("[MapBuild] no GameManager in the scene.");

        HudController hud = Object.FindAnyObjectByType<HudController>(FindObjectsInactive.Include);
        if (hud != null)
        {
            hud.blueFlag = blueFlag;
            hud.redFlag = redFlag;
        }

        // --- characters that are PLACED in the scene (the human) ---
        // Spawned AI are configured by CharacterSpawner, but BluePlayer is hand-placed, so its own
        // home base and the flag its rig carries were nulled with the old Environment.
        CharacterStatus[] placed = Object.FindObjectsByType<CharacterStatus>(FindObjectsInactive.Include);

        int rebound = 0;
        for (int i = 0; i < placed.Length; i++)
        {
            CharacterStatus status = placed[i];
            if (status == null) continue;

            if (status.homeBase == null)
            {
                status.homeBase = status.team == Team.Blue ? blueBase : redBase;
                rebound++;
            }

            CharacterRigAnimator rig = status.GetComponent<CharacterRigAnimator>();
            if (rig != null && rig.flag == null)
            {
                // A character can only ever pick up the OTHER team's flag.
                rig.flag = status.team == Team.Blue ? redFlag : blueFlag;
                rebound++;
            }
        }

        // --- the camera ---
        FollowCamera camera = Object.FindAnyObjectByType<FollowCamera>(FindObjectsInactive.Include);
        if (camera != null && map != null)
        {
            camera.arenaCenter = map.arenaCenter;
            camera.arenaSize = map.arenaSize;
            camera.visibleWidthPercent = map.visibleWidthPercent;
        }

        Debug.Log("[MapBuild] bound " + (map != null ? map.displayName : "map") +
                  ": bases/flags/prisons wired, " + rebound + " placed-character slots restored, " +
                  "camera framing " + (map != null ? map.arenaSize.ToString() : "unchanged") + ".");
    }

    // ============================================================== navigation

    /// <summary>Adds the NavMeshSurface if it is missing and bakes this scene's NavMesh.</summary>
    public static void EnsureSurfaceAndBake()
    {
        Transform env = GameObject.Find("Game/Environment") != null
            ? GameObject.Find("Game/Environment").transform
            : null;

        if (env == null)
        {
            Debug.LogError("[MapBuild] no Game/Environment to bake a NavMesh for.");
            return;
        }

        NavMeshSurface surface = env.GetComponent<NavMeshSurface>();
        if (surface == null) surface = env.gameObject.AddComponent<NavMeshSurface>();

        // CollectObjects.All + PhysicsColliders + a LayerMask: the NavMesh is cooked from the
        // exact BoxColliders of the scenery on the Ground and Environment layers, and from
        // nothing else. Decorations have no collider, characters are on the Characters layer and
        // the UI is on UI - so no awning, no character and no canvas can ever carve a hole in
        // the route the AI walks.
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = LayerMaskOf("Ground", "Environment");

        // Agent size (radius 0.5, height 2, slope 45) comes from the project's Humanoid agent
        // type, which already matches these 1 m wide, 1.55 m tall characters. Only the gap
        // filter is set here: it keeps slivers out of the way of the AI's routing.
        surface.minRegionArea = 2f;

        surface.BuildNavMesh();

        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        int triangles = tri.indices.Length / 3;

        if (triangles == 0)
        {
            Debug.LogError("[MapBuild] the NavMesh for " + SceneManager.GetActiveScene().name +
                           " came out EMPTY - the AI will have no routing at all. Check that the " +
                           "Ground is on the Ground layer with its BoxCollider.");
        }
        else
        {
            Debug.Log("[MapBuild] baked a NavMesh with " + triangles + " triangles for " +
                      SceneManager.GetActiveScene().name + ".");
        }

        EditorUtility.SetDirty(surface);
    }

    // ================================================================== report

    /// <summary>
    /// Proves the map is PLAYABLE, not just built: every important pair of places has to be
    /// connected by a complete path. A map can look perfect and still be a dead end for the AI.
    /// </summary>
    public static void VerifyRoutes()
    {
        string scene = SceneManager.GetActiveScene().name;

        Transform env = GameObject.Find("Game/Environment") != null
            ? GameObject.Find("Game/Environment").transform
            : null;

        if (env == null)
        {
            Debug.LogError("[Routes] " + scene + ": no Game/Environment.");
            return;
        }

        // Every journey the game needs to be able to make.
        string[][] journeys =
        {
            new string[] { "Base_Blue", "Base_Red" },
            new string[] { "Base_Red", "Base_Blue" },
            new string[] { "Base_Blue", "Flag_Red" },
            new string[] { "Base_Red", "Flag_Blue" },
            new string[] { "Base_Blue", "PrisonForRed" },
            new string[] { "Base_Red", "PrisonForBlue" }
        };

        NavMeshPath path = new NavMeshPath();
        int complete = 0;

        for (int i = 0; i < journeys.Length; i++)
        {
            Vector3 from = env.Find(journeys[i][0]).position;
            Vector3 to = env.Find(journeys[i][1]).position;

            bool routable = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) &&
                            path.status == NavMeshPathStatus.PathComplete;

            float length = 0f;
            for (int c = 1; c < path.corners.Length; c++)
            {
                length += Vector3.Distance(path.corners[c - 1], path.corners[c]);
            }

            if (routable) complete++;

            string line = "[Routes] " + scene + ": " + journeys[i][0] + " -> " + journeys[i][1] + "  " +
                          path.status + "  " + path.corners.Length + " corners, " +
                          length.ToString("F1") + " m";

            if (routable) Debug.Log(line);
            else Debug.LogError(line + "   ** THIS JOURNEY IS NOT POSSIBLE **");
        }

        Debug.Log("[Routes] " + scene + ": " + complete + " of " + journeys.Length +
                  " required journeys are routable.");
    }

    /// <summary>Prints what a built map actually contains. Used as the build's own proof.</summary>
    public static void Report(string label)
    {
        Transform env = GameObject.Find("Game/Environment") != null
            ? GameObject.Find("Game/Environment").transform
            : null;

        if (env == null)
        {
            Debug.LogError("[MapBuild] " + label + ": no Game/Environment.");
            return;
        }

        int objects = env.GetComponentsInChildren<Transform>(true).Length - 1;
        int solids = 0;
        int colliders = 0;
        int renderers = 0;

        Collider[] found = env.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < found.Length; i++)
        {
            colliders++;
            if (found[i].GetComponent<MeshRenderer>() != null) solids++;
        }

        renderers = env.GetComponentsInChildren<MeshRenderer>(true).Length;

        Vector3 ground = Vector3.zero;
        GameObject groundObject = GameObject.Find("Game/Environment/Ground");
        if (groundObject != null) ground = groundObject.transform.localScale;

        Debug.Log("[MapBuild] " + label + ": " + objects + " objects, " + solids + " solid pieces, " +
                  colliders + " colliders, " + renderers + " renderers, ground " +
                  ground.x.ToString("F0") + " x " + ground.z.ToString("F0") + " m.");
    }

    // ================================================================= helpers

    static Transform Marker(Transform env, string name)
    {
        Transform found = env.Find(name);
        if (found == null) Debug.LogError("[MapBuild] missing marker: " + name);
        return found;
    }

    static T MarkerComponent<T>(Transform env, string name) where T : Component
    {
        Transform found = Marker(env, name);
        if (found == null) return null;

        T component = found.GetComponent<T>();
        if (component == null) Debug.LogError("[MapBuild] " + name + " has no " + typeof(T).Name);
        return component;
    }

    /// <summary>Gives a renderer one of the project's existing materials, so the palette stays shared.</summary>
    public static void Paint(GameObject go, string materialName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + materialName + ".mat");

        if (material == null)
        {
            Debug.LogWarning("[MapBuild] material not found: " + materialName);
            return;
        }

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    /// <summary>Joins layer names into a mask, ignoring any that do not exist.</summary>
    static LayerMask LayerMaskOf(params string[] names)
    {
        int mask = 0;

        for (int i = 0; i < names.Length; i++)
        {
            int layer = LayerMask.NameToLayer(names[i]);
            if (layer < 0)
            {
                Debug.LogWarning("[MapBuild] layer not found: " + names[i]);
                continue;
            }

            mask |= 1 << layer;
        }

        return mask;
    }

    /// <summary>
    /// Marks a piece of scenery static so the renderer is batched with its neighbours and
    /// occlusion-tested. The NavMesh is NOT driven from here: NavMeshSurface collects the
    /// Ground and Environment colliders by layer, which is what keeps collider-less decorations
    /// out of the baked route.
    /// </summary>
    static void MarkStatic(GameObject go, bool solid)
    {
        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic |
                                  StaticEditorFlags.OccluderStatic |
                                  StaticEditorFlags.OccludeeStatic;

        GameObjectUtility.SetStaticEditorFlags(go, flags);
    }
}
