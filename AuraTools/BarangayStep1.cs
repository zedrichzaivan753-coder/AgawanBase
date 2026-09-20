using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot EDITOR build step 1 of the barangay visual upgrade.
/// Palette + camera tilt + court foundation + chalk markings + the two flags.
/// Scene and material assets only. No gameplay script, collider or logic is touched.
/// </summary>
public static class BarangayStep1
{
    const string MAT = "Assets/Materials";
    static int created;

    public static void Execute()
    {
        created = 0;

        // ============================================================ 1. PALETTE

        // Retint the three existing scene materials that are pure backdrop.
        Retint("Mat_Ground", new Color(0.42f, 0.46f, 0.30f));
        Retint("Mat_Wall",   new Color(0.62f, 0.58f, 0.52f));
        Retint("Mat_Zone",   new Color(0.86f, 0.84f, 0.79f));

        // Mat_Blue and Mat_Red are deliberately NOT touched: the plan guarantees that no
        // decor object ever uses them, so the two team colours stay unambiguous.
        Material mBlue = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/Mat_Blue.mat");
        Material mRed  = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/Mat_Red.mat");
        if (mBlue == null || mRed == null) Debug.LogWarning("[Step1] Mat_Blue / Mat_Red not found.");

        Material mAsphalt = Lit("Mat_Asphalt",    new Color(0.32f, 0.31f, 0.30f));
        Material mChalk   = Lit("Mat_Chalk",      new Color(0.94f, 0.92f, 0.86f));
        Material mHouse   = Lit("Mat_HouseWall",  new Color(0.88f, 0.78f, 0.62f));
        Material mTin     = Lit("Mat_TinRoof",    new Color(0.58f, 0.42f, 0.33f));
        Material mLeaf    = Lit("Mat_Foliage",    new Color(0.38f, 0.50f, 0.32f));
        Material mSkin    = Lit("Mat_Skin",       new Color(0.85f, 0.66f, 0.50f));
        Material mDark    = Lit("Mat_Dark",       new Color(0.20f, 0.19f, 0.20f));
        Material mAccent  = Lit("Mat_WarmAccent", new Color(0.86f, 0.58f, 0.24f));
        Blob("Mat_Blob", new Color(0f, 0f, 0f, 0.35f));

        // Keep these referenced so the compiler does not complain about unused locals.
        if (mHouse == null || mTin == null || mLeaf == null || mSkin == null ||
            mDark == null || mAccent == null) { /* created by the helpers above */ }

        AssetDatabase.SaveAssets();

        // ============================================================ 2. CAMERA

        GameObject rig = FindPath("Game/CameraRig");
        GameObject camGo = FindPath("Game/CameraRig/Main Camera");
        if (rig == null || camGo == null)
        {
            Debug.LogError("[Step1] camera rig not found.");
            return;
        }

        // Pitch the rig down 35 degrees from horizontal = a 55 degree camera tilt.
        rig.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);

