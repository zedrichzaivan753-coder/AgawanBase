using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Turns "walk at that point" into "walk around whatever is in the way".
///
/// WHY THIS EXISTS
/// ---------------
/// EnemyAI is a pure steering brain: every state calls motor.Move(FlatDirection(me, target)).
/// That is fine on the original open field, where the only obstacles are four perimeter walls,
/// but on an obstacle-dense map (market stalls, classroom blocks) a straight line simply walks
/// the character into a wall and holds it there for the rest of the match.
///
/// This component answers the SAME question - which way should I go? - but from a baked NavMesh:
/// it asks for a path to the target, then steers at the next corner of that path. CharacterMotor
/// is still the only thing that moves anybody, and the AI's decisions are untouched, so the
/// brain, the rules and the physics all stay exactly as they were.
///
/// With NO NavMesh baked (the original scene before its first bake) CalculatePath fails and the
/// component silently behaves like the old straight-line code, so nothing can regress to "the AI
/// does not move at all".
///
/// Cheapest possible: at most one path a third of a second per character, into a reused
/// NavMeshPath, with no allocation in the per-frame path.
/// </summary>
public class MapNavigator : MonoBehaviour
{
    [Header("Path")]
    [Tooltip("Seconds between path recomputations. The path is only a heading, so a third of a " +
             "second is plenty and keeps eight characters cheap on a phone.")]
    public float recomputeInterval = 0.3f;

    [Tooltip("Recompute early if the target has moved this far since the last path, in metres.")]
    public float targetDrift = 1f;

    [Tooltip("How close to a corner counts as having reached it, so the character stops cutting " +
             "the corner and heads for the next one, in metres.")]
    public float cornerLookAhead = 0.6f;

    [Tooltip("How close to the FINAL path point counts as arrived, in metres.")]
    public float arriveRadius = 0.5f;

    [Header("Stuck recovery")]
    [Tooltip("Seconds of being asked to walk while barely moving before it counts as stuck.")]
    public float stuckSeconds = 2.5f;

    [Tooltip("How long a stuck character sidesteps along the obstacle, in seconds.")]
    public float nudgeSeconds = 0.5f;

    [Tooltip("Speed in metres per second below which a character that was told to walk counts " +
             "as not actually moving.")]
    public float stuckSpeed = 0.4f;

    [Tooltip("Seconds between two 'blocked' warnings for the same character. Keeps the console " +
             "readable when a map still has a bad corner in it.")]
    public float warnCooldown = 4f;

    NavMeshPath path;

    Vector3 lastTarget;
    float repathTimer;
    bool hasPath;

    bool usedThisFrame;

    Vector3 lastPosition;
    float stuckTimer;
    float nudgeTimer;
    float nudgeSign = 1f;
    float nextWarnTime;

    bool loggedNoNavMesh;

    /// <summary>
    /// The direction to walk to make progress towards <paramref name="worldTarget"/>.
    /// Same contract as the straight-line maths it replaces: a unit-ish vector on the ground
    /// plane (x = world X, y = world Z).
    /// </summary>
    public Vector2 DirectionTo(Vector3 worldTarget)
    {
        usedThisFrame = true;

        Vector2 straight = FlatDirection(transform.position, worldTarget);
        Vector2 steer = straight;

        if (EnsurePath(worldTarget))
        {
            steer = SteerAlongPath(worldTarget);
        }
        else if (!loggedNoNavMesh)
        {
            loggedNoNavMesh = true;
            Debug.Log("[Nav] " + name + ": no NavMesh reachable from here, so it will walk " +
                      "straight at its targets. Bake a NavMesh for this scene to get routing.");
        }

        // A blocked character sidesteps along the obstacle for a moment. Without this it can
        // press into a corner forever, because a CharacterController only slides, it never turns.
        if (nudgeTimer > 0f)
        {
            Vector2 side;
            if (steer.sqrMagnitude > 0.0001f)
            {
                side = new Vector2(-steer.y, steer.x) * nudgeSign;
                steer = (steer * 0.35f + side).normalized;
            }
            else
            {
                side = new Vector2(nudgeSign, 0f);
                steer = side;
            }
        }

        return steer;
    }

