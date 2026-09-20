using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Small editor helpers used while building the barangay visual upgrade.</summary>
public static class BarangayUtils
{
    /// <summary>Puts the Scene View camera where the game camera sits, for readable captures.</summary>
    public static void SetSceneView()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) { Debug.LogWarning("[Utils] no active Scene View."); return; }

        sv.in2DMode = false;
        sv.orthographic = true;
        sv.LookAt(new Vector3(0f, 0f, 4.06f), Quaternion.Euler(55f, 0f, 0f), 30f);
        sv.size = 12.2f;
        sv.Repaint();
        Debug.Log("[Utils] Scene View moved to the game camera angle.");
    }

    /// <summary>Full bird's eye of the whole arena, for geometry checks.</summary>
    public static void TopDown()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        sv.in2DMode = false;
        sv.orthographic = true;
        sv.LookAt(new Vector3(0f, 0f, 4f), Quaternion.Euler(90f, 0f, 0f), 40f);
        sv.size = 30f;
        sv.Repaint();
        Debug.Log("[Utils] Scene View moved to top-down overview.");
    }

    /// <summary>
    /// Play mode only. Dismisses the title screen so the arena is visible in the Game view.
    /// Calls the existing public GameManager.StartGame - it changes no logic at all.
    /// </summary>
    public static void StartMatch()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null) { Debug.LogWarning("[Utils] no GameManager found (are we in play mode?)."); return; }
        gm.StartGame();
        Debug.Log("[Utils] match started, state = " + gm.State);
    }

    /// <summary>Play mode only. Temporarily zooms the game camera in for a close-up check.
    /// Reverts on Stop, so the saved camera settings are never changed.</summary>
    public static void Zoom(float size)
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[Utils] no Main Camera."); return; }
        cam.orthographicSize = size;
        Debug.Log("[Utils] camera zoomed to " + size.ToString("F2"));
    }

    /// <summary>
    /// Play mode only. Points the game camera at a ground spot and zooms in, for close-up
    /// checking. Reverts when play mode stops, so the saved camera is untouched.
    /// Moving an orthographic camera by (target - oldViewCentre) centres the target exactly,
    /// because the camera's yaw is 0 and it is not rolled.
    /// </summary>
    public static void CentreOn(float x, float z, float size)
    {
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[Utils] no Main Camera."); return; }
        cam.orthographicSize = size;
        cam.transform.position = new Vector3(x, 24.58f, -13.185f + (z - 4.06f));
        Debug.Log("[Utils] camera centred on (" + x.ToString("F1") + ", " + z.ToString("F1") + ") size " + size);
    }

    /// <summary>Play mode only. Makes the Blue player pick up the Red flag, to show the carry pose.</summary>
    public static void ForceCarry()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null || gm.match == null) { Debug.LogWarning("[Utils] no GameManager/MatchManager."); return; }

        Flag red = gm.redFlag;
        CharacterStatus player = gm.match.player;
        if (red == null || player == null) { Debug.LogWarning("[Utils] carry test: missing refs."); return; }

        bool ok = red.TryPickUp(player.transform, player.team);
        Debug.Log("[Utils] ForceCarry -> picked up=" + ok + " flagState=" + red.state +
                  " carrier=" + (red.carrier != null ? red.carrier.name : "none"));
    }

    /// <summary>Play mode only. Parks the Blue player mid-field and freezes both enemies first, so the
    /// flag-carry pose can be photographed without the match ending the instant he picks the flag up.</summary>
    public static void StageCarryPose()
    {
        // A paused Editor does not tick frames, so nothing would move or animate.
        EditorApplication.isPaused = false;

        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null || gm.match == null) { Debug.LogWarning("[Utils] carry pose: no GameManager/MatchManager."); return; }

        MatchManager m = gm.match;
        CharacterStatus player = m.player;
        if (player == null || m.redFlag == null) { Debug.LogWarning("[Utils] carry pose: missing refs."); return; }

        if (gm.State != GameManager.GameState.Playing) gm.StartGame();

        // Park him well outside both bases: standing in his own base holding the flag ends the match.
        CharacterMotor motor = player.GetComponent<CharacterMotor>();
        if (motor != null) motor.TeleportTo(new Vector3(-4f, 0f, -3f));

        // Freeze the AI so a tag cannot interrupt the shot.
        for (int i = 0; i < m.enemies.Length; i++)
        {
            if (m.enemies[i] == null) continue;
            EnemyAI ai = m.enemies[i].GetComponent<EnemyAI>();
            if (ai != null) ai.enabled = false;
        }

        bool ok = m.redFlag.TryPickUp(player.transform, player.team);
        Debug.Log("[Utils] StageCarryPose -> player at " + player.transform.position.ToString("F1") +
                  " pickup=" + ok + " carrier=" + (m.redFlag.carrier != null ? m.redFlag.carrier.name : "none"));
    }

    /// <summary>Play mode only. Captures Red enemy 1, to show the grey slumped prison pose.</summary>
    public static void ForceCapture()
    {
        MatchManager m = Object.FindAnyObjectByType<MatchManager>();
        if (m == null || m.enemies == null || m.enemies.Length == 0 || m.prisonForRed == null)
        {
            Debug.LogWarning("[Utils] capture test: missing refs.");
            return;
        }

        CharacterStatus e = m.enemies[0];
        if (e == null) { Debug.LogWarning("[Utils] capture test: enemy null."); return; }

        Vector3 spot = new Vector3(m.prisonForRed.position.x, 0f, m.prisonForRed.position.z);
        e.Capture(spot);
        Debug.Log("[Utils] ForceCapture -> " + e.name + " isCaptured=" + e.isCaptured +
                  " at " + e.transform.position.ToString("F1"));
    }

    /// <summary>Play mode only. Drops the Red flag where the player stands, to check the carry pose
    /// reverts to the normal one.</summary>
    public static void DropFlag()
    {
        MatchManager m = Object.FindAnyObjectByType<MatchManager>();
        if (m == null || m.redFlag == null || m.player == null) { Debug.LogWarning("[Utils] drop: missing refs."); return; }
        m.redFlag.Drop(m.player.transform.position);
        Debug.Log("[Utils] DropFlag -> state=" + m.redFlag.state + " carrier=" +
                  (m.redFlag.carrier != null ? m.redFlag.carrier.name : "none"));
    }

    /// <summary>Play mode only. Unity pauses play mode whenever scripts are recompiled, so this
    /// resumes it. Also moves the Blue player clear of any dropped flag, leaving him NOT carrying,
    /// which is what the carry pose has to revert from.</summary>
    public static void TestCarryRevert()
    {
        EditorApplication.isPaused = false;

        MatchManager m = Object.FindAnyObjectByType<MatchManager>();
        if (m == null || m.player == null) { Debug.LogWarning("[Utils] revert test: no MatchManager/player."); return; }

        // 6 m from where the flag was dropped, and well outside both bases, so nothing re-grabs it.
        CharacterMotor motor = m.player.GetComponent<CharacterMotor>();
        if (motor != null) motor.TeleportTo(new Vector3(-4f, 0f, 3f));

        Debug.Log("[Utils] TestCarryRevert -> player at " + m.player.transform.position.ToString("F1") +
                  " flagState=" + (m.redFlag != null ? m.redFlag.state.ToString() : "none") +
                  " carrier=" + (m.redFlag != null && m.redFlag.carrier != null ? m.redFlag.carrier.name : "none"));
    }

    static EditorApplication.CallbackFunction walkDriver;
    static int walkTicks;

    /// <summary>Play mode only. Drives the Blue player forward on the editor clock so the walk
    /// animation can be photographed. PlayerController is switched off first, because its own
    /// zero input would otherwise overwrite ours every frame. Call StopWalkTest() to finish.</summary>
    public static void WalkTest()
    {
        EditorApplication.isPaused = false;

        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null || gm.match == null || gm.match.player == null)
        {
            Debug.LogWarning("[Utils] walk test: missing GameManager/MatchManager/player.");
            return;
        }

        if (gm.State != GameManager.GameState.Playing) gm.StartGame();
        if (gm.playerController != null) gm.playerController.enabled = false;

        walkTicks = 0;
        if (walkDriver == null) walkDriver = DriveWalk;
        EditorApplication.update -= walkDriver;
        EditorApplication.update += walkDriver;
        Debug.Log("[Utils] WalkTest -> driving the player forward.");
    }

    /// <summary>Editor-clock tick that feeds forward input to the Blue player's motor.</summary>
    static void DriveWalk()
    {
        if (!EditorApplication.isPlaying) { StopWalkTest(); return; }

        MatchManager m = Object.FindAnyObjectByType<MatchManager>();
        CharacterMotor motor = (m != null && m.player != null) ? m.player.GetComponent<CharacterMotor>() : null;
        if (motor == null) { StopWalkTest(); return; }

        motor.Move(new Vector2(1f, 0f));   // straight "right" on the stick
        walkTicks++;
        if (walkTicks % 60 == 0) Debug.Log("[Utils] walk ticks=" + walkTicks);
    }

    /// <summary>Play mode only. Stops the walk test and hands control back to PlayerController.</summary>
    public static void StopWalkTest()
    {
        if (walkDriver != null) EditorApplication.update -= walkDriver;

        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null && gm.playerController != null) gm.playerController.enabled = true;

        Debug.Log("[Utils] WalkTest stopped after " + walkTicks + " ticks.");
    }

    /// <summary>Editor. Builds the Android APK the plan's step 7 asks for and logs its size.</summary>
    public static void BuildApk()
    {
        var scenes = new System.Collections.Generic.List<string>();
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            if (EditorBuildSettings.scenes[i].enabled) scenes.Add(EditorBuildSettings.scenes[i].path);

        if (scenes.Count == 0) { Debug.LogError("[Build] no enabled scenes in the build settings."); return; }
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogError("[Build] active target is " + EditorUserBuildSettings.activeBuildTarget + ", not Android.");
            return;
        }

        string dir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Builds");
        System.IO.Directory.CreateDirectory(dir);
        string apk = System.IO.Path.Combine(dir, "AgawanBase.apk");

        Debug.Log("[Build] building " + apk + " (id=" + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android) + ") ...");

        var opts = new UnityEditor.BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = apk,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(opts);
        UnityEditor.Build.Reporting.BuildSummary s = report.summary;

        Debug.Log("[Build] result=" + s.result +
                  " errors=" + s.totalErrors +
                  " sizeBytes=" + s.totalSize +
                  " sizeMB=" + (s.totalSize / 1048576.0).ToString("F2") +
                  " apk=" + apk);
    }

    /// <summary>Editor. Prints the Android build facts the plan's V5 asks for, without building.</summary>
    public static void ReportBuild()
    {
        Debug.Log("[Build] active target=" + EditorUserBuildSettings.activeBuildTarget +
                  " scriptingBackend=" + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) +
                  " architectures=" + PlayerSettings.Android.targetArchitectures +
                  " minSdk=" + PlayerSettings.Android.minSdkVersion +
                  " orientation=" + PlayerSettings.defaultInterfaceOrientation +
                  " bundleVersion=" + PlayerSettings.bundleVersion);

        QualitySettings.GetQualityLevel();  // ensure settings are loaded before reading
        Debug.Log("[Build] quality='" + QualitySettings.names[QualitySettings.GetQualityLevel()] +
                  "' shadowDistance=" + QualitySettings.shadowDistance +
                  "' shadows=" + QualitySettings.shadows +
                  " vSync=" + QualitySettings.vSyncCount +
                  " targetFps=" + Application.targetFrameRate);

        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
            scenes[i] = (EditorBuildSettings.scenes[i].enabled ? "ON  " : "off ") + EditorBuildSettings.scenes[i].path;
        Debug.Log("[Build] scenes: " + string.Join(", ", scenes));
    }

    /// <summary>Editor cleanup. Deletes the superseded first-pass builder, which BarangayBuild.cs
    /// replaced.</summary>
    public static void DeleteStaleBuilder()
    {
        const string path = "Assets/Editor/BarangayStep1.cs";
        bool ok = AssetDatabase.DeleteAsset(path);
        if (ok) AssetDatabase.Refresh();
        Debug.Log("[Cleanup] delete " + path + " -> " + ok);
    }

    /// <summary>Editor repair. Deletes the stray diff-marker lines that an earlier partial edit
    /// left inside BarangayBuild.cs, between the "Shed_E" house and the first coconut tree.</summary>
    public static void RepairBuildFile()
    {
        const string path = "Assets/Editor/BarangayBuild.cs";
        string[] lines = System.IO.File.ReadAllLines(path);

        int shed = -1, coconut = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (shed < 0 && lines[i].Contains("\"Shed_E\"")) shed = i;
            if (shed >= 0 && coconut < 0 && lines[i].Contains("\"Coconut_1\"")) coconut = i;
        }

        if (shed < 0 || coconut < 0 || coconut <= shed + 1)
        {
            Debug.LogWarning("[Repair] markers not found (shed=" + shed + " coconut=" + coconut + ").");
            return;
        }

        var kept = new System.Collections.Generic.List<string>();
        for (int i = 0; i <= shed; i++) kept.Add(lines[i]);
        kept.Add("        // Coconut trees, tall enough to break the skyline between the roofs.");
        for (int i = coconut; i < lines.Length; i++) kept.Add(lines[i]);

        System.IO.File.WriteAllLines(path, kept.ToArray());
        AssetDatabase.Refresh();
        Debug.Log("[Repair] deleted " + (coconut - shed - 1) + " stray diff lines from " + path + ".");
    }

    /// <summary>Play mode only. Dumps every Flag in the scene plus the frame clock, to see whether
    /// the carried flag is actually riding its carrier.</summary>
    public static void ReportFlag()
    {
        Debug.Log("[Utils] clock: paused=" + UnityEditor.EditorApplication.isPaused +
                  " playing=" + UnityEditor.EditorApplication.isPlaying +
                  " timeScale=" + Time.timeScale +
                  " frame=" + Time.frameCount +
                  " time=" + Time.time.ToString("F2"));

        Flag[] flags = Object.FindObjectsByType<Flag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("[Utils] flags in scene: " + flags.Length);
        for (int i = 0; i < flags.Length; i++)
        {
            Flag f = flags[i];
            Debug.Log("[Utils] flag[" + i + "] " + f.name +
                      " scene=" + f.gameObject.scene.name +
                      " enabled=" + f.enabled + " activeSelf=" + f.gameObject.activeSelf +
                      " ownerTeam=" + f.ownerTeam +
                      " state=" + f.state +
                      " carrier=" + (f.carrier != null ? f.carrier.name : "none") +
                      " pos=" + f.transform.position.ToString("F3") +
                      " home=" + f.HomePosition.ToString("F3") +
                      " carryHeight=" + f.carryHeight);
        }
    }

    /// <summary>Play mode only. Prints the live pose so the states can be checked as values, not pixels.</summary>
    public static void ReportPose()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        if (gm == null || gm.match == null) { Debug.LogWarning("[Utils] report: no GameManager/MatchManager."); return; }

        MatchManager m = gm.match;
        Debug.Log("[Utils] state=" + gm.State);

        if (m.redFlag != null)
        {
            Flag f = m.redFlag;
            Debug.Log("[Utils] redFlag state=" + f.state + " carrier=" +
                      (f.carrier != null ? f.carrier.name : "none") + " IsCarried=" + f.IsCarried +
                      " pos=" + f.transform.position.ToString("F2") +
                      " active=" + f.gameObject.activeInHierarchy);
        }

        CharacterStatus player = m.player;
        if (player != null)
        {
            Debug.Log("[Utils] player pos=" + player.transform.position.ToString("F2") +
                      " captured=" + player.isCaptured + " inHomeBase=" + player.IsInHomeBase);
            ReportRig(player.transform);
        }

        for (int i = 0; i < m.enemies.Length; i++)
        {
            CharacterStatus e = m.enemies[i];
            if (e == null) continue;
            Debug.Log("[Utils] enemy " + e.name + " pos=" + e.transform.position.ToString("F2") +
                      " captured=" + e.isCaptured);
            ReportRig(e.transform);
        }
    }

    /// <summary>Prints one character's rig pivots and jersey material.</summary>
    static void ReportRig(Transform root)
    {
        Transform visual = root.Find("Visual");
        if (visual == null) { Debug.LogWarning("[Utils] " + root.name + ": no 'Visual' child."); return; }

        Transform upper = visual.Find("UpperBody");
        string[] names = { "ArmPivot_R", "ArmPivot_L" };
        if (upper == null) Debug.LogWarning("[Utils] " + root.name + ": 'UpperBody' missing.");
        else
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform t = upper.Find(names[i]);
                if (t == null) { Debug.LogWarning("[Utils] " + root.name + ": '" + names[i] + "' missing."); continue; }
                Debug.Log("[Utils] " + root.name + "/UpperBody/" + names[i] +
                          " euler=" + t.localEulerAngles.ToString("F1"));
            }
            Debug.Log("[Utils] " + root.name + "/UpperBody euler=" + upper.localEulerAngles.ToString("F1") +
                      " localPos=" + upper.localPosition.ToString("F3"));
        }

        string[] legs = { "LegPivot_R", "LegPivot_L" };
        for (int i = 0; i < legs.Length; i++)
        {
            Transform t = visual.Find(legs[i]);
            if (t == null) { Debug.LogWarning("[Utils] " + root.name + ": '" + legs[i] + "' missing."); continue; }
            Debug.Log("[Utils] " + root.name + "/" + legs[i] +
                      " euler=" + t.localEulerAngles.ToString("F1"));
        }

        Transform torso = visual.Find("UpperBody/Torso");
        MeshRenderer mr = torso != null ? torso.GetComponent<MeshRenderer>() : null;
        Debug.Log("[Utils] " + root.name + "/Torso material=" +
                  (mr != null && mr.sharedMaterial != null ? mr.sharedMaterial.name : "none"));

        CharacterRigAnimator anim = root.GetComponent<CharacterRigAnimator>();
        Debug.Log("[Utils] " + root.name + "/Animator enabled=" + (anim != null ? anim.enabled.ToString() : "MISSING"));
    }

    public static void SaveScene()
    {
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[Utils] scene saved: " + SceneManager.GetActiveScene().path);
    }
}
