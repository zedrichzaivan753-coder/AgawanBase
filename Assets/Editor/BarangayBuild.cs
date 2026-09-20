using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the whole barangay street set for Agawan Base. Re-runnable: every object is
/// found by name and updated in place, so running it twice never duplicates anything.
///
/// It creates GEOMETRY ONLY. It never touches a gameplay script, a collider in the play
/// area, the render pipeline, the NavMesh, or an imported asset.
/// </summary>
public static class BarangayBuild
{
    const string MAT = "Assets/Materials";
    static int made, updated;

    // ==================================================================== entry

    public static void All()
    {
        made = 0; updated = 0;

        Palette();
        CameraAndLighting();
        Arena();
        Foundation();
        CourtMarkings();
        Prisons();
        Flags();
        Backdrop();
        Vehicles();
        Hud();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Report();
    }

    // ==================================================================== palette

    public static void Palette()
    {
        // Retint the three existing backdrop materials. Mat_Blue and Mat_Red are NEVER
        // touched and NEVER used by decor, so the two team colours stay unambiguous.
        Retint("Mat_Ground", new Color(0.42f, 0.46f, 0.30f));
        Retint("Mat_Wall",   new Color(0.62f, 0.58f, 0.52f));
        Retint("Mat_Zone",   new Color(0.86f, 0.84f, 0.79f));

        Lit("Mat_Asphalt",    new Color(0.32f, 0.31f, 0.30f));
        Lit("Mat_Chalk",      new Color(0.94f, 0.92f, 0.86f));
        // Deliberately a touch darker and greyer than the sky above it, so the roofline and
        // the wall silhouettes still separate from the sky band. Still warm, still low sat.
        Lit("Mat_HouseWall",  new Color(0.82f, 0.74f, 0.60f));
        Lit("Mat_TinRoof",    new Color(0.58f, 0.42f, 0.33f));
        Lit("Mat_Foliage",    new Color(0.38f, 0.50f, 0.32f));
        Lit("Mat_Skin",       new Color(0.85f, 0.66f, 0.50f));
        Lit("Mat_Dark",       new Color(0.20f, 0.19f, 0.20f));
        Lit("Mat_WarmAccent", new Color(0.86f, 0.58f, 0.24f));
        Blob("Mat_Blob", new Color(0f, 0f, 0f, 0.35f));

        AssetDatabase.SaveAssets();
    }

    // ============================================================ camera + lighting

    public static void CameraAndLighting()
    {
        GameObject rig = FindPath("Game/CameraRig");
        GameObject camGo = FindPath("Game/CameraRig/Main Camera");
        if (rig == null || camGo == null) { Debug.LogError("[Build] camera rig missing."); return; }

        // 35 degrees off vertical = a 55 degree camera tilt. Yaw stays 0, so the virtual
        // joystick still maps 1:1 onto world X/Z: stick-up is still up the screen.
        rig.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);

