using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Week 16 instrumentation: measures the running game instead of guessing at it.
///
/// One CSV row per frame, written from the PLAYER (not the editor), so any number quoted in SA6 or
/// AS3 can be traced back to a file somebody else can open and re-parse. It only ever switches
/// itself on when asked: the editor menu (Tools &gt; Week16) sets <see cref="pendingRun"/>, and a
/// player build needs "-perf" on its command line. A normal launch is untouched.
///
/// Two things this has to survive, both found the hard way on the first smoke run:
///
/// 1. The frame-rate cap. The game asks for 30 fps in GameManager.Awake, and with a cap in place a
///    60 ms frame and a 20 ms frame both report 33.3 ms, which would hide exactly the improvement a
///    before/after comparison is meant to show. Captures therefore run uncapped unless
///    "-perf-capped" is passed, and the shipped cap is restored afterwards.
/// 2. The match ending, and the auto-pause on focus loss (Week 15's OnApplicationFocus). A 4v4 HARD
///    match can be won inside twenty seconds, and a game-over or paused screen is not a frame cost.
///    So a capture is built out of SEGMENTS: the scenario resumes a paused match, and when a match
///    ends the harness restarts it and carries on, every row stamped with its segment number so the
///    analyst can see exactly where a reload sits.
/// </summary>
[DisallowMultipleComponent]
public class PerfCapture : MonoBehaviour
{
    // ---------------------------------------------------------------- entry points

    /// <summary>Set by the editor menu before play mode starts. Domain reload is off, so it survives.</summary>
    public static bool pendingRun;
    public static string pendingLabel = "editor_run";
    public static float pendingDuration = 60f;

    const string PerfSwitch = "-perf";
    const long NanoPerMilli = 1000000L;

    static bool booted;
    static PerfCapture active;

    // ---------------------------------------------------------------- samples

    /// <summary>One frame. Kept in memory and written once at the end, so disk I/O never lands
    /// inside the measurement it is supposed to be taking.</summary>
    struct Sample
    {
        public int segment;
        public int frame;
        public float elapsed;
        public float interval;
        public float main;
        public float gpu;
        public long gc;
        public long drawCalls;
        public long batches;
        public long triangles;
        public long memory;
        public long textureMemory;
        public long meshMemory;
        public long gfxMemory;
        public long gcUsedMemory;
        public long totalReserved;
        public long textureCount;
    }

    readonly List<Sample> samples = new List<Sample>(32768);
    readonly FrameTiming[] timings = new FrameTiming[1];

    ProfilerRecorder recMain, recGc, recMemory;
    ProfilerRecorder recTriangles;

    // Memory composition, so a 241 MB footprint can be attributed instead of guessed at. The editor's
    // own counters cannot do this job: the editor reported 1,355 textures / 222 MB of texture memory
    // while the project itself ships 9 textures worth roughly 15 MB. Almost all of that figure is
    // editor-only and package-only texture data, so only the player can say where the budget goes.
    ProfilerRecorder recTexMemory, recMeshMemory, recGfxMemory, recGcUsed, recReserved, recTexCount;

    // Unity 6 does not publish a single "Draw Calls Count" under Render - the counter is split by the
    // path that submitted it (SRP batcher, standard, instanced), and the only marker literally named
    // "Draw Calls Count" belongs to UI Toolkit. Asking for the old name returns an invalid recorder,
    // which is why every earlier capture wrote -1 into draw_calls and batches. The names below were
    // read out of the editor with Tools > Week16 > Dump Render Markers.
    ProfilerRecorder recDrawSrp, recDrawStandard, recDrawInstanced;
    ProfilerRecorder recBatchesDynamic, recBatchesStatic, recBatchesInstanced;
    ProfilerRecorder recSrpInstances;

    PerfScenario scenario;
    GameManager game;
    MatchManager match;

    bool capturing;
    bool prepared;
    bool quitWhenDone;
    bool keepCap;
    bool capLifted;
    string label = "run";
    string outputDirectory = string.Empty;
    string description = "no scenario";
    float duration = 60f;
    float capturedSeconds;
    float segmentStartedAt;
    float lastRealtime;
    int segment;
    int segmentFirstSample;
    bool pinnedOnce;
    int frameIndex;
    int shippedFrameRate;

