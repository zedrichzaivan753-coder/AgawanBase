using UnityEditor;
using UnityEngine;

/// <summary>
/// Replaces the plain sphere body of the two character prefabs with a low-poly kid rig.
///
/// What it touches, and nothing else:
///   - deletes the "Body" CHILD (which only ever had Transform + MeshFilter + MeshRenderer)
///   - adds a "Visual" child holding the rig
///   - adds CharacterRigAnimator to the prefab ROOT and wires its fields
///
/// The CharacterController, CharacterMotor, CharacterStatus, PlayerController, EnemyAI and
/// SprintStamina components on the root are NOT modified in any way.
/// </summary>
public static class BarangayRig
{
    const string MAT  = "Assets/Materials";
    const string BLUE = "Assets/Prefabs/BluePlayer.prefab";
    const string RED  = "Assets/Prefabs/RedEnemy.prefab";

    public static void All()
    {
        Rig(BLUE, "Mat_Blue");
        Rig(RED,  "Mat_Red");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        VerifySceneInstances();
    }

    static void Rig(string prefabPath, string jerseyName)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) { Debug.LogError("[Rig] cannot load " + prefabPath); return; }

        // ---- 1. the old sphere/capsule body goes ----
        Transform old = root.transform.Find("Body");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // ---- 2. the new rig ----
        Transform visual = root.transform.Find("Visual");
        if (visual == null)
        {
            GameObject v = new GameObject("Visual");
            v.transform.SetParent(root.transform, false);
            visual = v.transform;
        }
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        // UpperBody carries the bob and the captured slump, so the head and arms follow the
        // torso instead of sliding off it.
        Transform upper = Group(visual, "UpperBody");

        Transform torso = P(upper, PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.84f, 0f),
                            new Vector3(0.62f, 0.40f, 0.50f), jerseyName);
        Transform hem = P(upper, PrimitiveType.Cube, "JerseyHem", new Vector3(0f, 0.56f, 0f),
                          new Vector3(0.64f, 0.10f, 0.52f), jerseyName);

        P(upper, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.38f, 0f),
          new Vector3(0.34f, 0.34f, 0.34f), "Mat_Skin");
        P(upper, PrimitiveType.Cube, "Hair", new Vector3(0f, 1.50f, 0f),
          new Vector3(0.30f, 0.10f, 0.32f), "Mat_Dark");
        P(upper, PrimitiveType.Cube, "Face", new Vector3(0f, 1.38f, 0.17f),
          new Vector3(0.16f, 0.06f, 0.03f), "Mat_Dark");

        // Limbs hang under empty pivots at the shoulder / hip, so the animator swings them
        // from the joint instead of spinning them around their own middle.
        Transform armL = Group(upper, "ArmPivot_L");
        armL.localPosition = new Vector3(-0.35f, 1.05f, 0f);
        P(armL, PrimitiveType.Cube, "Arm_L", new Vector3(0f, -0.20f, 0f), new Vector3(0.11f, 0.40f, 0.12f), "Mat_Skin");

        Transform armR = Group(upper, "ArmPivot_R");
        armR.localPosition = new Vector3(0.35f, 1.05f, 0f);
        P(armR, PrimitiveType.Cube, "Arm_R", new Vector3(0f, -0.20f, 0f), new Vector3(0.11f, 0.40f, 0.12f), "Mat_Skin");

        Transform legL = Group(visual, "LegPivot_L");
        legL.localPosition = new Vector3(-0.12f, 0.50f, 0f);
        P(legL, PrimitiveType.Cube, "Leg_L",  new Vector3(0f, -0.21f, 0f),   new Vector3(0.13f, 0.42f, 0.13f), "Mat_Skin");
        P(legL, PrimitiveType.Cube, "Shoe_L", new Vector3(0f, -0.46f, 0.02f), new Vector3(0.16f, 0.08f, 0.22f), "Mat_Dark");

        Transform legR = Group(visual, "LegPivot_R");
        legR.localPosition = new Vector3(0.12f, 0.50f, 0f);
        P(legR, PrimitiveType.Cube, "Leg_R",  new Vector3(0f, -0.21f, 0f),   new Vector3(0.13f, 0.42f, 0.13f), "Mat_Skin");
        P(legR, PrimitiveType.Cube, "Shoe_R", new Vector3(0f, -0.46f, 0.02f), new Vector3(0.16f, 0.08f, 0.22f), "Mat_Dark");

        // Blob shadow: one flat unlit quad, no depth write. Stands in for a real shadow on a
        // build that has shadows switched off.
        P(visual, PrimitiveType.Quad, "Blob", new Vector3(0f, 0.06f, 0f),
          new Vector3(1.0f, 1.0f, 1.0f), "Mat_Blob", new Vector3(90f, 0f, 0f));

        // ---- 3. the animator on the root ----
        CharacterRigAnimator anim = root.GetComponent<CharacterRigAnimator>();
        if (anim == null) anim = root.AddComponent<CharacterRigAnimator>();

        anim.visual  = visual;
        anim.torso   = upper;
        anim.legLeft = legL;
        anim.legRight = legR;
        anim.armLeft = armL;
        anim.armRight = armR;
        anim.jerseyA = torso != null ? torso.GetComponent<Renderer>() : null;
        anim.jerseyB = hem != null ? hem.GetComponent<Renderer>() : null;
        anim.teamJersey = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/" + jerseyName + ".mat");
        anim.capturedJersey = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/Mat_Wall.mat");
        anim.flag = null;   // found at runtime from the character's own team
        EditorUtility.SetDirty(anim);
        EditorUtility.SetDirty(root);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log("[Rig] rebuilt " + prefabPath + " with the low-poly kid rig.");
    }

    /// <summary>
    /// Prints the scene instances' component values. This is the check that editing the
    /// prefab did not quietly break the Inspector references the gameplay scripts depend on.
    /// </summary>
    public static void VerifySceneInstances()
    {
        CharacterStatus[] statuses = Object.FindObjectsByType<CharacterStatus>();
        Debug.Log("[Rig] scene characters found: " + statuses.Length);

        for (int i = 0; i < statuses.Length; i++)
        {
            CharacterStatus s = statuses[i];
            CharacterMotor m = s.GetComponent<CharacterMotor>();
            EnemyAI ai = s.GetComponent<EnemyAI>();
            PlayerController pc = s.GetComponent<PlayerController>();
            CharacterRigAnimator anim = s.GetComponent<CharacterRigAnimator>();
            CharacterController cc = s.GetComponent<CharacterController>();

            int renderers = s.GetComponentsInChildren<Renderer>(true).Length;
            int colliders = s.GetComponentsInChildren<Collider>(true).Length;

            Debug.Log(string.Format(
                "[Rig] {0}: team={1} homeBase={2} motor={3} cc(h={4:F1},r={5:F1}) anim={6} renderers={7} colliders={8}\n" +
                "        ai={9} aiFlag={10} playerCtl={11} joystick={12} sprintBtn={13}",
                s.name, s.team,
                s.homeBase != null ? s.homeBase.name : "NULL",
                m != null ? "ok" : "MISSING",
                cc != null ? cc.height : -1f, cc != null ? cc.radius : -1f,
                anim != null ? "wired" : "MISSING",
                renderers, colliders,
                ai != null ? ai.state.ToString() : "-",
                ai != null && ai.ourFlag != null ? ai.ourFlag.name : "NULL",
                pc != null ? "ok" : "-",
                pc != null && pc.joystick != null ? pc.joystick.name : "NULL",
                pc != null && pc.sprintButton != null ? pc.sprintButton.name : "NULL"));
        }

        Flag[] flags = Object.FindObjectsByType<Flag>();
        for (int i = 0; i < flags.Length; i++)
        {
            Debug.Log("[Rig] flag " + flags[i].name + " ownerTeam=" + flags[i].ownerTeam +
                      " state=" + flags[i].state + " home=" + flags[i].HomePosition.ToString("F2"));
        }
    }

    // ---------------------------------------------------------------- helpers

    static Transform Group(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    /// <summary>
    /// Find-or-create a rig part. NOTE: no static flags and no colliders - a character's
    /// parts must never be batched as static, and must never add collision.
    /// </summary>
    static Transform P(Transform parent, PrimitiveType type, string name, Vector3 pos,
                       Vector3 scale, string materialName, Vector3 euler)
    {
        Transform found = parent.Find(name);
        GameObject go = found != null ? found.gameObject : GameObject.CreatePrimitive(type);
        go.name = name;

        if (found == null) go.transform.SetParent(parent, false);

        Collider c = go.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);

        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        GameObjectUtility.SetStaticEditorFlags(go, 0);

        Renderer r = go.GetComponent<Renderer>();
        Material m = AssetDatabase.LoadAssetAtPath<Material>(MAT + "/" + materialName + ".mat");
        if (r != null && m != null) r.sharedMaterial = m;

        return go.transform;
    }

    static Transform P(Transform parent, PrimitiveType type, string name, Vector3 pos,
                       Vector3 scale, string materialName)
    {
        return P(parent, type, name, pos, scale, materialName, Vector3.zero);
    }
}
