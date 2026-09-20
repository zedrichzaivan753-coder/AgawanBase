using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// What a character's marker is currently saying, relative to the human player.
/// Neutral is the "nothing to act on" state: an opponent who is captured, immune, out of
/// range, or so close to the player in fieldTime that the tag rule would call it a draw.
/// </summary>
public enum IndicatorState
{
    Neutral,
    CanTag,
    Danger,
    You,
    YouDim
}

/// <summary>
/// One character's world-space marker: a ground ring, an icon above the head, and (for the
/// character the player controls only) a bobbing chevron.
///
/// This script is display only. It never touches the rules, the AI, a collider or a path - the
/// state it shows is handed to it by IndicatorController, which is the only thing that asks
/// MatchManager who would win a touch.
///
/// It builds its own three children in Awake rather than carrying them in the prefab. That is
/// deliberate: the parts are pure code-generated geometry, so building them once per instance
/// keeps the prefabs free of runtime-only objects, guarantees no collider can ever be added to
/// a marker by accident, and means 1v1 through 4v4 all work with no per-size setup.
///
/// COST: three MeshRenderers, no colliders, no shadows, no light probes. The only work in the
/// per-frame path is pinning the marker's rotation and (for the player only) a sine bob.
/// </summary>
[DisallowMultipleComponent]
public class CharacterIndicator : MonoBehaviour
{
    [Header("Height of each part above the character's feet, in metres")]
    [Tooltip("Court_Slab's top face sits at y = 0.03, so 0.04 clears it without z-fighting.")]
    public float ringY = 0.04f;

    [Tooltip("Above a 1.55 m head, so the icon sits over the character and never over the body.")]
    public float iconY = 1.95f;

    public float arrowY = 2.05f;

    [Header("Bob - the only per-frame motion")]
    public float bobHeight = 0.07f;
    public float bobHz = 1.6f;

    [Header("Neutral")]
    [Tooltip("Hide the ring completely when the verdict is Neutral. Grey rings on every " +
             "opponent at 4v4 is clutter, and Neutral means there is nothing to act on.")]
    public bool hideWhenNeutral = true;

    [Header("State - read only")]
    public IndicatorState state = IndicatorState.Neutral;

    [Tooltip("True once the controller has handed over the shared materials. Until then the " +
             "marker stays invisible rather than rendering with a null material.")]
    public bool isConfigured;

    MeshRenderer ringRenderer;
    MeshRenderer iconRenderer;
    MeshRenderer arrowRenderer;

    MeshFilter ringFilter;
    MeshFilter iconFilter;
    MeshFilter arrowFilter;

    Transform iconT;
    Transform arrowT;

    Vector3 iconBase;
    Vector3 arrowBase;

    Material youMat, youDimMat, canTagMat, dangerMat, neutralMat;
    Quaternion billboard = Quaternion.identity;

    // A verdict must survive this long before it is shown, so a ring cannot strobe.
    IndicatorState pending = IndicatorState.Neutral;
    float pendingTimer;

    float bobPhase;
    bool visible;

    void Awake()
    {
        Transform ringT = EnsureChild("Ring");
        iconT = EnsureChild("Icon");
        arrowT = EnsureChild("Arrow");

        ringFilter = ringT.GetComponent<MeshFilter>();
        iconFilter = iconT.GetComponent<MeshFilter>();
        arrowFilter = arrowT.GetComponent<MeshFilter>();

        ringRenderer = ringT.GetComponent<MeshRenderer>();
        iconRenderer = iconT.GetComponent<MeshRenderer>();
        arrowRenderer = arrowT.GetComponent<MeshRenderer>();

        Neutralise(ringRenderer);
        Neutralise(iconRenderer);
        Neutralise(arrowRenderer);

        // The ring lies flat: the mesh is built in the XY plane, so 90 degrees about X lays it
        // on the ground. Its rotation is never touched again, which is what stops it spinning
        // when the character turns.
        ringT.localPosition = new Vector3(0f, ringY, 0f);
        ringT.localRotation = Quaternion.Euler(90f, 0f, 0f);

        iconBase = new Vector3(0f, iconY, 0f);
        arrowBase = new Vector3(0f, arrowY, 0f);
        iconT.localPosition = iconBase;
        arrowT.localPosition = arrowBase;

        // Invisible until the controller says otherwise. A marker that appears on the title
        // screen would be worse than one that appears a frame late.
        visible = false;
        Apply();
    }

    /// <summary>
    /// Hands over the five shared materials and the constant billboard rotation. Called by
    /// IndicatorController, so the materials exist in exactly one place in the whole project.
    /// </summary>
    public void Configure(Material you, Material youDim, Material canTag, Material danger,
                          Material neutral, Quaternion cameraRotation)
    {
        youMat = you;
        youDimMat = youDim;
        canTagMat = canTag;
        dangerMat = danger;
        neutralMat = neutral;

        billboard = cameraRotation;

        // The camera is static, so this is a constant, not per-frame work. The shader culls
        // nothing, so the exact facing does not matter - only that the quad is broadside on.
        if (iconT != null) iconT.localRotation = billboard;
        if (arrowT != null) arrowT.localRotation = billboard;

        isConfigured = true;
        Apply();
    }