    /// <summary>What was applied to the player before the first row was taken, and one line per
    /// segment recording the condition that segment really ran under. A capture row is evidence only
    /// if its own header is true for the whole file, so the header has to be able to show that and not
    /// merely assert it once at the end.</summary>
    string qualityProfile = "never applied";
    readonly StringBuilder segmentConditions = new StringBuilder();

    // ---------------------------------------------------------------- boot

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (booted || active != null) return;

        bool fromTools = pendingRun;
        bool fromCommandLine = HasArg(PerfSwitch);
        if (!fromTools && !fromCommandLine) return;

        booted = true;
        pendingRun = false;

        GameObject host = new GameObject("PerfCapture");
        DontDestroyOnLoad(host);

        PerfCapture capture = host.AddComponent<PerfCapture>();
        PerfScenario driver = host.AddComponent<PerfScenario>();
        capture.scenario = driver;
        active = capture;

        capture.label = fromTools ? pendingLabel : ArgText("-perf-label", "player_run");
        capture.duration = fromTools ? pendingDuration : ArgNumber("-perf-duration", 60f);
        capture.outputDirectory = ArgText("-perf-out", string.Empty);
        capture.quitWhenDone = !fromTools && !HasArg("-perf-hold");
        capture.keepCap = HasArg("-perf-capped");
        capture.shippedFrameRate = Application.targetFrameRate;