    // ------------------------------------------------------------------- the path

    /// <summary>
    /// Makes sure a usable path to the target exists, recomputing when it is missing, when the
    /// target has moved far enough to matter, or when the timer runs out.
    /// </summary>
    bool EnsurePath(Vector3 worldTarget)
    {
        if (path == null) path = new NavMeshPath();

        float dx = worldTarget.x - lastTarget.x;
        float dz = worldTarget.z - lastTarget.z;
        bool targetMoved = (dx * dx + dz * dz) > (targetDrift * targetDrift);

        repathTimer -= Time.deltaTime;

        if (hasPath && !targetMoved && repathTimer > 0f) return true;

        repathTimer = recomputeInterval;
        lastTarget = worldTarget;

        bool calculated = NavMesh.CalculatePath(transform.position, worldTarget, NavMesh.AllAreas, path);

        // PathInvalid means there is no route at all; a partial path is still worth following,
        // because it at least carries the character as close as the NavMesh goes.
        hasPath = calculated && path.status != NavMeshPathStatus.PathInvalid && path.corners.Length > 0;
        return hasPath;
    }

    /// <summary>Steers at the next corner of the current path, skipping corners already reached.</summary>
    Vector2 SteerAlongPath(Vector3 worldTarget)
    {
        Vector3[] corners = path.corners;
        if (corners.Length < 2) return FlatDirection(transform.position, worldTarget);

        int next = 1;
        while (next < corners.Length - 1 &&
               FlatDistance(transform.position, corners[next]) < cornerLookAhead)
        {
            next++;
        }

        Vector3 aim = corners[next];

        // On the last leg the NavMesh point can be a little short of the real target (a flag
        // standing 1 m from a wall, say), and the AI has to actually touch the thing.
        if (next == corners.Length - 1 &&
            FlatDistance(transform.position, aim) < arriveRadius)
        {
            aim = worldTarget;
        }

        return FlatDirection(transform.position, aim);
    }

    // ------------------------------------------------------------- stuck recovery

    void LateUpdate()
    {
        bool wasAsked = usedThisFrame;
        usedThisFrame = false;

        float moved = FlatDistance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (nudgeTimer > 0f) nudgeTimer -= Time.deltaTime;

        if (!wasAsked)
        {
            stuckTimer = 0f;
            return;
        }

        float speed = moved / Mathf.Max(0.0001f, Time.deltaTime);

        if (speed < stuckSpeed) stuckTimer += Time.deltaTime;
        else stuckTimer = 0f;

        if (stuckTimer < stuckSeconds) return;

        stuckTimer = 0f;
        nudgeTimer = nudgeSeconds;
        nudgeSign = nudgeSign > 0f ? -1f : 1f;

        // Throw the old route away: it is what led into the wall in the first place.
        hasPath = false;
        repathTimer = 0f;

        if (Time.time >= nextWarnTime)
        {
            nextWarnTime = Time.time + warnCooldown;
            Debug.LogWarning("[Nav] " + name + " was blocked for " + stuckSeconds.ToString("F1") +
                             " s at " + transform.position.ToString("F1") +
                             " - sidestepping and recomputing its path.");
        }
    }

    void Start()
    {
        lastPosition = transform.position;
    }

    void OnEnable()
    {
        lastPosition = transform.position;
        stuckTimer = 0f;
        nudgeTimer = 0f;
        hasPath = false;
    }

    // ------------------------------------------------------------------- helpers

    static Vector2 Flat(Vector3 p)
    {
        return new Vector2(p.x, p.z);
    }

    static Vector2 FlatDirection(Vector3 from, Vector3 to)
    {
        float dx = to.x - from.x;
        float dz = to.z - from.z;

        float length = Mathf.Sqrt(dx * dx + dz * dz);
        if (length < 0.0001f) return Vector2.zero;

        return new Vector2(dx / length, dz / length);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