    /// <summary>Shows or hides the whole marker. Used to keep the field clean outside a match.</summary>
    public void SetVisible(bool value)
    {
        if (visible == value) return;
        visible = value;
        Apply();
    }

    /// <summary>Applies a verdict immediately, skipping the hold.</summary>
    public void SetState(IndicatorState next)
    {
        pending = next;
        pendingTimer = 0f;

        if (next == state) return;

        state = next;
        Apply();
    }

    /// <summary>
    /// Offers a verdict. A change that does not survive `hold` seconds is discarded, which is
    /// the second line of defence against a flickering ring behind the fieldTime margin.
    /// </summary>
    public void RequestState(IndicatorState next, float dt, float hold)
    {
        if (next == state)
        {
            pending = next;
            pendingTimer = 0f;
            return;
        }

        // A brand new candidate restarts the clock rather than inheriting the old one.
        if (next != pending)
        {
            pending = next;
            pendingTimer = 0f;
            return;
        }

        pendingTimer += dt;
        if (pendingTimer >= hold) SetState(next);
    }

    /// <summary>Pushes the current state onto the three renderers. Only ever called on a change.</summary>
    void Apply()
    {
        if (ringRenderer == null || iconRenderer == null || arrowRenderer == null) return;

        if (!visible || !isConfigured)
        {
            ringRenderer.gameObject.SetActive(false);
            iconRenderer.gameObject.SetActive(false);
            arrowRenderer.gameObject.SetActive(false);
            return;
        }

        bool showRing = true;
        bool showIcon = false;
        bool showArrow = false;

        switch (state)
        {
            case IndicatorState.You:
                SetMesh(ringFilter, ringRenderer, IndicatorMeshes.YouRingMesh, youMat);
                SetMaterial(arrowRenderer, youMat);
                showArrow = true;
                break;

            case IndicatorState.YouDim:
                SetMesh(ringFilter, ringRenderer, IndicatorMeshes.YouRingMesh, youDimMat);
                SetMaterial(arrowRenderer, youDimMat);
                showArrow = true;
                break;

            case IndicatorState.CanTag:
                SetMesh(ringFilter, ringRenderer, IndicatorMeshes.TagRingMesh, canTagMat);
                SetMesh(iconFilter, iconRenderer, IndicatorMeshes.CheckMesh, canTagMat);
                showIcon = true;
                break;

            case IndicatorState.Danger:
                // Dashed ring AND a cross. Either cue alone is enough to read the state, which
                // is what makes this survive colour blindness.
                SetMesh(ringFilter, ringRenderer, IndicatorMeshes.TagRingDashedMesh, dangerMat);
                SetMesh(iconFilter, iconRenderer, IndicatorMeshes.CrossMesh, dangerMat);
                showIcon = true;
                break;

            default:
                showRing = !hideWhenNeutral;
                if (showRing) SetMesh(ringFilter, ringRenderer, IndicatorMeshes.TagRingMesh, neutralMat);
                break;
        }

        ringRenderer.gameObject.SetActive(showRing);
        iconRenderer.gameObject.SetActive(showIcon);
        arrowRenderer.gameObject.SetActive(showArrow);
    }

    void LateUpdate()
    {
        // Pin the marker axis-aligned in world space. CharacterMotor turns the character root,
        // and a ground ring that spun with the body would read as part of the character rather
        // than as a marker on the floor. Setting the WORLD rotation is what cancels the yaw.
        transform.rotation = Quaternion.identity;

        // Only the controlled character bobs. A captured player's marker is dimmed AND still,
        // so "inactive" is two cues rather than one.
        if (state != IndicatorState.You) return;

        bobPhase += Time.deltaTime * bobHz * Mathf.PI * 2f;
        if (bobPhase > Mathf.PI * 2f) bobPhase -= Mathf.PI * 2f;

        float offset = Mathf.Sin(bobPhase) * bobHeight;

        if (iconT != null) iconT.localPosition = iconBase + Vector3.up * offset;
        if (arrowT != null) arrowT.localPosition = arrowBase + Vector3.up * offset;
    }

    // ------------------------------------------------------------------ construction

    /// <summary>Creates one marker part. MeshFilter + MeshRenderer only - never a collider.</summary>
    Transform EnsureChild(string childName)
    {
        Transform existing = transform.Find(childName);
        if (existing != null) return existing;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        return go.transform;
    }

    /// <summary>
    /// Strips a marker renderer of everything that could cost money on a mobile build. The
    /// indicators cast no shadows, receive none, and sample no probes.
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

    static void SetMesh(MeshFilter filter, MeshRenderer renderer, Mesh mesh, Material material)
    {
        if (filter != null && filter.sharedMesh != mesh) filter.sharedMesh = mesh;
        SetMaterial(renderer, material);
    }

    /// <summary>
    /// Assigns a SHARED material, and only when it actually changes. Writing sharedMaterial every
    /// frame would be a needless state change, and a per-instance material would break the SRP
    /// Batcher - this project has m_UseSRPBatcher = 1, so that matters.
    /// </summary>
    static void SetMaterial(MeshRenderer renderer, Material material)
    {
        if (renderer == null || material == null) return;
        if (renderer.sharedMaterial != material) renderer.sharedMaterial = material;
    }
}