        Debug.Log("[Perf] capture armed: label '" + capture.label + "', " +
                  capture.duration.ToString("F0", CultureInfo.InvariantCulture) + " s of live match, " +
                  (capture.keepCap ? "keeping the " + capture.shippedFrameRate + " fps cap" : "uncapped") +
                  (fromTools ? ", requested by the editor menu." : ", requested by the command line."));
    }

    void Start()
    {
        StartCoroutine(RunRoutine());
    }

    // ---------------------------------------------------------------- the run

    IEnumerator RunRoutine()
    {
        // Let the scene finish its own Awake/Start so the objects we look for exist.
        yield return null;

        if (scenario == null)
        {
            Debug.LogError("[Perf] no PerfScenario component - nothing to drive, capture aborted.");
            yield break;
        }

        // The roster is built at scene load from MatchSettings, so the tested configuration is only
        // guaranteed by setting it and reloading once.
        scenario.ApplySettings();
        Debug.Log("[Perf] scenario settings: " + scenario.SettingsSummary());

        // A Windows player is built with only ONE quality level (see ApplyMobileProfile), so the
        // settings the mobile build ships with are applied by value instead of by level name.
        ApplyMobileProfile();

        float deadline = Time.realtimeSinceStartup + duration * 4f + 120f;
        capturedSeconds = 0f;
        while (capturedSeconds < duration && Time.realtimeSinceStartup < deadline)
        {
            yield return PrepareMatch();

            if (!prepared)
            {
                Debug.LogError("[Perf] the match never reached Playing with a spawned player after the " +
                               "reload. Capture aborted rather than measuring a frozen title screen.");
                yield break;
            }

            ApplyMobileProfile();

            if (!keepCap)
            {
                // The reloaded scene's Awake re-applies the game's own 30 fps cap on EVERY reload, so
                // the cap has to be lifted again after every segment, not once per run. Missing this
                // left the first baseline with segment 1 uncapped and every later segment pinned at
                // exactly 33.3 ms - two different conditions inside one file.
                if (!capLifted) shippedFrameRate = Application.targetFrameRate;
                Application.targetFrameRate = -1;
                QualitySettings.vSyncCount = 0;
                capLifted = true;
            }

            segment++;
            RecordSegmentCondition();
            scenario.Begin(match, game);
            description = scenario.Describe();

            BeginCapture();
            segmentStartedAt = Time.realtimeSinceStartup;

            float remaining = duration - capturedSeconds;
            while (capturing)
            {
                if (Time.realtimeSinceStartup - segmentStartedAt >= remaining) break;
                if (scenario.MatchOver) break;
                if (Time.realtimeSinceStartup > deadline) break;
                yield return null;
            }

            EndCapture();
            scenario.End();

            float segmentSeconds = Time.realtimeSinceStartup - segmentStartedAt;
            capturedSeconds += segmentSeconds;

            if (capturedSeconds < duration)
            {
                Debug.Log("[Perf] match segment " + segment + " ended after " +
                          segmentSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s (" +
                          scenario.EndReason + "); restarting the match for the next segment. " +
                          capturedSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s of " +
                          duration.ToString("F0", CultureInfo.InvariantCulture) + " s captured so far.");
            }
        }

        if (!keepCap && capLifted)
        {
            Application.targetFrameRate = shippedFrameRate;
            Debug.Log("[Perf] frame cap restored to " + shippedFrameRate + " fps.");
        }

        string path = WriteCsv();
        LogSummary(path);

        if (quitWhenDone)
        {
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }
    }

    /// <summary>Puts the player into the settings the MOBILE quality level defines.
    ///
    /// It cannot select that level: ProjectSettings/QualitySettings.asset excludes Standalone from it,
    /// so a Windows player is built with exactly one level and that level is called "PC" (verified in a
    /// player log: "requested 'Mobile' -> index 0 of [PC]"). The old code called SetQualityLevel(0),
    /// which was a no-op, and an earlier capture therefore reported "PC" in its header while believing
    /// it had pinned "Mobile". Worse, the surviving level points at PC_RPAsset - render scale 1.0 with
    /// 50 m main-light shadows - not the Mobile_RPAsset (0.8, no shadows) the Android build renders
    /// with, which is a heavier path and would have flattered every optimisation measured against it.
    ///
    /// The scalars below are copied from that level; the render pipeline itself is swapped at build
    /// time (Tools &gt; Week16 &gt; Perf Build, in Assets/Editor/PerfBuildPipeline.cs) because a runtime
    /// cannot load an asset that was stripped out of the build. Values are re-applied per segment
    /// because every reload re-applies the project's per-platform defaults.</summary>
    void ApplyMobileProfile()
    {
        // Values copied from the Mobile entry of ProjectSettings/QualitySettings.asset.
        QualitySettings.pixelLightCount = 1;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowDistance = 0f;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.antiAliasing = 0;
        QualitySettings.softParticles = false;
        QualitySettings.softVegetation = true;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.billboardsFaceCameraPosition = true;
        QualitySettings.vSyncCount = 0;
        QualitySettings.lodBias = 1f;
        QualitySettings.meshLodThreshold = 1f;
        QualitySettings.maximumLODLevel = 0;
        QualitySettings.enableLODCrossFade = true;
        QualitySettings.streamingMipmapsActive = false;

        qualityProfile = "mobile scalars applied (aniso " + QualitySettings.anisotropicFiltering +
                         ", lodBias " + QualitySettings.lodBias.ToString("F2", CultureInfo.InvariantCulture) +
                         ", skinWeights " + QualitySettings.skinWeights +
                         ", AA " + QualitySettings.antiAliasing +
                         ", shadows " + QualitySettings.shadows +
                         ") on level " + QualitySettings.GetQualityLevel() + " '" +
                         QualitySettings.names[QualitySettings.GetQualityLevel()] + "' of [" +
                         string.Join(", ", QualitySettings.names) + "]";

        Debug.Log("[Perf] quality: " + qualityProfile + "; render path " + DescribeRenderPath() +
                  (pinnedOnce ? " (re-applied after a reload)." : "."));
        pinnedOnce = true;
    }

    /// <summary>Names the render pipeline actually in force and its two costliest knobs. URP is read by
    /// reflection because this assembly does not reference it, and a diagnostics file is not a good
    /// enough reason to widen the runtime assembly's dependencies.</summary>
    static string DescribeRenderPath()
    {
        RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
        if (asset == null) return "built-in pipeline (no SRP asset)";

        string text = asset.name;
        try
        {
            Type type = asset.GetType();

            PropertyInfo scale = type.GetProperty("renderScale");
            if (scale != null && scale.PropertyType == typeof(float))
                text += ", render scale " + ((float)scale.GetValue(asset)).ToString("F2", CultureInfo.InvariantCulture);

            PropertyInfo shadows = type.GetProperty("supportsMainLightShadows");
            if (shadows != null && shadows.PropertyType == typeof(bool))
                text += (bool)shadows.GetValue(asset) ? ", main-light shadows ON" : ", main-light shadows off";
        }
        catch (Exception e)
        {
            text += " (details unavailable: " + e.GetType().Name + ")";
        }
        return text;
    }

    /// <summary>Stamps the condition THIS segment actually ran under. The reload re-applies the
    /// project's per-platform defaults, so a value that held in segment 1 proves nothing about
    /// segment 5 - it gets written down every time.</summary>
    void RecordSegmentCondition()
    {
        int level = QualitySettings.GetQualityLevel();
        segmentConditions.Append("# segment ").Append(segment).Append(" ran: quality ")
            .Append(level).Append(" ('").Append(QualitySettings.names[level]).Append("'), aniso ")
            .Append(QualitySettings.anisotropicFiltering).Append(", lodBias ")
            .Append(QualitySettings.lodBias.ToString("F2", CultureInfo.InvariantCulture))
            .Append(", frame cap ")
            .Append(Application.targetFrameRate < 0 ? "uncapped (-1)" : Application.targetFrameRate + " fps")
            .Append('\n');
    }

    /// <summary>Reloads into a live match and waits for it to actually be live with a spawned player.</summary>
    IEnumerator PrepareMatch()
    {
        prepared = false;

        game = FindAnyObjectByType<GameManager>();
        if (game == null)
        {
            Debug.LogError("[Perf] no GameManager in the scene - nothing to capture.");
            yield break;
        }

        game.StartMatchAndReload();
        yield return null;

        float guard = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < guard)
        {
            game = FindAnyObjectByType<GameManager>();
            match = FindAnyObjectByType<MatchManager>();

            if (game != null && match != null && match.matchActive && match.player != null &&
                game.State == GameManager.GameState.Playing)
            {
                prepared = true;
                break;
            }

            yield return null;
        }

        if (!prepared)
        {
            Debug.LogError("[Perf] after the reload the game is in state " +
                           (game != null ? game.State.ToString() : "no GameManager") + " with match " +
                           (match != null ? (match.matchActive ? "active" : "inactive") : "missing") + ".");
        }
    }

    void BeginCapture()
    {
        // A new segment means fresh recorders: one that was disposed at the end of the previous
        // segment cannot be reused, and stale buffered values would land in the first row.
        DisposeRecorders();
        recMain = StartRecorder(ProfilerCategory.Internal, "PlayerLoop");
        recGc = StartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
        recTriangles = StartRecorder(ProfilerCategory.Render, "Triangles Count");
        recMemory = StartRecorder(ProfilerCategory.Memory, "Total Used Memory");
        recTexMemory = StartRecorder(ProfilerCategory.Memory, "Texture Memory");
        recMeshMemory = StartRecorder(ProfilerCategory.Memory, "Mesh Memory");
        recGfxMemory = StartRecorder(ProfilerCategory.Memory, "Gfx Used Memory");
        recGcUsed = StartRecorder(ProfilerCategory.Memory, "GC Used Memory");
        recReserved = StartRecorder(ProfilerCategory.Memory, "Total Reserved Memory");
        recTexCount = StartRecorder(ProfilerCategory.Memory, "Texture Count");

        recDrawSrp = StartRecorder(ProfilerCategory.Render, "SRP Batcher Draw Calls Count");
        recDrawStandard = StartRecorder(ProfilerCategory.Render, "Standard Draw Calls Count");
        recDrawInstanced = StartRecorder(ProfilerCategory.Render, "Standard Instanced Draw Calls Count");
        recSrpInstances = StartRecorder(ProfilerCategory.Render, "SRP Batcher Instances Count");
        recBatchesDynamic = StartRecorder(ProfilerCategory.Render, "Dynamic Batches Count");
        recBatchesStatic = StartRecorder(ProfilerCategory.Render, "Static Batches Count");
        recBatchesInstanced = StartRecorder(ProfilerCategory.Render, "Instanced Batches Count");

        // Render counters only exist while the profiler core is recording in a player build. Switching
        // it on for the capture (and off again afterwards) is what makes draw calls / batches /
        // triangles readable at all; without it those columns stay at -1.
        EnableProfilerRecording(true);

        lastRealtime = Time.realtimeSinceStartup;
        segmentFirstSample = samples.Count;
        capturing = true;

        Debug.Log("[Perf] segment " + segment + " capture started (frame " + frameIndex + "): " + description);
    }

    void EndCapture()
    {
        if (!capturing) return;
        capturing = false;
        EnableProfilerRecording(false);
        Debug.Log("[Perf] segment " + segment + " capture stopped after " +
                  (samples.Count - segmentFirstSample) + " frames (total " +
                  samples.Count.ToString(CultureInfo.InvariantCulture) + ").");
    }

    /// <summary>A marker that does not exist in this build must not abort the run: it is logged once
    /// and that column stays at -1, so the CSV always says which numbers are real.</summary>
    ProfilerRecorder StartRecorder(ProfilerCategory category, string statName)
    {
        try
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, statName, 1);
            if (!recorder.Valid)
            {
                Debug.LogWarning("[Perf] recorder '" + statName + "' is not available in this build; " +
                                 "its column will read -1.");
            }
            return recorder;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Perf] recorder '" + statName + "' could not be created (" + e.Message +
                             "); its column will read -1.");
            return default(ProfilerRecorder);
        }
    }

    void DisposeRecorders()
    {
        recMain.Dispose();
        recGc.Dispose();
        recTriangles.Dispose();
        recMemory.Dispose();
        recTexMemory.Dispose();
        recMeshMemory.Dispose();
        recGfxMemory.Dispose();
        recGcUsed.Dispose();
        recReserved.Dispose();
        recTexCount.Dispose();

        recDrawSrp.Dispose();
        recDrawStandard.Dispose();
        recDrawInstanced.Dispose();
        recSrpInstances.Dispose();
        recBatchesDynamic.Dispose();
        recBatchesStatic.Dispose();
        recBatchesInstanced.Dispose();
    }

    /// <summary>Draw calls are the sum over the paths that submitted them, and a path this build does
    /// not exercise simply contributes zero. Returns -1 only when every component is unavailable.</summary>
    static long SumDrawCalls(ProfilerRecorder srp, ProfilerRecorder standard, ProfilerRecorder instanced)
    {
        bool any = false;
        long total = 0;
        if (srp.Valid) { total += srp.LastValue; any = true; }
        if (standard.Valid) { total += standard.LastValue; any = true; }
        if (instanced.Valid) { total += instanced.LastValue; any = true; }
        return any ? total : -1L;
    }

    static long SumBatches(ProfilerRecorder dynamicBatches, ProfilerRecorder staticBatches, ProfilerRecorder instancedBatches)
    {
        bool any = false;
        long total = 0;
        if (dynamicBatches.Valid) { total += dynamicBatches.LastValue; any = true; }
        if (staticBatches.Valid) { total += staticBatches.LastValue; any = true; }
        if (instancedBatches.Valid) { total += instancedBatches.LastValue; any = true; }
        return any ? total : -1L;
    }

    /// <summary>The profiler core is switched on only while rows are being taken, so a normal launch
    /// carries none of its cost.</summary>
    void EnableProfilerRecording(bool on)
    {
        bool before = UnityEngine.Profiling.Profiler.enabled;
        UnityEngine.Profiling.Profiler.enabled = on;
        Debug.Log("[Perf] Profiler.enabled " + before + " -> " + on + " for the capture.");
    }

    // ---------------------------------------------------------------- per frame

    void LateUpdate()
    {
        if (!capturing) return;

        float now = Time.realtimeSinceStartup;
        float interval = (now - lastRealtime) * 1000f;
        lastRealtime = now;

        // The PlayerLoop marker is the engine's own main-thread cost and does not care what the
        // frame-rate cap is doing. If it is missing or implausible, the wall-clock interval stands in
        // for it and the row still carries a usable number.
        float main = recMain.Valid ? recMain.LastValue / (float)NanoPerMilli : -1f;
        if (main <= 0f || main > 1000f) main = interval;

        float gpu = -1f;
        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, timings) == 1)
        {
            // gpuFrameTime occasionally comes back uninitialised when no timing was captured for that
            // frame - one baseline run averaged 64,049,859 ms of "GPU" time because of it. A number
            // that large is an artefact, not a slow frame, so it is recorded as -1 (not measured).
            double frameGpu = timings[0].gpuFrameTime;
            if (frameGpu > 0d && frameGpu <= 1000d) gpu = (float)frameGpu;
        }

        Sample s = new Sample();
        s.segment = segment;
        s.frame = frameIndex++;
        s.elapsed = now - segmentStartedAt;
        s.interval = interval;
        s.main = main;
        s.gpu = gpu;
        s.gc = recGc.Valid ? recGc.LastValue : -1L;
        // A marker that reads zero for a whole frame is written as 0, not as -1: with the split
        // counters, zero legitimately means "this draw path was not used in this frame".
        s.drawCalls = SumDrawCalls(recDrawSrp, recDrawStandard, recDrawInstanced);
        s.batches = SumBatches(recBatchesDynamic, recBatchesStatic, recBatchesInstanced);
        s.triangles = recTriangles.Valid ? recTriangles.LastValue : -1L;
        s.memory = recMemory.Valid ? recMemory.LastValue : -1L;
        s.textureMemory = recTexMemory.Valid ? recTexMemory.LastValue : -1L;
        s.meshMemory = recMeshMemory.Valid ? recMeshMemory.LastValue : -1L;
        s.gfxMemory = recGfxMemory.Valid ? recGfxMemory.LastValue : -1L;
        s.gcUsedMemory = recGcUsed.Valid ? recGcUsed.LastValue : -1L;
        s.totalReserved = recReserved.Valid ? recReserved.LastValue : -1L;
        s.textureCount = recTexCount.Valid ? recTexCount.LastValue : -1L;

        samples.Add(s);
    }

    // ---------------------------------------------------------------- output

    string ResolveOutputDirectory()
    {
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            // A relative -perf-out is taken from the folder holding the executable, so a shell script
            // and the player never disagree about where the evidence went.
            return Path.IsPathRooted(outputDirectory)
                ? outputDirectory
                : Path.Combine(PlayerFolder(), outputDirectory);
        }

        if (Application.isEditor)
            return Path.Combine(ProjectRoot(), Path.Combine("Docs", Path.Combine("Zedrich Abrera",
                Path.Combine("Week_16", Path.Combine("Evidence", "raw_csv")))));

        return Path.Combine(PlayerFolder(), "PerfRuns");
    }

    static string PlayerFolder()
    {
        try
        {
            return Path.GetDirectoryName(Application.dataPath);
        }
        catch (Exception)
        {
            return Directory.GetCurrentDirectory();
        }
    }

    static string ProjectRoot()
    {
        // Application.dataPath is <project>/Assets in the editor.
        DirectoryInfo assets = new DirectoryInfo(Application.dataPath);
        return assets.Parent != null ? assets.Parent.FullName : Application.dataPath;
    }

    string WriteCsv()
    {
        string directory = ResolveOutputDirectory();
        Directory.CreateDirectory(directory);

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string path = Path.Combine(directory, "perf_" + Sanitise(label) + "_" + stamp + ".csv");

        StringBuilder text = new StringBuilder(512 * 1024);
        text.AppendLine("# Week 16 performance capture - Abrera, Zedrich Zaivan B. - AgawanBase v0.2");
        text.AppendLine("# label: " + label);
        text.AppendLine("# unity: " + Application.unityVersion);
        text.AppendLine("# platform: " + Application.platform);
        text.AppendLine("# development build: " + Debug.isDebugBuild);
        text.AppendLine("# device: " + SystemInfo.deviceModel + " | " + SystemInfo.processorType + " x" +
                        SystemInfo.processorCount + " | " + SystemInfo.graphicsDeviceName + " | " +
                        (SystemInfo.systemMemorySize / 1024) + " GB RAM");
        text.AppendLine("# screen: " + Screen.width + "x" + Screen.height);
        text.AppendLine("# quality level: " + QualitySettings.names[QualitySettings.GetQualityLevel()] +
                        " (index " + QualitySettings.GetQualityLevel() + "); " + qualityProfile);
        text.AppendLine("# render path: " + DescribeRenderPath());
        text.AppendLine("# frame cap during capture: " +
                        (keepCap ? shippedFrameRate + " fps (capped)" : "uncapped (-1)"));
        text.AppendLine("# draw_calls column: NOT RELIABLE (DEF-16-04). It sums three split render counters and " +
                        "over-reports by roughly 10x: the Frame Debugger enumerates 17 render events for a live " +
                        "match frame and the batches column averages 3.8, while draw_calls averages 132. Quote " +
                        "batches, never draw_calls.");
        text.AppendLine("# segments: " + segment + " (a segment is one live match; a segment boundary is a reload)");
        text.AppendLine("# scenario: " + description);
        text.Append(segmentConditions);
        text.AppendLine("segment,frame,elapsed_s,interval_ms,main_thread_ms,gpu_ms,gc_bytes,draw_calls,batches," +
                        "triangles,total_memory_mb,texture_mb,mesh_mb,gfx_mb,gc_heap_mb,reserved_mb,texture_count");

        CultureInfo inv = CultureInfo.InvariantCulture;
        for (int i = 0; i < samples.Count; i++)
        {
            Sample s = samples[i];
            text.Append(s.segment.ToString(inv)).Append(',');
            text.Append(s.frame.ToString(inv)).Append(',');
            text.Append(s.elapsed.ToString("F3", inv)).Append(',');
            text.Append(s.interval.ToString("F3", inv)).Append(',');
            text.Append(s.main.ToString("F3", inv)).Append(',');
            text.Append(s.gpu.ToString("F3", inv)).Append(',');
            text.Append(s.gc.ToString(inv)).Append(',');
            text.Append(s.drawCalls.ToString(inv)).Append(',');
            text.Append(s.batches.ToString(inv)).Append(',');
            text.Append(s.triangles.ToString(inv)).Append(',');
            text.Append(Megabytes(s.memory).ToString("F2", inv)).Append(',');
            text.Append(Megabytes(s.textureMemory).ToString("F2", inv)).Append(',');
            text.Append(Megabytes(s.meshMemory).ToString("F2", inv)).Append(',');
            text.Append(Megabytes(s.gfxMemory).ToString("F2", inv)).Append(',');
            text.Append(Megabytes(s.gcUsedMemory).ToString("F2", inv)).Append(',');
            text.Append(Megabytes(s.totalReserved).ToString("F2", inv)).Append(',');
            text.Append(s.textureCount.ToString(inv));
            text.Append('\n');
        }

        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        return path;
    }

    static float Megabytes(long bytes)
    {
        return bytes < 0 ? -1f : bytes / (1024f * 1024f);
    }

    static string Sanitise(string value)
    {
        if (string.IsNullOrEmpty(value)) return "run";

        StringBuilder clean = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            clean.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
        }
        return clean.ToString();
    }

    /// <summary>Mean, median, p95 and worst are all printed, because a mean alone hides the spikes a
    /// player actually feels and a worst case alone says nothing about the average frame.</summary>
    void LogSummary(string path)
    {
        if (samples.Count == 0)
        {
            Debug.LogError("[Perf] no frames were captured; the CSV is empty. Check the scenario log above.");
            return;
        }

        List<float> intervals = new List<float>(samples.Count);
        List<float> mains = new List<float>(samples.Count);
        double gcTotal = 0;
        long drawTotal = 0;
        int drawCounted = 0;
        double gpuTotal = 0;
        int gpuCounted = 0;

        for (int i = 0; i < samples.Count; i++)
        {
            Sample s = samples[i];
            intervals.Add(s.interval);
            mains.Add(s.main);
            if (s.gc >= 0) gcTotal += s.gc;
            if (s.drawCalls >= 0) { drawTotal += s.drawCalls; drawCounted++; }
            if (s.gpu > 0) { gpuTotal += s.gpu; gpuCounted++; }
        }

        intervals.Sort();
        mains.Sort();

        float avgInterval = Average(intervals);
        float p95Interval = Percentile(intervals, 0.95f);
        float worstInterval = intervals[intervals.Count - 1];
        float avgMain = Average(mains);
        float p95Main = Percentile(mains, 0.95f);
        float worstMain = mains[mains.Count - 1];

        Debug.Log("[Perf] ==== summary (" + label + ", " + samples.Count + " frames, " + capturedSeconds.ToString("F1",
                  CultureInfo.InvariantCulture) + " s of live match, " + segment + " segment(s)) ====");
        Debug.Log("[Perf] frame interval ms: avg " + avgInterval.ToString("F2", CultureInfo.InvariantCulture) +
                  ", p95 " + p95Interval.ToString("F2", CultureInfo.InvariantCulture) +
                  ", worst " + worstInterval.ToString("F2", CultureInfo.InvariantCulture) +
                  " -> avg " + (avgInterval > 0f ? (1000f / avgInterval).ToString("F0", CultureInfo.InvariantCulture) : "?") +
                  " fps");
        Debug.Log("[Perf] main thread ms:   avg " + avgMain.ToString("F2", CultureInfo.InvariantCulture) +
                  ", p95 " + p95Main.ToString("F2", CultureInfo.InvariantCulture) +
                  ", worst " + worstMain.ToString("F2", CultureInfo.InvariantCulture));
        if (gpuCounted > 0)
            Debug.Log("[Perf] gpu ms:            avg " + (gpuTotal / gpuCounted).ToString("F2", CultureInfo.InvariantCulture) +
                      " (" + gpuCounted + " frames reported)");
        else
            Debug.Log("[Perf] gpu ms:            not reported by this build.");
        Debug.Log("[Perf] avg GC per frame:  " + (samples[0].gc >= 0
                  ? (gcTotal / samples.Count).ToString("F0", CultureInfo.InvariantCulture) + " bytes"
                  : "recorder unavailable"));
        if (drawCounted > 0)
            Debug.Log("[Perf] avg draw calls:    " + (drawTotal / (double)drawCounted).ToString("F1", CultureInfo.InvariantCulture) +
                      " over " + drawCounted + " frames (last frame's split: SRP " + recDrawSrp.LastValue +
                      " + standard " + recDrawStandard.LastValue + " + instanced " + recDrawInstanced.LastValue +
                      ", " + recSrpInstances.LastValue + " SRP-batched instances; batches " +
                      recBatchesDynamic.LastValue + " dynamic + " + recBatchesStatic.LastValue + " static + " +
                      recBatchesInstanced.LastValue + " instanced)");
        else
            Debug.Log("[Perf] avg draw calls:    not reported by this build.");
        double texSum = 0, meshSum = 0, gfxSum = 0, heapSum = 0, resSum = 0, totalSum = 0, texCountSum = 0;
        int memFrames = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            Sample m = samples[i];
            if (m.memory < 0) continue;
            memFrames++;
            totalSum += m.memory;
            if (m.textureMemory >= 0) texSum += m.textureMemory;
            if (m.meshMemory >= 0) meshSum += m.meshMemory;
            if (m.gfxMemory >= 0) gfxSum += m.gfxMemory;
            if (m.gcUsedMemory >= 0) heapSum += m.gcUsedMemory;
            if (m.totalReserved >= 0) resSum += m.totalReserved;
            if (m.textureCount >= 0) texCountSum += m.textureCount;
        }
        if (memFrames > 0)
        {
            double per = memFrames;
            Debug.Log("[Perf] memory avg:        total " + (totalSum / per / (1024d * 1024d)).ToString("F1", CultureInfo.InvariantCulture) +
                      " MB = texture " + (texSum / per / (1024d * 1024d)).ToString("F1", CultureInfo.InvariantCulture) +
                      " + mesh " + (meshSum / per / (1024d * 1024d)).ToString("F2", CultureInfo.InvariantCulture) +
                      " + gfx " + (gfxSum / per / (1024d * 1024d)).ToString("F1", CultureInfo.InvariantCulture) +
                      " + managed " + (heapSum / per / (1024d * 1024d)).ToString("F1", CultureInfo.InvariantCulture) +
                      "; reserved " + (resSum / per / (1024d * 1024d)).ToString("F1", CultureInfo.InvariantCulture) +
                      " MB over " + (texCountSum / per).ToString("F0", CultureInfo.InvariantCulture) + " textures");
        }
        Debug.Log("[Perf] csv: " + path);
    }

    static float Average(List<float> values)
    {
        if (values.Count == 0) return 0f;
        double total = 0;
        for (int i = 0; i < values.Count; i++) total += values[i];
        return (float)(total / values.Count);
    }

    static float Percentile(List<float> sorted, float fraction)
    {
        if (sorted.Count == 0) return 0f;
        int index = Mathf.Clamp(Mathf.RoundToInt(fraction * (sorted.Count - 1)), 0, sorted.Count - 1);
        return sorted[index];
    }

    void OnDestroy()
    {
        if (active == this) active = null;

        // A recorder left running keeps its own buffers alive across play sessions.
        DisposeRecorders();

        if (!keepCap && capLifted) Application.targetFrameRate = shippedFrameRate;
    }

    // ---------------------------------------------------------------- command line

    static bool HasArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static string ArgText(string name, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        return fallback;
    }

    static float ArgNumber(string name, float fallback)
    {
        string raw = ArgText(name, string.Empty);
        float parsed;
        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && parsed > 1f)
            return parsed;
        return fallback;
    }
}
