using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Smooth, edge-clamped follow camera for the top-down arena.
///
/// WHY IT LIVES ON THE RIG AND NEVER ROTATES IT
/// -------------------------------------------
/// Game/CameraRig is pitched 55 degrees, and Game/CameraRig/Main Camera carries the remaining 90.
/// That pitch is what makes the on-screen joystick map 1:1 onto world X/Z (stick-up really is up
/// the pitch). It is also what IndicatorController samples ONCE to billboard every character's
/// icon. So this script only ever TRANSLATES the rig: no rotation is touched, and the joystick and
/// the icons keep working exactly as they did.
///
/// Camera localPosition is (0, 30.01, 0) - directly above the rig's origin along the rig's own up
/// axis - which makes the rig's world X/Z position BE the point on the ground at the centre of the
/// screen. That is why the clamp can be applied straight to the rig's position: no projection maths,
/// nothing to get wrong.
///
/// ZOOM IS EXPRESSED AS "HOW MUCH OF THE ARENA WIDTH IS VISIBLE"
/// ----------------------------------------------------------
/// The camera is ORTHOGRAPHIC, so field of view is ignored and the camera's distance from the
/// ground does not change the zoom at all - only what the near and far planes clip. The one knob
/// that frames the shot is the orthographic size, and it is DERIVED from the arena width, the
/// serialized visible fraction and the live screen aspect:
///
///     size = (arenaWidth * visibleWidthPercent) / (2 * aspect)
///
/// so 16:9 and 20:9 both show the SAME slice of the arena from side to side. A 20:9 phone simply
/// sees less depth, because all its extra pixels are height. No per-device code, no FOV guessing.
///
/// PERFORMANCE: about twenty float operations per frame, no allocation, no raycast, no search.
/// LateUpdate is used so the camera follows the position the player has already reached this frame.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Header("What to follow")]
    [Tooltip("The character the camera tracks. Left empty, the human player is found once at startup.")]
    public Transform target;

    [Tooltip("Snap straight onto the target on the first frame instead of sliding in from wherever " +
             "the rig was left in the scene file.")]
    public bool snapOnStart = true;

    [Header("Zoom - by how much of the arena WIDTH is visible")]
    [Range(0.25f, 1.2f)]
    [Tooltip("0.65 shows 65% of the arena's width. The field therefore looks about 1.5x bigger " +
             "than it did at the old fixed orthographic size of 14.")]
    public float visibleWidthPercent = 0.65f;

    [Header("Arena - the rectangle the view may never leave")]
    [Tooltip("Centre of the playing rectangle, on the ground plane (x, z). Court_Slab is 34.5 x 19 " +
             "centred on (0, -1).")]
    public Vector2 arenaCenter = new Vector2(0f, -1f);

    [Tooltip("Size of the playing rectangle (width, depth).")]
    public Vector2 arenaSize = new Vector2(34.5f, 19f);

    [Tooltip("How far past the arena edge the view is allowed to reach, in metres. 5 keeps the four " +
             "walls, the outside floor and the north backdrop on screen, so the background colour " +
             "can never show through at an edge.")]
    public float edgeMargin = 5f;

    [Header("Follow")]
    [Tooltip("The camera aims this far ahead of the target, in metres on the ground plane. " +
             "(0, 1.5) lifts the view 1.5 m 'up the pitch' so the objective ahead of you is " +
             "visible, which is about a third of the visible depth.")]
    public Vector2 followLookAhead = new Vector2(0f, 1.5f);

    [Tooltip("How quickly the camera catches the target. Higher is snappier. The smoothing is " +
             "exponential, so the camera can never overshoot and wobble.")]
    public float followSmoothing = 12f;

    [Header("Optional: pinch to zoom - OFF by default")]
    [Tooltip("Two-finger pinch changes the visible fraction between the two limits below. The same " +
             "clamp still applies, so zooming out can never reveal the void past the scenery.")]
    public bool pinchEnabled;

    [Range(0.25f, 1.2f)] public float minVisiblePercent = 0.45f;
    [Range(0.25f, 1.2f)] public float maxVisiblePercent = 0.9f;

    [Tooltip("How much a pinch must change the finger spacing (in screen pixels) before the zoom moves.")]
    public float pinchDeadZonePixels = 2f;

    [Header("Debug")]
    [Tooltip("Log the orthographic size whenever the aspect changes, and log the first time the view " +
             "reaches each of the four clamp edges. Never logs per frame.")]
    public bool logView = true;

    [Tooltip("Draw the arena, the clamp rectangle and the rectangle the camera can actually see.")]
    public bool drawClampGizmos = true;

    Camera cam;

    float lastAspect;
    bool sawFirstAspect;

    // One flag per clamp edge, so an edge is announced once per visit instead of every frame.
    bool atWest, atEast, atSouth, atNorth;

    float pinchDistance;

    /// <summary>Ground width the camera can see, in metres. Constant across phone aspect ratios.</summary>
    public float VisibleWidth
    {
        get { return arenaSize.x * visibleWidthPercent; }
    }

    /// <summary>
    /// Ground depth the camera can see, in metres. This DOES change with the aspect ratio: a taller
    /// screen sees less depth for the same width. Measured from the camera's own pitch rather than
    /// from a hard-coded angle, so it stays right if the rig is ever re-tilted.
    /// </summary>
    public float VisibleDepth
    {
        get
        {
            if (cam == null) return VisibleWidth * 0.6f;

            // For this pitched camera, |forward.y| is exactly sin(pitch), so the ground distance
            // covered by the screen's height is the screen's vertical world extent / sin(pitch).
            float sinPitch = Mathf.Abs(cam.transform.forward.y);
            if (sinPitch < 0.05f) sinPitch = 0.819f;      // the tuned 55 degree fallback

            return (cam.orthographicSize * 2f) / sinPitch;
        }
    }

    /// <summary>The orthographic size this aspect and zoom setting call for.</summary>
    public float WantedOrthographicSize
    {
        get { return VisibleWidth / (2f * Aspect); }
    }

    static float Aspect
    {
        get
        {
            // Game view can report a zero height for a frame while it is being resized.
            float a = (float)Screen.width / Mathf.Max(1, Screen.height);
            return a > 0.05f ? a : (16f / 9f);
        }
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInChildren<Camera>();

        if (cam == null)
        {
            Debug.LogError("FollowCamera: no Camera on this object or below it, so the view will " +
                           "never be framed. Put this component on Game/CameraRig.");
            return;
        }

        // The whole design assumes a flat, parallel projection. Make that explicit rather than
        // trusting whatever the scene file happens to say.
        cam.orthographic = true;
    }

    void Start()
    {
        if (cam == null) return;

        ResolveTarget();
        lastAspect = Aspect;
        sawFirstAspect = true;

        if (snapOnStart) Frame(1f);
    }

    void LateUpdate()
    {
        if (cam == null) return;

        ResolveTarget();

        float aspect = Aspect;
        cam.orthographicSize = WantedOrthographicSize;

        if (logView && sawFirstAspect && !Mathf.Approximately(aspect, lastAspect))
        {
            lastAspect = aspect;
            Debug.Log("[FollowCamera] aspect " + aspect.ToString("F3") +
                      " -> orthographic size " + cam.orthographicSize.ToString("F2") +
                      "  (visible " + VisibleWidth.ToString("F1") + " m wide x " +
                      VisibleDepth.ToString("F1") + " m deep)");
        }

        if (pinchEnabled) ApplyPinch();

        // Exponential smoothing: 1 - e^(-k dt). Framerate independent, never overshoots, and
        // turns into a snap when deltaTime is large (the first frame after a scene load).
        float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, followSmoothing) * Time.deltaTime);
        Frame(t);
    }

    /// <summary>
    /// Moves the rig so the view centre lands on <paramref name="t"/> of the way towards the
    /// target, then clamps it. t = 1 snaps; smaller values ease in.
    /// </summary>
    void Frame(float t)
    {
        Vector3 current = transform.position;

        Vector2 wanted = default(Vector2);

        if (target != null)
        {
            wanted.x = target.position.x + followLookAhead.x;
            wanted.y = target.position.z + followLookAhead.y;
        }
        else
        {
            // No target resolved yet: hold station rather than drifting to the origin.
            wanted.x = current.x;
            wanted.y = current.z;
        }

        float halfWidth = VisibleWidth * 0.5f;
        float halfDepth = VisibleDepth * 0.5f;

        float arenaMinX = arenaCenter.x - arenaSize.x * 0.5f - edgeMargin;
        float arenaMaxX = arenaCenter.x + arenaSize.x * 0.5f + edgeMargin;
        float arenaMinZ = arenaCenter.y - arenaSize.y * 0.5f - edgeMargin;
        float arenaMaxZ = arenaCenter.y + arenaSize.y * 0.5f + edgeMargin;

        // The centre may travel from one visible half-width inside one limit to one inside the
        // other. On an extreme aspect the two can cross: Mathf.Min/Max then pins the axis to the
        // middle of the arena instead of inverting the range, so the camera simply stops following
        // that axis rather than flying off into empty space.
        float minX = arenaMinX + halfWidth;
        float maxX = arenaMaxX - halfWidth;
        float minZ = arenaMinZ + halfDepth;
        float maxZ = arenaMaxZ - halfDepth;

        float x = Mathf.Clamp(wanted.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
        float z = Mathf.Clamp(wanted.y, Mathf.Min(minZ, maxZ), Mathf.Max(minZ, maxZ));

        Vector3 next = new Vector3(Mathf.Lerp(current.x, x, t), current.y, Mathf.Lerp(current.z, z, t));

        if (next == current) return;

        transform.position = next;
        ReportEdges(next.x, next.z, minX, maxX, minZ, maxZ);
    }

    /// <summary>
    /// Says so once, the first time the view is held against each edge. This is the diagnostic that
    /// proves the clamp is working without flooding the console at 30 frames a second.
    /// </summary>
    void ReportEdges(float x, float z, float minX, float maxX, float minZ, float maxZ)
    {
        if (!logView) return;

        CheckEdge(ref atWest, "WEST (left)", x <= minX + 0.01f);
        CheckEdge(ref atEast, "EAST (right)", x >= maxX - 0.01f);
        CheckEdge(ref atSouth, "SOUTH (bottom)", z <= minZ + 0.01f);
        CheckEdge(ref atNorth, "NORTH (top)", z >= maxZ - 0.01f);
    }

    void CheckEdge(ref bool flag, string edgeName, bool isAtEdge)
    {
        if (isAtEdge == flag) return;

        flag = isAtEdge;

        if (isAtEdge)
        {
            Debug.Log("[FollowCamera] view clamped at the " + edgeName + " edge of the framed area " +
                      "- the camera stops, the player keeps walking towards the screen edge. " +
                      "Visible " + VisibleWidth.ToString("F1") + " m x " + VisibleDepth.ToString("F1") + " m.");
        }
    }

    /// <summary>
    /// Finds the human once. TeamManager.IsHuman is the only thing in the project that knows who the
    /// player is, and the roster is a registry the characters fill in themselves - so this is a walk
    /// over at most four entries, and it runs only until it succeeds.
    /// </summary>
    void ResolveTarget()
    {
        if (target != null) return;

        Team[] teams = TeamUtil.All;

        for (int t = 0; t < teams.Length; t++)
        {
            System.Collections.Generic.List<CharacterStatus> members = TeamManager.Members(teams[t]);

            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] != null && TeamManager.IsHuman(members[i]))
                {
                    target = members[i].transform;
                    return;
                }
            }
        }
    }

    /// <summary>Two-finger pinch. Only runs while pinchEnabled is ticked.</summary>
    void ApplyPinch()
    {
        Touchscreen ts = Touchscreen.current;
        if (ts == null) { pinchDistance = 0f; return; }

        TouchControl a = ts.touches[0];
        TouchControl b = ts.touches[1];

        if (!a.press.isPressed || !b.press.isPressed)
        {
            pinchDistance = 0f;
            return;
        }

        float spacing = Vector2.Distance(a.position.ReadValue(), b.position.ReadValue());

        if (pinchDistance > 0f)
        {
            float change = spacing - pinchDistance;

            if (Mathf.Abs(change) > pinchDeadZonePixels)
            {
                // Fingers further apart = see more of the arena = a larger fraction.
                float scale = spacing / Mathf.Max(1f, pinchDistance);
                visibleWidthPercent = Mathf.Clamp(visibleWidthPercent * scale,
                                                  minVisiblePercent, maxVisiblePercent);
            }
        }

        pinchDistance = spacing;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawClampGizmos) return;

        // The playing rectangle.
        Gizmos.color = new Color(0.2f, 0.9f, 1f);
        Gizmos.DrawWireCube(new Vector3(arenaCenter.x, 0f, arenaCenter.y),
                            new Vector3(arenaSize.x, 0f, arenaSize.y));

        // The rectangle the view may reach into, and the rectangle the camera can see right now.
        Gizmos.color = new Color(1f, 0.8f, 0.2f);
        Gizmos.DrawWireCube(new Vector3(arenaCenter.x, 0f, arenaCenter.y),
                            new Vector3(arenaSize.x + edgeMargin * 2f, 0f, arenaSize.y + edgeMargin * 2f));

        Gizmos.color = new Color(0.3f, 1f, 0.4f);
        Gizmos.DrawWireCube(new Vector3(transform.position.x, 0f, transform.position.z),
                            new Vector3(VisibleWidth, 0f, VisibleDepth));
    }
}
