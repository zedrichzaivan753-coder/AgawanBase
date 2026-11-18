using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places the AI-generated hero props in the two new maps, fits a BoxCollider to each one, then
/// rebakes and re-proves the NavMesh. Run it once with execute_script (method Apply).
///
/// Every prop is a model prefab instance, rotated to its final orientation, scaled to a real-world
/// standing height, planted 2 cm into the ground so no face of it can share a plane with the ground
/// and z-fight, and added to the Environment layer so the NavMesh bakes it as an obstacle.
/// The prop positions stay outside the 8 m clear disc around each base and off the main avenues; the
/// VerifyRoutes pass at the end is the proof that they did not close any journey.
/// </summary>
public static class HeroProps
{
    const string PalengkeScene = "Assets/Scenes/Map_Palengke.unity";
    const string SchoolScene = "Assets/Scenes/Map_SchoolCampus.unity";

    const string StallModel = "Assets/Generated_Models/PalengkeStall/PalengkeStall.fbx";
    const string CrateModel = "Assets/Generated_Models/CrateStack/CrateStack.fbx";
    const string TricycleModel = "Assets/Generated_Models/Tricycle/Tricycle.fbx";
    const string HoopModel = "Assets/Generated_Models/BasketballHoop/BasketballHoop.fbx";
    const string BenchModel = "Assets/Generated_Models/SchoolBench/SchoolBench.fbx";

    struct Prop
    {
        public string model;
        public string name;
        public Vector3 position;
        public float yaw;
        public float pitch;
        public float roll;
        public float height;

        public Prop(string model, string name, Vector3 position, float yaw, float height,
                    float pitch = 0f, float roll = 0f)
        {
            this.model = model;
            this.name = name;
            this.position = position;
            this.yaw = yaw;
            this.pitch = pitch;
            this.roll = roll;
            this.height = height;
        }
    }

    /// <summary>
    /// How far below y = 0 a prop is planted. The ground's top face is at exactly y = 0, so a prop
    /// dropped to touch it shares that plane along its base and flashes. 2 cm of overlap hides every
    /// bottom face under the ground instead.
    /// </summary>
    const float GroundClip = -0.02f;

    public static void Apply()
    {
        // PALENGKE: two stalls with their counters to the market aisles, a crate stack squared to
        // the aisle, and a delivery tricycle parked along the east lane (Cross_E runs at x = 18).
        PlaceInScene(PalengkeScene, "Palengke", new Prop[]
        {
            new Prop(StallModel, "HeroStall_West", new Vector3(-20.5f, 0f, 6.5f), 90f, 2.7f),
            new Prop(StallModel, "HeroStall_East", new Vector3(20.5f, 0f, 6.5f), -90f, 2.7f),
            new Prop(CrateModel, "HeroCrates", new Vector3(-20.5f, 0f, -6.5f), 0f, 1.1f),
            new Prop(TricycleModel, "HeroTricycle", new Vector3(20.5f, 0f, -6.5f), 90f, 1.5f)
        });

        // SCHOOL: two benches laid flat on the walkways (the model is generated lying on its back,
        // so it is pitched -90 degrees to stand it up on its legs).
        PlaceInScene(SchoolScene, "School Campus", new Prop[]
        {
            new Prop(BenchModel, "HeroBench_North", new Vector3(15f, 0f, 9.4f), 180f, 0.5f, -90f, 0f),
            new Prop(BenchModel, "HeroBench_South", new Vector3(-6f, 0f, -9.9f), 0f, 0.5f, -90f, 0f)
        });

        PlacePrimitiveHoop();

        AddScenesToBuild();
    }

    static void PlaceInScene(string scenePath, string label, Prop[] props)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GameObject envObject = GameObject.Find("Game/Environment");

        if (envObject == null)
        {
            Debug.LogError("[Props] " + label + ": no Game/Environment, nothing placed.");
            return;
        }

        Transform old = envObject.transform.Find("HeroProps");
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        GameObject group = new GameObject("HeroProps");
        group.transform.SetParent(envObject.transform, false);

        int placed = 0;

        for (int i = 0; i < props.Length; i++)
        {
            if (Place(group.transform, props[i])) placed++;
        }

