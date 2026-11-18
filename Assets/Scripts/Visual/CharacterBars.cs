using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One character's overhead bars: GREEN on top = freshness (how long it has been out of its own
/// base) and YELLOW underneath = stamina. Both are read straight from that character's own
/// components, so the bar always shows the exact number the rules use.
///
/// Display only. It never changes freshness, stamina, the AI, a collider or a path.
///
/// Like CharacterIndicator it builds its own children in Awake from code-generated geometry, so it
/// needs no prefab wiring, can never gain a collider, and follows the same cost rules: no shadows,
/// no probes, shared materials only (this project has the SRP Batcher on, and a per-character
/// material would break it). IndicatorController owns the three shared materials and hands them
/// over through Configure().
/// </summary>
[DisallowMultipleComponent]
public class CharacterBars : MonoBehaviour
{
    [Header("Height above the character's feet, in metres")]
    [Tooltip("Above CharacterIndicator's icon (1.95) and chevron (2.05), so nothing overlaps.")]
    public float freshnessY = 2.40f;

    public float staminaY = 2.26f;

    [Header("Size, in metres")]
    public float barWidth = 0.90f;
    public float barHeight = 0.085f;

    [Header("State - read only")]
    [Tooltip("True once IndicatorController has handed over the shared materials. Until then the " +
             "bars stay invisible rather than rendering with a null material.")]
    public bool isConfigured;

    // Cached once: these are the two values the bars display.
    CharacterStatus status;
    SprintStamina stamina;
    bool warnedAboutMissingStamina;

    MeshRenderer freshBackRenderer, freshFillRenderer, stamBackRenderer, stamFillRenderer;
    Transform freshFillT, stamFillT;

    Material backMat, freshMat, stamMat;
    Quaternion billboard = Quaternion.identity;

    bool visible;

    // True while the character is standing under cover. Held as a field so the four renderers are
    // re-applied only on the frame the cover state flips, never every frame.
    bool shownHidden;

    // Last values pushed onto the transforms. Writing a transform every frame for a value that has
    // not moved is pure waste, so a fill is only touched when it actually changes.
    float freshShown = -1f;
    float stamShown = -1f;

    // ONE quad shared by every bar of every character in the match, built on first use.
    static Mesh quadMesh;

    static Mesh Quad
    {
        get
        {
            if (quadMesh != null) return quadMesh;

            quadMesh = new Mesh();
            quadMesh.name = "BarQuad";

            quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };

            quadMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };

            quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quadMesh.RecalculateBounds();
            quadMesh.UploadMeshData(true);
            return quadMesh;
        }
    }

    void Awake()
    {
        status = GetComponent<CharacterStatus>();
        stamina = GetComponent<SprintStamina>();

        Transform freshBackT = EnsureChild("BarFreshBack");
        freshFillT = EnsureChild("BarFreshFill");
        Transform stamBackT = EnsureChild("BarStamBack");
        stamFillT = EnsureChild("BarStamFill");

        freshBackRenderer = freshBackT.GetComponent<MeshRenderer>();
        freshFillRenderer = freshFillT.GetComponent<MeshRenderer>();
        stamBackRenderer = stamBackT.GetComponent<MeshRenderer>();
        stamFillRenderer = stamFillT.GetComponent<MeshRenderer>();

        Neutralise(freshBackRenderer);
        Neutralise(freshFillRenderer);
        Neutralise(stamBackRenderer);
        Neutralise(stamFillRenderer);

        // Backgrounds are always full width; the fills are scaled to the value every frame.
        freshBackT.localPosition = new Vector3(0f, freshnessY, 0f);
        freshBackT.localScale = new Vector3(barWidth, barHeight, 1f);

        stamBackT.localPosition = new Vector3(0f, staminaY, 0f);
        stamBackT.localScale = new Vector3(barWidth, barHeight, 1f);

        freshFillT.localPosition = new Vector3(0f, freshnessY, 0f);
        stamFillT.localPosition = new Vector3(0f, staminaY, 0f);

        // Invisible until IndicatorController says the match is live.
        visible = false;
        Apply();
    }

    /// <summary>
    /// Hands over the three shared materials and the constant billboard rotation. Called by
    /// IndicatorController, so the materials exist in exactly one place in the whole project.
    /// </summary>
    public void Configure(Material back, Material fresh, Material stam, Quaternion cameraRotation)
    {
        backMat = back;
        freshMat = fresh;
        stamMat = stam;

        billboard = cameraRotation;

        // The camera never rotates, so this is a constant, not per-frame work.
        if (freshFillT != null) freshFillT.localRotation = billboard;
        if (stamFillT != null) stamFillT.localRotation = billboard;

        for (int i = 0; i < 4; i++)
        {
            Transform bar = transform.Find(BarChildNames[i]);
            if (bar != null) bar.localRotation = billboard;
        }

        isConfigured = true;
        Apply();

        Debug.Log("[Bars] " + name + " overhead bars configured (" +
                  (status != null ? "freshness from CharacterStatus" : "NO CharacterStatus") + ", " +
                  (stamina != null ? "stamina from SprintStamina" : "NO SprintStamina") + ").");
    }

    static readonly string[] BarChildNames =
    {
        "BarFreshBack", "BarFreshFill", "BarStamBack", "BarStamFill"
    };

    /// <summary>Shows or hides both bars. Kept in step with the match state by IndicatorController.</summary>
    public void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;

        // The cover state can never be evaluated here - only in LateUpdate - but a bar turned back
        // on while the body is under cover must not flash for a frame.
        shownHidden = HiddenByCover;
        Apply();
    }

    /// <summary>Pushes visibility and the shared materials onto the four renderers.</summary>
    void Apply()
    {
        if (freshBackRenderer == null || freshFillRenderer == null ||
            stamBackRenderer == null || stamFillRenderer == null) return;

        // A character under cover loses its bars together with its body, so nothing floats over
        // the bushes. The ground ring and the chevron are a DIFFERENT component (CharacterIndicator)
        // and deliberately stay on, so a hidden body can still be located.
        bool show = visible && isConfigured && !HiddenByCover;

        freshBackRenderer.gameObject.SetActive(show);
        freshFillRenderer.gameObject.SetActive(show);
        stamBackRenderer.gameObject.SetActive(show);
        stamFillRenderer.gameObject.SetActive(show);

        if (!show) return;

        SetMaterial(freshBackRenderer, backMat);
        SetMaterial(stamBackRenderer, backMat);
        SetMaterial(freshFillRenderer, freshMat);
        SetMaterial(stamFillRenderer, stamMat);

        // Force the next LateUpdate to write both fills, so a bar can never appear stale.
        freshShown = -1f;
        stamShown = -1f;
    }

    void LateUpdate()
    {
        if (!visible || !isConfigured) return;

        // Cover can change on any frame. Re-applying only on the flip keeps the normal frame at
        // two float comparisons; IndicatorController's 0.15 s pass can never undo it because the
        // gate lives here, inside Apply().
        bool hidden = HiddenByCover;
        if (hidden != shownHidden)
        {
            shownHidden = hidden;
            Apply();
        }

        if (hidden) return;

        UpdateFill(freshFillT, ref freshShown, FreshnessValue);
        UpdateFill(stamFillT, ref stamShown, StaminaValue);
    }

    /// <summary>
    /// True while the game says this character is standing under cover. It reads the SAME flag
    /// MatchManager and EnemyAI already honour (CharacterStatus.isHidden), and stays false while a
    /// flag is carried or the character is imprisoned - so a body is only ever invisible when it
    /// really is untaggable.
    /// </summary>
    bool HiddenByCover
    {
        get { return status != null && status.isHidden && !status.isCaptured; }
    }

    /// <summary>Freshness 0..1: 1 = just reset at its base, 0 = very stale. Frozen while captured.</summary>
    float FreshnessValue
    {
        get { return status != null ? status.Freshness01 : 1f; }
    }

    /// <summary>Stamina 0..1 for this body. Shared script, so the AI drain by the same rules.</summary>
    float StaminaValue
    {
        get
        {
            if (stamina != null) return stamina.Stamina01;

            if (!warnedAboutMissingStamina)
            {
                warnedAboutMissingStamina = true;
                Debug.LogWarning("[Bars] " + name + " has no SprintStamina, so its yellow stamina " +
                                 "bar has nothing to read and stays full. Add SprintStamina to the " +
                                 "character prefab.");
            }

            return 1f;
        }
    }

    /// <summary>
    /// Grows/shrinks one fill from its LEFT edge: the quad is 1x1 and centred, so scaling x and
    /// sliding it left by half the missing width keeps the bar anchored where it started.
    /// </summary>
    void UpdateFill(Transform fill, ref float shown, float value01)
    {
        if (fill == null) return;

        float clamped = Mathf.Clamp01(value01);
        if (Mathf.Abs(clamped - shown) < 0.001f) return;

        shown = clamped;

        // A hair of width at zero keeps the bar from flickering out of existence.
        float width = Mathf.Max(0.001f, barWidth * clamped);

        fill.localScale = new Vector3(width, barHeight, 1f);
        fill.localPosition = new Vector3(-(barWidth - width) * 0.5f, fill.localPosition.y, 0f);
    }

    // ------------------------------------------------------------------ construction

    /// <summary>Creates one bar part. MeshFilter + MeshRenderer only - never a collider.</summary>
    Transform EnsureChild(string childName)
    {
        Transform existing = transform.Find(childName);
        if (existing != null) return existing;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = Quad;

        go.AddComponent<MeshRenderer>();
        return go.transform;
    }

    /// <summary>
    /// Strips a bar renderer of everything that could cost money on an Android build. Bars cast no
    /// shadows, receive none, and sample no probes.
    /// </summary>
    static void Neutralise(MeshRenderer r)
    {
        if (r == null) return;

        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        r.allowOcclusionWhenDynamic = false;
    }

    /// <summary>Assigns a SHARED material, and only when it actually changes.</summary>
    static void SetMaterial(MeshRenderer renderer, Material material)
    {
        if (renderer == null || material == null) return;
        if (renderer.sharedMaterial != material) renderer.sharedMaterial = material;
    }
}