        Camera cam = camGo.GetComponent<Camera>();
        if (cam != null)
        {
            // Orthographic: yaw stays 0 so the joystick still maps 1:1 onto world X/Z.
            cam.orthographic = true;
            cam.orthographicSize = 12.2f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.94f, 0.78f, 0.62f, 1f);  // warm afternoon sky
        }
        camGo.transform.localPosition = new Vector3(0f, 27.7f, 3.3f);

        // ============================================================ 3. LIGHTING

        GameObject lightGo = FindPath("Game/CameraRig/Directional Light");
        if (lightGo != null)
        {
            Light l = lightGo.GetComponent<Light>();
            if (l != null)
            {
                l.shadows = LightShadows.None;          // hard limit: no real-time shadows
                l.color = new Color(1.0f, 0.95f, 0.86f); // warm afternoon sun
                l.intensity = 1.15f;
            }
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.50f, 0.47f, 0.42f);
        RenderSettings.fog = false;

        // ============================================================ 4. FOUNDATION

        GameObject env = FindPath("Game/Environment");
        if (env == null)
        {
            Debug.LogError("[Step1] Game/Environment not found.");
            return;
        }

        Transform court = Group(env.transform, "Decor_Court");

        // Outside world floor - sits just below the arena Ground (top y=0) so it fills the
        // backdrop band and the sliver visible under the near wall without a visible seam.
        Prim(PrimitiveType.Cube, court, "Floor_Outside",
             new Vector3(0f, -0.04f, 6f), new Vector3(62f, 0.06f, 50f), "Mat_Ground");

        // The street/court slab. Tucks slightly under the four walls so there is no gap.
        Prim(PrimitiveType.Cube, court, "Court_Slab",
             new Vector3(0f, 0.015f, 0f), new Vector3(31f, 0.03f, 17.4f), "Mat_Asphalt");

        // ============================================================ 5. CHALK MARKINGS

        const float yLine = 0.04f;
        Vector3 thin = new Vector3(0.1f, 0.02f, 17.4f);   // runs along Z
        Vector3 wide = new Vector3(30f, 0.02f, 0.1f);     // runs along X

        Prim(PrimitiveType.Cube, court, "Line_Sideline_W", new Vector3(-15f, yLine, 0f), thin, "Mat_Chalk");
        Prim(PrimitiveType.Cube, court, "Line_Sideline_E", new Vector3( 15f, yLine, 0f), thin, "Mat_Chalk");
        Prim(PrimitiveType.Cube, court, "Line_Baseline_N", new Vector3(0f, yLine,  8.4f), wide, "Mat_Chalk");
        Prim(PrimitiveType.Cube, court, "Line_Baseline_S", new Vector3(0f, yLine, -8.4f), wide, "Mat_Chalk");
        Prim(PrimitiveType.Cube, court, "Line_Halfway",    new Vector3(0f, yLine,  0f),   thin, "Mat_Chalk");

        // Centre circle, 12 short dashes so it reads as a circle, not a hexagon.
        Dashes(court, "Line_CentreCircle", Vector3.zero, 3f, 12, 1.55f, 0.1f, yLine, "Mat_Chalk");

        // ============================================================ 6. BASE RINGS

        // Drawn at radius 4.35, i.e. just OUTSIDE the 4 m safe circle (and outside the 3x3
        // base plate, top y=0.06) so nothing intersects the plate.
        GameObject baseBlue = FindPath("Game/Environment/Base_Blue");
        GameObject baseRed  = FindPath("Game/Environment/Base_Red");

        if (baseBlue != null)
            Dashes(court, "Ring_Blue", Flat(baseBlue.transform.position), 4.35f, 16, 1.2f, 0.14f, yLine, "Mat_Blue");
        if (baseRed != null)
            Dashes(court, "Ring_Red", Flat(baseRed.transform.position), 4.35f, 16, 1.2f, 0.14f, yLine, "Mat_Red");

        // ============================================================ 7. PRISONS

        GameObject prisonRed  = FindPath("Game/Environment/PrisonForRed");
        GameObject prisonBlue = FindPath("Game/Environment/PrisonForBlue");

        if (prisonRed  != null) ChalkSquare(court, "ChalkPrison_Red",  Flat(prisonRed.transform.position),  mChalk, yLine);
        if (prisonBlue != null) ChalkSquare(court, "ChalkPrison_Blue", Flat(prisonBlue.transform.position), mChalk, yLine);

        // The eight prison posts were Mat_Red, which competed with a Red enemy at a glance.
        // Repaint them warm concrete so red now means "enemy and their ring" only.
        Material mWall = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/Mat_Wall.mat");
        string[] prisons = { "Game/Environment/PrisonForRed", "Game/Environment/PrisonForBlue" };
        foreach (string p in prisons)
        {
            GameObject prison = FindPath(p);
            if (prison == null) continue;
            for (int i = 1; i <= 4; i++)
            {
                GameObject post = FindPath(p + "/Post_" + i);
                if (post == null) continue;
                Renderer r = post.GetComponent<Renderer>();
                if (r != null && mWall != null) r.sharedMaterial = mWall;
            }
        }

        // ============================================================ 8. FLAGS

        // Only the CHILD meshes are resized. Flag.cs moves and rotates the ROOT, and it
        // records HomePosition / homeRotation at Awake, so the root is never touched here.
        BiggerFlag("Game/Environment/Flag_Red");
        BiggerFlag("Game/Environment/Flag_Blue");

        // ============================================================ done

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[Step1] done. " + created + " scene objects created. " +
                  "Camera rig pitch -35, camera local (0, 27.7, 3.3), ortho size 12.2.");
    }

    // ------------------------------------------------------------------ helpers

    static void BiggerFlag(string rootPath)
    {
        GameObject root = FindPath(rootPath);
        if (root == null) { Debug.LogWarning("[Step1] flag not found: " + rootPath); return; }

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

    static void ChalkSquare(Transform parent, string name, Vector3 centre, Material mat, float y)
    {
        const float half = 1.7f;
        const float len = 3.5f;
        Prim(PrimitiveType.Cube, parent, name + "_N", new Vector3(centre.x, y, centre.z + half), new Vector3(len, 0.02f, 0.12f), "Mat_Chalk");
        Prim(PrimitiveType.Cube, parent, name + "_S", new Vector3(centre.x, y, centre.z - half), new Vector3(len, 0.02f, 0.12f), "Mat_Chalk");
        Prim(PrimitiveType.Cube, parent, name + "_E", new Vector3(centre.x + half, y, centre.z), new Vector3(0.12f, 0.02f, len), "Mat_Chalk");
        Prim(PrimitiveType.Cube, parent, name + "_W", new Vector3(centre.x - half, y, centre.z), new Vector3(0.12f, 0.02f, len), "Mat_Chalk");
    }

    /// <summary>Places dashes around a circle, each one tangent to it.</summary>
    static void Dashes(Transform parent, string name, Vector3 centre, float radius, int count,
                       float dashLength, float width, float y, string materialName)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(centre.x + Mathf.Sin(rad) * radius, y, centre.z + Mathf.Cos(rad) * radius);
            Prim(PrimitiveType.Cube, parent, name + "_" + i, pos,
                 new Vector3(width, 0.02f, dashLength), materialName, new Vector3(0f, angle + 90f, 0f));
        }
    }

    static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    static GameObject Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go;
    }

    static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos,
                           Vector3 localScale, string materialName, Vector3? euler = null)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;

        // CreatePrimitive always adds a collider. Decor must never have one, or it would
        // block a CharacterController and change where the AI can walk.
        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);

        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
        go.transform.localScale = localScale;

        Renderer r = go.GetComponent<Renderer>();
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/" + materialName + ".mat");
        if (r != null && m != null) r.sharedMaterial = m;

        // Static batching. Deliberately NOT NavigationStatic - the project has no NavMesh
        // and must not gain one.
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

        created++;
        return go;
    }

    static Material Lit(string name, Color c)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/" + name + ".mat");
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) { Debug.LogError("[Step1] URP/Lit shader not found."); return null; }
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
        return m;
    }

    static Material Retint(string name, Color c)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/" + name + ".mat");
        if (m == null) { Debug.LogWarning("[Step1] material missing: " + name); return null; }
        m.SetColor("_BaseColor", c);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Blob(string name, Color c)
    {
        string path = MAT + "/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) { Debug.LogError("[Step1] URP/Unlit shader not found."); return; }
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }

        m.SetColor("_BaseColor", c);
        m.SetFloat("_Surface", 1f);       // transparent
        m.SetFloat("_Blend", 0f);         // alpha blend
        m.SetFloat("_AlphaClip", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);        // do not write depth: one flat quad per character
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
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