        MapBuild.EnsureSurfaceAndBake();
        MapBuild.VerifyRoutes();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("[Props] " + label + ": " + placed + " of " + props.Length +
                  " hero props placed, NavMesh rebaked, scene saved.");
    }

    static bool Place(Transform parent, Prop prop)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(prop.model);

        if (model == null)
        {
            Debug.LogError("[Props] missing model " + prop.model);
            return false;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.name = prop.name;
        instance.transform.SetParent(parent, false);

        Quaternion rotation = Quaternion.Euler(prop.pitch, prop.yaw, prop.roll);
        instance.transform.rotation = rotation;
        instance.transform.localScale = Vector3.one;

        if (!Measure(instance, out Bounds local) || local.size.y < 0.01f)
        {
            Debug.LogError("[Props] " + prop.name + ": the model has no measurable mesh.");
            Object.DestroyImmediate(instance);
            return false;
        }

        // Height is the prop's final STANDING height: the rotated footprint decides the scale, so a
        // model that has to be laid flat (a bench modelled on its end) still ends up 0.45 m tall.
        Vector3 rotatedSize = RotatedSize(rotation, local.size);
        float scale = prop.height / Mathf.Max(rotatedSize.y, 0.0001f);
        instance.transform.localScale = new Vector3(scale, scale, scale);

        Vector3 centreWorld = rotation * (local.center * scale);
        float halfHeight = rotatedSize.y * scale * 0.5f;

        instance.transform.position = new Vector3(prop.position.x - centreWorld.x,
                                                  GroundClip - (centreWorld.y - halfHeight),
                                                  prop.position.z - centreWorld.z);

        // One box around the whole prop: it is what the NavMesh and the characters collide with.
        // Center and size are model-space, so the box turns with the prop instead of sticking out.
        BoxCollider box = instance.GetComponent<BoxCollider>();
        if (box == null) box = instance.AddComponent<BoxCollider>();
        box.center = local.center;
        box.size = local.size;

        int environment = LayerMask.NameToLayer(MapBuild.EnvironmentLayer);
        if (environment >= 0) instance.layer = environment;

        SetStatic(instance);

        Vector3 finalSize = rotatedSize * scale;
        Debug.Log("[Props] " + prop.name + " planted at " + instance.transform.position.ToString("F2") +
                  " (yaw " + prop.yaw + ", pitch " + prop.pitch + ", roll " + prop.roll +
                  ") - stands " + prop.height.ToString("F2") + " m tall, world size " +
                  finalSize.ToString("F2") + ", base " + GroundClip.ToString("F2") + " m into the ground.");
        return true;
    }

    /// <summary>World-axis size of a model-space box after rotating it, i.e. the AABB it needs.</summary>
    static Vector3 RotatedSize(Quaternion rotation, Vector3 size)
    {
        Matrix4x4 r = Matrix4x4.Rotate(rotation);

        return new Vector3(
            Mathf.Abs(r.m00) * size.x + Mathf.Abs(r.m01) * size.y + Mathf.Abs(r.m02) * size.z,
            Mathf.Abs(r.m10) * size.x + Mathf.Abs(r.m11) * size.y + Mathf.Abs(r.m12) * size.z,
            Mathf.Abs(r.m20) * size.x + Mathf.Abs(r.m21) * size.y + Mathf.Abs(r.m22) * size.z);
    }

    /// <summary>
    /// The basketball hoop is the one prop the generator got wrong beyond rotating: the whole hoops
    /// (post, board, rim and net) is a single mesh whose board is a 3.6 m curved sheet with a
    /// scrambled rim beside it, so no angle makes it read as a backboard. This builds the same prop
    /// out of the map's own blocks instead - post, backboard, rim and net - so the court has a hoop
    /// that looks like a hoop.
    /// </summary>
    static void PlacePrimitiveHoop()
    {
        Scene scene = EditorSceneManager.OpenScene(SchoolScene, OpenSceneMode.Single);

        GameObject envObject = GameObject.Find("Game/Environment");
        Transform group = envObject != null ? envObject.transform.Find("HeroProps") : null;

        if (group == null)
        {
            Debug.LogError("[Props] School Campus: no HeroProps group to put the hoop in.");
            return;
        }

        Vector3 at = new Vector3(10f, 0f, 8.6f);

        // Post and its footing: the only solid parts, so the NavMesh bakes them as an obstacle.
        MapBuild.Solid(group, "HeroHoop_Foot", new Vector3(at.x, 0.05f, at.z), new Vector3(0.55f, 0.1f, 0.55f), "Mat_Dark");
        MapBuild.Solid(group, "HeroHoop_Post", new Vector3(at.x, 1.75f, at.z), new Vector3(0.16f, 3.4f, 0.16f), "Mat_Dark");

        // Backboard, square to the covered court (-z). The rim sits at 3.05 m, a real hoop height.
        float boardZ = at.z - 0.22f;
        MapBuild.Decor(group, "HeroHoop_Board", new Vector3(at.x, 3.5f, boardZ), new Vector3(1.8f, 1.05f, 0.07f), "Mat_Chalk");
        MapBuild.Decor(group, "HeroHoop_Target", new Vector3(at.x, 3.3f, boardZ - 0.05f), new Vector3(0.62f, 0.45f, 0.02f), "Mat_WarmAccent");

        // Rim: an octagon of eight small blocks, which reads as a ring from the game camera.
        float rimZ = boardZ - 0.34f;
        const int segments = 8;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 centre = new Vector3(at.x + Mathf.Cos(angle) * 0.23f, 3.05f, rimZ + Mathf.Sin(angle) * 0.23f);

            MapBuild.Decor(group, "HeroHoop_Rim_" + i, centre, new Vector3(0.18f, 0.05f, 0.05f),
                           "Mat_WarmAccent", -angle * Mathf.Rad2Deg);
        }

        // Net: four slats hanging inside the rim.
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
            Vector3 centre = new Vector3(at.x + Mathf.Cos(angle) * 0.19f, 2.86f, rimZ + Mathf.Sin(angle) * 0.19f);

            MapBuild.Decor(group, "HeroHoop_Net_" + i, centre, new Vector3(0.05f, 0.36f, 0.02f),
                           "Mat_Chalk", -angle * Mathf.Rad2Deg);
        }

        MapBuild.EnsureSurfaceAndBake();
        MapBuild.VerifyRoutes();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SchoolScene);

        Debug.Log("[Props] HeroHoop: the generated hoop mesh was replaced by a block hoop at (10.00, 0.00, 8.60), " +
                  "board facing the court, rim at 3.05 m; NavMesh rebaked, scene saved.");
    }

    /// <summary>Bounds of every mesh under the object, expressed in the object's own local space.</summary>
    static bool Measure(GameObject root, out Bounds local)
    {
        local = new Bounds();
        bool any = false;

        Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshFilter filter = renderers[i].GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;

            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;

            for (int c = 0; c < 8; c++)
            {
                Vector3 corner = new Vector3(
                    (c & 1) == 0 ? mesh.min.x : mesh.max.x,
                    (c & 2) == 0 ? mesh.min.y : mesh.max.y,
                    (c & 4) == 0 ? mesh.min.z : mesh.max.z);

                Vector3 point = matrix.MultiplyPoint3x4(corner);

                if (!any)
                {
                    local = new Bounds(point, Vector3.zero);
                    any = true;
                }
                else
                {
                    local.Encapsulate(point);
                }
            }
        }

        return any;
    }

    static void SetStatic(GameObject root)
    {
        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic |
                                  StaticEditorFlags.OccluderStatic |
                                  StaticEditorFlags.OccludeeStatic |
                                  StaticEditorFlags.ReflectionProbeStatic;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            GameObjectUtility.SetStaticEditorFlags(all[i].gameObject, flags);
        }
    }

    static void AddScenesToBuild()
    {
        string[] wanted =
        {
            "Assets/Scenes/Game.unity",
            PalengkeScene,
            SchoolScene
        };

        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        System.Collections.Generic.List<EditorBuildSettingsScene> list =
            new System.Collections.Generic.List<EditorBuildSettingsScene>();

        // Keep whatever is already in the list, then append what is missing.
        for (int i = 0; i < existing.Length; i++)
        {
            list.Add(new EditorBuildSettingsScene(existing[i].path, existing[i].enabled));
        }

        for (int i = 0; i < wanted.Length; i++)
        {
            bool found = false;

            for (int j = 0; j < list.Count; j++)
            {
                if (list[j].path == wanted[i]) { found = true; break; }
            }

            if (!found) list.Add(new EditorBuildSettingsScene(wanted[i], true));
        }

        EditorBuildSettings.scenes = list.ToArray();

        string paths = "";
        for (int i = 0; i < list.Count; i++) paths += "\n  [" + i + "] " + list[i].path;

        Debug.Log("[Props] build settings now hold " + list.Count + " scenes:" + paths);
    }
}