        Camera cam = camGo.GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            // Framing for the team-sized arena. Arena() sets the same value, so the two can never
            // drift apart: 12.2 only ever framed the old 32 x 18 solo arena.
            cam.orthographicSize = 14f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.97f, 0.85f, 0.70f, 1f);   // warm afternoon sky
        }
        camGo.transform.localPosition = new Vector3(0f, 27.7f, 3.3f);

        GameObject lightGo = FindPath("Game/CameraRig/Directional Light");
        if (lightGo != null)
        {
            Light l = lightGo.GetComponent<Light>();
            if (l != null)
            {
                l.shadows = LightShadows.None;            // hard limit: no real-time shadows
                l.color = new Color(1.0f, 0.95f, 0.86f);  // warm afternoon sun
                l.intensity = 1.15f;
            }
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.50f, 0.47f, 0.42f);
        RenderSettings.fog = false;
    }

    // ==================================================================== arena

    /// <summary>
    /// Resizes the play area for team matches (up to 4v4), and NOTHING else.
    ///
    /// Ground, the four walls, the base plates and the camera are set by writing their Transform
    /// and component fields DIRECTLY - they are deliberately never routed through Prim(), which
    /// destroys whatever collider it finds. Ground and Wall_N/S/E/W carry the BoxColliders that
    /// hold every character up and keep them in; deleting those would drop the whole roster
    /// through the floor.
    ///
    /// The arena grows SOUTH and EAST/WEST only. Wall_N stays at z = 8.75, because that is what
    /// preserves the tuned backdrop: it is the wall that hides where the arena ends, and every
    /// backdrop body sits at z >= 10.15. Pushing the north wall out would open a strip of bare
    /// ground between the wall and the houses.
    /// </summary>
    public static void Arena()
    {
        // 35.5 x 20, its south edge pulled out from z = -9 to z = -11.
        Move("Game/Environment/Ground", new Vector3(0f, -0.1f, -1f), new Vector3(35.5f, 0.2f, 20f));

        // North wall: widened to cover the new width, but left exactly where it was.
        Move("Game/Environment/Wall_N", new Vector3(0f, 1f, 8.75f),  new Vector3(35.5f, 2f, 0.5f));
        Move("Game/Environment/Wall_S", new Vector3(0f, 1f, -10.75f), new Vector3(35.5f, 2f, 0.5f));
        Move("Game/Environment/Wall_E", new Vector3(17.5f, 1f, -1f),  new Vector3(0.5f, 2f, 20f));
        Move("Game/Environment/Wall_W", new Vector3(-17.5f, 1f, -1f), new Vector3(0.5f, 2f, 20f));

        // Bases stay on the x = +-11 lane and shrink from 8 m to 6.4 m, so the safe circle still
        // exactly matches the plate a character can see under their feet.
        ShrinkBase("Game/Environment/Base_Blue", -11f);
        ShrinkBase("Game/Environment/Base_Red", 11f);

        // The two slim posts behind the bases follow the smaller plates outward.
        Move("Game/Environment/Decor_Backdrop/BasePost_Blue", new Vector3(-11f, 0f, 4.9f), null);
        Move("Game/Environment/Decor_Backdrop/BasePost_Red",  new Vector3(11f, 0f, 4.9f), null);

        // A wider arena only helps if the player can SEE it: the orthographic size grows so the
        // new south edge and both corners stay on screen. Yaw stays 0, so the joystick is still
        // 1:1 with world X/Z.
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographicSize = 14f;
            EditorUtility.SetDirty(cam);
        }
    }

    /// <summary>Writes a position (and optionally a scale) straight onto an existing object.
    /// Does NOT touch its collider - see the Arena() comment.</summary>
    static void Move(string path, Vector3 localPosition, Vector3? localScale)
    {
        GameObject go = FindPath(path);
        if (go == null) { Debug.LogWarning("[Build] Arena: missing " + path); return; }

        go.transform.localPosition = localPosition;
        if (localScale.HasValue) go.transform.localScale = localScale.Value;

        EditorUtility.SetDirty(go);
    }

    /// <summary>Resizes a base plate and its safe circle together, so they can never disagree.</summary>
    static void ShrinkBase(string path, float x)
    {
        GameObject go = FindPath(path);
        if (go == null) { Debug.LogWarning("[Build] Arena: missing " + path); return; }

        go.transform.localPosition = new Vector3(x, 0.04f, 0f);
        go.transform.localScale = new Vector3(6.4f, 0.04f, 6.4f);

        BaseZone zone = go.GetComponent<BaseZone>();
        if (zone != null) zone.radius = 3.2f;

        EditorUtility.SetDirty(go);
    }

    // ================================================================ foundation

    public static void Foundation()
    {
        Transform court = Group("Game/Environment", "Decor_Court");

        // Outside world floor. Its FAR EDGE at z = 16.5 is what puts a horizon on screen
        // (about 92% up), so a strip of warm sky shows above the rooftops. Extending it
        // further would fill the whole top of the frame with flat ground.
        Prim(court, PrimitiveType.Cube, "Floor_Outside",
             new Vector3(0f, -0.04f, -0.3f), new Vector3(62f, 0.06f, 37.4f), "Mat_Ground");

        // The street/court slab. Tucks slightly under the four walls so there is no gap.
        Prim(court, PrimitiveType.Cube, "Court_Slab",
             new Vector3(0f, 0.015f, -1f), new Vector3(34.5f, 0.03f, 19f), "Mat_Asphalt");
    }

    // ============================================================ chalk markings

    public static void CourtMarkings()
    {
        Transform court = Group("Game/Environment", "Decor_Court");

        const float y = 0.04f;
        Vector3 alongZ = new Vector3(0.1f, 0.02f, 19f);
        Vector3 alongX = new Vector3(33.6f, 0.02f, 0.1f);

        Prim(court, PrimitiveType.Cube, "Line_Sideline_W", new Vector3(-16.75f, y, -1f), alongZ, "Mat_Chalk");
        Prim(court, PrimitiveType.Cube, "Line_Sideline_E", new Vector3( 16.75f, y, -1f), alongZ, "Mat_Chalk");
        Prim(court, PrimitiveType.Cube, "Line_Baseline_N", new Vector3(0f, y,  8.4f), alongX, "Mat_Chalk");
        Prim(court, PrimitiveType.Cube, "Line_Baseline_S", new Vector3(0f, y, -10.4f), alongX, "Mat_Chalk");
        Prim(court, PrimitiveType.Cube, "Line_Halfway",    new Vector3(0f, y, -1f),   alongZ, "Mat_Chalk");
        Dashes(court, "Line_CentreCircle", new Vector3(0f, 0f, -1f), 3f, 12, 1.55f, 0.1f, y, "Mat_Chalk");
    }

    // ============================================================== bases/prisons

    public static void Prisons()
    {
        Transform court = Group("Game/Environment", "Decor_Court");
        const float y = 0.04f;

        // Team-coloured dashed rings at radius 4.35: just OUTSIDE the 4 m safe circle and
        // outside the 8x8 base plate (top y = 0.06), so nothing intersects the plate.
        GameObject bb = FindPath("Game/Environment/Base_Blue");
        GameObject br = FindPath("Game/Environment/Base_Red");
        if (bb != null) Dashes(court, "Ring_Blue", Flat(bb.transform.position), 3.55f, 16, 1.1f, 0.14f, y, "Mat_Blue");
        if (br != null) Dashes(court, "Ring_Red",  Flat(br.transform.position), 3.55f, 16, 1.1f, 0.14f, y, "Mat_Red");

        // Chalk squares around the two 3x3 prison plates.
        GameObject pr = FindPath("Game/Environment/PrisonForRed");
        GameObject pb = FindPath("Game/Environment/PrisonForBlue");
        if (pr != null) ChalkSquare(court, "ChalkPrison_Red",  Flat(pr.transform.position), y);
        if (pb != null) ChalkSquare(court, "ChalkPrison_Blue", Flat(pb.transform.position), y);

        // The eight prison posts were Mat_Red and competed with a Red enemy. Repaint them
        // warm concrete so red on the field now means only "enemy / their ring".
        Material wall = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/Mat_Wall.mat");
        string[] prisons = { "Game/Environment/PrisonForRed", "Game/Environment/PrisonForBlue" };
        for (int p = 0; p < prisons.Length; p++)
        {
            for (int i = 1; i <= 4; i++)
            {
                GameObject post = FindPath(prisons[p] + "/Post_" + i);
                if (post == null) continue;
                Renderer r = post.GetComponent<Renderer>();
                if (r != null && wall != null) r.sharedMaterial = wall;
            }
        }
    }

    // ==================================================================== flags

    public static void Flags()
    {
        // Only the CHILD meshes are resized. Flag.cs owns the ROOT (it records HomePosition
        // and homeRotation in Awake and moves/rotates the root), so the root is untouched.
        BiggerFlag("Game/Environment/Flag_Red");
        BiggerFlag("Game/Environment/Flag_Blue");
    }

    // ================================================================= backdrop

    public static void Backdrop()
    {
        Transform bd = Group("Game/Environment", "Decor_Backdrop");

        // Everything here is BEYOND the far wall (z > 10.2), which is exactly the line
        // above which the 2 m wall stops hiding the ground. Nothing is inside the arena,
        // and nothing has a collider.
        //
        // Far row of tin-roof houses. Kept deliberately SHORT (ridge tops land around 91%
        // of screen height) so the ground horizon at 92% leaves a strip of warm sky above
        // the skyline instead of the roofs running off the top of the frame.
        House(bd, "House_Far_1", new Vector3(-19f, 0f, 13.6f),  6f, 4.2f, 3.0f, 2.8f);
        House(bd, "House_Far_2", new Vector3( -9f, 0f, 13.5f), -4f, 4.8f, 3.4f, 3.0f);
        House(bd, "House_Far_3", new Vector3(  7f, 0f, 13.7f),  3f, 4.4f, 3.3f, 2.8f);
        House(bd, "House_Far_4", new Vector3( 17f, 0f, 13.5f), -7f, 5.0f, 2.9f, 3.0f);

        // Nearer row: the sari-sari store plus two houses and two small sheds. These sit
        // just clear of the wall so they are fully visible above its silhouette, and lower
        // than the far row, which is what makes the backdrop read as layers rather than a wall.
        Store(bd, "SariSari_Store", new Vector3(-10.5f, 0f, 11.4f), 2f);
        House(bd, "House_Mid_1", new Vector3(  2.0f, 0f, 11.4f), -3f, 4.0f, 2.6f, 2.4f);
        House(bd, "House_Mid_2", new Vector3( 13.0f, 0f, 11.3f),  5f, 4.4f, 2.5f, 2.4f);
        House(bd, "Shed_W",      new Vector3(-19.5f, 0f, 11.2f), -5f, 3.4f, 2.1f, 2.2f);
        House(bd, "Shed_E",      new Vector3( 20.5f, 0f, 11.3f),  8f, 3.6f, 2.0f, 2.2f);
        // Coconut trees, tall enough to break the skyline between the roofs.
        Coconut(bd, "Coconut_1", new Vector3(-15f, 0f, 13.2f));
        Coconut(bd, "Coconut_2", new Vector3( -4f, 0f, 13.0f));
        Coconut(bd, "Coconut_3", new Vector3( 4.5f, 0f, 13.4f));
        Coconut(bd, "Coconut_4", new Vector3( 15f, 0f, 13.1f));
        Coconut(bd, "Coconut_5", new Vector3( -23f, 0f, 12.6f));
        Coconut(bd, "Coconut_6", new Vector3(  23f, 0f, 12.6f));
        Coconut(bd, "Coconut_7", new Vector3( 10f, 0f, 15.3f));

        // Utility poles. The two at x = +-14 are the tall ones the wires hang from; they
        // are deliberately NOT behind the in-arena bases, so nothing lines up twice.
        UtilityPole(bd, "UtilityPole_W", new Vector3(-14f, 0f, 10.7f),   7.0f, 0.36f);
        UtilityPole(bd, "UtilityPole_E", new Vector3( 14f, 0f, 10.7f),   7.0f, 0.36f);
        UtilityPole(bd, "UtilityPole_WW", new Vector3(-22.5f, 0f, 11.4f), 6.4f, 0.34f);
        UtilityPole(bd, "UtilityPole_EE", new Vector3( 22.5f, 0f, 11.4f), 6.4f, 0.34f);

        // Banderitas: two bunting runs, pennants only, well above head height and well
        // outside the arena.
        Banderitas(bd, "Banderitas_A", new Vector3(0f, 4.4f, 10.4f), 32f, 10);
        Banderitas(bd, "Banderitas_B", new Vector3(0f, 3.8f, 13.2f), 40f, 10);

        // Wires between the pole tops.
        Prim(bd, PrimitiveType.Cube, "Wire_A", new Vector3(0f, 6.1f, 10.7f), new Vector3(28f, 0.05f, 0.05f), "Mat_Dark");
        Prim(bd, PrimitiveType.Cube, "Wire_B", new Vector3(0f, 5.6f, 11.4f), new Vector3(45f, 0.05f, 0.05f), "Mat_Dark");

        // ---- the ONLY decor inside the play area ----
        // Two slim utility posts on the FAR side of each base, 1.6 m outside the safe
        // circle and off the base-to-base lane. 0.18 m thick, so even if a character stands
        // directly behind one, the post covers under a third of their width.
        UtilityPole(bd, "BasePost_Blue", new Vector3(-11f, 0f, 4.9f), 7.0f, 0.18f);
        UtilityPole(bd, "BasePost_Red",  new Vector3( 11f, 0f, 4.9f), 7.0f, 0.18f);
    }

    public static void Vehicles()
    {
        Transform vh = Group("Game/Environment", "Decor_Vehicles");
        Jeepney(vh, "Jeepney", new Vector3(6.5f, 0f, 10.8f), 12f);
        Tricycle(vh, "Tricycle_W", new Vector3(-16.5f, 0f, 10.9f), -8f);
        Tricycle(vh, "Tricycle_E", new Vector3( 17.5f, 0f, 11.0f), 14f);
    }

    // ====================================================================== hud

    public static void Hud()
    {
        // The SPRINT button used to sit at (-250, 250) from the bottom-right corner, which put
        // it straight on top of PrisonForBlue at 4:3 (where the arena fills the full width) and
        // clipped it at 16:9. Nudging it up and out to the corner clears the prison at every
        // aspect AND keeps it clear of the Red flag banner, which sits at canvas x <= 1609 at
        // 4:3. It stays inside the bottom-right corner, so it is still a right-thumb button.
        // Only its RectTransform moves. SprintButton's own script is untouched.
        GameObject sprint = FindPath("Game/UI/Panel_Hud/SprintButton");
        if (sprint == null) { Debug.LogWarning("[Build] SprintButton not found."); return; }

        RectTransform rt = sprint.GetComponent<RectTransform>();
        if (rt == null) { Debug.LogWarning("[Build] SprintButton has no RectTransform."); return; }

        rt.anchoredPosition = new Vector2(-180f, 340f);
        EditorUtility.SetDirty(rt);
        Debug.Log("[Build] SPRINT button moved to anchoredPosition " + rt.anchoredPosition.ToString("F0") +
                  " (was -250, 250) so it no longer covers PrisonForBlue.");
    }

    // ==================================================================== report

    public static void Report()
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[Build] no Main Camera."); return; }

        Vector3 fwd = cam.transform.forward;
        float pitch = Mathf.Atan2(-fwd.y, new Vector3(fwd.x, 0f, fwd.z).magnitude) * Mathf.Rad2Deg;

        int renderers = 0, decorColliders = 0, decorTri = 0;
        string[] groups = { "Game/Environment/Decor_Court", "Game/Environment/Decor_Backdrop", "Game/Environment/Decor_Vehicles" };
        for (int g = 0; g < groups.Length; g++)
        {
            GameObject root = FindPath(groups[g]);
            if (root == null) continue;
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                renderers++;
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) decorTri += mf.sharedMesh.triangles.Length / 3;
            }
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true)) decorColliders++;
        }

        Debug.Log(string.Format(
            "[Build] objects created={0} updated={1}\n" +
            "  camera world={2} pitch={3:F1}deg orthoSize={4:F1} aspect={5:F2}\n" +
            "  arena near edge z=-11 at {6:F1}% of screen height\n" +
            "  far wall top      at {7:F1}%\n" +
            "  ground horizon z=18.4 at {8:F1}%  (above that = sky)\n" +
            "  decor renderers={9} decor colliders={10} decor tris={11}",
            made, updated,
            cam.transform.position.ToString("F2"), pitch, cam.orthographicSize, cam.aspect,
            Frac(new Vector3(0f, 0f, -11f)) * 100f,
            Frac(new Vector3(0f, 2f, 8.75f)) * 100f,
            Frac(new Vector3(0f, 0f, 18.4f)) * 100f,
            renderers, decorColliders, decorTri));
    }

    static float Frac(Vector3 world)
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return 0f;
        float vy = Vector3.Dot(world - cam.transform.position, cam.transform.up);
        return (vy + cam.orthographicSize) / (2f * cam.orthographicSize);
    }

    // =================================================================== props

    static void House(Transform parent, string name, Vector3 pos, float yaw, float w, float h, float d)
    {
        GameObject root = Group(parent, name);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Prim(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), "Mat_HouseWall");
        Gable(root.transform, w, h, d, 0.22f);

        Prim(root.transform, PrimitiveType.Cube, "Door", new Vector3(0f, 0.8f, -d * 0.5f - 0.03f), new Vector3(0.8f, 1.6f, 0.06f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Window_L", new Vector3(-w * 0.3f, h * 0.62f, -d * 0.5f - 0.03f), new Vector3(0.7f, 0.6f, 0.06f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Window_R", new Vector3( w * 0.3f, h * 0.62f, -d * 0.5f - 0.03f), new Vector3(0.7f, 0.6f, 0.06f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Awning", new Vector3(0f, Mathf.Min(h * 0.7f, 2.6f), -d * 0.5f - 0.45f), new Vector3(w * 0.9f, 0.06f, 0.9f), "Mat_TinRoof");
    }

    static void Store(Transform parent, string name, Vector3 pos, float yaw)
    {
        GameObject root = Group(parent, name);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        float w = 5.0f, h = 2.8f, d = 2.4f;
        Prim(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), "Mat_HouseWall");
        Gable(root.transform, w, h, d, 0.20f);

        // Open front: a counter, and sachet rows on the wall behind it.
        Prim(root.transform, PrimitiveType.Cube, "Counter", new Vector3(0f, 0.5f, -d * 0.5f - 0.35f), new Vector3(w * 0.9f, 1.0f, 0.7f), "Mat_TinRoof");
        Prim(root.transform, PrimitiveType.Cube, "Sachets_1", new Vector3(-w * 0.28f, 2.3f, -d * 0.5f - 0.05f), new Vector3(1.0f, 0.5f, 0.06f), "Mat_WarmAccent");
        Prim(root.transform, PrimitiveType.Cube, "Sachets_2", new Vector3(0f,        2.3f, -d * 0.5f - 0.05f), new Vector3(1.0f, 0.5f, 0.06f), "Mat_WarmAccent");
        Prim(root.transform, PrimitiveType.Cube, "Sachets_3", new Vector3( w * 0.28f, 2.3f, -d * 0.5f - 0.05f), new Vector3(1.0f, 0.5f, 0.06f), "Mat_WarmAccent");
        Prim(root.transform, PrimitiveType.Cube, "Drinks",    new Vector3( w * 0.34f, 1.05f, -d * 0.5f - 0.35f), new Vector3(1.0f, 1.0f, 0.6f), "Mat_Dark");

        // Striped awning over the counter.
        for (int i = 0; i < 5; i++)
        {
            Prim(root.transform, PrimitiveType.Cube, "Awning_" + i,
                 new Vector3(-w * 0.4f + i * w * 0.2f, h - 0.15f, -d * 0.5f - 0.65f),
                 new Vector3(w * 0.2f, 0.07f, 1.3f),
                 (i % 2 == 0) ? "Mat_WarmAccent" : "Mat_HouseWall");
        }
    }

    /// <summary>Two sloped tin sheets meeting at a ridge, plus a ridge cap.</summary>
    static void Gable(Transform root, float w, float h, float d, float riseRatio)
    {
        float rise = w * riseRatio;
        float half = w * 0.5f;
        float slope = Mathf.Sqrt(half * half + rise * rise);
        float ang = Mathf.Atan2(rise, half) * Mathf.Rad2Deg;

        Prim(root, PrimitiveType.Cube, "Roof_L", new Vector3(-w * 0.25f, h + rise * 0.5f, 0f),
             new Vector3(slope, 0.10f, d * 1.18f), "Mat_TinRoof", new Vector3(0f, 0f, ang));
        Prim(root, PrimitiveType.Cube, "Roof_R", new Vector3(w * 0.25f, h + rise * 0.5f, 0f),
             new Vector3(slope, 0.10f, d * 1.18f), "Mat_TinRoof", new Vector3(0f, 0f, -ang));
        Prim(root, PrimitiveType.Cube, "Roof_Ridge", new Vector3(0f, h + rise, 0f),
             new Vector3(0.20f, 0.10f, d * 1.18f), "Mat_TinRoof");
    }

    static void Coconut(Transform parent, string name, Vector3 pos)
    {
        GameObject root = Group(parent, name);
        root.transform.localPosition = pos;

        // Taller than any roof, so the palm crowns break the skyline into the sky band.
        const float trunkH = 5.2f;
        // Unity's Cylinder is 2 units tall at scale 1, so scale.y = height / 2.
        Prim(root.transform, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, trunkH * 0.5f, 0f),
             new Vector3(0.34f, trunkH * 0.5f, 0.34f), "Mat_TinRoof");

        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f;
            const float droop = 12f;
            float rad = a * Mathf.Deg2Rad, dr = droop * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(dr) * Mathf.Sin(rad), -Mathf.Sin(dr), Mathf.Cos(dr) * Mathf.Cos(rad));
            Vector3 p = new Vector3(0f, trunkH, 0f) + dir * 0.85f;
            Prim(root.transform, PrimitiveType.Cube, "Frond_" + i, p,
                 new Vector3(0.22f, 0.09f, 1.8f), "Mat_Foliage", new Vector3(droop, a, 0f));
        }

        Prim(root.transform, PrimitiveType.Cube, "Coconut_1", new Vector3(0.16f, trunkH - 0.16f, 0.14f), new Vector3(0.2f, 0.2f, 0.2f), "Mat_TinRoof");
        Prim(root.transform, PrimitiveType.Cube, "Coconut_2", new Vector3(-0.18f, trunkH - 0.22f, -0.1f), new Vector3(0.2f, 0.2f, 0.2f), "Mat_TinRoof");
    }

    static void UtilityPole(Transform parent, string name, Vector3 pos, float h, float thickness)
    {
        GameObject root = Group(parent, name);
        root.transform.localPosition = pos;

        Prim(root.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, h * 0.5f, 0f),
             new Vector3(thickness, h * 0.5f, thickness), "Mat_TinRoof");
        Prim(root.transform, PrimitiveType.Cube, "CrossArm", new Vector3(0f, h - 0.7f, 0f), new Vector3(2.4f, 0.16f, 0.16f), "Mat_TinRoof");
        Prim(root.transform, PrimitiveType.Cube, "Insulator_L", new Vector3(-0.9f, h - 0.5f, 0f), new Vector3(0.14f, 0.24f, 0.14f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Insulator_R", new Vector3( 0.9f, h - 0.5f, 0f), new Vector3(0.14f, 0.24f, 0.14f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Transformer", new Vector3(0.4f, h - 1.7f, 0.2f), new Vector3(0.5f, 0.7f, 0.5f), "Mat_Dark");
    }

    static void Banderitas(Transform parent, string name, Vector3 pos, float length, int count)
    {
        GameObject root = Group(parent, name);
        root.transform.localPosition = pos;

        Prim(root.transform, PrimitiveType.Cube, "Line", Vector3.zero, new Vector3(length, 0.04f, 0.04f), "Mat_Dark");

        float step = length / count;
        for (int i = 0; i < count; i++)
        {
            float x = -length * 0.5f + step * (i + 0.5f);
            string colour = (i % 3 == 0) ? "Mat_WarmAccent" : ((i % 3 == 1) ? "Mat_Blue" : "Mat_Chalk");
            // Pennants are the only decor allowed to use Mat_Blue: they are 10 m outside the
            // arena and 4 m up, so they can never be confused with a player.
            Prim(root.transform, PrimitiveType.Cube, "Pennant_" + i,
                 new Vector3(x, -0.3f, 0f), new Vector3(step * 0.55f, 0.55f, 0.05f), colour);
        }
    }

    static void Jeepney(Transform parent, string name, Vector3 pos, float yaw)
    {
        GameObject root = Group(parent, name);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Prim(root.transform, PrimitiveType.Cube, "Chassis", new Vector3(0f, 0.35f, 0f), new Vector3(1.95f, 0.4f, 4.3f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, 0.9f, -0.1f), new Vector3(1.9f, 0.75f, 4.1f), "Mat_WarmAccent");
        Prim(root.transform, PrimitiveType.Cube, "Cabin", new Vector3(0f, 1.65f, -0.35f), new Vector3(1.75f, 0.9f, 2.9f), "Mat_WarmAccent");
        Prim(root.transform, PrimitiveType.Cube, "Roof", new Vector3(0f, 2.15f, -0.35f), new Vector3(1.8f, 0.1f, 3.1f), "Mat_TinRoof");
        Prim(root.transform, PrimitiveType.Cube, "Hood", new Vector3(0f, 1.25f, 1.8f), new Vector3(1.75f, 0.45f, 0.8f), "Mat_HouseWall");
        Prim(root.transform, PrimitiveType.Cube, "Windshield", new Vector3(0f, 1.55f, 1.35f), new Vector3(1.65f, 0.6f, 0.08f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Stripe", new Vector3(0f, 1.05f, -0.1f), new Vector3(1.94f, 0.18f, 4.12f), "Mat_Chalk");

        Wheel(root.transform, "Wheel_FL", new Vector3(-0.98f, 0.34f,  1.35f));
        Wheel(root.transform, "Wheel_FR", new Vector3( 0.98f, 0.34f,  1.35f));
        Wheel(root.transform, "Wheel_RL", new Vector3(-0.98f, 0.34f, -1.35f));
        Wheel(root.transform, "Wheel_RR", new Vector3( 0.98f, 0.34f, -1.35f));
    }

    static void Tricycle(Transform parent, string name, Vector3 pos, float yaw)
    {
        GameObject root = Group(parent, name);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        Prim(root.transform, PrimitiveType.Cube, "Body", new Vector3(-0.35f, 0.45f, 0f), new Vector3(0.7f, 0.5f, 1.5f), "Mat_WarmAccent");
        Prim(root.transform, PrimitiveType.Cube, "Roof", new Vector3(0.1f, 1.35f, 0f), new Vector3(1.0f, 0.08f, 1.6f), "Mat_TinRoof");
        Prim(root.transform, PrimitiveType.Cube, "Post_F", new Vector3(0.1f, 0.95f, 0.62f), new Vector3(0.06f, 0.8f, 0.06f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Post_B", new Vector3(0.1f, 0.95f, -0.62f), new Vector3(0.06f, 0.8f, 0.06f), "Mat_Dark");
        Prim(root.transform, PrimitiveType.Cube, "Sidecar", new Vector3(0.72f, 0.5f, 0f), new Vector3(0.7f, 0.6f, 1.3f), "Mat_HouseWall");
        Prim(root.transform, PrimitiveType.Cube, "Headlight", new Vector3(-0.35f, 0.6f, 0.78f), new Vector3(0.25f, 0.25f, 0.06f), "Mat_Chalk");

        Wheel(root.transform, "Wheel_F", new Vector3(-0.35f, 0.3f,  0.62f));
        Wheel(root.transform, "Wheel_RL", new Vector3(-0.35f, 0.3f, -0.62f));
        Wheel(root.transform, "Wheel_RR", new Vector3( 0.62f, 0.3f, -0.3f));
    }

    static void Wheel(Transform root, string name, Vector3 pos)
    {
        Prim(root, PrimitiveType.Cube, name, pos, new Vector3(0.3f, 0.62f, 0.62f), "Mat_Dark");
    }

    // ================================================================== helpers

    static void BiggerFlag(string rootPath)
    {
        GameObject root = FindPath(rootPath);
        if (root == null) { Debug.LogWarning("[Build] flag missing: " + rootPath); return; }

        Transform pole = root.transform.Find("Pole");
        if (pole != null)
        {
            pole.localPosition = new Vector3(0f, 1.2f, 0f);
            pole.localScale = new Vector3(0.22f, 2.4f, 0.22f);
        }

        Transform banner = root.transform.Find("Banner");
        if (banner != null)
        {
            banner.localPosition = new Vector3(-0.525f, 1.95f, 0f);
            banner.localScale = new Vector3(1.05f, 0.9f, 0.14f);
        }
    }

    static void ChalkSquare(Transform parent, string name, Vector3 centre, float y)
    {
        const float half = 1.7f;
        const float len = 3.5f;
        Prim(parent, PrimitiveType.Cube, name + "_N", new Vector3(centre.x, y, centre.z + half), new Vector3(len, 0.02f, 0.12f), "Mat_Chalk");
        Prim(parent, PrimitiveType.Cube, name + "_S", new Vector3(centre.x, y, centre.z - half), new Vector3(len, 0.02f, 0.12f), "Mat_Chalk");
        Prim(parent, PrimitiveType.Cube, name + "_E", new Vector3(centre.x + half, y, centre.z), new Vector3(0.12f, 0.02f, len), "Mat_Chalk");
        Prim(parent, PrimitiveType.Cube, name + "_W", new Vector3(centre.x - half, y, centre.z), new Vector3(0.12f, 0.02f, len), "Mat_Chalk");
    }

    static void Dashes(Transform parent, string name, Vector3 centre, float radius, int count,
                       float dashLength, float width, float y, string materialName)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(centre.x + Mathf.Sin(rad) * radius, y, centre.z + Mathf.Cos(rad) * radius);
            Prim(parent, PrimitiveType.Cube, name + "_" + i, pos,
                 new Vector3(width, 0.02f, dashLength), materialName, new Vector3(0f, angle + 90f, 0f));
        }
    }

    static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    static Transform Group(string parentPath, string name)
    {
        GameObject parent = FindPath(parentPath);
        if (parent == null) { Debug.LogError("[Build] parent missing: " + parentPath); return null; }

        Transform existing = parent.transform.Find(name);
        if (existing != null) return existing;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    static GameObject Group(Transform parent, string name)
    {
        if (parent == null) return null;
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    /// <summary>
    /// Find-or-create a primitive under parent. Reusing by name is what makes the whole
    /// builder safe to run twice. CreatePrimitive's collider is always removed: decor must
    /// never block a CharacterController, or the AI would walk a different path.
    /// </summary>
    static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 localPos,
                           Vector3 localScale, string materialName)
    {
        return Prim(parent, type, name, localPos, localScale, materialName, Vector3.zero);
    }

    static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 localPos,
                           Vector3 localScale, string materialName, Vector3 euler)
    {
        if (parent == null) return null;

        Transform found = parent.Find(name);
        GameObject go;
        if (found != null)
        {
            go = found.gameObject;
            updated++;
        }
        else
        {
            go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            made++;
        }

        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);

        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = localScale;

        Renderer r = go.GetComponent<Renderer>();
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/" + materialName + ".mat");
        if (r != null && m != null) r.sharedMaterial = m;

        // Static batching. Deliberately NOT NavigationStatic: the project has no NavMesh
        // and must not gain one.
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

        return go;
    }

    static void Lit(string name, Color c)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/" + name + ".mat");
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) { Debug.LogError("[Build] URP/Lit shader not found."); return; }
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, MAT + "/" + name + ".mat");
        }

        m.SetColor("_BaseColor", c);
        m.SetFloat("_Metallic", 0f);
        m.SetFloat("_Smoothness", 0.05f);
        m.SetFloat("_SpecularHighlights", 0f);
        m.SetFloat("_EnvironmentReflections", 0f);
        m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        DisableMotionVectors(m);
        EditorUtility.SetDirty(m);
    }

    static void Retint(string name, Color c)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/" + name + ".mat");
        if (m == null) { Debug.LogWarning("[Build] material missing: " + name); return; }
        m.SetColor("_BaseColor", c);
        EditorUtility.SetDirty(m);
    }

    static void Blob(string name, Color c)
    {
        string path = MAT + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) { Debug.LogError("[Build] URP/Unlit shader not found."); return; }
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }

        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1f);   // transparent
        m.SetFloat("_Blend", 0f);     // alpha blend
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);    // one flat quad per character, no depth write
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
    }

    static void DisableMotionVectors(Material m)
    {
        SerializedObject so = new SerializedObject(m);
        SerializedProperty arr = so.FindProperty("m_DisabledShaderPasses");
        if (arr == null || !arr.isArray) return;
        arr.arraySize = 1;
        arr.GetArrayElementAtIndex(0).stringValue = "MOTIONVECTORS";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject FindPath(string path)
    {
        string[] parts = path.Split('/');
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

        GameObject cur = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == parts[0]) { cur = roots[i]; break; }
        }
        if (cur == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            Transform next = null;
            Transform t = cur.transform;
            for (int j = 0; j < t.childCount; j++)
            {
                if (t.GetChild(j).name == parts[i]) { next = t.GetChild(j); break; }
            }
            if (next == null) return null;
            cur = next.gameObject;
        }
        return cur;
    }
}
